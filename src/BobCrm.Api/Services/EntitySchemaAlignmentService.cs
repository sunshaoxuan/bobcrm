using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

/// <summary>
/// 实体结构对齐服务
/// 负责检查并对齐动态实体的数据库表结构与 EntityDefinition 定义
/// </summary>
public class EntitySchemaAlignmentService
{
    private readonly AppDbContext _db;
    private readonly DDLExecutionService _ddlService;
    private readonly ILogger<EntitySchemaAlignmentService> _logger;

    public EntitySchemaAlignmentService(
        AppDbContext db,
        DDLExecutionService ddlService,
        ILogger<EntitySchemaAlignmentService> logger)
    {
        _db = db;
        _ddlService = ddlService;
        _logger = logger;
    }

    /// <summary>
    /// 对齐所有已发布的动态实体的表结构
    /// </summary>
    public async Task AlignAllPublishedEntitiesAsync()
    {
        _logger.LogInformation("[SchemaAlign] ========== Starting schema alignment for all published entities ==========");

        // 查找所有已发布的自定义实体
        var publishedEntities = await _db.EntityDefinitions
            .Include(ed => ed.Fields)
            .Where(ed => ed.Source == EntitySource.Custom && ed.Status == EntityStatus.Published)
            .ToListAsync();

        if (publishedEntities.Count == 0)
        {
            _logger.LogInformation("[SchemaAlign] No published custom entities found");
            return;
        }

        var alignedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;

        foreach (var entity in publishedEntities)
        {
            try
            {
                var result = await AlignEntitySchemaAsync(entity);
                if (result == AlignmentResult.Aligned)
                    alignedCount++;
                else if (result == AlignmentResult.AlreadyAligned)
                    skippedCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex, "[SchemaAlign] Failed to align entity: {EntityName}", entity.EntityName);
            }
        }

