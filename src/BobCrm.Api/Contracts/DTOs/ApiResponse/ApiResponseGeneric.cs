namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 统一 API 泛型响应
/// </summary>
public record ApiResponse<T>(bool Success, T? Data, string? Message = null, ErrorDetail? Error = null);
