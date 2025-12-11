using BobCrm.Api.Utils;

namespace BobCrm.Api.Tests.Utils;

public class MultilingualHelperTests
{
    [Fact]
    public void Resolve_WithValidLang_ReturnsCorrectTranslation()
    {
        // Arrange
        var dict = new Dictionary<string, string?>
        {
            { "zh", "编码" },
            { "ja", "コード" },
            { "en", "Code" }
        };

        // Act & Assert
        Assert.Equal("编码", dict.Resolve("zh"));
        Assert.Equal("コード", dict.Resolve("ja"));
        Assert.Equal("Code", dict.Resolve("en"));
    }

    [Fact]
    public void Resolve_WithInvalidLang_ReturnsFallback()
    {
        // Arrange
        var dict = new Dictionary<string, string?>
        {
            { "zh", "编码" },
            { "ja", "コード" }
        };

        // Act
        var result = dict.Resolve("fr");

        // Assert - 应返回第一个非空值
        Assert.True(result == "编码" || result == "コード");
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Resolve_WithNullDict_ReturnsEmpty()
    {
        // Arrange
        Dictionary<string, string?>? dict = null;

        // Act
        var result = dict.Resolve("zh");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_WithEmptyDict_ReturnsEmpty()
    {
        // Arrange
        var dict = new Dictionary<string, string?>();

        // Act
        var result = dict.Resolve("zh");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_WithNullValues_SkipsNullAndReturnsNonNull()
    {
        // Arrange
        var dict = new Dictionary<string, string?>
        {
            { "zh", null },
            { "ja", "コード" },
            { "en", "Code" }
        };

        // Act
        var result = dict.Resolve("zh");

        // Assert
        Assert.Equal("コード", result);
    }

    [Fact]
    public void ResolveBatch_WithMultipleDicts_ReturnsAllResolved()
    {
        // Arrange
        Dictionary<string, Dictionary<string, string?>?>? nullDicts = null;
        var nullResult = nullDicts.ResolveBatch("zh");

        var emptyResult = new Dictionary<string, Dictionary<string, string?>?>().ResolveBatch("zh");

        var dicts = new Dictionary<string, Dictionary<string, string?>?>
        {
            { "field1", new Dictionary<string, string?> { { "zh", "字段1" }, { "ja", "フィールド1" } } },
            { "field2", new Dictionary<string, string?> { { "zh", "字段2" }, { "ja", "フィールド2" } } }
        };

        // Act
        var result = dicts.ResolveBatch("zh");

        // Assert
        Assert.Empty(nullResult);
        Assert.Empty(emptyResult);
        Assert.Equal(2, result.Count);
        Assert.Equal("字段1", result["field1"]);
        Assert.Equal("字段2", result["field2"]);
    }
}
