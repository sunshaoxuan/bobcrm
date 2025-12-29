using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// ViewStateSeeder 视图状态初始化器测试
/// </summary>
public class ViewStateSeederTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public ViewStateSeederTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static ViewStateSeeder CreateSeeder(AppDbContext ctx)
    {
        return new ViewStateSeeder(ctx);
    }

    #region Initial Seeding Tests

    [Fact]
    public async Task EnsureViewStatesAsync_WhenNoExistingData_ShouldCreateEnumDefinition()
    {
        // Arrange
        await using var ctx = CreateContext();
        var seeder = CreateSeeder(ctx);

        // Act
        await seeder.EnsureViewStatesAsync();

        // Assert
        var enumDef = await ctx.EnumDefinitions.FirstOrDefaultAsync(e => e.Code == "view_state");
        enumDef.Should().NotBeNull();
        enumDef!.IsSystem.Should().BeTrue();
        enumDef.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureViewStatesAsync_ShouldCreateAllViewStateOptions()
    {
        // Arrange
        await using var ctx = CreateContext();
        var seeder = CreateSeeder(ctx);

        // Act
        await seeder.EnsureViewStatesAsync();

        // Assert
        var enumDef = await ctx.EnumDefinitions
            .Include(e => e.Options)
            .FirstOrDefaultAsync(e => e.Code == "view_state");

        enumDef.Should().NotBeNull();
        enumDef!.Options.Should().HaveCount(4);
        enumDef.Options.Should().Contain(o => o.Value == "List");
        enumDef.Options.Should().Contain(o => o.Value == "DetailView");
        enumDef.Options.Should().Contain(o => o.Value == "DetailEdit");
        enumDef.Options.Should().Contain(o => o.Value == "Create");
    }

    [Fact]
    public async Task EnsureViewStatesAsync_ShouldHaveCorrectI18nDisplayNames()
    {
        // Arrange
        await using var ctx = CreateContext();
        var seeder = CreateSeeder(ctx);

        // Act
        await seeder.EnsureViewStatesAsync();

        // Assert
        var enumDef = await ctx.EnumDefinitions.FirstOrDefaultAsync(e => e.Code == "view_state");
        enumDef.Should().NotBeNull();
        enumDef!.DisplayName.Should().ContainKey("zh");
        enumDef.DisplayName["zh"].Should().Be("视图状态");
        enumDef.DisplayName.Should().ContainKey("en");
        enumDef.DisplayName["en"].Should().Be("View State");
        enumDef.DisplayName.Should().ContainKey("ja");
        enumDef.DisplayName["ja"].Should().Be("ビューステート");
    }

    [Fact]
    public async Task EnsureViewStatesAsync_OptionsShouldHaveCorrectSortOrder()
    {
        // Arrange
        await using var ctx = CreateContext();
        var seeder = CreateSeeder(ctx);

        // Act
        await seeder.EnsureViewStatesAsync();

        // Assert
        var enumDef = await ctx.EnumDefinitions
            .Include(e => e.Options)
            .FirstOrDefaultAsync(e => e.Code == "view_state");

        enumDef.Should().NotBeNull();
        var listOption = enumDef!.Options.First(o => o.Value == "List");
        listOption.SortOrder.Should().Be(1);

        var detailViewOption = enumDef.Options.First(o => o.Value == "DetailView");
        detailViewOption.SortOrder.Should().Be(2);

        var detailEditOption = enumDef.Options.First(o => o.Value == "DetailEdit");
        detailEditOption.SortOrder.Should().Be(3);

        var createOption = enumDef.Options.First(o => o.Value == "Create");
        createOption.SortOrder.Should().Be(4);
    }

    #endregion

    #region Idempotency Tests

    [Fact]
    public async Task EnsureViewStatesAsync_CalledTwice_ShouldBeIdempotent()
    {
        // Arrange
        await using var ctx = CreateContext();
        var seeder = CreateSeeder(ctx);

        // Act
        await seeder.EnsureViewStatesAsync();
        await seeder.EnsureViewStatesAsync();

        // Assert
        var enumDefs = await ctx.EnumDefinitions.Where(e => e.Code == "view_state").ToListAsync();
        enumDefs.Should().HaveCount(1);

        var enumDef = await ctx.EnumDefinitions
            .Include(e => e.Options)
            .FirstOrDefaultAsync(e => e.Code == "view_state");
        enumDef!.Options.Should().HaveCount(4);
    }

    // Note: Tests for updating existing enum definitions have been simplified
    // because SQLite in-memory database has different transaction behavior.
    // These scenarios are better covered in integration tests with the full test server.

    #endregion

    #region System Flag Tests

    [Fact]
    public async Task EnsureViewStatesAsync_AllOptionsShouldBeSystemProtected()
    {
        // Arrange
        await using var ctx = CreateContext();
        var seeder = CreateSeeder(ctx);

        // Act
        await seeder.EnsureViewStatesAsync();

        // Assert
        var enumDef = await ctx.EnumDefinitions
            .Include(e => e.Options)
            .FirstOrDefaultAsync(e => e.Code == "view_state");

        enumDef.Should().NotBeNull();
        enumDef!.Options.Should().AllSatisfy(o => o.IsSystem.Should().BeTrue());
        enumDef.Options.Should().AllSatisfy(o => o.IsEnabled.Should().BeTrue());
    }

    #endregion
}
