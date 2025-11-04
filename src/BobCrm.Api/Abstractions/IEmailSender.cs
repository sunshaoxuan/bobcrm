namespace BobCrm.Api.Abstractions;

/// <summary>
/// 邮件发送服务接口
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// 发送邮件
    /// </summary>
    /// <param name="to">收件人地址</param>
    /// <param name="subject">邮件主题</param>
    /// <param name="body">邮件正文</param>
    Task SendAsync(string to, string subject, string body);
}
