using BobCrm.Api.Abstractions;

namespace BobCrm.Api.Infrastructure;

/// <summary>
/// 控制台邮件发送实现（开发环境使用）
/// 将邮件内容输出到控制台而不实际发送
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string body)
    {
        Console.WriteLine($"[EMAIL] To:{to} Subject:{subject} Body:{body}");
        return Task.CompletedTask;
    }
}
