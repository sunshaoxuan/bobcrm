using System.Text.Json;
using System.Text.Json.Serialization;

namespace BobCrm.App.Services;

/// <summary>
/// 统一处理带 SuccessResponse 包装或裸数据的响应解析。
/// </summary>
internal static class ApiResponseHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<JsonElement> ReadAsJsonAsync(HttpResponseMessage response)
    {
        var root = await response.Content.ReadFromJsonAsync<JsonElement>(Options);
        return root.ValueKind == JsonValueKind.Undefined ? default : root;
    }

    public static JsonElement Unwrap(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("data", out var data)) return data;
            if (root.TryGetProperty("Data", out var dataPascal)) return dataPascal;
        }
        return root;
    }

    public static async Task<T?> ReadDataAsync<T>(HttpResponseMessage response)
    {
        var root = await ReadAsJsonAsync(response);
        var data = Unwrap(root);
        return data.ValueKind == JsonValueKind.Undefined ? default : data.Deserialize<T>(Options);
    }
}
