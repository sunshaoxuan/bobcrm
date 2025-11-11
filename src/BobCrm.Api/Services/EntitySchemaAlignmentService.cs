using BobCrm.Api.Domain.Models;
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

        return AlignmentResult.Aligned;
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
            var alterSql = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{field.PropertyName}\" {MapDataTypeToSQL(field)};";
            scripts.Add((DDLScriptType.Alter, alterSql));
        }

        await _ddlService.ExecuteDDLBatchAsync(entityId, scripts, "System");

        _logger.LogInformation("[SchemaAlign] ✓ Added {Count} columns to {TableName}",
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
}

/// <summary>
/// 对齐结果枚举
/// </summary>
public enum AlignmentResult
{
    /// <summary>已对齐（执行了修改）</summary>
    Aligned,
    /// <summary>已经对齐（无需修改）</summary>
    AlreadyAligned,
    /// <summary>对齐失败</summary>
    Failed
}
