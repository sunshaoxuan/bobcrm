using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Contracts.Requests.Layout;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BobCrm.Api.Tests;

public class LayoutEndpointsPhase10Tests
{
    [Fact]
    public async Task DeprecatedCustomerLayout_DefaultScope_NonAdmin_ShouldForbidSaveAndDelete()
    {
        using var factory = CreateTestAuthFactory(userName: "user", role: "user");
        var client = factory.CreateClient();

        var save = await client.PostAsJsonAsync("/api/layout/123?scope=default", new { a = 1 });
        save.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var delete = await client.DeleteAsync("/api/layout/123?scope=default");
        delete.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeprecatedCustomerLayout_DefaultScope_Admin_ShouldPersistToDefaultUser()
    {
        using var factory = CreateTestAuthFactory(userName: "admin", role: "admin");
        var client = factory.CreateClient();

        var save = await client.PostAsJsonAsync("/api/layout/123?scope=default", new { a = 1, v = "deprecated" });
        save.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.UserLayouts.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == "__default__");
        saved.Should().NotBeNull();
        saved!.LayoutJson.Should().Contain("\"deprecated\"");
    }

    [Fact]
    public async Task GenerateLayout_TagsMissing_ShouldReturnValidationError()
    {
        using var factory = CreateTestAuthFactory(userName: "admin", role: "admin");
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/layout/1/generate",
            new GenerateLayoutRequest(Array.Empty<string>(), Mode: null, Save: null, Scope: null));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resp.ReadAsJsonAsync()).GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task GenerateLayout_FreeMode_ShouldReturnPositions_AndNotPersistWhenSaveFalse()
    {
        using var factory = CreateTestAuthFactory(userName: "admin", role: "admin");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.FieldDefinitions.Add(new FieldDefinition { Key = "A", DisplayName = "A", DataType = "String", Tags = "[\"x\"]" });
            db.FieldDefinitions.Add(new FieldDefinition { Key = "B", DisplayName = "B", DataType = "String", Tags = "[\"y\"]" });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/layout/2/generate",
            new GenerateLayoutRequest(new[] { "x" }, Mode: "free", Save: false, Scope: null));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await resp.ReadDataAsJsonAsync();
        data.GetProperty("mode").GetString().Should().Be("free");

        var items = data.GetProperty("items");
        items.ValueKind.Should().Be(JsonValueKind.Object);
        items.TryGetProperty("A", out var aItem).Should().BeTrue();
        aItem.GetProperty("x").GetInt32().Should().BeGreaterOrEqualTo(0);
        aItem.GetProperty("y").GetInt32().Should().BeGreaterOrEqualTo(0);

        using var scope2 = factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db2.UserLayouts.Where(UserLayoutScope.ForUser("test-user", 2)).CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GenerateLayout_SaveDefault_NonAdmin_ShouldForbid()
    {
        using var factory = CreateTestAuthFactory(userName: "user", role: "user");
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/layout/3/generate",
            new GenerateLayoutRequest(new[] { "x" }, Mode: null, Save: true, Scope: "default"));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GenerateLayout_SaveDefault_Admin_ShouldPersistToDefaultUserAndCustomerScope()
    {
        using var factory = CreateTestAuthFactory(userName: "admin", role: "admin");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.FieldDefinitions.Add(new FieldDefinition { Key = "A", DisplayName = "A", DataType = "String", Tags = "[\"x\"]" });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/layout/5/generate",
            new GenerateLayoutRequest(new[] { "x" }, Mode: "flow", Save: true, Scope: "default"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope2 = factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db2.UserLayouts.AsNoTracking()
            .Where(UserLayoutScope.ForUser("__default__", 5))
            .FirstOrDefaultAsync();
        saved.Should().NotBeNull();
        saved!.LayoutJson.Should().Contain("\"mode\":\"flow\"");
    }

    [Fact]
    public async Task Fields_GetDefinitions_ShouldReturnOk()
    {
        using var factory = CreateTestAuthFactory(userName: "admin", role: "admin");
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/api/fields");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resp.ReadDataAsJsonAsync()).ValueKind.Should().Be(JsonValueKind.Array);
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
