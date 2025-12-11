using System;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.DTOs.Access;
using BobCrm.Api.Contracts.Requests.Access;
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
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

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

    private static AccessService CreateService(AppDbContext context)
    {
        var multilingual = new MultilingualFieldService(context, NullLogger<MultilingualFieldService>.Instance);
        return new AccessService(context, CreateUserManager(context), CreateRoleManager(context), multilingual);
    }

    private static async Task EnsureLocalizationResourcesAsync(AppDbContext context)
    {
        if (await context.LocalizationResources.AnyAsync())
        {
            return;
        }

        var resources = await I18nResourceLoader.LoadResourcesAsync();
        await context.LocalizationResources.AddRangeAsync(resources);
        await context.SaveChangesAsync();
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

        var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        var logger = services.GetRequiredService<ILogger<UserManager<IdentityUser>>>();

        return new UserManager<IdentityUser>(store, options, passwordHasher, userValidators, passwordValidators, normalizer, describer, services, logger);
    }

    private static RoleManager<IdentityRole> CreateRoleManager(AppDbContext context)
    {
        var store = new RoleStore<IdentityRole>(context);
        var roleValidators = new List<IRoleValidator<IdentityRole>> { new RoleValidator<IdentityRole>() };
        var normalizer = new UpperInvariantLookupNormalizer();
        var describer = new IdentityErrorDescriber();

        var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        var logger = services.GetRequiredService<ILogger<RoleManager<IdentityRole>>>();

        return new RoleManager<IdentityRole>(store, roleValidators, normalizer, describer, logger);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldPersistFunctionsAndScopes()
    {
        await using var ctx = CreateContext();
        var function = new FunctionNode { Code = "CRM.CUSTOMER.VIEW", Name = "View Customers" };
        ctx.FunctionNodes.Add(function);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
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

        var service = CreateService(ctx);
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
        await EnsureLocalizationResourcesAsync(ctx);
        var service = CreateService(ctx);
        await service.SeedSystemAdministratorAsync();

        var role = await ctx.RoleProfiles.Include(r => r.Functions).Include(r => r.DataScopes).FirstOrDefaultAsync(r => r.IsSystem);
        role.Should().NotBeNull();
        var functionCodes = await ctx.FunctionNodes.Select(f => f.Code).ToListAsync();
        functionCodes.Should().Contain("SYS.SET.CONFIG");
        role!.Functions.Should().HaveCount(functionCodes.Count);
        role.DataScopes.Should().ContainSingle(s => s.ScopeType == RoleDataScopeTypes.All);
    }

    [Fact]
    public async Task SeedSystemAdministratorAsync_ShouldAssignExistingAdminUser()
    {
        await using var ctx = CreateContext();
        await EnsureLocalizationResourcesAsync(ctx);
        ctx.Users.Add(new IdentityUser
        {
            UserName = "admin",
            NormalizedUserName = "ADMIN",
            Email = "admin@local",
            EmailConfirmed = true
        });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        await service.SeedSystemAdministratorAsync();

        var assignment = await ctx.RoleAssignments.Include(a => a.Role).FirstOrDefaultAsync();
        assignment.Should().NotBeNull();
        assignment!.Role!.IsSystem.Should().BeTrue();
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldPreventDuplicateAssignment()
    {
        await using var ctx = CreateContext();
        var role = new RoleProfile { Code = "TEST.ROLE", Name = "Test Role" };
        ctx.RoleProfiles.Add(role);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        await service.AssignRoleAsync(new AssignRoleRequest
        {
            UserId = "user-1",
            RoleId = role.Id,
            OrganizationId = null
        });

        var act = async () => await service.AssignRoleAsync(new AssignRoleRequest
        {
            UserId = "user-1",
            RoleId = role.Id,
            OrganizationId = null
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Assignment already exists.");
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldAllowSameRoleDifferentOrganization()
    {
        await using var ctx = CreateContext();
        var role = new RoleProfile { Code = "TEST.ROLE", Name = "Test Role" };
        ctx.RoleProfiles.Add(role);
        await ctx.SaveChangesAsync();

        var orgId1 = Guid.NewGuid();
        var orgId2 = Guid.NewGuid();

        var service = CreateService(ctx);
        var assignment1 = await service.AssignRoleAsync(new AssignRoleRequest
        {
            UserId = "user-1",
            RoleId = role.Id,
            OrganizationId = orgId1
        });

        var assignment2 = await service.AssignRoleAsync(new AssignRoleRequest
        {
            UserId = "user-1",
            RoleId = role.Id,
            OrganizationId = orgId2
        });

        assignment1.OrganizationId.Should().Be(orgId1);
        assignment2.OrganizationId.Should().Be(orgId2);
        (await ctx.RoleAssignments.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldSupportValidityPeriod()
    {
        await using var ctx = CreateContext();
        var role = new RoleProfile { Code = "TEMP.ROLE", Name = "Temporary Role" };
        ctx.RoleProfiles.Add(role);
        await ctx.SaveChangesAsync();

        var validFrom = DateTime.UtcNow.AddDays(1);
        var validTo = DateTime.UtcNow.AddDays(30);

        var service = CreateService(ctx);
        var assignment = await service.AssignRoleAsync(new AssignRoleRequest
        {
            UserId = "user-1",
            RoleId = role.Id,
            ValidFrom = validFrom,
            ValidTo = validTo
        });

        assignment.ValidFrom.Should().Be(validFrom);
        assignment.ValidTo.Should().Be(validTo);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldValidateRequiredFields()
    {
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        var act = async () => await service.CreateRoleAsync(new CreateRoleRequest
        {
            Code = "",
            Name = "Test Role"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Role code and name are required.");
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldPreventDuplicateCode()
    {
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        await service.CreateRoleAsync(new CreateRoleRequest
        {
            Code = "DUPLICATE.ROLE",
            Name = "First Role"
        });

        var act = async () => await service.CreateRoleAsync(new CreateRoleRequest
        {
            Code = "DUPLICATE.ROLE",
            Name = "Second Role"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Role code already exists within the organization.");
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldAllowSameCodeInDifferentOrganizations()
    {
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        var role1 = await service.CreateRoleAsync(new CreateRoleRequest
        {
            Code = "SALES.MANAGER",
            Name = "Sales Manager",
            OrganizationId = Guid.NewGuid()
        });

        var role2 = await service.CreateRoleAsync(new CreateRoleRequest
        {
            Code = "SALES.MANAGER",
            Name = "Sales Manager",
            OrganizationId = Guid.NewGuid()
        });

        role1.OrganizationId.Should().HaveValue();
        role2.OrganizationId.Should().HaveValue();
        role1.OrganizationId!.Value.Should().NotBe(role2.OrganizationId!.Value);
        (await ctx.RoleProfiles.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldCreateRoleWithDataScopes()
    {
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        var role = await service.CreateRoleAsync(new CreateRoleRequest
        {
            Code = "DATA.SCOPED.ROLE",
            Name = "Data Scoped Role",
            DataScopes = new List<RoleDataScopeDto>
            {
                new() { EntityName = "Customer", ScopeType = RoleDataScopeTypes.Organization },
                new() { EntityName = "Order", ScopeType = RoleDataScopeTypes.Self },
                new() { EntityName = "*", ScopeType = RoleDataScopeTypes.All }
            }
        });

        role.DataScopes.Should().HaveCount(3);
        role.DataScopes.Should().Contain(s => s.EntityName == "Customer" && s.ScopeType == RoleDataScopeTypes.Organization);
        role.DataScopes.Should().Contain(s => s.EntityName == "Order" && s.ScopeType == RoleDataScopeTypes.Self);
        role.DataScopes.Should().Contain(s => s.EntityName == "*" && s.ScopeType == RoleDataScopeTypes.All);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldCreateRoleWithCustomFilterExpression()
    {
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        var role = await service.CreateRoleAsync(new CreateRoleRequest
        {
            Code = "CUSTOM.FILTER.ROLE",
            Name = "Custom Filter Role",
            DataScopes = new List<RoleDataScopeDto>
            {
                new()
                {
                    EntityName = "Customer",
                    ScopeType = RoleDataScopeTypes.Custom,
                    FilterExpression = "Region == 'APAC' && Status == 'Active'"
                }
            }
        });

        var customScope = role.DataScopes.Should().ContainSingle(s => s.ScopeType == RoleDataScopeTypes.Custom).Subject;
        customScope.FilterExpression.Should().Be("Region == 'APAC' && Status == 'Active'");
    }

    [Fact]
    public async Task CreateFunctionAsync_ShouldCreateFunctionWithParent()
    {
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        var parent = await service.CreateFunctionAsync(new CreateFunctionRequest
        {
            Code = "APP.CRM",
            Name = "CRM Module"
        });

        var child = await service.CreateFunctionAsync(new CreateFunctionRequest
        {
            Code = "APP.CRM.CUSTOMERS",
            Name = "Customers",
            ParentId = parent.Id,
            Route = "/customers",
            Icon = "team",
            IsMenu = true,
            SortOrder = 10
        });

        child.ParentId.Should().Be(parent.Id);
        child.Route.Should().Be("/customers");
        child.Icon.Should().Be("team");
        child.IsMenu.Should().BeTrue();
        child.SortOrder.Should().Be(10);
        child.DisplayNameKey.Should().BeNull();
        child.DisplayName.Should().NotBeNull();
        child.DisplayName!["zh"].Should().Be("Customers");
    }

    [Fact]
    public async Task CreateFunctionAsync_ShouldPreventDuplicateCode()
    {
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        await service.CreateFunctionAsync(new CreateFunctionRequest
        {
            Code = "DUPLICATE.FUNCTION",
            Name = "First Function"
        });

        var act = async () => await service.CreateFunctionAsync(new CreateFunctionRequest
        {
            Code = "DUPLICATE.FUNCTION",
            Name = "Second Function"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Function code already exists.");
    }

    [Fact]
    public async Task CreateFunctionAsync_ShouldAttachTemplateBinding()
    {
        await using var ctx = CreateContext();
        await EnsureLocalizationResourcesAsync(ctx);

        var template = new FormTemplate
        {
            Name = "Customer Detail",
            EntityType = "customer",
            UserId = "system",
            UsageType = FormTemplateUsageType.Detail,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();

        var binding = new TemplateBinding
        {
            EntityType = "customer",
            UsageType = FormTemplateUsageType.Detail,
            TemplateId = template.Id,
            IsSystem = true,
            UpdatedBy = "seed",
            UpdatedAt = DateTime.UtcNow
        };
        ctx.TemplateBindings.Add(binding);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var node = await service.CreateFunctionAsync(new CreateFunctionRequest
        {
            Code = "CRM.CUSTOMER.DETAIL",
            Name = "Customer Detail",
            TemplateId = binding.Id
        });

        node.TemplateBindingId.Should().Be(binding.Id);
        node.TemplateId.Should().Be(template.Id);
    }

    [Fact]
    public async Task CreateFunctionAsync_ShouldThrowWhenTemplateBindingMissing()
    {
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        var act = async () => await service.CreateFunctionAsync(new CreateFunctionRequest
        {
            Code = "CRM.INVALID",
            Name = "Invalid",
            TemplateId = 999
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Template binding not found.");
    }

    [Fact]
    public async Task SeedSystemAdministratorAsync_ShouldUpdateExistingSystemRole()
    {
        await using var ctx = CreateContext();
        await EnsureLocalizationResourcesAsync(ctx);
        var service = CreateService(ctx);

        // First seed
        await service.SeedSystemAdministratorAsync();
        var role = await ctx.RoleProfiles.Include(r => r.Functions).FirstOrDefaultAsync(r => r.IsSystem);
        var initialFunctionCount = role!.Functions.Count;

        // Add a new function
        ctx.FunctionNodes.Add(new FunctionNode
        {
            Code = "APP.NEW_FEATURE",
            Name = "New Feature",
            IsMenu = true,
            SortOrder = 999
        });
        await ctx.SaveChangesAsync();

        // Second seed should add the new function to admin role
        await service.SeedSystemAdministratorAsync();
        await ctx.Entry(role).ReloadAsync();
        await ctx.Entry(role).Collection(r => r.Functions).LoadAsync();

        role.Functions.Count.Should().Be(initialFunctionCount + 1);
        role.Functions.Should().Contain(f => f.FunctionId == ctx.FunctionNodes.First(fn => fn.Code == "APP.NEW_FEATURE").Id);
    }
}
