using System.Text.Json;
using BobCrm.Api.Abstractions;

namespace BobCrm.Api.Services;

/// <summary>
/// 运行时字段过滤服务 - 根据用户权限过滤 JSON 响应中的字段
/// </summary>
public class FieldFilterService
{
    private readonly IFieldPermissionService _permissionService;
    private readonly ILogger<FieldFilterService> _logger;

    public FieldFilterService(
        IFieldPermissionService permissionService,
        ILogger<FieldFilterService> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// 过滤单个对象的字段
    /// </summary>
    public async Task<JsonDocument?> FilterFieldsAsync(
        string userId,
        string entityType,
        JsonDocument? document,
        bool isWrite = false)
    {
        if (document == null)
        {
            return null;
        }

        // 获取用户对该实体的可读/可写字段
        var allowedFields = isWrite
            ? await _permissionService.GetWritableFieldsAsync(userId, entityType)
            : await _permissionService.GetReadableFieldsAsync(userId, entityType);

        // 如果没有显式权限设置，默认允许所有字段（宽松模式）
        if (!allowedFields.Any())
        {
            _logger.LogDebug("[FieldFilter] No explicit permissions for user {UserId}, entity {EntityType}. Allowing all fields.",
                userId, entityType);
            return document;
        }

        var allowedFieldSet = new HashSet<string>(allowedFields, StringComparer.OrdinalIgnoreCase);

        // 过滤 JSON 文档
        var filtered = FilterJsonElement(document.RootElement, allowedFieldSet);

        if (filtered.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return JsonDocument.Parse(filtered.GetRawText());
    }

    /// <summary>
    /// 过滤对象数组的字段
    /// </summary>
    public async Task<JsonDocument?> FilterFieldsArrayAsync(
        string userId,
        string entityType,
        JsonDocument? document,
        bool isWrite = false)
    {
        if (document == null || document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return document;
        }

        // 获取用户对该实体的可读/可写字段
        var allowedFields = isWrite
            ? await _permissionService.GetWritableFieldsAsync(userId, entityType)
            : await _permissionService.GetReadableFieldsAsync(userId, entityType);

        if (!allowedFields.Any())
        {
            _logger.LogDebug("[FieldFilter] No explicit permissions for user {UserId}, entity {EntityType}. Allowing all fields.",
                userId, entityType);
            return document;
        }

        var allowedFieldSet = new HashSet<string>(allowedFields, StringComparer.OrdinalIgnoreCase);

        // 过滤数组中的每个对象
        var filteredArray = new List<JsonElement>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            var filtered = FilterJsonElement(item, allowedFieldSet);
            if (filtered.ValueKind != JsonValueKind.Null)
            {
                filteredArray.Add(filtered);
            }
        }

        var json = JsonSerializer.Serialize(filteredArray);
        return JsonDocument.Parse(json);
    }

    /// <summary>
    /// 验证写入请求中的字段权限
    /// </summary>
    public async Task<(bool IsValid, List<string> UnauthorizedFields)> ValidateWriteFieldsAsync(
        string userId,
        string entityType,
        JsonDocument? document)
    {
        if (document == null)
        {
            return (true, new List<string>());
        }

        var writableFields = await _permissionService.GetWritableFieldsAsync(userId, entityType);

        // 如果没有显式权限，允许所有字段（宽松模式）
        if (!writableFields.Any())
        {
            return (true, new List<string>());
        }

        var writableFieldSet = new HashSet<string>(writableFields, StringComparer.OrdinalIgnoreCase);
        var unauthorizedFields = new List<string>();

        // 检查请求中的所有字段
        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!writableFieldSet.Contains(property.Name))
                {
                    unauthorizedFields.Add(property.Name);
                }
            }
        }

        return (unauthorizedFields.Count == 0, unauthorizedFields);
    }

    /// <summary>
    /// 过滤字典对象的字段
    /// </summary>
    public async Task<Dictionary<string, object?>> FilterFieldsDictionaryAsync(
        string userId,
        string entityType,
        Dictionary<string, object?> data,
        bool isWrite = false)
    {
        var allowedFields = isWrite
            ? await _permissionService.GetWritableFieldsAsync(userId, entityType)
            : await _permissionService.GetReadableFieldsAsync(userId, entityType);

        if (!allowedFields.Any())
        {
            return data;
        }

        var allowedFieldSet = new HashSet<string>(allowedFields, StringComparer.OrdinalIgnoreCase);
        return data.Where(kvp => allowedFieldSet.Contains(kvp.Key))
                   .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// 递归过滤 JsonElement
    /// </summary>
    private JsonElement FilterJsonElement(JsonElement element, HashSet<string> allowedFields)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return element;
        }

        var filteredProperties = new Dictionary<string, JsonElement>();

        foreach (var property in element.EnumerateObject())
        {
            // 只保留允许的字段
            if (allowedFields.Contains(property.Name))
            {
                // 递归处理嵌套对象
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    filteredProperties[property.Name] = FilterJsonElement(property.Value, allowedFields);
                }
                else if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    // 对数组中的每个对象递归过滤
                    var filteredArray = new List<JsonElement>();
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            filteredArray.Add(FilterJsonElement(item, allowedFields));
                        }
                        else
                        {
                            filteredArray.Add(item);
                        }
                    }
                    var arrayJson = JsonSerializer.Serialize(filteredArray);
                    filteredProperties[property.Name] = JsonDocument.Parse(arrayJson).RootElement;
                }
                else
                {
                    filteredProperties[property.Name] = property.Value;
                }
            }
            else
            {
                _logger.LogTrace("[FieldFilter] Filtered out field: {FieldName}", property.Name);
            }
        }

        var json = JsonSerializer.Serialize(filteredProperties);
        return JsonDocument.Parse(json).RootElement;
    }

    /// <summary>
    /// 检查用户是否可以读取指定字段
    /// </summary>
    public async Task<bool> CanReadFieldAsync(string userId, string entityType, string fieldName)
    {
        return await _permissionService.CanUserReadFieldAsync(userId, entityType, fieldName);
    }

    /// <summary>
    /// 检查用户是否可以写入指定字段
    /// </summary>
    public async Task<bool> CanWriteFieldAsync(string userId, string entityType, string fieldName)
    {
        return await _permissionService.CanUserWriteFieldAsync(userId, entityType, fieldName);
    }
}
