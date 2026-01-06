using System.Reflection;
using System.Security.Cryptography;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Base.Models;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BobCrm.Api.Tests;

public class DbSmtpEmailSenderTests
{
    [Fact]
    public async Task SendAsync_WhenSystemSettingsMissing_ShouldReturn()
    {
        using var factory = new TestWebAppFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.SystemSettings.RemoveRange(await db.SystemSettings.ToListAsync());
        await db.SaveChangesAsync();

        var sender = new DbSmtpEmailSender(db, DataProtectionProvider.Create("test"), NullLogger<DbSmtpEmailSender>.Instance);
        await sender.SendAsync("to@example.com", "subject", "body");
    }

    [Fact]
    public async Task SendAsync_WhenSmtpNotConfigured_ShouldReturn()
    {
        using var factory = new TestWebAppFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.SystemSettings.RemoveRange(await db.SystemSettings.ToListAsync());
        db.SystemSettings.Add(new SystemSettings
        {
            DefaultLanguage = "zh",
            SmtpHost = "",
            SmtpFromAddress = ""
        });
        await db.SaveChangesAsync();

        var sender = new DbSmtpEmailSender(db, DataProtectionProvider.Create("test"), NullLogger<DbSmtpEmailSender>.Instance);
        await sender.SendAsync("to@example.com", "subject", "body");
    }

    [Fact]
    public void TryUnprotect_WhenProtectedValueMissing_ShouldReturnNull()
    {
        using var factory = new TestWebAppFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sender = new DbSmtpEmailSender(db, DataProtectionProvider.Create("test"), NullLogger<DbSmtpEmailSender>.Instance);
        InvokeTryUnprotect(sender, protectedValue: null).Should().BeNull();
        InvokeTryUnprotect(sender, protectedValue: "   ").Should().BeNull();
    }

    [Fact]
    public void TryUnprotect_WhenUnprotectThrows_ShouldReturnNull()
    {
        using var factory = new TestWebAppFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sender = new DbSmtpEmailSender(db, new ThrowingDataProtectionProvider(), NullLogger<DbSmtpEmailSender>.Instance);
        InvokeTryUnprotect(sender, protectedValue: "not-a-protected-value").Should().BeNull();
    }

    private static string? InvokeTryUnprotect(DbSmtpEmailSender sender, string? protectedValue)
    {
        var mi = typeof(DbSmtpEmailSender).GetMethod("TryUnprotect", BindingFlags.Instance | BindingFlags.NonPublic);
        mi.Should().NotBeNull();
        return (string?)mi!.Invoke(sender, new object?[] { protectedValue });
    }

    private sealed class ThrowingDataProtectionProvider : IDataProtectionProvider
    {
        public IDataProtector CreateProtector(string purpose) => new ThrowingProtector();

        private sealed class ThrowingProtector : IDataProtector
        {
            public IDataProtector CreateProtector(string purpose) => this;

            public byte[] Protect(byte[] plaintext) => throw new NotSupportedException();

            public byte[] Unprotect(byte[] protectedData) => throw new CryptographicException("boom");
        }
    }
}
