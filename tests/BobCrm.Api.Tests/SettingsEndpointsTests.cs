using BobCrm.Api.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// SettingsEndpoints 设置端点测试
/// </summary>
public class SettingsEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public SettingsEndpointsTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    #region System Settings Tests

    [Fact]
    public async Task GetSystemSettings_WithoutAuth_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/settings/system");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSystemSettings_WithAdminAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/settings/system");

        // Assert - Admin should have access (not 401)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region User Settings Tests

    [Fact]
    public async Task GetUserSettings_WithoutAuth_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/settings/user");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserSettings_WithAuth_ShouldReturn200()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/settings/user");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserSettings_ShouldReturnUserSettingsSnapshot()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/settings/user");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateUserSettings_WithAuth_ShouldReturn200()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var request = new
        {
            theme = "dark",
            language = "zh"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/settings/user", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region SMTP Test Endpoint Tests

    [Fact]
    public async Task SendSmtpTestEmail_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        var request = new { to = "test@test.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/settings/system/smtp/test", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    [Fact]
    public async Task SendSmtpTestEmail_WithMissingRecipient_ShouldReturn400()
    {
        using var factory = CreateAdminRoleFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/settings/system/smtp/test", new { to = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("SMTP_TEST_RECIPIENT_REQUIRED");
    }

    [Fact]
    public async Task SendSmtpTestEmail_WhenSmtpNotConfigured_ShouldReturn400()
    {
        using var factory = CreateAdminRoleFactory();
        var client = factory.CreateClient();

        await client.PutAsJsonAsync("/api/settings/system", new
        {
            smtpHost = (string?)null,
            smtpFromAddress = (string?)null
        });

        var response = await client.PostAsJsonAsync("/api/settings/system/smtp/test", new { to = "x@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("SMTP_NOT_CONFIGURED");
    }

    [Fact]
    public async Task SendSmtpTestEmail_WhenEmailSenderThrows_ShouldReturnDomainError()
    {
        var email = new Mock<IEmailSender>();
        email.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        using var factory = CreateAdminRoleFactory(services =>
        {
            services.RemoveAll(typeof(IEmailSender));
            services.AddSingleton(email.Object);
        });

        var client = factory.CreateClient();

        await client.PutAsJsonAsync("/api/settings/system", new
        {
            companyName = "OneCRM",
            smtpHost = "smtp.local",
            smtpFromAddress = "noreply@local"
        });

        var response = await client.PostAsJsonAsync("/api/settings/system/smtp/test", new { to = "x@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("SMTP_TEST_FAILED");
    }

    private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> CreateAdminRoleFactory(Action<IServiceCollection>? configureServices = null)
    {
        return new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

                services.PostConfigureAll<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultScheme = "Test";
                });

                services.PostConfigureAll<JwtBearerOptions>(o =>
                {
                    // Ensure JWT bearer doesn't take precedence in tests
                    o.Events = new JwtBearerEvents();
                });

                configureServices?.Invoke(services);
            });
        });
    }
}

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new System.Security.Claims.ClaimsIdentity("Test");
        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-admin"));
        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "test-admin"));
        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "admin"));

        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
