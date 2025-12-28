using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Tests;

/// <summary>
/// SystemMenuSeeder 测试
/// 覆盖系统菜单初始化
/// </summary>
public class SystemMenuSeederTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    #region EnsureSystemMenusAsync Tests

    [Fact]
    public async Task EnsureSystemMenusAsync_ShouldCreateRootNode()
    {
        // Arrange
        await using var ctx = CreateContext();
        var seeder = new SystemMenuSeeder(ctx);

        // Act
        await seeder.EnsureSystemMenusAsync();

        // Assert
        var root = await ctx.FunctionNodes.FirstOrDefaultAsync(f => f.Code == "APP.ROOT");
        root.Should().NotBeNull();
        root!.IsMenu.Should().BeTrue();
        root.Icon.Should().Be("appstore");
    }

    [Fact]
    public async Task EnsureSystemMenusAsync_ShouldCreateSystemDomain()
    {
        // Arrange
        await using var ctx = CreateContext();
        var seeder = new SystemMenuSeeder(ctx);

        // Act
        await seeder.EnsureSystemMenusAsync();

        // Assert
        var sysDomain = await ctx.FunctionNodes.FirstOrDefaultAsync(f => f.Code == "SYS");
        sysDomain.Should().NotBeNull();
        sysDomain!.Name.Should().Contain("系统");
        sysDomain.Icon.Should().Be("setting");
    }

    [Fact]
    public async Task EnsureSystemMenusAsync_ShouldCreateEntityEditorMenu()
    {
        // Arrange
        await using var ctx = CreateContext();
        var seeder = new SystemMenuSeeder(ctx);

        // Act
        await seeder.EnsureSystemMenusAsync();

        // Assert
        var entityEditor = await ctx.FunctionNodes.FirstOrDefaultAsync(f => f.Code == "SYS.ENTITY.EDITOR");
        entityEditor.Should().NotBeNull();
        entityEditor!.Route.Should().Be("/entity-definitions");
    }

    [Fact]
    public async Task EnsureSystemMenusAsync_ShouldCreateEnumMenu()
    {
        // Arrange
        await using var ctx = CreateContext();
        var seeder = new SystemMenuSeeder(ctx);

        // Act
        await seeder.EnsureSystemMenusAsync();

        // Assert
        var enumMenu = await ctx.FunctionNodes.FirstOrDefaultAsync(f => f.Code == "SYS.ENTITY.ENUM");
        enumMenu.Should().NotBeNull();
        enumMenu!.Route.Should().Be("/system/enums");
    }

    [Fact]
    public async Task EnsureSystemMenusAsync_ShouldCreateMenuEditorMenu()
    {
        // Arrange
        await using var ctx = CreateContext();
        var seeder = new SystemMenuSeeder(ctx);

        // Act
        await seeder.EnsureSystemMenusAsync();

        // Assert
        var menuEditor = await ctx.FunctionNodes.FirstOrDefaultAsync(f => f.Code == "SYS.SET.MENU");
        menuEditor.Should().NotBeNull();
        menuEditor!.Route.Should().Be("/menus");
    }

    [Fact]
    public async Task EnsureSystemMenusAsync_ShouldBeIdempotent()
    {
        // Arrange
        await using var ctx = CreateContext();
        var seeder = new SystemMenuSeeder(ctx);

        // Act
        await seeder.EnsureSystemMenusAsync();
        await seeder.EnsureSystemMenusAsync();

        // Assert
        var roots = await ctx.FunctionNodes.CountAsync(f => f.Code == "APP.ROOT");
        roots.Should().Be(1);
    }

    [Fact]
    public async Task EnsureSystemMenusAsync_ShouldSetCorrectHierarchy()
    {
        // Arrange
        await using var ctx = CreateContext();
        var seeder = new SystemMenuSeeder(ctx);

        // Act
        await seeder.EnsureSystemMenusAsync();

        // Assert
        var root = await ctx.FunctionNodes.FirstOrDefaultAsync(f => f.Code == "APP.ROOT");
        var sysDomain = await ctx.FunctionNodes.FirstOrDefaultAsync(f => f.Code == "SYS");

        root.Should().NotBeNull();
        sysDomain.Should().NotBeNull();
        sysDomain!.ParentId.Should().Be(root!.Id);
    }

    [Fact]
    public async Task EnsureSystemMenusAsync_ShouldCreateAdminPermissions()
    {
        // Arrange
        await using var ctx = CreateContext();
        // Create admin role
        var adminRole = new RoleProfile { Code = "ADMIN", Name = "Administrator" };
        ctx.RoleProfiles.Add(adminRole);
        await ctx.SaveChangesAsync();

        var seeder = new SystemMenuSeeder(ctx);

        // Act
        await seeder.EnsureSystemMenusAsync();

        // Assert
        var permissions = await ctx.RoleFunctionPermissions
            .Where(p => p.RoleId == adminRole.Id)
            .ToListAsync();

        // Admin should have permissions to system menus
        permissions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task EnsureSystemMenusAsync_ShouldSetDisplayNameTranslations()
    {
        // Arrange
        await using var ctx = CreateContext();
        var seeder = new SystemMenuSeeder(ctx);

        // Act
        await seeder.EnsureSystemMenusAsync();

        // Assert
        var entityGroup = await ctx.FunctionNodes.FirstOrDefaultAsync(f => f.Code == "SYS.ENTITY");
        entityGroup.Should().NotBeNull();
        entityGroup!.DisplayName.Should().NotBeNull();
        entityGroup.DisplayName.Should().ContainKey("zh");
        entityGroup.DisplayName.Should().ContainKey("en");
        entityGroup.DisplayName.Should().ContainKey("ja");
    }

    #endregion
}
