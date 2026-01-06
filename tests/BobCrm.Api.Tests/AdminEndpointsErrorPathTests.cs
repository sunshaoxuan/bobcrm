using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BobCrm.Api.Tests;

public class AdminEndpointsErrorPathTests
{
    private static async Task<HttpClient> CreateAdminClientAsync(TestWebAppFactory factory)
    {
        var client = factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    [Fact]
    public async Task ResetPassword_WhenAdminRoleMissing_ShouldReturn404()
    {
        using var factory = new TestWebAppFactory();
        var client = await CreateAdminClientAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var role = await rm.FindByNameAsync("admin");
            if (role != null)
            {
                (await rm.DeleteAsync(role)).Succeeded.Should().BeTrue();
            }
        }

        var response = await client.PostAsJsonAsync("/api/admin/reset-password", new { userName = "admin", newPassword = "NewPassword123!" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("ADMIN_ROLE_NOT_FOUND");
    }

    [Fact]
    public async Task ResetPassword_WhenNoAdminUserInRole_ShouldReturn404()
    {
        using var factory = new TestWebAppFactory();
        var client = await CreateAdminClientAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var admin = await um.FindByNameAsync("admin");
            admin.Should().NotBeNull();
            if (admin != null)
            {
                (await um.RemoveFromRoleAsync(admin, "admin")).Succeeded.Should().BeTrue();
            }
        }

        var response = await client.PostAsJsonAsync("/api/admin/reset-password", new { userName = "admin", newPassword = "NewPassword123!" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("ADMIN_USER_NOT_FOUND");
    }

    [Fact]
    public async Task RegenerateTemplates_ForMissingEntity_ShouldReturn404()
    {
        using var factory = new TestWebAppFactory();
        var client = await CreateAdminClientAsync(factory);

        var response = await client.PostAsync("/api/admin/templates/nonexistent_entity_zzz/regenerate", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("ENTITY_NOT_FOUND");
    }

    [Fact]
    public async Task ResetTemplates_ForMissingEntity_ShouldReturn404()
    {
        using var factory = new TestWebAppFactory();
        var client = await CreateAdminClientAsync(factory);

        var response = await client.PostAsync("/api/admin/templates/nonexistent_entity_zzz/reset", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("ENTITY_NOT_FOUND");
    }
}

