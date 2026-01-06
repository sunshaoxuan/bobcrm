using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BobCrm.Api.Base;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BobCrm.Api.Tests;

public class LayoutEndpointsFinalSprintTests
{
    [Fact]
    public async Task SaveLayout_DefaultScope_NonAdmin_ShouldForbid()
    {
        using var factory = CreateTestAuthFactory(userName: "user", role: "user");
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/layout?scope=default", new { v = 1 });
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SaveLayout_DefaultScope_Admin_ShouldPersistToDefault()
    {
        using var factory = CreateTestAuthFactory(userName: "admin", role: "admin");
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/layout?scope=default", new { v = 2, src = "final" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.UserLayouts.AsNoTracking()
            .Where(UserLayoutScope.ForUser("__default__", 0))
            .FirstOrDefaultAsync();

        saved.Should().NotBeNull();
        saved!.LayoutJson.Should().Contain("\"final\"");
    }

    [Fact]
    public async Task DeleteLayout_DefaultScope_NonAdmin_ShouldForbid()
    {
        using var factory = CreateTestAuthFactory(userName: "user", role: "user");
        var client = factory.CreateClient();

        var resp = await client.DeleteAsync("/api/layout?scope=default");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CustomerAlias_GetAndDelete_ShouldReturnOk()
    {
        using var factory = CreateTestAuthFactory(userName: "admin", role: "admin");
        var client = factory.CreateClient();

        var get = await client.GetAsync("/api/layout/customer?scope=effective");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        (await get.ReadDataAsJsonAsync()).ValueKind.Should().Be(JsonValueKind.Object);

        var del = await client.DeleteAsync("/api/layout/customer");
        del.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EntityTypeLayout_CRUD_ShouldSucceed()
    {
        using var factory = CreateTestAuthFactory(userName: "admin", role: "admin");
        var client = factory.CreateClient();

        var entityType = $"customer-{Guid.NewGuid():N}";
        var save = await client.PostAsJsonAsync($"/api/layout/entity/{entityType}", new { a = 1, t = entityType });
        save.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await client.GetAsync($"/api/layout/entity/{entityType}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var del = await client.DeleteAsync($"/api/layout/entity/{entityType}");
        del.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static WebApplicationFactory<Program> CreateTestAuthFactory(string userName, string role)
    {
        return new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(new TestAuthState(userName, role));

                services.AddAuthentication("Test")
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, LayoutTestAuthHandler>("Test", _ => { });

                services.PostConfigureAll<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultScheme = "Test";
                });
            });
        });
    }
}
