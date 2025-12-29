using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BobCrm.Api.Tests;

public class SystemEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public SystemEndpointsTests(TestWebAppFactory factory)
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

    [Fact]
    public async Task SystemInfo_WithAuth_ShouldReturnSuccess()
    {
        await EnsureAdminHasSystemPermissionsAsync(_factory.Services);
        var client = await GetAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/system/info");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task AuditLogs_InvalidPagination_ShouldReturn400()
    {
        await EnsureAdminHasSystemPermissionsAsync(_factory.Services);
        var client = await GetAuthenticatedClientAsync();

        var resp = await client.GetAsync("/api/system/audit-logs?page=0&pageSize=10");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var root = await resp.ReadAsJsonAsync();
        Assert.Equal("INVALID_PAGINATION", GetErrorCode(root));
    }

    [Fact]
    public async Task AuditLogModules_ShouldReturnDistinctSorted()
    {
        await EnsureAdminHasSystemPermissionsAsync(_factory.Services);
        await SeedAuditLogAsync(_factory.Services, "B");
        await SeedAuditLogAsync(_factory.Services, "A");
        await SeedAuditLogAsync(_factory.Services, "B");

        var client = await GetAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/system/audit-logs/modules?limit=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var modules = await resp.Content.ReadFromJsonAsync<string[]>();
        Assert.NotNull(modules);
        Assert.Contains("A", modules);
        Assert.Contains("B", modules);

        var sorted = modules.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(sorted, modules);
    }

    [Fact]
    public async Task Jobs_InvalidPagination_ShouldReturn400()
    {
        await EnsureAdminHasSystemPermissionsAsync(_factory.Services);
        var client = await GetAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/system/jobs?page=1&pageSize=999");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var root = await resp.ReadAsJsonAsync();
        Assert.Equal("INVALID_PAGINATION", GetErrorCode(root));
    }

    [Fact]
    public async Task Jobs_WithAuth_ShouldReturn200()
    {
        await EnsureAdminHasSystemPermissionsAsync(_factory.Services);
        var client = await GetAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/system/jobs?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task SystemEndpoints_WithoutAuth_ShouldReturn401()
    {
        var resp = await _client.GetAsync("/api/system/info");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    private static async Task SeedAuditLogAsync(IServiceProvider services, string module)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AuditLogs.Add(new AuditLog
        {
            Module = module,
            OperationType = "C",
            OccurredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static async Task EnsureAdminHasSystemPermissionsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var access = scope.ServiceProvider.GetRequiredService<AccessService>();
        await access.SeedSystemAdministratorAsync();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var required = new[] { "SYS.ADMIN", "SYS.AUDIT", "SYS.JOBS", "SYS.I18N" };
        var existing = await db.FunctionNodes
            .Where(x => required.Contains(x.Code))
            .ToListAsync();

        foreach (var code in required)
        {
            if (existing.All(x => x.Code != code))
            {
                db.FunctionNodes.Add(new FunctionNode
                {
                    Code = code,
                    Name = code,
                    DisplayName = new Dictionary<string, string?> { ["zh"] = code }
                });
            }
        }
        await db.SaveChangesAsync();

        var adminRole = await db.RoleProfiles
            .Include(r => r.Functions)
            .FirstAsync(r => r.IsSystem);

        var functionMap = await db.FunctionNodes
            .Where(x => required.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, x => x.Id);

        var existingIds = adminRole.Functions.Select(x => x.FunctionId).ToHashSet();
        foreach (var id in functionMap.Values)
        {
            if (!existingIds.Contains(id))
            {
                adminRole.Functions.Add(new RoleFunctionPermission { RoleId = adminRole.Id, FunctionId = id });
            }
        }
        await db.SaveChangesAsync();
    }

    private static string? GetErrorCode(JsonElement root)
    {
        if (root.TryGetProperty("code", out var code))
        {
            return code.GetString();
        }

        if (root.TryGetProperty("Code", out var codePascal))
        {
            return codePascal.GetString();
        }

        return null;
    }
}