        _logger.LogInformation(
            "[SchemaAlign] ========== Schema alignment completed: {Aligned} aligned, {Skipped} skipped, {Failed} failed ==========",
            alignedCount, skippedCount, failedCount);
    }

    /// <summary>
    /// 对齐单个实体的表结构
    /// </summary>
    public async Task<AlignmentResult> AlignEntitySchemaAsync(EntityDefinition entity)
    {
        var tableName = entity.DefaultTableName;

        _logger.LogInformation("[SchemaAlign] Checking entity {EntityName} (table: {TableName})",
            entity.EntityName, tableName);

        // 检查表是否存在
        var tableExists = await _ddlService.TableExistsAsync(tableName);

        if (!tableExists)
        {
            // 表不存在，创建表
            _logger.LogInformation("[SchemaAlign] Table {TableName} does not exist, creating...", tableName);
            await CreateTableAsync(entity, tableName);
            return AlignmentResult.Aligned;
        }

        // 表存在，检查列是否匹配
        var actualColumns = await _ddlService.GetTableColumnsAsync(tableName);
        var expectedFields = entity.Fields;

        var missingColumns = GetMissingColumns(expectedFields, actualColumns);
        var extraColumns = GetExtraColumns(expectedFields, actualColumns);
        var mismatchedColumns = GetMismatchedColumns(expectedFields, actualColumns);

        if (missingColumns.Count == 0 && extraColumns.Count == 0 && mismatchedColumns.Count == 0)
        {
            _logger.LogDebug("[SchemaAlign] Table {TableName} is already aligned", tableName);
            return AlignmentResult.AlreadyAligned;
        }

        // 需要对齐：添加缺失的列
        if (missingColumns.Count > 0)
        {
            _logger.LogInformation("[SchemaAlign] Adding {Count} missing columns to {TableName}",
                missingColumns.Count, tableName);
            await AddMissingColumnsAsync(entity.Id, tableName, missingColumns);
        }

        // 警告：有多余的列（不自动删除，避免数据丢失）
        if (extraColumns.Count > 0)
        {
            _logger.LogWarning("[SchemaAlign] Table {TableName} has {Count} extra columns (not removed): {Columns}",
                tableName, extraColumns.Count, string.Join(", ", extraColumns.Select(c => c.ColumnName)));
        }

        // 警告：有不匹配的列（不自动修改，避免数据丢失）
        if (mismatchedColumns.Count > 0)
        {
            _logger.LogWarning("[SchemaAlign] Table {TableName} has {Count} mismatched columns (not altered): {Columns}",
                tableName, mismatchedColumns.Count, string.Join(", ", mismatchedColumns.Select(c => c.ColumnName)));
        }

        // 如果添加了缺失的列，返回 Aligned；否则返回 AlreadyAligned
        return missingColumns.Count > 0 ? AlignmentResult.Aligned : AlignmentResult.AlreadyAligned;
    }

    /// <summary>
    /// 创建表
    /// </summary>
    private async Task CreateTableAsync(EntityDefinition entity, string tableName)
    {
        var createTableSql = GenerateCreateTableSQL(entity, tableName);

        await _ddlService.ExecuteDDLAsync(
            entity.Id,
            DDLScriptType.Create,
            createTableSql,
            "System");

        _logger.LogInformation("[SchemaAlign] ✓ Table {TableName} created successfully", tableName);
    }

    /// <summary>
    /// 添加缺失的列
    /// </summary>
    private async Task AddMissingColumnsAsync(Guid entityId, string tableName, List<FieldMetadata> missingFields)
    {
        var scripts = new List<(string ScriptType, string SqlScript)>();

        foreach (var field in missingFields)
        {
            // 1. 添加列（先设为可空，避免非空约束导致失败）
            var dataType = MapDataTypeToSQL(field).Replace(" NOT NULL", ""); // 暂时移除 NOT NULL
            var alterSql = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{field.PropertyName}\" {dataType};";
            scripts.Add((DDLScriptType.Alter, alterSql));

            // 2. ✅ 业务数据对齐：填充默认值到现有记录
            var defaultValue = GetDefaultValueForDataType(field);
            if (defaultValue != null)
            {
                var updateSql = $"UPDATE \"{tableName}\" SET \"{field.PropertyName}\" = {defaultValue} WHERE \"{field.PropertyName}\" IS NULL;";
                scripts.Add((DDLScriptType.Alter, updateSql));

                _logger.LogInformation("[SchemaAlign] Filling default value for {FieldName}: {DefaultValue}",
                    field.PropertyName, defaultValue);
            }

            // 3. 如果字段是必填，添加 NOT NULL 约束
            if (field.IsRequired && defaultValue != null)
            {
                var constraintSql = $"ALTER TABLE \"{tableName}\" ALTER COLUMN \"{field.PropertyName}\" SET NOT NULL;";
                scripts.Add((DDLScriptType.Alter, constraintSql));
            }
        }

        await _ddlService.ExecuteDDLBatchAsync(entityId, scripts, "System");

        _logger.LogInformation("[SchemaAlign] ✓ Added {Count} columns to {TableName} with data alignment",
            missingFields.Count, tableName);
    }

    /// <summary>
    /// 生成 CREATE TABLE SQL
    /// </summary>
    private string GenerateCreateTableSQL(EntityDefinition entity, string tableName)
    {
        var columns = new List<string>
        {
            "\"Id\" uuid PRIMARY KEY DEFAULT gen_random_uuid()",
            "\"CreatedAt\" timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc')",
            "\"UpdatedAt\" timestamp without time zone NULL"
        };

        foreach (var field in entity.Fields.OrderBy(f => f.SortOrder))
        {
            columns.Add($"\"{field.PropertyName}\" {MapDataTypeToSQL(field)}");
        }

        return $@"
CREATE TABLE ""{tableName}"" (
    {string.Join(",\n    ", columns)}
);";
    }

    /// <summary>
    /// 将 FieldMetadata 的数据类型映射为 PostgreSQL SQL 类型
    /// </summary>
    private string MapDataTypeToSQL(FieldMetadata field)
    {
        var sqlType = field.DataType switch
        {
            "String" => field.Length.HasValue ? $"varchar({field.Length})" : "text",
            "Int32" => "integer",
            "Int64" => "bigint",
            "Decimal" => field.Precision.HasValue && field.Scale.HasValue
                ? $"numeric({field.Precision},{field.Scale})"
                : "numeric",
            "Boolean" => "boolean",
            "DateTime" => "timestamp without time zone",
            "Guid" => "uuid",
            "Json" => "jsonb",
            _ => "text"
        };

        // 添加 NOT NULL 约束
        if (field.IsRequired)
        {
            sqlType += " NOT NULL";
        }

        return sqlType;
    }

    /// <summary>
    /// 获取缺失的列（定义中有但数据库中没有）
    /// </summary>
    private List<FieldMetadata> GetMissingColumns(
        List<FieldMetadata> expectedFields,
        List<TableColumnInfo> actualColumns)
    {
        var actualColumnNames = actualColumns
            .Select(c => c.ColumnName.ToLowerInvariant())
            .ToHashSet();

        return expectedFields
            .Where(f => !actualColumnNames.Contains(f.PropertyName.ToLowerInvariant()))
            .ToList();
    }

    /// <summary>
    /// 获取多余的列（数据库中有但定义中没有）
    /// </summary>
    private List<TableColumnInfo> GetExtraColumns(
        List<FieldMetadata> expectedFields,
        List<TableColumnInfo> actualColumns)
    {
        var expectedColumnNames = expectedFields
            .Select(f => f.PropertyName.ToLowerInvariant())
            .ToHashSet();

        // 排除系统列
        var systemColumns = new HashSet<string> { "id", "createdat", "updatedat", "createdby", "updatedby" };

        return actualColumns
            .Where(c => !expectedColumnNames.Contains(c.ColumnName.ToLowerInvariant()))
            .Where(c => !systemColumns.Contains(c.ColumnName.ToLowerInvariant()))
            .ToList();
    }

    /// <summary>
    /// 获取不匹配的列（类型或约束不一致）
    /// </summary>
    private List<TableColumnInfo> GetMismatchedColumns(
        List<FieldMetadata> expectedFields,
        List<TableColumnInfo> actualColumns)
    {
        // 简化版本：只检测严重的不匹配（后续可扩展）
        // 例如：varchar vs integer 等明显的类型不匹配
        var mismatched = new List<TableColumnInfo>();

        var fieldDict = expectedFields.ToDictionary(
            f => f.PropertyName.ToLowerInvariant(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var column in actualColumns)
        {
            if (fieldDict.TryGetValue(column.ColumnName.ToLowerInvariant(), out var field))
            {
                var expectedType = GetBaseDataType(MapDataTypeToSQL(field));
                var actualType = GetBaseDataType(column.DataType);

                if (!TypesAreCompatible(expectedType, actualType))
                {
                    mismatched.Add(column);
                }
            }
        }

        return mismatched;
    }

    /// <summary>
    /// 获取基础数据类型（去掉长度和精度）
    /// </summary>
    private string GetBaseDataType(string dataType)
    {
        return dataType
            .Split(new[] { '(', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .First()
            .ToLowerInvariant();
    }

    /// <summary>
    /// 检查两个类型是否兼容
    /// </summary>
    private bool TypesAreCompatible(string type1, string type2)
    {
        // 完全匹配
        if (type1 == type2) return true;

        // varchar 和 text 兼容
        if ((type1 == "varchar" || type1 == "text") &&
            (type2 == "varchar" || type2 == "text" || type2 == "character"))
            return true;

        // timestamp 的各种变体
        if (type1.Contains("timestamp") && type2.Contains("timestamp"))
            return true;

        return false;
    }

    /// <summary>
    /// ✅ 获取字段类型的默认值（用于业务数据对齐）
    /// </summary>
    private string? GetDefaultValueForDataType(FieldMetadata field)
    {
        // 优先使用字段定义中的默认值
        if (!string.IsNullOrEmpty(field.DefaultValue))
        {
            return field.DataType switch
            {
                "String" => $"'{field.DefaultValue}'",
                "Int32" or "Int64" or "Decimal" => field.DefaultValue,
                "Boolean" => field.DefaultValue.ToLower() == "true" ? "TRUE" : "FALSE",
                "DateTime" => $"'{field.DefaultValue}'",
                "Guid" => $"'{field.DefaultValue}'",
                _ => $"'{field.DefaultValue}'"
            };
        }

        // 使用类型的默认值
        return field.DataType switch
        {
            "String" => "''", // 空字符串
            "Int32" or "Int64" => "0",
            "Decimal" => "0.0",
            "Boolean" => "FALSE",
            "DateTime" => "NOW()",
            "Guid" => "gen_random_uuid()",
            "Json" => "'{}'::jsonb",
            _ => null
        };
    }

    /// <summary>
    /// ✅ 删除字段（物理删除或逻辑删除）
    /// </summary>
    /// <param name="entityId">实体定义ID</param>
    /// <param name="fieldId">字段ID</param>
    /// <param name="physicalDelete">true=物理删除列，false=仅删除元数据（逻辑删除）</param>
    /// <param name="performedBy">操作人</param>
    public async Task<DeleteFieldResult> DeleteFieldAsync(
        Guid entityId,
        Guid fieldId,
        bool physicalDelete = false,
        string? performedBy = null)
    {
        var result = new DeleteFieldResult { FieldId = fieldId };

        try
        {
            // 1. 加载实体定义
            var entity = await _db.EntityDefinitions
                .Include(e => e.Fields)
                .FirstOrDefaultAsync(e => e.Id == entityId);

            if (entity == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Entity definition {entityId} not found";
                return result;
            }

            var field = entity.Fields.FirstOrDefault(f => f.Id == fieldId);
            if (field == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Field {fieldId} not found";
                return result;
            }

            var tableName = entity.DefaultTableName;
            var columnName = field.PropertyName;

            _logger.LogInformation("[SchemaAlign] Deleting field {FieldName} from {EntityName} (Physical={Physical})",
                columnName, entity.EntityName, physicalDelete);

            // 2. 从元数据中删除字段
            _db.FieldMetadatas.Remove(field);
            await _db.SaveChangesAsync();

            result.LogicalDeleteCompleted = true;

            // 3. 如果是物理删除，删除数据库列
            if (physicalDelete)
            {
                var tableExists = await _ddlService.TableExistsAsync(tableName);
                if (tableExists)
                {
                    var dropColumnSql = $"ALTER TABLE \"{tableName}\" DROP COLUMN IF EXISTS \"{columnName}\";";

                    var scriptRecord = await _ddlService.ExecuteDDLAsync(
                        entityId,
                        DDLScriptType.Alter,
                        dropColumnSql,
                        performedBy ?? "System"
                    );

                    if (scriptRecord.Status == DDLScriptStatus.Success)
                    {
                        result.PhysicalDeleteCompleted = true;
                        _logger.LogInformation("[SchemaAlign] ✓ Column {ColumnName} physically deleted from {TableName}",
                            columnName, tableName);
                    }
                    else
                    {
                        result.PhysicalDeleteCompleted = false;
                        result.ErrorMessage = scriptRecord.ErrorMessage;
                        _logger.LogWarning("[SchemaAlign] Failed to physically delete column {ColumnName}: {Error}",
                            columnName, scriptRecord.ErrorMessage);
                    }
                }
                else
                {
                    result.PhysicalDeleteCompleted = false;
                    result.ErrorMessage = $"Table {tableName} does not exist";
                }
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[SchemaAlign] Failed to delete field: {Error}", ex.Message);
        }

        return result;
    }
}
