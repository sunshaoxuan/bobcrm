namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 简化成功/失败响应（非泛型）
/// </summary>
public record ApiResponse(bool Success, string? Message = null);
