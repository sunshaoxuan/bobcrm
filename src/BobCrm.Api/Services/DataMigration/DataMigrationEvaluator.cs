using BobCrm.Api.Infrastructure;
using BobCrm.Api.Base.Models;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services.DataMigration;

/// <summary>
/// 数据迁移影响评估器
/// 在发布实体变更前，评估对现有数据的影响
/// </summary>
public class DataMigrationEvaluator : IDataMigrationEvaluator
{
    private readonly AppDbContext _context;
    private readonly ILogger<DataMigrationEvaluator> _logger;

    public DataMigrationEvaluator(
        AppDbContext context,
        ILogger<DataMigrationEvaluator> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 评估实体变更的数据迁移影响
    /// </summary>
    /// <param name="entityId">实体定义ID</param>
    /// <param name="newFields">新的字段列表</param>
    /// <returns>迁移影响分析</returns>
    public async Task<MigrationImpact> EvaluateImpactAsync(Guid entityId, List<FieldMetadata> newFields)
    {
        var entity = await _context.EntityDefinitions
            .Include(e => e.Fields)
            .FirstOrDefaultAsync(e => e.Id == entityId);

        if (entity == null)
        {
            throw new ArgumentException($"Entity with ID {entityId} not found");
        }

        var impact = new MigrationImpact
        {
            EntityName = entity.EntityName,
            TableName = entity.DefaultTableName
        };

        // 只有已发布的实体才需要评估迁移影响
        if (entity.Status != EntityStatus.Published)
        {
            impact.RiskLevel = RiskLevel.Low;
            impact.Warnings.Add("This is a draft entity, no data migration needed");
            return impact;
        }

        // 获取现有数据行数
        impact.AffectedRows = await GetTableRowCountAsync(entity.DefaultTableName);

        var oldFields = entity.Fields.ToDictionary(f => f.PropertyName);
        var newFieldsDict = newFields.ToDictionary(f => f.PropertyName);

        // 1. 检测删除的字段
        foreach (var oldField in oldFields.Values)
        {
            if (!newFieldsDict.ContainsKey(oldField.PropertyName))
            {
                impact.Operations.Add(new MigrationOperation
                {
                    OperationType = MigrationOperationType.DropColumn,
                    FieldName = oldField.PropertyName,
                    OldDataType = oldField.DataType,
                    MayLoseData = impact.AffectedRows > 0,
                    Description = $"Drop column '{oldField.PropertyName}' of type '{oldField.DataType}'",
                    SqlPreview = $"ALTER TABLE \"{entity.DefaultTableName}\" DROP COLUMN \"{oldField.PropertyName}\";"
                });

                if (impact.AffectedRows > 0)
                {
                    impact.Errors.Add($"Dropping column '{oldField.PropertyName}' will cause data loss for {impact.AffectedRows} rows");
                }
            }
        }

        // 2. 检测新增的字段
        foreach (var newField in newFieldsDict.Values)
        {
            if (!oldFields.ContainsKey(newField.PropertyName))
            {
                var mayLoseData = newField.IsRequired && string.IsNullOrEmpty(newField.DefaultValue) && impact.AffectedRows > 0;

                impact.Operations.Add(new MigrationOperation
                {
                    OperationType = MigrationOperationType.AddColumn,
                    FieldName = newField.PropertyName,
                    NewDataType = newField.DataType,
                    MayLoseData = mayLoseData,
                    Description = $"Add column '{newField.PropertyName}' of type '{newField.DataType}'",
                    SqlPreview = GenerateAddColumnSql(entity.DefaultTableName, newField)
                });

                if (mayLoseData)
                {
                    impact.Errors.Add($"Adding required column '{newField.PropertyName}' without default value for {impact.AffectedRows} existing rows");
                }
                else if (newField.IsRequired)
                {
                    impact.Warnings.Add($"Adding required column '{newField.PropertyName}' with default value '{newField.DefaultValue}'");
                }
            }
        }

        // 3. 检测修改的字段
        foreach (var newField in newFieldsDict.Values)
        {
            if (oldFields.TryGetValue(newField.PropertyName, out var oldField))
            {
                var changes = DetectFieldChanges(oldField, newField);
                if (changes.Count > 0)
                {
                    var requiresConversion = oldField.DataType != newField.DataType;
                    var mayLoseData = CheckDataLossRisk(oldField, newField) && impact.AffectedRows > 0;

                    impact.Operations.Add(new MigrationOperation
                    {
                        OperationType = MigrationOperationType.AlterColumn,
                        FieldName = newField.PropertyName,
                        OldDataType = oldField.DataType,
                        NewDataType = newField.DataType,
                        MayLoseData = mayLoseData,
                        RequiresConversion = requiresConversion,
                        Description = $"Alter column '{newField.PropertyName}': {string.Join(", ", changes)}",
                        SqlPreview = GenerateAlterColumnSql(entity.DefaultTableName, newField)
                    });

                    if (mayLoseData)
                    {
                        impact.Errors.Add($"Altering column '{newField.PropertyName}' may cause data loss: {string.Join(", ", changes)}");
                    }
                    else if (requiresConversion)
                    {
                        impact.Warnings.Add($"Altering column '{newField.PropertyName}' requires data conversion: {oldField.DataType} → {newField.DataType}");
                    }
                }
            }
        }

        // 计算风险等级
        impact.RiskLevel = CalculateRiskLevel(impact);

        return impact;
    }

    /// <summary>
    /// 获取表的行数
    /// </summary>
    private async Task<long> GetTableRowCountAsync(string tableName)
    {
        try
        {
            var sql = $"SELECT COUNT(*) FROM \"{tableName}\"";
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var result = await command.ExecuteScalarAsync();

            return result != null ? Convert.ToInt64(result) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get row count for table {TableName}, table may not exist", tableName);
            return 0;
        }
    }

    /// <summary>
    /// 检测字段变更
    /// </summary>
    private List<string> DetectFieldChanges(FieldMetadata oldField, FieldMetadata newField)
    {
        var changes = new List<string>();

        if (oldField.DataType != newField.DataType)
        {
            changes.Add($"type changed from {oldField.DataType} to {newField.DataType}");
        }

        if (oldField.Length != newField.Length)
        {
            changes.Add($"length changed from {oldField.Length} to {newField.Length}");
        }

        if (oldField.Precision != newField.Precision)
        {
            changes.Add($"precision changed from {oldField.Precision} to {newField.Precision}");
        }

        if (oldField.Scale != newField.Scale)
        {
            changes.Add($"scale changed from {oldField.Scale} to {newField.Scale}");
        }

        if (oldField.IsRequired != newField.IsRequired)
        {
            changes.Add($"required changed from {oldField.IsRequired} to {newField.IsRequired}");
        }

        return changes;
    }

    /// <summary>
    /// 检查是否有数据丢失风险
    /// </summary>
    private bool CheckDataLossRisk(FieldMetadata oldField, FieldMetadata newField)
    {
        // 数据类型转换可能导致数据丢失
        if (oldField.DataType != newField.DataType)
        {
            if (IsHighRiskConversion(oldField.DataType, newField.DataType))
            {
                return true;
            }
        }

        // 长度缩短可能导致数据截断
        if (oldField.Length.HasValue && newField.Length.HasValue && newField.Length < oldField.Length)
        {
            return true;
        }

        // 精度降低可能导致数据丢失
        if (oldField.Precision.HasValue && newField.Precision.HasValue && newField.Precision < oldField.Precision)
        {
            return true;
        }

        // 从可空变为必填但没有默认值
        if (!oldField.IsRequired && newField.IsRequired && string.IsNullOrEmpty(newField.DefaultValue))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 判断是否是高风险的数据类型转换
    /// </summary>
    private bool IsHighRiskConversion(string fromType, string toType)
    {
        // 数值转字符串：安全
        if ((fromType is FieldDataType.Integer or FieldDataType.Long or FieldDataType.Decimal) && toType == FieldDataType.String)
        {
            return false;
        }

        // 字符串转数值：高风险（可能包含非数字字符）
        if (fromType == FieldDataType.String && toType is FieldDataType.Integer or FieldDataType.Long or FieldDataType.Decimal)
        {
            return true;
        }

        // Long/Decimal 转 Integer：可能溢出
        if ((fromType is FieldDataType.Long or FieldDataType.Decimal) && toType == FieldDataType.Integer)
        {
            return true;
        }

        // DateTime 转 Date：丢失时间部分
        if (fromType == FieldDataType.DateTime && toType == FieldDataType.Date)
        {
            return true;
        }

        // 其他不同类型的转换都视为高风险
        return fromType != toType;
    }

    /// <summary>
    /// 计算整体风险等级
    /// </summary>
    private string CalculateRiskLevel(MigrationImpact impact)
    {
        if (impact.Errors.Count > 0)
        {
            return RiskLevel.Critical;
        }

        var dataLossOps = impact.Operations.Count(op => op.MayLoseData);
        if (dataLossOps > 0)
        {
            return RiskLevel.High;
        }

        var conversionOps = impact.Operations.Count(op => op.RequiresConversion);
        if (conversionOps > 0 || impact.Warnings.Count > 2)
        {
            return RiskLevel.Medium;
        }

        return RiskLevel.Low;
    }

    /// <summary>
    /// 生成添加列的SQL
    /// </summary>
    private string GenerateAddColumnSql(string tableName, FieldMetadata field)
    {
        var sqlType = MapToSqlType(field);
        var nullable = field.IsRequired ? "NOT NULL" : "NULL";
        var defaultValue = string.IsNullOrEmpty(field.DefaultValue) ? "" : $" DEFAULT {FormatDefaultValue(field)}";

        return $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{field.PropertyName}\" {sqlType} {nullable}{defaultValue};";
    }

    /// <summary>
    /// 生成修改列的SQL
    /// </summary>
    private string GenerateAlterColumnSql(string tableName, FieldMetadata field)
    {
        var sqlType = MapToSqlType(field);
        var nullable = field.IsRequired ? "SET NOT NULL" : "DROP NOT NULL";

        return $"ALTER TABLE \"{tableName}\" ALTER COLUMN \"{field.PropertyName}\" TYPE {sqlType}, ALTER COLUMN \"{field.PropertyName}\" {nullable};";
    }

    /// <summary>
    /// 映射到SQL数据类型
    /// </summary>
    private string MapToSqlType(FieldMetadata field)
    {
        return field.DataType switch
        {
            FieldDataType.String => field.Length.HasValue ? $"VARCHAR({field.Length})" : "TEXT",  // 注意：Text是String的别名
            FieldDataType.Int32 => "INTEGER",
            FieldDataType.Int64 => "BIGINT",
            FieldDataType.Decimal => $"DECIMAL({field.Precision ?? 18}, {field.Scale ?? 2})",
            FieldDataType.Boolean => "BOOLEAN",
            FieldDataType.DateTime => "TIMESTAMP",  // 注意：Date是DateTime的别名
            FieldDataType.Guid => "UUID",
            _ => "TEXT"
        };
    }

    /// <summary>
    /// 格式化默认值
    /// </summary>
    private string FormatDefaultValue(FieldMetadata field)
    {
        if (string.IsNullOrEmpty(field.DefaultValue))
        {
            return "NULL";
        }

        return field.DefaultValue.ToUpper() switch
        {
            "NOW" or "CURRENT_TIMESTAMP" => "CURRENT_TIMESTAMP",
            "NEWID" or "UUID" => "gen_random_uuid()",
            "TRUE" => "TRUE",
            "FALSE" => "FALSE",
            _ => field.DataType == FieldDataType.String ? $"'{field.DefaultValue}'" : field.DefaultValue
        };
    }
}
