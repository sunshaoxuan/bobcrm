using System.Net;
using System.Net.Http.Json;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BobCrm.Api.Tests;

public class SettingsEndpointsFinalSprintTests
{
    private static WebApplicationFactory<Program> CreateTestAuthFactory(string userName, string role, Action<IServiceCollection>? configure = null)
    {
        return new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(new TestAuthState(userName, role));

                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, LayoutTestAuthHandler>("Test", _ => { });

                services.PostConfigureAll<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultScheme = "Test";
                });

                configure?.Invoke(services);
            });
        });
    }

    [Fact]
    public async Task SystemSettings_WithoutAuth_ShouldReturn401()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/api/settings/system/");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SystemSettings_WithAdmin_ShouldReturnOk()
    {
        using var factory = CreateTestAuthFactory(userName: "admin", role: "admin");
        var client = factory.CreateClient();

        (await client.GetAsync("/api/settings/system/")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserSettings_GetAndUpdate_ShouldReturnOk()
    {
        using var factory = CreateTestAuthFactory(userName: "admin", role: "admin");
        var client = factory.CreateClient();

        (await client.GetAsync("/api/settings/user/")).StatusCode.Should().Be(HttpStatusCode.OK);

        var resp = await client.PutAsJsonAsync("/api/settings/user/",
            new UpdateUserSettingsRequest("dark", "#fff", "zh", "/home", "top"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SmtpTest_RecipientMissing_ShouldReturn400()
    {
        using var factory = CreateTestAuthFactory(userName: "admin", role: "admin");
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/settings/system/smtp/test", new SendTestEmailRequest(""));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SmtpTest_WhenNotConfigured_ShouldReturn400()
    {
        using var factory = CreateTestAuthFactory(userName: "admin", role: "admin");
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/settings/system/smtp/test", new SendTestEmailRequest("a@b.com"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SmtpTest_WhenEmailSenderThrows_ShouldReturnDomainException()
    {
        var email = new Mock<IEmailSender>(MockBehavior.Strict);
        email.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        using var factory = CreateTestAuthFactory(userName: "admin", role: "admin", services =>
        {
            services.RemoveAll(typeof(IEmailSender));
            services.AddSingleton(email.Object);
        });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var system = await db.SystemSettings.FirstAsync();
            system.SmtpHost = "localhost";
            system.SmtpFromAddress = "noreply@local";
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/settings/system/smtp/test", new SendTestEmailRequest("a@b.com"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("SMTP_TEST_FAILED");
    }
}
