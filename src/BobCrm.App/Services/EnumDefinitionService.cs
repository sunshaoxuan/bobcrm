using System.Net.Http.Json;
using BobCrm.Api.Contracts.DTOs.Enum;
using BobCrm.Api.Contracts.Requests.Enum;

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
        var resp = await http.GetAsync("/api/enums");
        if (!resp.IsSuccessStatusCode)
        {
            return new List<EnumDefinitionDto>();
        }

        return await ApiResponseHelper.ReadDataAsync<List<EnumDefinitionDto>>(resp) ?? new List<EnumDefinitionDto>();
    }

    /// <summary>
    /// 根据ID获取枚举定义
    /// </summary>
    public async Task<EnumDefinitionDto?> GetByIdAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var resp = await http.GetAsync($"/api/enums/{id}");
        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        return await ApiResponseHelper.ReadDataAsync<EnumDefinitionDto>(resp);
    }

    /// <summary>
    /// 创建枚举定义
    /// </summary>
    public async Task<EnumDefinitionDto?> CreateAsync(CreateEnumDefinitionRequest request)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsJsonAsync("/api/enums", request);
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<EnumDefinitionDto>(response);
    }

    /// <summary>
    /// 更新枚举定义
    /// </summary>
    public async Task<EnumDefinitionDto?> UpdateAsync(Guid id, UpdateEnumDefinitionRequest request)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PutAsJsonAsync($"/api/enums/{id}", request);
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<EnumDefinitionDto>(response);
    }

    /// <summary>
    /// 删除枚举定义
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.DeleteAsync($"/api/enums/{id}");
        response.EnsureSuccessStatusCode();
    }
}
