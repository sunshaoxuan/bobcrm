using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.Requests.Access;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Tests;

public class RoleServiceTests
{
    [Fact]
    public async Task CreateRoleAsync_CreatesRoleWithFunctionsAndScopes()
    {
        await using var db = CreateContext();
        var service = new RoleService(db);

        var organizationId = Guid.NewGuid();
        var fn1 = new FunctionNode { Code = "APP.ROOT", Name = "Root", SortOrder = 0 };
        var fn2 = new FunctionNode { Code = "CRM.CORE", Name = "Core", SortOrder = 1 };
        db.FunctionNodes.AddRange(fn1, fn2);
        await db.SaveChangesAsync();

        var role = await service.CreateRoleAsync(new CreateRoleRequest
        {
            OrganizationId = organizationId,
            Code = "  CRM.ADMIN  ",
            Name = "  管理员  ",
            Description = "desc",
            IsEnabled = true,
            FunctionIds = new List<Guid> { fn1.Id, fn2.Id },
            DataScopes = new List<BobCrm.Api.Contracts.DTOs.Access.RoleDataScopeDto>
            {
                new() { EntityName = "customer", ScopeType = RoleDataScopeTypes.All, FilterExpression = null }
            }
        });

        role.Id.Should().NotBe(Guid.Empty);
        role.Code.Should().Be("CRM.ADMIN");
        role.Name.Should().Be("管理员");
        role.IsSystem.Should().BeFalse();

        var stored = await db.RoleProfiles
            .Include(r => r.Functions)
            .Include(r => r.DataScopes)
            .AsNoTracking()
            .SingleAsync(r => r.Id == role.Id);

        stored.Functions.Should().HaveCount(2);
        stored.DataScopes.Should().ContainSingle(ds => ds.EntityName == "customer");
    }

    [Fact]
    public async Task CreateRoleAsync_ThrowsWhenDuplicateCodeInSameOrg()
    {
        await using var db = CreateContext();
        var service = new RoleService(db);

        var organizationId = Guid.NewGuid();
        db.RoleProfiles.Add(new RoleProfile
        {
            OrganizationId = organizationId,
            Code = "CRM.ADMIN",
            Name = "Admin",
            IsEnabled = true,
            IsSystem = false
        });
        await db.SaveChangesAsync();

        var act = async () => await service.CreateRoleAsync(new CreateRoleRequest
        {
            OrganizationId = organizationId,
            Code = "CRM.ADMIN",
            Name = "Other"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Role code already exists within the organization.");
    }

    [Fact]
    public async Task DeleteRoleAsync_ThrowsWhenRoleHasAssignments()
    {
        await using var db = CreateContext();
        var service = new RoleService(db);

        var role = new RoleProfile { Code = "R1", Name = "Role", IsEnabled = true, IsSystem = false };
        db.RoleProfiles.Add(role);
        await db.SaveChangesAsync();

        db.RoleAssignments.Add(new RoleAssignment { UserId = "u1", RoleId = role.Id, OrganizationId = null });
        await db.SaveChangesAsync();

        var act = async () => await service.DeleteRoleAsync(role.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Role has active assignments.");
    }

    [Fact]
    public async Task UpdateRolePermissionsAsync_ReplacesFunctionsAndScopes()
    {
        await using var db = CreateContext();
        var service = new RoleService(db);

        var fn1 = new FunctionNode { Code = "A", Name = "A", SortOrder = 0 };
        var fn2 = new FunctionNode { Code = "B", Name = "B", SortOrder = 1 };
        db.FunctionNodes.AddRange(fn1, fn2);

        var role = new RoleProfile
        {
            Code = "R1",
            Name = "Role",
            IsEnabled = true,
            IsSystem = false,
            Functions = new List<RoleFunctionPermission>
            {
                new() { FunctionId = fn1.Id }
            },
            DataScopes = new List<RoleDataScope>
            {
                new() { EntityName = "customer", ScopeType = RoleDataScopeTypes.All, FilterExpression = null }
            }
        };
        db.RoleProfiles.Add(role);
        await db.SaveChangesAsync();

        var initialScopes = await db.RoleDataScopes.AsNoTracking().Where(s => s.RoleId == role.Id).ToListAsync();
        initialScopes.Should().ContainSingle(s => s.EntityName == "customer");

        await service.UpdateRolePermissionsAsync(role.Id, new UpdatePermissionsRequest
        {
            FunctionIds = new List<Guid> { fn2.Id },
            FunctionPermissions = new List<FunctionPermissionSelectionDto>
            {
                new() { FunctionId = fn1.Id, TemplateBindingId = 123 }
            },
            DataScopes = new List<DataScopeDto>
            {
                new("order", RoleDataScopeTypes.Organization, "OrgId = 1")
            }
        });

        var stored = await db.RoleProfiles
            .Include(r => r.Functions)
            .Include(r => r.DataScopes)
            .AsNoTracking()
            .SingleAsync(r => r.Id == role.Id);

        stored.Functions.Select(f => f.FunctionId).ToHashSet().Should().BeEquivalentTo(new[] { fn1.Id, fn2.Id });
        stored.Functions.Single(f => f.FunctionId == fn1.Id).TemplateBindingId.Should().Be(123);
        stored.Functions.Single(f => f.FunctionId == fn2.Id).TemplateBindingId.Should().BeNull();

        var scopesByRole = await db.RoleDataScopes
            .AsNoTracking()
            .Where(s => s.RoleId == role.Id)
            .ToListAsync();

        var allScopes = await db.RoleDataScopes.AsNoTracking().ToListAsync();
        allScopes.Should().Contain(s => s.EntityName == "order");

        scopesByRole.Should().HaveCount(1);
        scopesByRole[0].EntityName.Should().Be("order");
        scopesByRole[0].ScopeType.Should().Be(RoleDataScopeTypes.Organization);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
