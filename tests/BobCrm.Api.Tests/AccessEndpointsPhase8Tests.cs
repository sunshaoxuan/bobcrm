using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Xunit;

namespace BobCrm.Api.Tests;

public class AccessEndpointsPhase8Tests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public AccessEndpointsPhase8Tests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(TestWebAppFactory factory)
    {
        var client = factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static async Task<string> GetAdminUserIdAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var admin = await um.FindByNameAsync("admin");
        admin.Should().NotBeNull();
        return admin!.Id;
    }

    private static async Task EnsureAdminHasFunctionAsync(IServiceProvider services, string userId, string functionCode)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var fn = await db.FunctionNodes.FirstOrDefaultAsync(f => f.Code == functionCode);
        if (fn == null)
        {
            fn = new FunctionNode
            {
                Code = functionCode,
                Name = functionCode,
                DisplayName = new Dictionary<string, string?> { ["en"] = functionCode },
                IsMenu = false,
                SortOrder = 1
            };
            db.FunctionNodes.Add(fn);
        }

        var role = await db.RoleProfiles.FirstOrDefaultAsync(r => r.Code == "P8_ADMIN");
        if (role == null)
        {
            role = new RoleProfile
            {
                Code = "P8_ADMIN",
                Name = "Phase8 Admin",
                IsSystem = false,
                IsEnabled = true
            };
            db.RoleProfiles.Add(role);
        }

        var hasPermission = await db.RoleFunctionPermissions.AnyAsync(p => p.RoleId == role.Id && p.FunctionId == fn.Id);
        if (!hasPermission)
        {
            db.RoleFunctionPermissions.Add(new RoleFunctionPermission
            {
                RoleId = role.Id,
                FunctionId = fn.Id
            });
        }

        var hasAssignment = await db.RoleAssignments.AnyAsync(a => a.UserId == userId && a.RoleId == role.Id);
        if (!hasAssignment)
        {
            db.RoleAssignments.Add(new RoleAssignment
            {
                UserId = userId,
                RoleId = role.Id
            });
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Functions_WithoutAuth_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/access/functions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Roles_GetById_WhenMissing_ShouldReturnNotFoundError()
    {
        var client = await CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync($"/api/access/roles/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("ROLE_NOT_FOUND");
    }

    [Fact]
    public async Task Assignments_UserQuery_WithAuth_ShouldReturn200()
    {
        var client = await CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/access/assignments/user/admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
    }

    [Fact]
    public async Task FunctionsMe_WithoutAuth_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/access/functions/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Roles_CRUD_ShouldWork()
    {
        var client = await CreateAuthenticatedClientAsync(_factory);
        var adminUserId = await GetAdminUserIdAsync(_factory.Services);

        var code = $"P8.TEST.{Guid.NewGuid():N}".ToUpperInvariant();

        var create = await client.PostAsJsonAsync("/api/access/roles", new
        {
            code,
            name = "Phase8 Role",
            description = "p8"
        });
        create.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await create.ReadDataAsJsonAsync();
        var roleId = Guid.Parse(created.GetProperty("id").GetString()!);

        var get = await client.GetAsync($"/api/access/roles/{roleId}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var update = await client.PutAsJsonAsync($"/api/access/roles/{roleId}", new
        {
            name = "Phase8 Role Updated",
            description = "p8u",
            isEnabled = true
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var assign = await client.PostAsJsonAsync("/api/access/assignments", new
        {
            userId = adminUserId,
            roleId,
            organizationId = (string?)null
        });
        assign.StatusCode.Should().Be(HttpStatusCode.OK);
        var assignment = await assign.ReadDataAsJsonAsync();
        var assignmentId = Guid.Parse(assignment.GetProperty("id").GetString()!);

        var listAssignments = await client.GetAsync($"/api/access/assignments/user/{adminUserId}");
        listAssignments.StatusCode.Should().Be(HttpStatusCode.OK);

        var delAssignment = await client.DeleteAsync($"/api/access/assignments/{assignmentId}");
        delAssignment.StatusCode.Should().Be(HttpStatusCode.OK);

        var delRole = await client.DeleteAsync($"/api/access/roles/{roleId}");
        delRole.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Functions_WithAuthAndPermission_ShouldReturnOk()
    {
        var adminUserId = await GetAdminUserIdAsync(_factory.Services);
        await EnsureAdminHasFunctionAsync(_factory.Services, adminUserId, "BAS.AUTH.ROLE.PERM");

        var client = await CreateAuthenticatedClientAsync(_factory);
        var response = await client.GetAsync("/api/access/functions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FunctionsManage_WithAuthAndPermission_ShouldReturnOk()
    {
        var adminUserId = await GetAdminUserIdAsync(_factory.Services);
        await EnsureAdminHasFunctionAsync(_factory.Services, adminUserId, "SYS.SET.MENU");

        var client = await CreateAuthenticatedClientAsync(_factory);
        var response = await client.GetAsync("/api/access/functions/manage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FunctionsExport_WithAuthAndPermission_ShouldReturnOk()
    {
        var adminUserId = await GetAdminUserIdAsync(_factory.Services);
        await EnsureAdminHasFunctionAsync(_factory.Services, adminUserId, "SYS.SET.MENU");

        var client = await CreateAuthenticatedClientAsync(_factory);
        var response = await client.GetAsync("/api/access/functions/export");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task FunctionsImport_WithAuthAndPermission_ShouldReturnOk()
    {
        var adminUserId = await GetAdminUserIdAsync(_factory.Services);
        await EnsureAdminHasFunctionAsync(_factory.Services, adminUserId, "SYS.SET.MENU");

        var client = await CreateAuthenticatedClientAsync(_factory);
        var response = await client.PostAsJsonAsync("/api/access/functions/import", new
        {
            mergeStrategy = "skip",
            functions = new[]
            {
                new
                {
                    code = $"P8.IMPORT.{Guid.NewGuid():N}".ToUpperInvariant(),
                    name = "Imported",
                    isMenu = false,
                    sortOrder = 1
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("imported").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }
}
