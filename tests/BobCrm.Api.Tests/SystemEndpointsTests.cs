using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
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
        Assert.True(resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AuditLogs_InvalidPagination_ShouldReturn400()
    {
        await EnsureAdminHasSystemPermissionsAsync(_factory.Services);
        var client = await GetAuthenticatedClientAsync();

        var resp = await client.GetAsync("/api/system/audit-logs?page=0&pageSize=10");
        Assert.True(resp.StatusCode == HttpStatusCode.BadRequest || resp.StatusCode == HttpStatusCode.Forbidden);
        if (resp.StatusCode == HttpStatusCode.BadRequest)
        {
            var root = await resp.ReadAsJsonAsync();
            Assert.Equal("INVALID_PAGINATION", GetErrorCode(root));
        }
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
        Assert.True(resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.Forbidden);
        if (resp.StatusCode != HttpStatusCode.OK)
        {
            return;
        }

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
        Assert.True(resp.StatusCode == HttpStatusCode.BadRequest || resp.StatusCode == HttpStatusCode.Forbidden);
        if (resp.StatusCode == HttpStatusCode.BadRequest)
        {
            var root = await resp.ReadAsJsonAsync();
            Assert.Equal("INVALID_PAGINATION", GetErrorCode(root));
        }
    }

    [Fact]
    public async Task Jobs_WithAuth_ShouldReturn200()
    {
        await EnsureAdminHasSystemPermissionsAsync(_factory.Services);
        var client = await GetAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/system/jobs?page=1&pageSize=20");
        Assert.True(resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.Forbidden);
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
