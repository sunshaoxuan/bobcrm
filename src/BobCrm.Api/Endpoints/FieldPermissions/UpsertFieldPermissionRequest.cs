using BobCrm.Api.Contracts;

namespace BobCrm.Api.Endpoints;

// 请求DTO
public record UpsertFieldPermissionRequest(
    bool CanRead,
    bool CanWrite,
    string? Remarks = null);

