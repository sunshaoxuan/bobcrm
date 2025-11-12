using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Domain.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Tests;

public class AccessServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldPersistFunctionsAndScopes()
    {
        await using var ctx = CreateContext();
        var function = new FunctionNode { Code = "CRM.CUSTOMER.VIEW", Name = "View Customers" };
        ctx.FunctionNodes.Add(function);
        await ctx.SaveChangesAsync();

        var service = new AccessService(ctx);
        var role = await service.CreateRoleAsync(new CreateRoleRequest
        {
            Code = "CRM.OP",
            Name = "CRM Operator",
            FunctionIds = new List<Guid> { function.Id },
            DataScopes = new List<RoleDataScopeDto>
            {
                new() { EntityName = "Customer", ScopeType = RoleDataScopeTypes.Organization }
            }
        });

        role.Functions.Should().HaveCount(1);
        role.DataScopes.Should().ContainSingle(s => s.EntityName == "Customer" && s.ScopeType == RoleDataScopeTypes.Organization);
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldCreateAssignment()
    {
        await using var ctx = CreateContext();
        var role = new RoleProfile { Code = "SYS.TEST", Name = "Test Role" };
        ctx.RoleProfiles.Add(role);
        await ctx.SaveChangesAsync();

        var service = new AccessService(ctx);
        var assignment = await service.AssignRoleAsync(new AssignRoleRequest
        {
            UserId = "user-1",
            RoleId = role.Id,
            OrganizationId = Guid.NewGuid()
        });

        assignment.UserId.Should().Be("user-1");
        (await ctx.RoleAssignments.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SeedSystemAdministratorAsync_ShouldCreateSystemRole()
    {
        await using var ctx = CreateContext();
        var service = new AccessService(ctx);
        await service.SeedSystemAdministratorAsync();

        var role = await ctx.RoleProfiles.Include(r => r.Functions).Include(r => r.DataScopes).FirstOrDefaultAsync(r => r.IsSystem);
        role.Should().NotBeNull();
        role!.Functions.Should().NotBeEmpty();
        role.DataScopes.Should().ContainSingle(s => s.ScopeType == RoleDataScopeTypes.All);
    }
}
