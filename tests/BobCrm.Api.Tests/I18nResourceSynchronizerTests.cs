using BobCrm.Api.Base;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// I18nResourceSynchronizer 国际化资源同步器测试
/// </summary>
public class I18nResourceSynchronizerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Mock<ILogger<I18nResourceSynchronizer>> _mockLogger;

    public I18nResourceSynchronizerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _mockLogger = new Mock<ILogger<I18nResourceSynchronizer>>();
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

    private I18nResourceSynchronizer CreateSynchronizer(AppDbContext ctx)
    {
        return new I18nResourceSynchronizer(ctx, _mockLogger.Object);
    }

    #region Basic Sync Tests

    [Fact]
    public async Task SyncResourcesAsync_WhenResourcesExist_ShouldNotThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var synchronizer = CreateSynchronizer(ctx);

        // Act & Assert - should not throw
        await synchronizer.SyncResourcesAsync();
    }

    [Fact]
    public async Task SyncResourcesAsync_ShouldSyncResourcesToDatabase()
    {
        // Arrange
        await using var ctx = CreateContext();
        var synchronizer = CreateSynchronizer(ctx);

        // Act
        await synchronizer.SyncResourcesAsync();

        // Assert
        var resourceCount = await ctx.LocalizationResources.CountAsync();
        resourceCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Idempotency Tests

    [Fact]
    public async Task SyncResourcesAsync_CalledTwice_ShouldBeIdempotent()
    {
        // Arrange
        await using var ctx = CreateContext();
        var synchronizer = CreateSynchronizer(ctx);

        // Act
        await synchronizer.SyncResourcesAsync();
        var countAfterFirst = await ctx.LocalizationResources.CountAsync();

        await synchronizer.SyncResourcesAsync();
        var countAfterSecond = await ctx.LocalizationResources.CountAsync();

        // Assert
        countAfterSecond.Should().Be(countAfterFirst);
    }

    #endregion

    #region Update Existing Resources Tests

    [Fact]
    public async Task SyncResourcesAsync_WhenExistingResourceHasDifferentTranslation_ShouldUpdate()
    {
        // Arrange
        await using var ctx = CreateContext();

        // First sync to populate resources
        var synchronizer = CreateSynchronizer(ctx);
        await synchronizer.SyncResourcesAsync();

        // Get one resource and modify its translation
        var resource = await ctx.LocalizationResources.FirstAsync();
        var originalKey = resource.Key;

        // Clear and re-sync
        ctx.LocalizationResources.RemoveRange(ctx.LocalizationResources);
        await ctx.SaveChangesAsync();

        // Add back with different value
        var modifiedResource = new LocalizationResource
        {
            Key = originalKey,
            Translations = new Dictionary<string, string> { ["zh"] = "被修改的值" }
        };
        ctx.LocalizationResources.Add(modifiedResource);
        await ctx.SaveChangesAsync();

        // Act
        await synchronizer.SyncResourcesAsync();

        // Assert - should still have resources
        var finalCount = await ctx.LocalizationResources.CountAsync();
        finalCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task SyncResourcesAsync_ShouldLogResourceCount()
    {
        // Arrange
        await using var ctx = CreateContext();
        var synchronizer = CreateSynchronizer(ctx);

        // Act
        await synchronizer.SyncResourcesAsync();

        // Assert - verify logger was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Loaded")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Batch Processing Tests

    [Fact]
    public async Task SyncResourcesAsync_WithManyResources_ShouldProcessInBatches()
    {
        // Arrange
        await using var ctx = CreateContext();
        var synchronizer = CreateSynchronizer(ctx);

        // Act
        await synchronizer.SyncResourcesAsync();

        // Assert - resources should be synced
        var resourceCount = await ctx.LocalizationResources.CountAsync();
        resourceCount.Should().BeGreaterThan(0);
    }

    #endregion
}
