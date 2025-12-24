using System.Net.Http.Json;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Base.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BobCrm.Api.Tests;

public class UserManagementTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public UserManagementTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Users_List_ShouldIncludeAdmin()
    {
        var client = _factory.CreateClient();
        var (token, _) = await client.LoginAsAdminAsync();
        client.UseBearer(token);

        var envelope = await client.GetFromJsonAsync<SuccessEnvelope<List<UserSummaryResponse>>>("/api/users");
        Assert.NotNull(envelope);
        Assert.Contains(envelope!.Data, u => u.UserName == "admin");
    }

    [Fact]
    public async Task CreateUser_ShouldPersistAndAssignRole()
    {
        var client = _factory.CreateClient();
        var (token, _) = await client.LoginAsAdminAsync();
        client.UseBearer(token);

        Guid roleId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var role = new RoleProfile
            {
                Code = $"USR.{Guid.NewGuid():N}",
                Name = "User Manager"
            };
            db.RoleProfiles.Add(role);
            await db.SaveChangesAsync();
            roleId = role.Id;
        }

        var username = $"user_{Guid.NewGuid():N}";
        var payload = new
        {
            userName = username,
            email = $"{username}@local",
            password = "User@12345",
            emailConfirmed = true,
            roles = new[]
            {
                new { roleId, organizationId = (Guid?)null }
            }
        };

        var resp = await client.PostAsJsonAsync("/api/users", payload);
        resp.EnsureSuccessStatusCode();
        var detailEnvelope = await resp.Content.ReadFromJsonAsync<SuccessEnvelope<UserDetailResponse>>();
        Assert.NotNull(detailEnvelope);
        Assert.Contains(detailEnvelope!.Data.Roles, r => r.RoleId == roleId);

        using var scope2 = _factory.Services.CreateScope();
        var dbContext = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var assignments = await dbContext.RoleAssignments.Where(a => a.UserId == detailEnvelope.Data.Id).ToListAsync();
        Assert.Single(assignments);
        Assert.Equal(roleId, assignments[0].RoleId);
    }

    [Fact]
    public async Task UpdateUserRoles_ShouldReplaceAssignments()
    {
        var client = _factory.CreateClient();
        var (token, _) = await client.LoginAsAdminAsync();
        client.UseBearer(token);

        var username = $"user_{Guid.NewGuid():N}";
        var createResp = await client.PostAsJsonAsync("/api/users", new
        {
            userName = username,
            email = $"{username}@local",
            password = "User@12345",
            emailConfirmed = true
        });
        createResp.EnsureSuccessStatusCode();
        var detailEnvelope = await createResp.Content.ReadFromJsonAsync<SuccessEnvelope<UserDetailResponse>>();
        Assert.NotNull(detailEnvelope);

        Guid roleA;
        Guid roleB;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ra = new RoleProfile { Code = $"DEMO.{Guid.NewGuid():N}", Name = "Demo Role A" };
            var rb = new RoleProfile { Code = $"DEMO.{Guid.NewGuid():N}", Name = "Demo Role B" };
            db.RoleProfiles.AddRange(ra, rb);
            await db.SaveChangesAsync();
            roleA = ra.Id;
            roleB = rb.Id;
        }

        var updateResp = await client.PutAsJsonAsync($"/api/users/{detailEnvelope!.Data.Id}/roles", new
        {
            roles = new[]
            {
                new { roleId = roleA, organizationId = (Guid?)null },
                new { roleId = roleB, organizationId = (Guid?)null }
            }
        });
        updateResp.EnsureSuccessStatusCode();

        using var scopeCheck = _factory.Services.CreateScope();
        var dbCheck = scopeCheck.ServiceProvider.GetRequiredService<AppDbContext>();
        var assignments = await dbCheck.RoleAssignments.Where(a => a.UserId == detailEnvelope.Data.Id).ToListAsync();
        Assert.Equal(2, assignments.Count);
        Assert.Contains(assignments, a => a.RoleId == roleA);
        Assert.Contains(assignments, a => a.RoleId == roleB);
    }

    private record SuccessEnvelope<T>(bool Success, T Data);

    private record UserSummaryResponse(string Id, string UserName);
    private record UserDetailResponse
    {
        public string Id { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public List<UserRoleResponse> Roles { get; init; } = new();
    }

    private record UserRoleResponse
    {
        public Guid RoleId { get; init; }
        public string RoleName { get; init; } = string.Empty;
    }
}
