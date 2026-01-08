using System.Net.Http.Json;
using System.Text.Json;
using BobCrm.App.Models;

namespace BobCrm.App.Services;

/// <summary>
/// 动态实体服务 - 负责与后端DynamicEntity API交互
/// </summary>
public class DynamicEntityService
{
    private readonly AuthService _auth;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public DynamicEntityService(AuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// 查询动态实体列表
    /// </summary>
    public async Task<DynamicEntityQueryResponse?> QueryAsync(string fullTypeName, DynamicEntityQueryRequest request)
    {
        var response = await _auth.PostAsJsonWithRefreshAsync(
            $"/api/dynamic-entities/{Uri.EscapeDataString(fullTypeName)}/query",
            request);
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<DynamicEntityQueryResponse>(response);
    }

    /// <summary>
    /// 根据ID查询单个实体
    /// </summary>
    public async Task<Dictionary<string, object>?> GetByIdAsync(string fullTypeName, int id)
    {
        var response = await _auth.GetWithRefreshAsync($"/api/dynamic-entities/{Uri.EscapeDataString(fullTypeName)}/{id}");
        response.EnsureSuccessStatusCode();
        return await ReadGetResultDataAsync(response);
    }

    /// <summary>
    /// 创建实体
    /// </summary>
    public async Task<Dictionary<string, object>?> CreateAsync(string fullTypeName, Dictionary<string, object> data)
    {
        var http = await _auth.CreateClientWithAuthAsync();
        var response = await http.PostAsJsonAsync($"/api/dynamic-entities/{Uri.EscapeDataString(fullTypeName)}", data);
        response.EnsureSuccessStatusCode();
        return await ReadGetResultDataAsync(response);
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    public async Task<Dictionary<string, object>?> UpdateAsync(string fullTypeName, int id, Dictionary<string, object> data)
    {
        var http = await _auth.CreateClientWithAuthAsync();
        var response = await http.PutAsJsonAsync($"/api/dynamic-entities/{Uri.EscapeDataString(fullTypeName)}/{id}", data);
        response.EnsureSuccessStatusCode();
        return await ReadGetResultDataAsync(response);
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public async Task DeleteAsync(string fullTypeName, int id)
    {
        var response = await _auth.DeleteWithRefreshAsync($"/api/dynamic-entities/{Uri.EscapeDataString(fullTypeName)}/{id}");
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// 统计数量
    /// </summary>
    public async Task<DynamicEntityCountResponse?> CountAsync(string fullTypeName, DynamicEntityCountRequest request)
    {
        var response = await _auth.PostAsJsonWithRefreshAsync(
            $"/api/dynamic-entities/{Uri.EscapeDataString(fullTypeName)}/count",
            request);
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<DynamicEntityCountResponse>(response);
    }

    /// <summary>
    /// 原始SQL查询
    /// </summary>
    public async Task<DynamicEntityQueryResponse?> QueryRawAsync(string tableName, DynamicEntityQueryRequest request)
    {
        var response = await _auth.PostAsJsonWithRefreshAsync(
            $"/api/dynamic-entities/raw/{Uri.EscapeDataString(tableName)}/query",
            request);
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<DynamicEntityQueryResponse>(response);
    }

    private static async Task<Dictionary<string, object>?> ReadGetResultDataAsync(HttpResponseMessage response)
    {
        var root = await ApiResponseHelper.ReadAsJsonAsync(response);
        var payload = ApiResponseHelper.Unwrap(root);

        // DynamicEntityGetResultDto wraps the actual entity in "data".
        if (payload.ValueKind == JsonValueKind.Object &&
            payload.TryGetProperty("data", out var entityData))
        {
            return entityData.Deserialize<Dictionary<string, object>>(JsonOptions);
        }

        return payload.Deserialize<Dictionary<string, object>>(JsonOptions);
    }
}
