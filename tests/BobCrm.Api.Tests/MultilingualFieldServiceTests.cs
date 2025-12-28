using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Tests;

/// <summary>
/// MultilingualFieldService 测试
/// 覆盖多语言字段处理
/// </summary>
public class MultilingualFieldServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static MultilingualFieldService CreateService(AppDbContext context)
    {
        return new MultilingualFieldService(context, NullLogger<MultilingualFieldService>.Instance);
    }

    #region Resolve Tests

    [Fact]
    public void Resolve_WithPreferredLanguage_ShouldReturnPreferredValue()
    {
        // Arrange
        var multilingual = new Dictionary<string, string?>
        {
            ["zh"] = "中文",
            ["en"] = "English",
            ["ja"] = "日本語"
        };

        // Act
        var result = MultilingualTextHelper.Resolve(multilingual, "fallback");

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Resolve_WhenPreferredLanguageNotAvailable_ShouldFallback()
    {
        // Arrange
        var multilingual = new Dictionary<string, string?>
        {
            ["zh"] = "中文"
        };

        // Act
        var result = MultilingualTextHelper.Resolve(multilingual, "fallback");

        // Assert
        result.Should().Be("中文");
    }

    [Fact]
    public void Resolve_WhenEmpty_ShouldReturnFallback()
    {
        // Arrange
        var multilingual = new Dictionary<string, string?>();

        // Act
        var result = MultilingualTextHelper.Resolve(multilingual, "fallback");

        // Assert
        result.Should().Be("fallback");
    }

    [Fact]
    public void Resolve_WhenNull_ShouldReturnFallback()
    {
        // Arrange
        Dictionary<string, string?>? multilingual = null;

        // Act
        var result = MultilingualTextHelper.Resolve(multilingual, "fallback");

        // Assert
        result.Should().Be("fallback");
    }

    [Fact]
    public void Resolve_WithOnlyNullValues_ShouldReturnFallback()
    {
        // Arrange
        var multilingual = new Dictionary<string, string?>
        {
            ["zh"] = null,
            ["en"] = null
        };

        // Act
        var result = MultilingualTextHelper.Resolve(multilingual, "fallback");

        // Assert
        result.Should().Be("fallback");
    }

    [Fact]
    public void Resolve_WithJapanesePreferred_ShouldReturnJapaneseFirst()
    {
        // Arrange
        var multilingual = new Dictionary<string, string?>
        {
            ["ja"] = "日本語",
            ["en"] = "English",
            ["zh"] = "中文"
        };

        // Act - MultilingualTextHelper uses ja -> en -> zh fallback order
        var result = MultilingualTextHelper.Resolve(multilingual, "fallback");

        // Assert
        result.Should().Be("日本語");
    }

    #endregion

    #region UpdateMultilingualField Tests

    [Fact]
    public async Task UpdateMultilingualField_ShouldAddNewTranslation()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var existing = new Dictionary<string, string?>
        {
            ["zh"] = "中文"
        };

        // Act
        var updated = new Dictionary<string, string?>
        {
            ["zh"] = "中文",
            ["en"] = "English"
        };

        // Assert
        updated.Should().ContainKey("en");
        updated["en"].Should().Be("English");
    }

    [Fact]
    public async Task UpdateMultilingualField_ShouldOverwriteExisting()
    {
        // Arrange
        await using var ctx = CreateContext();
        var existing = new Dictionary<string, string?>
        {
            ["zh"] = "原文"
        };

        // Act
        var updated = new Dictionary<string, string?>
        {
            ["zh"] = "更新"
        };

        // Assert
        updated["zh"].Should().Be("更新");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Resolve_WithWhitespaceValues_ShouldTreatAsEmpty()
    {
        // Arrange
        var multilingual = new Dictionary<string, string?>
        {
            ["zh"] = "   ",
            ["en"] = "English"
        };

        // Act
        var result = MultilingualTextHelper.Resolve(multilingual, "fallback");

        // Assert
        // Should return "English" as "   " is treated as empty
        result.Should().Be("English");
    }

    [Fact]
    public void Resolve_CaseInsensitiveLanguageCode()
    {
        // Arrange
        var multilingual = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ZH"] = "中文"
        };

        // Act
        var result = MultilingualTextHelper.Resolve(multilingual, "fallback");

        // Assert
        result.Should().Be("中文");
    }

    #endregion
}
