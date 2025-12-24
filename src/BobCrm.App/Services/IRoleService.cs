using BobCrm.App.Models;

namespace BobCrm.App.Services;

public interface IRoleService
{
    Task<List<RoleProfileDto>> GetRolesAsync(CancellationToken ct = default);
    Task<RoleProfileDto?> GetRoleAsync(Guid id, CancellationToken ct = default);
    Task<RoleProfileDto?> CreateRoleAsync(CreateRoleRequestDto request, CancellationToken ct = default);
    Task<bool> UpdateRoleAsync(Guid id, UpdateRoleRequestDto request, CancellationToken ct = default);
    Task<bool> UpdatePermissionsAsync(Guid id, UpdatePermissionsRequestDto request, CancellationToken ct = default);
    Task<FunctionTreeResponse> GetFunctionTreeAsync(bool forceRefresh = false, CancellationToken ct = default);
    Task<string?> GetFunctionTreeVersionAsync(CancellationToken ct = default);
    void InvalidateFunctionTreeCache();
}
