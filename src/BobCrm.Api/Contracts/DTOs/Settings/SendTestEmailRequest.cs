namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 发送 SMTP 测试邮件请求
/// </summary>
public record SendTestEmailRequest(string To);

