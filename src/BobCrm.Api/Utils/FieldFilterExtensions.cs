using System.Security.Claims;
using System.Text.Json;
using BobCrm.Api.Services;

namespace BobCrm.Api.Utils;

/// <summary>
/// 字段过滤扩展方法 - 方便在端点中使用字段级权限
/// </summary>
public static class FieldFilterExtensions
{
    /// <summary>
    /// 过滤响应对象的字段（基于当前用户权限）
    /// </summary>
    public static async Task<IResult> FilteredOkAsync(
        this FieldFilterService filterService,
        ClaimsPrincipal user,
        string entityType,
        object? data)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        if (data == null)
        {
            return Results.Ok(null);
        }

        // 将对象序列化为 JSON
        var json = JsonSerializer.Serialize(data);
        using var document = JsonDocument.Parse(json);

        // 应用字段过滤
        var filtered = await filterService.FilterFieldsAsync(userId, entityType, document, isWrite: false);

        if (filtered == null)
        {
            return Results.Ok(null);
        }

        // 反序列化回对象
        var result = JsonSerializer.Deserialize<object>(filtered.RootElement.GetRawText());
        return Results.Ok(result);
    }

    /// <summary>
    /// 过滤响应数组的字段（基于当前用户权限）
    /// </summary>
    public static async Task<IResult> FilteredOkArrayAsync<T>(
        this FieldFilterService filterService,
        ClaimsPrincipal user,
        string entityType,
        IEnumerable<T>? data)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        if (data == null || !data.Any())
        {
            return Results.Ok(Array.Empty<T>());
        }

        // 将数组序列化为 JSON
        var json = JsonSerializer.Serialize(data);
        using var document = JsonDocument.Parse(json);

        // 应用字段过滤
        var filtered = await filterService.FilterFieldsArrayAsync(userId, entityType, document, isWrite: false);

        if (filtered == null)
        {
            return Results.Ok(Array.Empty<T>());
        }

        // 反序列化回对象数组
        var result = JsonSerializer.Deserialize<List<object>>(filtered.RootElement.GetRawText());
        return Results.Ok(result);
    }

    /// <summary>
    /// 验证写入请求的字段权限
    /// </summary>
    public static async Task<(bool IsValid, IResult? ErrorResult)> ValidateWritePermissionsAsync(
        this FieldFilterService filterService,
        ClaimsPrincipal user,
        string entityType,
        object? data)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        if (data == null)
        {
            return (true, null);
        }

        // 将对象序列化为 JSON
        var json = JsonSerializer.Serialize(data);
        using var document = JsonDocument.Parse(json);

        // 验证字段权限
        var (isValid, unauthorizedFields) = await filterService.ValidateWriteFieldsAsync(userId, entityType, document);

        if (!isValid)
        {
            return (false, Results.Json(new
            {
                error = "Insufficient field permissions",
                unauthorizedFields = unauthorizedFields
            }, statusCode: 403));
        }

        return (true, null);
    }

    /// <summary>
    /// 验证并过滤写入请求（只保留有权限的字段）
    /// </summary>
    public static async Task<Dictionary<string, object?>> FilterWriteFieldsAsync(
        this FieldFilterService filterService,
        ClaimsPrincipal user,
        string entityType,
        Dictionary<string, object?> data)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return await filterService.FilterFieldsDictionaryAsync(userId, entityType, data, isWrite: true);
    }
}

/// <summary>
/// 字段权限验证结果扩展方法
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// 如果验证失败，返回错误结果；否则继续执行
    /// </summary>
    public static async Task<IResult> OrElseAsync(
        this (bool IsValid, IResult? ErrorResult) validation,
        Func<Task<IResult>> onSuccess)
    {
        if (!validation.IsValid && validation.ErrorResult != null)
        {
            return validation.ErrorResult;
        }

        return await onSuccess();
    }

    /// <summary>
    /// 如果验证失败，返回错误结果；否则继续执行（同步版本）
    /// </summary>
    public static IResult OrElse(
        this (bool IsValid, IResult? ErrorResult) validation,
        Func<IResult> onSuccess)
    {
        if (!validation.IsValid && validation.ErrorResult != null)
        {
            return validation.ErrorResult;
        }

        return onSuccess();
    }
}
