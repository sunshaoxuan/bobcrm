using System.Net;
using System.Net.Mail;
using BobCrm.Api.Abstractions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Infrastructure;

public sealed class DbSmtpEmailSender : IEmailSender
{
    private readonly AppDbContext _db;
    private readonly IDataProtector _smtpProtector;
    private readonly ILogger<DbSmtpEmailSender> _logger;

    public DbSmtpEmailSender(AppDbContext db, IDataProtectionProvider dataProtectionProvider, ILogger<DbSmtpEmailSender> logger)
    {
        _db = db;
        _smtpProtector = dataProtectionProvider.CreateProtector("BobCrm.SystemSettings.SmtpPassword.v1");
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var settings = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync();
        if (settings == null)
        {
            _logger.LogWarning("[Email] SystemSettings missing; skipping send to {To}", to);
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.SmtpHost) || string.IsNullOrWhiteSpace(settings.SmtpFromAddress))
        {
            _logger.LogInformation("[Email] SMTP not configured; skipping send to {To} subject {Subject}", to, subject);
            return;
        }

        var password = TryUnprotect(settings.SmtpPasswordEncrypted);

        using var client = new SmtpClient(settings.SmtpHost.Trim(), settings.SmtpPort <= 0 ? 25 : settings.SmtpPort)
        {
            EnableSsl = settings.SmtpEnableSsl
        };

        if (!string.IsNullOrWhiteSpace(settings.SmtpUsername))
        {
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(settings.SmtpUsername.Trim(), password ?? string.Empty);
        }

        var from = string.IsNullOrWhiteSpace(settings.SmtpDisplayName)
            ? new MailAddress(settings.SmtpFromAddress.Trim())
            : new MailAddress(settings.SmtpFromAddress.Trim(), settings.SmtpDisplayName.Trim());

        using var msg = new MailMessage(from, new MailAddress(to.Trim()))
        {
            Subject = subject ?? string.Empty,
            Body = body ?? string.Empty,
            IsBodyHtml = false
        };

        await client.SendMailAsync(msg);
    }

    private string? TryUnprotect(string? protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return null;
        }

        try
        {
            return _smtpProtector.Unprotect(protectedValue);
        }
        catch
        {
            _logger.LogWarning("[Email] Failed to decrypt SMTP password; will try with empty password");
            return null;
        }
    }
}
