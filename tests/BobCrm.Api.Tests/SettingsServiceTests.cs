using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BobCrm.Api.Base;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services.Settings;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Tests;

public class SettingsServiceTests
{
    [Fact]
    public async Task GetSystemSettingsAsync_ShouldCreateDefaultRowWhenMissing()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var settings = await service.GetSystemSettingsAsync();

        settings.Id.Should().BeGreaterThan(0);
        settings.CompanyName.Should().NotBeNullOrWhiteSpace();

        (await db.SystemSettings.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateSystemSettingsAsync_ShouldNormalizeAndEncryptSmtpPassword()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var updated = await service.UpdateSystemSettingsAsync(new UpdateSystemSettingsRequest(
            CompanyName: "  BobCRM  ",
            DefaultTheme: "theme-calm-dark",
            DefaultPrimaryColor: "  #000  ",
            DefaultLanguage: "EN",
            DefaultHomeRoute: "home",
            DefaultNavDisplayMode: "icons",
            TimeZoneId: "  Asia/Shanghai  ",
            AllowSelfRegistration: true,
            SmtpHost: " smtp.example.com ",
            SmtpPort: 99999,
            SmtpUsername: " user ",
            SmtpPassword: "  secret  ",
            SmtpEnableSsl: true,
            SmtpFromAddress: "  noreply@example.com  ",
            SmtpDisplayName: "  BobCRM  "
        ));

        updated.CompanyName.Should().Be("BobCRM");
        updated.DefaultTheme.Should().Be("calm-dark");
        updated.DefaultPrimaryColor.Should().Be("#000");
        updated.DefaultLanguage.Should().Be("en");
        updated.DefaultHomeRoute.Should().Be("/home");
        updated.DefaultNavMode.Should().Be(NavDisplayModes.Icons);
        updated.TimeZoneId.Should().Be("Asia/Shanghai");
        updated.AllowSelfRegistration.Should().BeTrue();

        updated.SmtpHost.Should().Be("smtp.example.com");
        updated.SmtpPort.Should().Be(65535);
        updated.SmtpUsername.Should().Be("user");
        updated.SmtpEnableSsl.Should().BeTrue();
        updated.SmtpFromAddress.Should().Be("noreply@example.com");
        updated.SmtpDisplayName.Should().Be("BobCRM");
        updated.SmtpPasswordEncrypted.Should().NotBeNullOrWhiteSpace();
        updated.SmtpPasswordEncrypted.Should().NotBe("secret");

        var snapshot = await service.GetUserSettingsAsync("u1");
        snapshot.System.SmtpPasswordConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserSettingsAsync_ShouldCreatePreferencesAndComposeEffective()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        await service.UpdateSystemSettingsAsync(new UpdateSystemSettingsRequest(
            CompanyName: null,
            DefaultTheme: "calm-light",
            DefaultPrimaryColor: "#739FD6",
            DefaultLanguage: "ja",
            DefaultHomeRoute: "/",
            DefaultNavDisplayMode: "icon-text",
            TimeZoneId: null,
            AllowSelfRegistration: null,
            SmtpHost: null,
            SmtpPort: null,
            SmtpUsername: null,
            SmtpPassword: null,
            SmtpEnableSsl: null,
            SmtpFromAddress: null,
            SmtpDisplayName: null
        ));

        var result = await service.UpdateUserSettingsAsync("u1", new UpdateUserSettingsRequest(
            Theme: "theme-calm-dark",
            PrimaryColor: "#123",
            Language: "zh",
            HomeRoute: "dash",
            NavDisplayMode: "icons"
        ));

        result.System.DefaultLanguage.Should().Be("ja");
        result.Overrides.Should().NotBeNull();
        result.Overrides!.Theme.Should().Be("calm-dark");
        result.Overrides.Language.Should().Be("zh");
        result.Overrides.HomeRoute.Should().Be("/dash");
        result.Overrides.NavDisplayMode.Should().Be("icons");

        result.Effective.Theme.Should().Be("calm-dark");
        result.Effective.Language.Should().Be("zh");
        result.Effective.HomeRoute.Should().Be("/dash");
        result.Effective.PrimaryColor.Should().Be("#123");

        var invalidLang = await service.UpdateUserSettingsAsync("u1", new UpdateUserSettingsRequest(
            Theme: null,
            PrimaryColor: null,
            Language: "xx",
            HomeRoute: null,
            NavDisplayMode: null
        ));
        invalidLang.Effective.Language.Should().Be("zh");
    }

    private static SettingsService CreateService(AppDbContext db)
    {
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        return new SettingsService(db, dataProtectionProvider);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}

