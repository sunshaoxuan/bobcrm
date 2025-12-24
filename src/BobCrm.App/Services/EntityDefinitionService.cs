using System.Net.Http.Json;
using BobCrm.App.Models;

namespace BobCrm.App.Services;

/// <summary>
/// 实体定义服务 - 负责与后端EntityDefinition API交互
/// </summary>
public class EntityDefinitionService
{
    private readonly AuthService _auth;

    public EntityDefinitionService(AuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// 获取实体定义列表
    /// </summary>
    public async Task<List<EntityDefinitionDto>> GetAllAsync()
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetAsync("/api/entity-definitions");
        response.EnsureSuccessStatusCode();
        var data = await ApiResponseHelper.ReadDataAsync<List<EntityDefinitionDto>>(response);
        return data ?? new List<EntityDefinitionDto>();
    }

    /// <summary>
    /// 根据ID获取实体定义
    /// </summary>
    public async Task<EntityDefinitionDto?> GetByIdAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetAsync($"/api/entity-definitions/{id}");
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<EntityDefinitionDto>(response);
    }

    /// <summary>
    /// 创建实体定义
    /// </summary>
    public async Task<EntityDefinitionDto?> CreateAsync(CreateEntityDefinitionRequest request)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsJsonAsync("/api/entity-definitions", request);
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<EntityDefinitionDto>(response);
    }

    /// <summary>
    /// 更新实体定义
    /// </summary>
    public async Task<EntityDefinitionDto?> UpdateAsync(Guid id, UpdateEntityDefinitionRequest request)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PutAsJsonAsync($"/api/entity-definitions/{id}", request);
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<EntityDefinitionDto>(response);
    }

    /// <summary>
    /// 删除实体定义
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.DeleteAsync($"/api/entity-definitions/{id}");
        response.EnsureSuccessStatusCode();
    }

    // ==================== 发布相关 ====================

    /// <summary>
    /// 预览DDL脚本
    /// </summary>
    public async Task<PreviewDDLResponse?> PreviewDDLAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetAsync($"/api/entity-definitions/{id}/preview-ddl");
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<PreviewDDLResponse>(response);
    }

    /// <summary>
    /// 发布新实体
    /// </summary>
    public async Task<PublishResponse?> PublishAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsync($"/api/entity-definitions/{id}/publish", null);
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<PublishResponse>(response);
    }

    /// <summary>
    /// 发布实体修改
    /// </summary>
    public async Task<PublishResponse?> PublishChangesAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsync($"/api/entity-definitions/{id}/publish-changes", null);
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<PublishResponse>(response);
    }

    /// <summary>
    /// 获取DDL历史
    /// </summary>
    public async Task<List<DDLHistoryDto>> GetDDLHistoryAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetAsync($"/api/entity-definitions/{id}/ddl-history");
        response.EnsureSuccessStatusCode();
        var data = await ApiResponseHelper.ReadDataAsync<List<DDLHistoryDto>>(response);
        return data ?? new List<DDLHistoryDto>();
    }

    // ==================== 代码生成与编译 ====================

    /// <summary>
    /// 生成实体代码
    /// </summary>
    public async Task<CodeGenerationResponse?> GenerateCodeAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetAsync($"/api/entity-definitions/{id}/generate-code");
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<CodeGenerationResponse>(response);
    }

    /// <summary>
    /// 验证实体代码
    /// </summary>
    public async Task<ValidationResponse?> ValidateCodeAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetAsync($"/api/entity-definitions/{id}/validate-code");
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<ValidationResponse>(response);
    }

    /// <summary>
    /// 编译实体
    /// </summary>
    public async Task<CompilationResponse?> CompileAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsync($"/api/entity-definitions/{id}/compile", null);
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<CompilationResponse>(response);
    }

    /// <summary>
    /// 重新编译实体
    /// </summary>
    public async Task<CompilationResponse?> RecompileAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsync($"/api/entity-definitions/{id}/recompile", null);
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<CompilationResponse>(response);
    }

    /// <summary>
    /// 批量编译实体
    /// </summary>
    public async Task<CompilationResponse?> CompileBatchAsync(List<Guid> entityIds)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsJsonAsync("/api/entity-definitions/compile-batch", new { entityIds });
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<CompilationResponse>(response);
    }

    /// <summary>
    /// 获取已加载的实体列表
    /// </summary>
    public async Task<LoadedEntitiesResponse?> GetLoadedEntitiesAsync()
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetAsync("/api/entity-definitions/loaded-entities");
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<LoadedEntitiesResponse>(response);
    }

    /// <summary>
    /// 获取实体类型信息
    /// </summary>
    public async Task<EntityTypeInfoResponse?> GetTypeInfoAsync(string fullTypeName)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetAsync($"/api/entity-definitions/type-info/{Uri.EscapeDataString(fullTypeName)}");
        response.EnsureSuccessStatusCode();
        return await ApiResponseHelper.ReadDataAsync<EntityTypeInfoResponse>(response);
    }

    /// <summary>
    /// 卸载实体
    /// </summary>
    public async Task UnloadEntityAsync(string fullTypeName)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.DeleteAsync($"/api/entity-definitions/loaded-entities/{Uri.EscapeDataString(fullTypeName)}");
        response.EnsureSuccessStatusCode();
    }
}
