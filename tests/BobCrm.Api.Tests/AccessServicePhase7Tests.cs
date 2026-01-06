using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BobCrm.Api.Tests;

public class AccessServicePhase7Tests
{
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static UserManager<IdentityUser> CreateUserManager(AppDbContext context)
    {
        var store = new UserStore<IdentityUser>(context);
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<IdentityUser>();
        var userValidators = new List<IUserValidator<IdentityUser>> { new UserValidator<IdentityUser>() };
        var passwordValidators = new List<IPasswordValidator<IdentityUser>> { new PasswordValidator<IdentityUser>() };
        var normalizer = new UpperInvariantLookupNormalizer();
        var describer = new IdentityErrorDescriber();

        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var logger = services.GetRequiredService<ILogger<UserManager<IdentityUser>>>();

        return new UserManager<IdentityUser>(store, options, passwordHasher, userValidators, passwordValidators, normalizer, describer, services, logger);
    }

    private static RoleManager<IdentityRole> CreateRoleManager(AppDbContext context)
    {
        var store = new RoleStore<IdentityRole>(context);
        var roleValidators = new List<IRoleValidator<IdentityRole>> { new RoleValidator<IdentityRole>() };
        var normalizer = new UpperInvariantLookupNormalizer();
        var describer = new IdentityErrorDescriber();

        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var logger = services.GetRequiredService<ILogger<RoleManager<IdentityRole>>>();

        return new RoleManager<IdentityRole>(store, roleValidators, normalizer, describer, logger);
    }

    private static AccessService CreateAccessService(AppDbContext db, TimeProvider timeProvider)
    {
        var multilingual = new MultilingualFieldService(db, NullLogger<MultilingualFieldService>.Instance);
        var functionService = new FunctionService(db, multilingual);
        var roleService = new RoleService(db);
        return new AccessService(db, CreateUserManager(db), CreateRoleManager(db), multilingual, functionService, roleService, timeProvider);
    }

    [Fact]
    public async Task HasFunctionAccessAsync_WhenFunctionCodeEmpty_ShouldReturnTrue()
    {
        await using var db = CreateContext();
        var access = CreateAccessService(db, TimeProvider.System);

        (await access.HasFunctionAccessAsync("u1", null)).Should().BeTrue();
        (await access.HasFunctionAccessAsync("u1", "   ")).Should().BeTrue();
    }

    [Fact]
    public async Task EnsureFunctionAccessAsync_WhenNoPermission_ShouldThrow()
    {
        await using var db = CreateContext();
        var access = CreateAccessService(db, TimeProvider.System);

        var act = async () => await access.EnsureFunctionAccessAsync("u1", "CRM.ENTITY.CUSTOMER");
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HasFunctionAccessAsync_ShouldRespectValidityWindow()
    {
        await using var db = CreateContext();
        var now = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var access = CreateAccessService(db, new FixedTimeProvider(now));

        var fn = new FunctionNode { Code = "CRM.ENTITY.CUSTOMER", Name = "Customer", SortOrder = 1 };
        var role = new RoleProfile { Code = "R1", Name = "Role1", IsEnabled = true };
        role.Functions.Add(new RoleFunctionPermission { RoleId = role.Id, FunctionId = fn.Id });
        db.FunctionNodes.Add(fn);
        db.RoleProfiles.Add(role);
        db.RoleAssignments.Add(new RoleAssignment
        {
            UserId = "u1",
            RoleId = role.Id,
            ValidFrom = now.UtcDateTime.AddDays(-1),
            ValidTo = now.UtcDateTime.AddDays(1)
        });
        await db.SaveChangesAsync();

        (await access.HasFunctionAccessAsync("u1", "CRM.ENTITY.CUSTOMER")).Should().BeTrue();

        db.RoleAssignments.RemoveRange(await db.RoleAssignments.ToListAsync());
        db.RoleAssignments.Add(new RoleAssignment
        {
            UserId = "u1",
            RoleId = role.Id,
            ValidFrom = now.UtcDateTime.AddDays(1),
            ValidTo = now.UtcDateTime.AddDays(2)
        });
        await db.SaveChangesAsync();

        (await access.HasFunctionAccessAsync("u1", "CRM.ENTITY.CUSTOMER")).Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateDataScopeAsync_WhenAllScopeExists_ShouldReturnAllAccess()
    {
        await using var db = CreateContext();
        var now = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var access = CreateAccessService(db, new FixedTimeProvider(now));

        var role = new RoleProfile { Code = "R1", Name = "Role1", IsEnabled = true };
        db.RoleProfiles.Add(role);
        db.RoleDataScopes.Add(new RoleDataScope
        {
            RoleId = role.Id,
            EntityName = "*",
            ScopeType = RoleDataScopeTypes.All
        });
        db.RoleAssignments.Add(new RoleAssignment
        {
            UserId = "u1",
            RoleId = role.Id,
            OrganizationId = Guid.NewGuid()
        });
        await db.SaveChangesAsync();

        var result = await access.EvaluateDataScopeAsync("u1", "Customer");
        result.HasFullAccess.Should().BeTrue();
        result.Scopes.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateDataScopeAsync_WhenScoped_ShouldReturnBindings()
    {
        await using var db = CreateContext();
        var now = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var access = CreateAccessService(db, new FixedTimeProvider(now));

        var role = new RoleProfile { Code = "R1", Name = "Role1", IsEnabled = true };
        var orgId = Guid.NewGuid();
        db.RoleProfiles.Add(role);
        db.RoleDataScopes.Add(new RoleDataScope
        {
            RoleId = role.Id,
            EntityName = "Customer",
            ScopeType = RoleDataScopeTypes.Organization
        });
        db.RoleAssignments.Add(new RoleAssignment
        {
            UserId = "u1",
            RoleId = role.Id,
            OrganizationId = orgId
        });
        await db.SaveChangesAsync();

        var result = await access.EvaluateDataScopeAsync("u1", "customer");
        result.HasFullAccess.Should().BeFalse();
        result.Scopes.Should().ContainSingle(b => b.OrganizationId == orgId && b.Scope.ScopeType == RoleDataScopeTypes.Organization);
    }
}
