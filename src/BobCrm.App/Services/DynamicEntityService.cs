using System.Net.Http.Json;
using BobCrm.App.Models;

namespace BobCrm.App.Services;

/// <summary>
/// 动态实体服务 - 负责与后端DynamicEntity API交互
/// </summary>
public class DynamicEntityService
{
    private readonly AuthService _auth;

    public DynamicEntityService(AuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// 查询动态实体列表
    /// </summary>
    public async Task<DynamicEntityQueryResponse?> QueryAsync(string fullTypeName, DynamicEntityQueryRequest request)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsJsonAsync($"/api/dynamic-entities/{Uri.EscapeDataString(fullTypeName)}/query", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DynamicEntityQueryResponse>();
    }

    /// <summary>
    /// 根据ID查询单个实体
    /// </summary>
    public async Task<Dictionary<string, object>?> GetByIdAsync(string fullTypeName, int id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetFromJsonAsync<Dictionary<string, object>>($"/api/dynamic-entities/{Uri.EscapeDataString(fullTypeName)}/{id}");
        return response;
    }

    /// <summary>
    /// 创建实体
    /// </summary>
    public async Task<Dictionary<string, object>?> CreateAsync(string fullTypeName, Dictionary<string, object> data)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsJsonAsync($"/api/dynamic-entities/{Uri.EscapeDataString(fullTypeName)}", data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    public async Task<Dictionary<string, object>?> UpdateAsync(string fullTypeName, int id, Dictionary<string, object> data)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PutAsJsonAsync($"/api/dynamic-entities/{Uri.EscapeDataString(fullTypeName)}/{id}", data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public async Task DeleteAsync(string fullTypeName, int id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.DeleteAsync($"/api/dynamic-entities/{Uri.EscapeDataString(fullTypeName)}/{id}");
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// 统计数量
    /// </summary>
    public async Task<DynamicEntityCountResponse?> CountAsync(string fullTypeName, DynamicEntityCountRequest request)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsJsonAsync($"/api/dynamic-entities/{Uri.EscapeDataString(fullTypeName)}/count", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DynamicEntityCountResponse>();
    }

    /// <summary>
    /// 原始SQL查询
    /// </summary>
    public async Task<DynamicEntityQueryResponse?> QueryRawAsync(string tableName, DynamicEntityQueryRequest request)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsJsonAsync($"/api/dynamic-entities/raw/{Uri.EscapeDataString(tableName)}/query", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DynamicEntityQueryResponse>();
    }
}
