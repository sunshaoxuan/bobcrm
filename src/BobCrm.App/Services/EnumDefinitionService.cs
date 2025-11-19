using System.Net.Http.Json;
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.App.Services;

/// <summary>
/// 枚举定义服务 - 负责与后端EnumDefinition API交互
/// </summary>
public class EnumDefinitionService
{
    private readonly AuthService _auth;

    public EnumDefinitionService(AuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// 获取所有枚举定义
    /// </summary>
    public async Task<List<EnumDefinitionDto>> GetAllAsync()
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetFromJsonAsync<List<EnumDefinitionDto>>("/api/enum-definitions");
        return response ?? new List<EnumDefinitionDto>();
    }

    /// <summary>
    /// 根据ID获取枚举定义
    /// </summary>
    public async Task<EnumDefinitionDto?> GetByIdAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetFromJsonAsync<EnumDefinitionDto>($"/api/enum-definitions/{id}");
        return response;
    }

    /// <summary>
    /// 创建枚举定义
    /// </summary>
    public async Task<EnumDefinitionDto?> CreateAsync(CreateEnumDefinitionRequest request)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsJsonAsync("/api/enum-definitions", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EnumDefinitionDto>();
    }

    /// <summary>
    /// 更新枚举定义
    /// </summary>
    public async Task<EnumDefinitionDto?> UpdateAsync(Guid id, UpdateEnumDefinitionRequest request)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PutAsJsonAsync($"/api/enum-definitions/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EnumDefinitionDto>();
    }

    /// <summary>
    /// 删除枚举定义
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.DeleteAsync($"/api/enum-definitions/{id}");
        response.EnsureSuccessStatusCode();
    }
}
