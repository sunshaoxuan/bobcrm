using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Tests;

/// <summary>
/// TemplateBindingAppService 测试
/// 覆盖模板绑定应用层
/// </summary>
public class TemplateBindingAppServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static TemplateBindingAppService CreateService(AppDbContext context)
    {
        return new TemplateBindingAppService(context, NullLogger<TemplateBindingAppService>.Instance);
    }

    #region GetMenuTemplateIntersectionsAsync Tests

    [Fact]
    public async Task GetMenuTemplateIntersectionsAsync_WhenNoAssignments_ShouldReturnEmpty()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        // Act
        var result = await service.GetMenuTemplateIntersectionsAsync("user1", "zh", "DetailView");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMenuTemplateIntersectionsAsync_WithAssignments_ShouldReturnIntersections()
    {
        // Arrange
        await using var ctx = CreateContext();
        
        // Create role
        var role = new RoleProfile { Code = "TEST.ROLE", Name = "Test Role" };
        ctx.RoleProfiles.Add(role);
        await ctx.SaveChangesAsync();
        
        // Create function with template binding
        var template = new FormTemplate
        {
            Name = "Customer Detail",
            EntityType = "customer",
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.Detail,
            IsSystemDefault = true
        };
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();
        
        var binding = new TemplateStateBinding
        {
            TemplateId = template.Id,
            EntityType = "customer",
            ViewState = "DetailView",
            Priority = 1,
            IsDefault = true
        };
        ctx.TemplateStateBindings.Add(binding);
        await ctx.SaveChangesAsync();
        
        var function = new FunctionNode
        {
            Code = "CRM.CUSTOMER.DETAIL",
            Name = "Customer Detail",
            IsMenu = true,
            TemplateStateBindingId = binding.Id
        };
        ctx.FunctionNodes.Add(function);
        await ctx.SaveChangesAsync();
        
        // Create role function permission
        ctx.RoleFunctionPermissions.Add(new RoleFunctionPermission
        {
            RoleId = role.Id,
            FunctionId = function.Id
        });
        await ctx.SaveChangesAsync();
        
        // Create role assignment for user
        ctx.RoleAssignments.Add(new RoleAssignment
        {
            UserId = "user1",
            RoleId = role.Id
        });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.GetMenuTemplateIntersectionsAsync("user1", "zh", "DetailView");

        // Assert
        result.Should().HaveCount(1);
        result.First().Binding.EntityType.Should().Be("customer");
    }

    [Fact]
    public async Task GetMenuTemplateIntersectionsAsync_FiltersByViewState()
    {
        // Arrange
        await using var ctx = CreateContext();
        
        var role = new RoleProfile { Code = "TEST.ROLE", Name = "Test Role" };
        ctx.RoleProfiles.Add(role);
        await ctx.SaveChangesAsync();
        
        var template = new FormTemplate
        {
            Name = "Customer List",
            EntityType = "customer",
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.List,
            IsSystemDefault = true
        };
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();
        
        var binding = new TemplateStateBinding
        {
            TemplateId = template.Id,
            EntityType = "customer",
            ViewState = "List", // List, not DetailView
            Priority = 1
        };
        ctx.TemplateStateBindings.Add(binding);
        await ctx.SaveChangesAsync();
        
        var function = new FunctionNode
        {
            Code = "CRM.CUSTOMER.LIST",
            Name = "Customer List",
            IsMenu = true,
            TemplateStateBindingId = binding.Id
        };
        ctx.FunctionNodes.Add(function);
        await ctx.SaveChangesAsync();
        
        ctx.RoleFunctionPermissions.Add(new RoleFunctionPermission
        {
            RoleId = role.Id,
            FunctionId = function.Id
        });
        
        ctx.RoleAssignments.Add(new RoleAssignment
        {
            UserId = "user1",
            RoleId = role.Id
        });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act - Query for DetailView (should not match List binding)
        var result = await service.GetMenuTemplateIntersectionsAsync("user1", "zh", "DetailView");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMenuTemplateIntersectionsAsync_WithExpiredAssignment_ShouldNotInclude()
    {
        // Arrange
        await using var ctx = CreateContext();
        
        var role = new RoleProfile { Code = "TEST.ROLE", Name = "Test Role" };
        ctx.RoleProfiles.Add(role);
        await ctx.SaveChangesAsync();
        
        var template = new FormTemplate
        {
            Name = "Customer Detail",
            EntityType = "customer",
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.Detail,
            IsSystemDefault = true
        };
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();
        
        var binding = new TemplateStateBinding
        {
            TemplateId = template.Id,
            EntityType = "customer",
            ViewState = "DetailView",
            Priority = 1
        };
        ctx.TemplateStateBindings.Add(binding);
        await ctx.SaveChangesAsync();
        
        var function = new FunctionNode
        {
            Code = "CRM.CUSTOMER.DETAIL",
            Name = "Customer Detail",
            IsMenu = true,
            TemplateStateBindingId = binding.Id
        };
        ctx.FunctionNodes.Add(function);
        await ctx.SaveChangesAsync();
        
        ctx.RoleFunctionPermissions.Add(new RoleFunctionPermission
        {
            RoleId = role.Id,
            FunctionId = function.Id
        });
        
        // Create expired assignment
        ctx.RoleAssignments.Add(new RoleAssignment
        {
            UserId = "user1",
            RoleId = role.Id,
            ValidTo = DateTime.UtcNow.AddDays(-1) // Expired yesterday
        });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.GetMenuTemplateIntersectionsAsync("user1", "zh", "DetailView");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMenuTemplateIntersectionsAsync_WithEmptyViewState_ShouldDefaultToDetailView()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        // Act - No assignments, but empty view state should default to DetailView
        var result = await service.GetMenuTemplateIntersectionsAsync("user1", "zh", "");

        // Assert
        result.Should().BeEmpty(); // No assignments, but should not throw
    }

    #endregion
}
