using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BobCrm.Api.Base;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BobCrm.Api.Tests;

public class LayoutEndpointsPhase9Tests
{
    [Fact]
    public async Task FieldTags_WhenTagsInvalidJson_ShouldReturnOk()
    {
        using var factory = new TestWebAppFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.FieldDefinitions.Add(new FieldDefinition
            {
                Key = "BadTags",
                DisplayName = "BadTags",
                DataType = "String",
                Tags = "not-json"
            });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);

        var response = await client.GetAsync("/api/fields/tags");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SaveLayoutCustomerAlias_DefaultScope_NonAdmin_ShouldForbid()
    {
        using var factory = CreateTestAuthFactory(userName: "user", role: "user");
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/layout/customer?scope=default", new { a = 1 });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SaveLayoutCustomerAlias_DefaultScope_Admin_ShouldSucceed()
    {
        using var factory = CreateTestAuthFactory(userName: "admin", role: "admin");
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/layout/customer?scope=default", new { a = 1 });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.UserLayouts.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == "__default__");
        saved.Should().NotBeNull();
        saved!.LayoutJson.Should().NotBeNullOrWhiteSpace();
    }

    private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> CreateTestAuthFactory(string userName, string role)
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
            });
        });
    }
}

internal sealed record TestAuthState(string UserName, string Role);

internal sealed class LayoutTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly TestAuthState _state;

    public LayoutTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        TestAuthState state)
        : base(options, logger, encoder)
    {
        _state = state;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new System.Security.Claims.ClaimsIdentity("Test");
        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user"));
        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, _state.UserName));
        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, _state.Role));

        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Test")));
    }
}
