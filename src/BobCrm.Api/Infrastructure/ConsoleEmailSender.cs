using BobCrm.Api.Abstractions;
using Microsoft.Extensions.Logging;

namespace BobCrm.Api.Infrastructure;

/// <summary>
/// 控制台邮件发送实现（开发环境使用）
/// 将邮件内容输出到控制台而不实际发送
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body)
    {
        _logger.LogInformation("[DEV EMAIL] To:{To} Subject:{Subject} Body:{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
