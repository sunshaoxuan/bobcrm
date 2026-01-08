using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.Json;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Base.Models;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

/// <summary>
/// 反射持久化服务
/// 使用反射实现动态实体的CRUD操作
/// </summary>
public class ReflectionPersistenceService
    : IReflectionPersistenceService
{
    private static readonly Regex IdentifierRegex = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
    private readonly AppDbContext _db;
    private readonly DynamicEntityService _dynamicEntityService;
    private readonly ILogger<ReflectionPersistenceService> _logger;

    public ReflectionPersistenceService(
        AppDbContext db,
        DynamicEntityService dynamicEntityService,
        ILogger<ReflectionPersistenceService> logger)
    {
        _db = db;
        _dynamicEntityService = dynamicEntityService;
        _logger = logger;
    }

    /// <summary>
    /// 查询实体列表
    /// </summary>
    public async Task<List<object>> QueryAsync(
        string fullTypeName,
        QueryOptions? options = null)
    {
        var entityType = _dynamicEntityService.GetEntityType(fullTypeName);
        if (entityType == null)
            throw new InvalidOperationException($"Entity type {fullTypeName} not loaded");

        _logger.LogInformation("[Persistence] Querying {EntityType}", fullTypeName);

        if (!IsEntityTypeInEfModel(entityType))
        {
            var entityDefinition = await GetEntityDefinitionAsync(fullTypeName);
            var rows = await QueryDynamicTableAsync(entityDefinition, options);
            return rows.Cast<object>().ToList();
        }

        // 获取DbSet<T>
        var dbSet = GetDbSet(entityType);
        var query = (IQueryable)dbSet;

        // 自动过滤逻辑删除的记录（系统级安全机制）
        if (HasProperty(entityType, "IsDeleted"))
        {
            query = ApplySoftDeleteFilter(query, entityType);
        }

        // 应用过滤条件
        if (options?.Filters != null && options.Filters.Any())
        {
            query = ApplyFilters(query, entityType, options.Filters);
        }

        // 应用排序
        if (!string.IsNullOrEmpty(options?.OrderBy))
        {
            query = ApplyOrderBy(query, entityType, options.OrderBy, options.OrderByDescending);
        }

        // 应用分页
        if (options?.Skip.HasValue == true && options.Skip.Value > 0)
        {
            query = ApplySkip(query, entityType, options.Skip.Value);
        }

        if (options?.Take.HasValue == true && options.Take.Value > 0)
        {
            query = ApplyTake(query, entityType, options.Take.Value);
        }

        var list = await ToListAsync(query, entityType);
        var results = list.Cast<object>().ToList();

        _logger.LogInformation("[Persistence] Found {Count} records", results.Count);

        return results;
    }

    /// <summary>
    /// 根据ID查询单个实体
    /// </summary>
    public async Task<object?> GetByIdAsync(string fullTypeName, int id)
    {
        var entityType = _dynamicEntityService.GetEntityType(fullTypeName);
        if (entityType == null)
            throw new InvalidOperationException($"Entity type {fullTypeName} not loaded");

        _logger.LogInformation("[Persistence] Getting {EntityType} with ID {Id}", fullTypeName, id);

        if (!IsEntityTypeInEfModel(entityType))
        {
            var entityDefinition = await GetEntityDefinitionAsync(fullTypeName);
            return await GetDynamicByIdAsync(entityDefinition, id);
        }

        var dbSet = GetDbSet(entityType);
        var query = (IQueryable)dbSet;

        // 自动过滤逻辑删除的记录
        if (HasProperty(entityType, "IsDeleted"))
        {
            query = ApplySoftDeleteFilter(query, entityType);
        }

        // 查询指定ID的记录（动态表达式，避免 EF 翻译失败）
        var predicate = BuildEqualsPredicate(entityType, "Id", id);
        var filtered = ApplyWhere(query, entityType, predicate);
        var result = await FirstOrDefaultAsync(filtered, entityType);

        return result;
    }

    /// <summary>
    /// 创建实体
    /// </summary>
    public async Task<object> CreateAsync(string fullTypeName, Dictionary<string, object> data)
    {
        var entityType = _dynamicEntityService.GetEntityType(fullTypeName);
        if (entityType == null)
            throw new InvalidOperationException($"Entity type {fullTypeName} not loaded");

        _logger.LogInformation("[Persistence] Creating {EntityType}", fullTypeName);

        if (!IsEntityTypeInEfModel(entityType))
        {
            var entityDefinition = await GetEntityDefinitionAsync(fullTypeName);
            return await CreateDynamicAsync(entityDefinition, data);
        }

        // 创建实体实例
        var entity = Activator.CreateInstance(entityType);
        if (entity == null)
            throw new InvalidOperationException($"Failed to create instance of {fullTypeName}");

        // 设置属性值
        SetProperties(entity, entityType, data);

        // 添加到DbContext
        var dbSet = GetDbSet(entityType);
        var addMethod = dbSet.GetType().GetMethod("Add", new[] { entityType });

        if (addMethod == null)
            throw new InvalidOperationException("Add method not found");

        addMethod.Invoke(dbSet, new[] { entity });
        await _db.SaveChangesAsync();

        _logger.LogInformation("[Persistence] Created {EntityType} successfully", fullTypeName);

        return entity;
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    public async Task<object?> UpdateAsync(string fullTypeName, int id, Dictionary<string, object> data)
    {
        var entityType = _dynamicEntityService.GetEntityType(fullTypeName);
        if (entityType == null)
            throw new InvalidOperationException($"Entity type {fullTypeName} not loaded");

        _logger.LogInformation("[Persistence] Updating {EntityType} with ID {Id}", fullTypeName, id);

        if (!IsEntityTypeInEfModel(entityType))
        {
            var entityDefinition = await GetEntityDefinitionAsync(fullTypeName);
            return await UpdateDynamicAsync(entityDefinition, id, data);
        }

        // 查找实体
        var entity = await GetByIdAsync(fullTypeName, id);
        if (entity == null)
            return null;

        // 更新属性值
        SetProperties(entity, entityType, data);

        await _db.SaveChangesAsync();

        _logger.LogInformation("[Persistence] Updated {EntityType} successfully", fullTypeName);

        return entity;
    }

    /// <summary>
    /// 删除实体（逻辑删除）
    /// </summary>
    public async Task<bool> DeleteAsync(string fullTypeName, int id, string? deletedBy = null)
    {
        var entityType = _dynamicEntityService.GetEntityType(fullTypeName);
        if (entityType == null)
            throw new InvalidOperationException($"Entity type {fullTypeName} not loaded");

        _logger.LogInformation("[Persistence] Soft deleting {EntityType} with ID {Id}", fullTypeName, id);

        if (!IsEntityTypeInEfModel(entityType))
        {
            var entityDefinition = await GetEntityDefinitionAsync(fullTypeName);
            return await DeleteDynamicAsync(entityDefinition, id, deletedBy);
        }

        // 查找实体
        var entity = await GetByIdAsync(fullTypeName, id);
        if (entity == null)
            return false;

        // 逻辑删除：设置 IsDeleted = true
        if (HasProperty(entityType, "IsDeleted"))
        {
            SetPropertyValue(entity, entityType, "IsDeleted", true);

            if (HasProperty(entityType, "DeletedAt"))
            {
                SetPropertyValue(entity, entityType, "DeletedAt", DateTime.UtcNow);
            }

            if (HasProperty(entityType, "DeletedBy") && !string.IsNullOrEmpty(deletedBy))
            {
                SetPropertyValue(entity, entityType, "DeletedBy", deletedBy);
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("[Persistence] Soft deleted {EntityType} successfully", fullTypeName);
        }
        else
        {
            _logger.LogWarning("[Persistence] Entity {EntityType} does not support soft delete, skipping", fullTypeName);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 统计记录数
    /// </summary>
    public async Task<int> CountAsync(string fullTypeName, List<FilterCondition>? filters = null)
    {
        var entityType = _dynamicEntityService.GetEntityType(fullTypeName);
        if (entityType == null)
            throw new InvalidOperationException($"Entity type {fullTypeName} not loaded");

        if (!IsEntityTypeInEfModel(entityType))
        {
            var entityDefinition = await GetEntityDefinitionAsync(fullTypeName);
            return await CountDynamicAsync(entityDefinition, filters);
        }

        var dbSet = GetDbSet(entityType);
        var query = (IQueryable)dbSet;

        if (HasProperty(entityType, "IsDeleted"))
        {
            query = ApplySoftDeleteFilter(query, entityType);
        }

        if (filters != null && filters.Any())
        {
            query = ApplyFilters(query, entityType, filters);
        }

        return await CountAsync(query, entityType);
    }

    private bool IsEntityTypeInEfModel(Type entityType)
    {
        return _db.Model.FindEntityType(entityType) != null;
    }

    private async Task<EntityDefinition> GetEntityDefinitionAsync(string fullTypeName, CancellationToken ct = default)
    {
        var entity = await _db.EntityDefinitions
            .Include(e => e.Fields)
            .Include(e => e.Interfaces)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.FullTypeName == fullTypeName, ct);

        if (entity == null)
        {
            throw new InvalidOperationException($"Entity definition {fullTypeName} not found");
        }

        return entity;
    }

    private static string QuoteIdentifier(string identifier)
    {
        if (!IdentifierRegex.IsMatch(identifier))
        {
            throw new InvalidOperationException($"Invalid identifier: {identifier}");
        }

        return $"\"{identifier}\"";
    }

    private static Dictionary<string, Type> BuildColumnTypeMap(EntityDefinition entity)
    {
        var map = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var field in entity.Fields)
        {
            map[field.PropertyName] = field.DataType switch
            {
                FieldDataType.String or FieldDataType.Text => typeof(string),
                FieldDataType.Int32 => typeof(int),
                FieldDataType.Int64 => typeof(long),
                FieldDataType.Decimal => typeof(decimal),
                FieldDataType.Boolean => typeof(bool),
                FieldDataType.DateTime => typeof(DateTime),
                FieldDataType.Date => typeof(DateOnly),
                FieldDataType.Guid => typeof(Guid),
                _ => typeof(string)
            };
        }

        foreach (var entityInterface in entity.Interfaces.Where(i => i.IsEnabled))
        {
            switch (entityInterface.InterfaceType)
            {
                case EntityInterfaceType.Base:
                    map.TryAdd("Id", typeof(int));
                    map.TryAdd("IsDeleted", typeof(bool));
                    map.TryAdd("DeletedAt", typeof(DateTime));
                    map.TryAdd("DeletedBy", typeof(string));
                    break;
                case EntityInterfaceType.Archive:
                    map.TryAdd("Code", typeof(string));
                    map.TryAdd("Name", typeof(string));
                    break;
                case EntityInterfaceType.Audit:
                    map.TryAdd("CreatedAt", typeof(DateTime));
                    map.TryAdd("CreatedBy", typeof(string));
                    map.TryAdd("UpdatedAt", typeof(DateTime));
                    map.TryAdd("UpdatedBy", typeof(string));
                    map.TryAdd("Version", typeof(int));
                    break;
                case EntityInterfaceType.Version:
                    map.TryAdd("Version", typeof(int));
                    break;
                case EntityInterfaceType.TimeVersion:
                    map.TryAdd("ValidFrom", typeof(DateTime));
                    map.TryAdd("ValidTo", typeof(DateTime));
                    map.TryAdd("VersionNo", typeof(int));
                    break;
                case EntityInterfaceType.Organization:
                    map.TryAdd("OrganizationId", typeof(Guid));
                    break;
            }
        }

        return map;
    }

    private async Task<List<Dictionary<string, object?>>> QueryDynamicTableAsync(
        EntityDefinition entity,
        QueryOptions? options,
        CancellationToken ct = default)
    {
        var columnTypeMap = BuildColumnTypeMap(entity);
        var tableName = QuoteIdentifier(entity.DefaultTableName);

        var sql = $"SELECT * FROM {tableName}";
        var whereClauses = new List<string>();
        using var command = _db.Database.GetDbConnection().CreateCommand();

        if (columnTypeMap.ContainsKey("IsDeleted"))
        {
            whereClauses.Add($"{QuoteIdentifier("IsDeleted")} = FALSE");
        }

        if (options?.Filters != null)
        {
            foreach (var filter in options.Filters)
            {
                if (string.IsNullOrWhiteSpace(filter.Field) || !columnTypeMap.TryGetValue(filter.Field, out var clrType))
                {
                    continue;
                }

                var column = QuoteIdentifier(filter.Field);
                var op = filter.Operator ?? FilterOperator.Equals;

                if (filter.Value == null)
                {
                    whereClauses.Add(op == FilterOperator.Equals
                        ? $"{column} IS NULL"
                        : $"{column} IS NOT NULL");
                    continue;
                }

                var paramName = $"@p{command.Parameters.Count}";
                var parameter = command.CreateParameter();
                parameter.ParameterName = paramName;

                object? convertedValue = ConvertValue(filter.Value, clrType);
                if (op == FilterOperator.Contains)
                {
                    convertedValue = $"%{convertedValue}%";
                }

                parameter.Value = convertedValue ?? DBNull.Value;
                command.Parameters.Add(parameter);

                var clause = op switch
                {
                    FilterOperator.Equals => $"{column} = {paramName}",
                    FilterOperator.Contains => $"{column} ILIKE {paramName}",
                    FilterOperator.GreaterThan => $"{column} > {paramName}",
                    FilterOperator.LessThan => $"{column} < {paramName}",
                    _ => $"{column} = {paramName}"
                };

                whereClauses.Add(clause);
            }
        }

        if (whereClauses.Any())
        {
            sql += " WHERE " + string.Join(" AND ", whereClauses);
        }

        if (!string.IsNullOrWhiteSpace(options?.OrderBy) && columnTypeMap.ContainsKey(options.OrderBy))
        {
            sql += $" ORDER BY {QuoteIdentifier(options.OrderBy)} {(options.OrderByDescending ? "DESC" : "ASC")}";
        }

        if (options?.Take is > 0)
        {
            var takeParam = command.CreateParameter();
            takeParam.ParameterName = "@take";
            takeParam.Value = options.Take.Value;
            command.Parameters.Add(takeParam);
            sql += " LIMIT @take";
        }

        if (options?.Skip is > 0)
        {
            var skipParam = command.CreateParameter();
            skipParam.ParameterName = "@skip";
            skipParam.Value = options.Skip.Value;
            command.Parameters.Add(skipParam);
            sql += " OFFSET @skip";
        }

        command.CommandText = sql;
        await _db.Database.OpenConnectionAsync(ct);

        try
        {
            var results = new List<Dictionary<string, object?>>();
            using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var row = new Dictionary<string, object?>(StringComparer.Ordinal);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }

                results.Add(row);
            }

            return results;
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    private async Task<int> CountDynamicAsync(
        EntityDefinition entity,
        List<FilterCondition>? filters,
        CancellationToken ct = default)
    {
        var columnTypeMap = BuildColumnTypeMap(entity);
        var tableName = QuoteIdentifier(entity.DefaultTableName);

        var sql = $"SELECT COUNT(*) FROM {tableName}";
        var whereClauses = new List<string>();
        using var command = _db.Database.GetDbConnection().CreateCommand();

        if (columnTypeMap.ContainsKey("IsDeleted"))
        {
            whereClauses.Add($"{QuoteIdentifier("IsDeleted")} = FALSE");
        }

        if (filters != null)
        {
            foreach (var filter in filters)
            {
                if (string.IsNullOrWhiteSpace(filter.Field) || !columnTypeMap.TryGetValue(filter.Field, out var clrType))
                {
                    continue;
                }

                var column = QuoteIdentifier(filter.Field);
                var op = filter.Operator ?? FilterOperator.Equals;

                if (filter.Value == null)
                {
                    whereClauses.Add(op == FilterOperator.Equals
                        ? $"{column} IS NULL"
                        : $"{column} IS NOT NULL");
                    continue;
                }

                var paramName = $"@p{command.Parameters.Count}";
                var parameter = command.CreateParameter();
                parameter.ParameterName = paramName;

                object? convertedValue = ConvertValue(filter.Value, clrType);
                if (op == FilterOperator.Contains)
                {
                    convertedValue = $"%{convertedValue}%";
                }

                parameter.Value = convertedValue ?? DBNull.Value;
                command.Parameters.Add(parameter);

                var clause = op switch
                {
                    FilterOperator.Equals => $"{column} = {paramName}",
                    FilterOperator.Contains => $"{column} ILIKE {paramName}",
                    FilterOperator.GreaterThan => $"{column} > {paramName}",
                    FilterOperator.LessThan => $"{column} < {paramName}",
                    _ => $"{column} = {paramName}"
                };

                whereClauses.Add(clause);
            }
        }

        if (whereClauses.Any())
        {
            sql += " WHERE " + string.Join(" AND ", whereClauses);
        }

        command.CommandText = sql;
        await _db.Database.OpenConnectionAsync(ct);

        try
        {
            var result = await command.ExecuteScalarAsync(ct);
            return result == null ? 0 : Convert.ToInt32(result);
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    private async Task<Dictionary<string, object?>?> GetDynamicByIdAsync(
        EntityDefinition entity,
        int id,
        CancellationToken ct = default)
    {
        var columnTypeMap = BuildColumnTypeMap(entity);
        if (!columnTypeMap.ContainsKey("Id"))
        {
            throw new InvalidOperationException($"Entity {entity.FullTypeName} does not support Id-based operations");
        }

        var tableName = QuoteIdentifier(entity.DefaultTableName);
        var idColumn = QuoteIdentifier("Id");

        using var command = _db.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT * FROM {tableName} WHERE {idColumn} = @id";

        var idParam = command.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = id;
        command.Parameters.Add(idParam);

        await _db.Database.OpenConnectionAsync(ct);
        try
        {
            using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
            {
                return null;
            }

            var row = new Dictionary<string, object?>(StringComparer.Ordinal);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            return row;
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    private async Task<Dictionary<string, object?>> CreateDynamicAsync(
        EntityDefinition entity,
        Dictionary<string, object> data,
        CancellationToken ct = default)
    {
        var columnTypeMap = BuildColumnTypeMap(entity);
        var tableName = QuoteIdentifier(entity.DefaultTableName);

        var columns = new List<string>();
        var values = new List<string>();

        using var command = _db.Database.GetDbConnection().CreateCommand();
        foreach (var (key, rawValue) in data)
        {
            if (!columnTypeMap.TryGetValue(key, out var clrType))
            {
                _logger.LogWarning("[Persistence] Property {PropertyName} not found or not writable", key);
                continue;
            }

            columns.Add(QuoteIdentifier(key));
            var paramName = $"@p{command.Parameters.Count}";
            values.Add(paramName);

            var parameter = command.CreateParameter();
            parameter.ParameterName = paramName;
            parameter.Value = ConvertValue(rawValue, clrType) ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        if (columns.Count == 0)
        {
            command.CommandText = $"INSERT INTO {tableName} DEFAULT VALUES RETURNING *";
        }
        else
        {
            command.CommandText = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)}) RETURNING *";
        }

        await _db.Database.OpenConnectionAsync(ct);
        try
        {
            using var reader = await command.ExecuteReaderAsync(ct);
            await reader.ReadAsync(ct);

            var row = new Dictionary<string, object?>(StringComparer.Ordinal);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            return row;
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    private async Task<Dictionary<string, object?>?> UpdateDynamicAsync(
        EntityDefinition entity,
        int id,
        Dictionary<string, object> data,
        CancellationToken ct = default)
    {
        var columnTypeMap = BuildColumnTypeMap(entity);
        if (!columnTypeMap.ContainsKey("Id"))
        {
            throw new InvalidOperationException($"Entity {entity.FullTypeName} does not support Id-based operations");
        }

        var tableName = QuoteIdentifier(entity.DefaultTableName);
        var idColumn = QuoteIdentifier("Id");

        var setClauses = new List<string>();
        using var command = _db.Database.GetDbConnection().CreateCommand();

        foreach (var (key, rawValue) in data)
        {
            if (key == "Id")
            {
                continue;
            }

            if (!columnTypeMap.TryGetValue(key, out var clrType))
            {
                _logger.LogWarning("[Persistence] Property {PropertyName} not found or not writable", key);
                continue;
            }

            var paramName = $"@p{command.Parameters.Count}";
            setClauses.Add($"{QuoteIdentifier(key)} = {paramName}");

            var parameter = command.CreateParameter();
            parameter.ParameterName = paramName;
            parameter.Value = ConvertValue(rawValue, clrType) ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        if (setClauses.Count == 0)
        {
            return await GetDynamicByIdAsync(entity, id, ct);
        }

        var idParam = command.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = id;
        command.Parameters.Add(idParam);

        command.CommandText = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {idColumn} = @id RETURNING *";

        await _db.Database.OpenConnectionAsync(ct);
        try
        {
            using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
            {
                return null;
            }

            var row = new Dictionary<string, object?>(StringComparer.Ordinal);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            return row;
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    private async Task<bool> DeleteDynamicAsync(
        EntityDefinition entity,
        int id,
        string? deletedBy,
        CancellationToken ct = default)
    {
        var columnTypeMap = BuildColumnTypeMap(entity);
        if (!columnTypeMap.ContainsKey("Id"))
        {
            throw new InvalidOperationException($"Entity {entity.FullTypeName} does not support Id-based operations");
        }

        var tableName = QuoteIdentifier(entity.DefaultTableName);
        var idColumn = QuoteIdentifier("Id");

        using var command = _db.Database.GetDbConnection().CreateCommand();
        var idParam = command.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = id;
        command.Parameters.Add(idParam);

        if (columnTypeMap.ContainsKey("IsDeleted"))
        {
            var sets = new List<string> { $"{QuoteIdentifier("IsDeleted")} = TRUE" };

            if (columnTypeMap.ContainsKey("DeletedAt"))
            {
                sets.Add($"{QuoteIdentifier("DeletedAt")} = @deletedAt");
                var deletedAtParam = command.CreateParameter();
                deletedAtParam.ParameterName = "@deletedAt";
                deletedAtParam.Value = DateTime.UtcNow;
                command.Parameters.Add(deletedAtParam);
            }

            if (columnTypeMap.ContainsKey("DeletedBy") && !string.IsNullOrWhiteSpace(deletedBy))
            {
                sets.Add($"{QuoteIdentifier("DeletedBy")} = @deletedBy");
                var deletedByParam = command.CreateParameter();
                deletedByParam.ParameterName = "@deletedBy";
                deletedByParam.Value = deletedBy!;
                command.Parameters.Add(deletedByParam);
            }

            command.CommandText = $"UPDATE {tableName} SET {string.Join(", ", sets)} WHERE {idColumn} = @id";
        }
        else
        {
            command.CommandText = $"DELETE FROM {tableName} WHERE {idColumn} = @id";
        }

        await _db.Database.OpenConnectionAsync(ct);
        try
        {
            var affected = await command.ExecuteNonQueryAsync(ct);
            return affected > 0;
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    private static IQueryable ApplySoftDeleteFilter(IQueryable query, Type entityType)
    {
        var prop = entityType.GetProperty("IsDeleted", BindingFlags.Public | BindingFlags.Instance);
        if (prop == null)
        {
            return query;
        }

        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var left = System.Linq.Expressions.Expression.Property(parameter, prop);
        var right = System.Linq.Expressions.Expression.Constant(false, prop.PropertyType);
        var body = System.Linq.Expressions.Expression.Equal(left, right);
        var lambda = System.Linq.Expressions.Expression.Lambda(body, parameter);
        return ApplyWhere(query, entityType, lambda);
    }

    private static IQueryable ApplySkip(IQueryable query, Type entityType, int count)
    {
        var method = typeof(Queryable).GetMethods()
            .Single(m =>
                m.Name == nameof(Queryable.Skip) &&
                m.GetParameters().Length == 2 &&
                m.GetParameters()[1].ParameterType == typeof(int))
            .MakeGenericMethod(entityType);
        return (IQueryable)method.Invoke(null, new object[] { query, count })!;
    }

    private static IQueryable ApplyTake(IQueryable query, Type entityType, int count)
    {
        var method = typeof(Queryable).GetMethods()
            .Single(m =>
                m.Name == nameof(Queryable.Take) &&
                m.GetParameters().Length == 2 &&
                m.GetParameters()[1].ParameterType == typeof(int))
            .MakeGenericMethod(entityType);
        return (IQueryable)method.Invoke(null, new object[] { query, count })!;
    }

    private static IQueryable ApplyWhere(IQueryable query, Type entityType, System.Linq.Expressions.LambdaExpression predicate)
    {
        var method = typeof(Queryable).GetMethods()
            .Where(m => m.Name == nameof(Queryable.Where) && m.GetParameters().Length == 2)
            .Single(m =>
            {
                var secondParam = m.GetParameters()[1].ParameterType;
                if (!secondParam.IsGenericType || secondParam.GetGenericTypeDefinition().Name != "Expression`1")
                {
                    return false;
                }

                var funcType = secondParam.GetGenericArguments().Single();
                if (!funcType.IsGenericType)
                {
                    return false;
                }

                // 只选 Where<T>(..., Expression<Func<T, bool>>) 这一支，避免 Where<T>(..., Expression<Func<T, int, bool>>)
                return funcType.GetGenericArguments().Length == 2;
            })
            .MakeGenericMethod(entityType);
        return (IQueryable)method.Invoke(null, new object[] { query, predicate })!;
    }

    private static async Task<object?> FirstOrDefaultAsync(IQueryable query, Type entityType, CancellationToken ct = default)
    {
        var method = typeof(EntityFrameworkQueryableExtensions).GetMethods()
            .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.FirstOrDefaultAsync))
            .Single(m => m.IsGenericMethodDefinition && m.GetParameters().Length == 2)
            .MakeGenericMethod(entityType);

        var task = (Task)method.Invoke(null, new object[] { query, ct })!;
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task);
    }

    private static async Task<System.Collections.IList> ToListAsync(IQueryable query, Type entityType, CancellationToken ct = default)
    {
        var method = typeof(EntityFrameworkQueryableExtensions).GetMethods()
            .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.ToListAsync))
            .Single(m => m.IsGenericMethodDefinition && m.GetParameters().Length == 2)
            .MakeGenericMethod(entityType);

        var task = (Task)method.Invoke(null, new object[] { query, ct })!;
        await task.ConfigureAwait(false);
        return (System.Collections.IList)task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static async Task<int> CountAsync(IQueryable query, Type entityType, CancellationToken ct = default)
    {
        var method = typeof(EntityFrameworkQueryableExtensions).GetMethods()
            .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.CountAsync))
            .Single(m => m.IsGenericMethodDefinition && m.GetParameters().Length == 2)
            .MakeGenericMethod(entityType);

        var task = (Task)method.Invoke(null, new object[] { query, ct })!;
        await task.ConfigureAwait(false);
        return (int)task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private System.Linq.Expressions.LambdaExpression BuildEqualsPredicate(Type entityType, string propertyName, object value)
    {
        var property = entityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property == null)
        {
            throw new InvalidOperationException($"Property {propertyName} not found on {entityType.Name}");
        }

        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var left = System.Linq.Expressions.Expression.Property(parameter, property);
        var constantValue = ConvertValue(value, property.PropertyType);
        var right = System.Linq.Expressions.Expression.Constant(constantValue, property.PropertyType);
        var body = System.Linq.Expressions.Expression.Equal(left, right);
        return System.Linq.Expressions.Expression.Lambda(body, parameter);
    }

    /// <summary>
    /// 获取DbSet
    /// </summary>
    private object GetDbSet(Type entityType)
    {
        var setMethod = _db.GetType().GetMethod("Set", Type.EmptyTypes);
        if (setMethod == null)
            throw new InvalidOperationException("Set method not found");

        var genericSetMethod = setMethod.MakeGenericMethod(entityType);
        var dbSet = genericSetMethod.Invoke(_db, null);

        if (dbSet == null)
            throw new InvalidOperationException($"Failed to get DbSet for {entityType.Name}");

        return dbSet;
    }

    /// <summary>
    /// 检查实体类型是否有指定属性
    /// </summary>
    private bool HasProperty(Type entityType, string propertyName)
    {
        return entityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance) != null;
    }

    /// <summary>
    /// 设置实体的单个属性值
    /// </summary>
    private void SetPropertyValue(object entity, Type entityType, string propertyName, object value)
    {
        var property = entityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property != null && property.CanWrite)
        {
            var convertedValue = ConvertValue(value, property.PropertyType);
            property.SetValue(entity, convertedValue);
        }
    }

    /// <summary>
    /// 设置实体属性值
    /// </summary>
    private void SetProperties(object entity, Type entityType, Dictionary<string, object> data)
    {
        foreach (var kvp in data)
        {
            var property = entityType.GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite)
            {
                _logger.LogWarning("[Persistence] Property {PropertyName} not found or not writable", kvp.Key);
                continue;
            }

            try
            {
                // 类型转换
                object? value = ConvertValue(kvp.Value, property.PropertyType);
                property.SetValue(entity, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Persistence] Failed to set property {PropertyName}", kvp.Key);
                throw new InvalidOperationException($"Failed to set property {kvp.Key}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 类型转换
    /// </summary>
    private object? ConvertValue(object value, Type targetType)
    {
        if (value == null)
            return null;

        // 处理可空类型
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // 如果值已经是目标类型
        if (value.GetType() == underlyingType)
            return value;

        // 处理JsonElement（从JSON反序列化）
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number when underlyingType == typeof(int) => jsonElement.GetInt32(),
                JsonValueKind.Number when underlyingType == typeof(long) => jsonElement.GetInt64(),
                JsonValueKind.Number when underlyingType == typeof(decimal) => jsonElement.GetDecimal(),
                JsonValueKind.Number when underlyingType == typeof(double) => jsonElement.GetDouble(),
                JsonValueKind.True or JsonValueKind.False => jsonElement.GetBoolean(),
                JsonValueKind.Null => null,
                _ => Convert.ChangeType(jsonElement.ToString(), underlyingType)
            };
        }

        // 字符串转换
        if (value is string str)
        {
            if (underlyingType == typeof(Guid))
                return Guid.Parse(str);
            if (underlyingType == typeof(DateTime))
                return DateTime.Parse(str);
            if (underlyingType == typeof(DateOnly))
                return DateOnly.Parse(str);
        }

        // 通用转换
        return Convert.ChangeType(value, underlyingType);
    }

    /// <summary>
    /// 应用过滤条件
    /// </summary>
    private IQueryable ApplyFilters(
        IQueryable query,
        Type entityType,
        List<FilterCondition> filters)
    {
        foreach (var filter in filters)
        {
            var property = entityType.GetProperty(filter.Field);
            if (property == null)
            {
                _logger.LogWarning("[Persistence] Filter field {Field} not found", filter.Field);
                continue;
            }

            try
            {
                query = ApplyFilter(query, entityType, property, filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Persistence] Failed to apply filter on {Field}", filter.Field);
                throw;
            }
        }

        return query;
    }

    private IQueryable ApplyFilter(IQueryable query, Type entityType, PropertyInfo property, FilterCondition filter)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var member = System.Linq.Expressions.Expression.Property(parameter, property);

        var constantValue = ConvertValue(filter.Value!, property.PropertyType);
        var constant = System.Linq.Expressions.Expression.Constant(constantValue, property.PropertyType);

        System.Linq.Expressions.Expression body = (filter.Operator ?? FilterOperator.Equals) switch
        {
            FilterOperator.Equals => System.Linq.Expressions.Expression.Equal(member, constant),
            FilterOperator.Contains when property.PropertyType == typeof(string) =>
                System.Linq.Expressions.Expression.AndAlso(
                    System.Linq.Expressions.Expression.NotEqual(member, System.Linq.Expressions.Expression.Constant(null, typeof(string))),
                    System.Linq.Expressions.Expression.Call(member, nameof(string.Contains), Type.EmptyTypes,
                        System.Linq.Expressions.Expression.Convert(constant, typeof(string)))),
            FilterOperator.GreaterThan => System.Linq.Expressions.Expression.GreaterThan(member, constant),
            FilterOperator.LessThan => System.Linq.Expressions.Expression.LessThan(member, constant),
            _ => throw new NotSupportedException($"Unsupported filter operator: {filter.Operator}")
        };

        var lambda = System.Linq.Expressions.Expression.Lambda(body, parameter);
        return ApplyWhere(query, entityType, lambda);
    }

    /// <summary>
    /// 应用排序
    /// </summary>
    private IQueryable ApplyOrderBy(
        IQueryable query,
        Type entityType,
        string orderBy,
        bool descending)
    {
        var property = entityType.GetProperty(orderBy);
        if (property == null)
        {
            _logger.LogWarning("[Persistence] OrderBy field {Field} not found", orderBy);
            return query;
        }

        // 使用反射调用OrderBy/OrderByDescending
        var methodName = descending ? "OrderByDescending" : "OrderBy";
        var orderByMethod = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(entityType, property.PropertyType);

        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var propertyAccess = System.Linq.Expressions.Expression.Property(parameter, property);
        var lambda = System.Linq.Expressions.Expression.Lambda(propertyAccess, parameter);

        var orderedQuery = orderByMethod.Invoke(null, new object[] { query, lambda });

        return (IQueryable)orderedQuery!;
    }

    /// <summary>
    /// 获取实体的所有记录（原始SQL）
    /// </summary>
    public async Task<List<Dictionary<string, object?>>> QueryRawAsync(
        string tableName,
        QueryOptions? options = null)
    {
        _logger.LogInformation("[Persistence] Raw query on table {TableName}", tableName);

        var sql = $"SELECT * FROM \"{tableName}\"";

        // 添加WHERE子句
        if (options?.Filters != null && options.Filters.Any())
        {
            var conditions = options.Filters
                .Select(f => $"\"{f.Field}\" = '{f.Value}'")
                .ToList();
            sql += " WHERE " + string.Join(" AND ", conditions);
        }

        // 添加ORDER BY
        if (!string.IsNullOrEmpty(options?.OrderBy))
        {
            sql += $" ORDER BY \"{options.OrderBy}\" {(options.OrderByDescending ? "DESC" : "ASC")}";
        }

        // 添加LIMIT和OFFSET
        if (options?.Take.HasValue == true)
        {
            sql += $" LIMIT {options.Take.Value}";
        }

        if (options?.Skip.HasValue == true)
        {
            sql += $" OFFSET {options.Skip.Value}";
        }

        // 执行SQL
        using var command = _db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        await _db.Database.OpenConnectionAsync();

        var results = new List<Dictionary<string, object?>>();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            results.Add(row);
        }

        await _db.Database.CloseConnectionAsync();

        _logger.LogInformation("[Persistence] Raw query returned {Count} records", results.Count);

        return results;
    }
}
