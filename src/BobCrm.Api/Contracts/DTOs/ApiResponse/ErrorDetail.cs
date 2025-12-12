namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 错误详情
/// </summary>
public record ErrorDetail(string Code, string Message, Dictionary<string, string[]>? ValidationErrors = null);
