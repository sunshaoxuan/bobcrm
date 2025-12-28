using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using BobCrm.Api.Contracts.Requests.I18n;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BobCrm.Api.Tests;

/// <summary>
/// I18nAdminService 测试
/// 覆盖 i18n 管理服务
/// </summary>
public class I18nAdminServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static I18nAdminService CreateService(AppDbContext context)
    {
        var mockLoc = new Mock<ILocalization>();
        mockLoc.Setup(l => l.T(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((key, lang) => key);

        return new I18nAdminService(context, mockLoc.Object);
    }

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WhenNoResources_ShouldReturnEmpty()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        // Act
        var result = await service.SearchAsync(1, 10, null, null, CancellationToken.None);

        // Assert
        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnPagedResults()
    {
        // Arrange
        await using var ctx = CreateContext();
        for (int i = 0; i < 25; i++)
        {
            ctx.LocalizationResources.Add(new LocalizationResource
            {
                Key = $"KEY_{i:D2}",
                Translations = new Dictionary<string, string> { ["zh"] = $"值{i}" }
            });
        }
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.SearchAsync(1, 10, null, "zh", CancellationToken.None);

        // Assert
        result.Data.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task SearchAsync_WithKeyFilter_ShouldFilterByKey()
    {
        // Arrange
        await using var ctx = CreateContext();
        ctx.LocalizationResources.AddRange(
            new LocalizationResource { Key = "MENU_HOME", Translations = new Dictionary<string, string> { ["zh"] = "首页" } },
            new LocalizationResource { Key = "BTN_SAVE", Translations = new Dictionary<string, string> { ["zh"] = "保存" } },
            new LocalizationResource { Key = "MENU_SETTINGS", Translations = new Dictionary<string, string> { ["zh"] = "设置" } }
        );
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.SearchAsync(1, 10, "MENU", "zh", CancellationToken.None);

        // Assert
        result.Data.Should().HaveCount(2);
        result.Data!.All(x => x.Key.Contains("MENU")).Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_WithCultureFilter_ShouldFilterByCulture()
    {
        // Arrange
        await using var ctx = CreateContext();
        ctx.LocalizationResources.Add(new LocalizationResource
        {
            Key = "TEST_KEY",
            Translations = new Dictionary<string, string>
            {
                ["zh"] = "中文",
                ["en"] = "English",
                ["ja"] = "日本語"
            }
        });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.SearchAsync(1, 10, null, "zh", CancellationToken.None);

        // Assert
        result.Data.Should().HaveCount(1);
        result.Data!.First().Culture.Should().Be("zh");
        result.Data!.First().Value.Should().Be("中文");
    }

    [Fact]
    public async Task SearchAsync_ShouldIdentifyProtectedKeys()
    {
        // Arrange
        await using var ctx = CreateContext();
        ctx.LocalizationResources.AddRange(
            new LocalizationResource { Key = "MENU_HOME", Translations = new Dictionary<string, string> { ["zh"] = "首页" } },
            new LocalizationResource { Key = "CUSTOM_KEY", Translations = new Dictionary<string, string> { ["zh"] = "自定义" } }
        );
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.SearchAsync(1, 10, null, "zh", CancellationToken.None);

        // Assert
        var menuItem = result.Data!.First(x => x.Key == "MENU_HOME");
        menuItem.IsProtectedKey.Should().BeTrue();

        var customItem = result.Data!.First(x => x.Key == "CUSTOM_KEY");
        customItem.IsProtectedKey.Should().BeFalse();
    }

    #endregion

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_WithNullRequest_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SaveAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task SaveAsync_WithEmptyKey_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var request = new SaveI18nResourceRequest { Key = "", Culture = "zh", Value = "测试" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task SaveAsync_WithNewKey_ShouldCreateResource()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var request = new SaveI18nResourceRequest { Key = "NEW_KEY", Culture = "zh", Value = "新值" };

        // Act
        await service.SaveAsync(request, CancellationToken.None);

        // Assert
        var resource = await ctx.LocalizationResources.FirstOrDefaultAsync(r => r.Key == "NEW_KEY");
        resource.Should().NotBeNull();
        resource!.Translations["zh"].Should().Be("新值");
    }

    [Fact]
    public async Task SaveAsync_WithExistingKey_ShouldUpdateResource()
    {
        // Arrange
        await using var ctx = CreateContext();
        ctx.LocalizationResources.Add(new LocalizationResource
        {
            Key = "EXISTING_KEY",
            Translations = new Dictionary<string, string> { ["zh"] = "旧值" }
        });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new SaveI18nResourceRequest { Key = "EXISTING_KEY", Culture = "zh", Value = "新值" };

        // Act
        await service.SaveAsync(request, CancellationToken.None);

        // Assert
        var resource = await ctx.LocalizationResources.FirstOrDefaultAsync(r => r.Key == "EXISTING_KEY");
        resource!.Translations["zh"].Should().Be("新值");
    }

    [Fact]
    public async Task SaveAsync_ShouldAddNewTranslation()
    {
        // Arrange
        await using var ctx = CreateContext();
        ctx.LocalizationResources.Add(new LocalizationResource
        {
            Key = "EXISTING_KEY",
            Translations = new Dictionary<string, string> { ["zh"] = "中文" }
        });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new SaveI18nResourceRequest { Key = "EXISTING_KEY", Culture = "en", Value = "English" };

        // Act
        await service.SaveAsync(request, CancellationToken.None);

        // Assert
        var resource = await ctx.LocalizationResources.FirstOrDefaultAsync(r => r.Key == "EXISTING_KEY");
        resource!.Translations.Should().ContainKey("zh");
        resource.Translations.Should().ContainKey("en");
        resource.Translations["en"].Should().Be("English");
    }

    #endregion
}
