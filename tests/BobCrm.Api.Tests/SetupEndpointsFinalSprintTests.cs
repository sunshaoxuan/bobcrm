using System.Net;
using System.Net.Http.Json;
using BobCrm.Api.Contracts.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BobCrm.Api.Tests;

public class SetupEndpointsFinalSprintTests
{
    [Fact]
    public async Task GetAdminInfo_ShouldReturnExistsTrue()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/api/setup/admin");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await resp.ReadDataAsJsonAsync();
        data.GetProperty("exists").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SetupAdmin_WithDefaultAdmin_ShouldUpdateAndReturnOk()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/setup/admin",
            new AdminSetupDto("admin2", "admin2@local", "NewAdmin@12345"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAdminInfo_WhenAdminRoleMissing_ShouldReturnExistsFalse()
    {
        using var factory = new TestWebAppFactory();

        using (var scope = factory.Services.CreateScope())
        {
            var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var adminRole = await rm.FindByNameAsync("admin");
            adminRole.Should().NotBeNull();
            await rm.DeleteAsync(adminRole!);
        }

        var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/setup/admin");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resp.ReadDataAsJsonAsync()).GetProperty("exists").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task GetAdminInfo_WhenNoAdminUser_ShouldReturnExistsFalse()
    {
        using var factory = new TestWebAppFactory();

        using (var scope = factory.Services.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var admin = await um.FindByNameAsync("admin");
            admin.Should().NotBeNull();
            await um.RemoveFromRoleAsync(admin!, "admin");
        }

        var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/setup/admin");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resp.ReadDataAsJsonAsync()).GetProperty("exists").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task SetupAdmin_WhenNoAdminUser_ShouldCreate()
    {
        using var factory = new TestWebAppFactory();

        using (var scope = factory.Services.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var existing = await um.FindByNameAsync("admin");
            existing.Should().NotBeNull();
            await um.DeleteAsync(existing!);
        }

        var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/setup/admin",
            new AdminSetupDto("admin3", "admin3@local", "NewAdmin@12345"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
