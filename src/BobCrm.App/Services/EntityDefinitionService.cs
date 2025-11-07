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
        var response = await http.GetFromJsonAsync<List<EntityDefinitionDto>>("/api/entity-definitions");
        return response ?? new List<EntityDefinitionDto>();
    }

    /// <summary>
    /// 根据ID获取实体定义
    /// </summary>
    public async Task<EntityDefinitionDto?> GetByIdAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetFromJsonAsync<EntityDefinitionDto>($"/api/entity-definitions/{id}");
        return response;
    }

    /// <summary>
    /// 创建实体定义
    /// </summary>
    public async Task<EntityDefinitionDto?> CreateAsync(CreateEntityDefinitionRequest request)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsJsonAsync("/api/entity-definitions", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EntityDefinitionDto>();
    }

    /// <summary>
    /// 更新实体定义
    /// </summary>
    public async Task<EntityDefinitionDto?> UpdateAsync(Guid id, UpdateEntityDefinitionRequest request)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PutAsJsonAsync($"/api/entity-definitions/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EntityDefinitionDto>();
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
        var response = await http.GetFromJsonAsync<PreviewDDLResponse>($"/api/entity-definitions/{id}/preview-ddl");
        return response;
    }

    /// <summary>
    /// 发布新实体
    /// </summary>
    public async Task<PublishResponse?> PublishAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsync($"/api/entity-definitions/{id}/publish", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PublishResponse>();
    }

    /// <summary>
    /// 发布实体修改
    /// </summary>
    public async Task<PublishResponse?> PublishChangesAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsync($"/api/entity-definitions/{id}/publish-changes", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PublishResponse>();
    }

    /// <summary>
    /// 获取DDL历史
    /// </summary>
    public async Task<List<DDLHistoryDto>> GetDDLHistoryAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetFromJsonAsync<List<DDLHistoryDto>>($"/api/entity-definitions/{id}/ddl-history");
        return response ?? new List<DDLHistoryDto>();
    }

    // ==================== 代码生成与编译 ====================

    /// <summary>
    /// 生成实体代码
    /// </summary>
    public async Task<CodeGenerationResponse?> GenerateCodeAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetFromJsonAsync<CodeGenerationResponse>($"/api/entity-definitions/{id}/generate-code");
        return response;
    }

    /// <summary>
    /// 验证实体代码
    /// </summary>
    public async Task<ValidationResponse?> ValidateCodeAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetFromJsonAsync<ValidationResponse>($"/api/entity-definitions/{id}/validate-code");
        return response;
    }

    /// <summary>
    /// 编译实体
    /// </summary>
    public async Task<CompilationResponse?> CompileAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsync($"/api/entity-definitions/{id}/compile", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CompilationResponse>();
    }

    /// <summary>
    /// 重新编译实体
    /// </summary>
    public async Task<CompilationResponse?> RecompileAsync(Guid id)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsync($"/api/entity-definitions/{id}/recompile", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CompilationResponse>();
    }

    /// <summary>
    /// 批量编译实体
    /// </summary>
    public async Task<CompilationResponse?> CompileBatchAsync(List<Guid> entityIds)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.PostAsJsonAsync("/api/entity-definitions/compile-batch", new { entityIds });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CompilationResponse>();
    }

    /// <summary>
    /// 获取已加载的实体列表
    /// </summary>
    public async Task<LoadedEntitiesResponse?> GetLoadedEntitiesAsync()
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetFromJsonAsync<LoadedEntitiesResponse>("/api/entity-definitions/loaded-entities");
        return response;
    }

    /// <summary>
    /// 获取实体类型信息
    /// </summary>
    public async Task<EntityTypeInfoResponse?> GetTypeInfoAsync(string fullTypeName)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var response = await http.GetFromJsonAsync<EntityTypeInfoResponse>($"/api/entity-definitions/type-info/{Uri.EscapeDataString(fullTypeName)}");
        return response;
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

/// <summary>
/// 预览DDL响应
/// </summary>
public class PreviewDDLResponse
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DDLScript { get; set; } = string.Empty;
}

/// <summary>
/// 验证响应
/// </summary>
public class ValidationResponse
{
    public bool IsValid { get; set; }
    public List<CompilationErrorDto> Errors { get; set; } = new();
}
