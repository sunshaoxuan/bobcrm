using System.Text.Json;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Extensions;
using BobCrm.Api.Infrastructure;
using Moq;

namespace BobCrm.Api.Tests.Extensions;

/// <summary>
/// DtoExtensions 扩展方法测试
/// 验证单语/多语双模式转换和优先级解析逻辑
/// </summary>
public class DtoExtensionsTests
{
    [Fact]
    public void ToSummaryDto_WithLang_ReturnsSingleLanguage()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            EntityName = "Customer",
            EntityRoute = "customer",
            ApiEndpoint = "/api/customers",
            DisplayName = new Dictionary<string, string?>
            {
                { "zh", "客户" },
                { "ja", "顧客" },
                { "en", "Customer" }
            },
            Description = new Dictionary<string, string?>
            {
                { "zh", "客户实体" },
                { "ja", "顧客エンティティ" },
                { "en", "Customer entity" }
            }
        };

        // Act
        var dto = entity.ToSummaryDto("zh");

        // Assert
        Assert.Equal("客户", dto.DisplayName);
        Assert.Equal("客户实体", dto.Description);
        Assert.Null(dto.DisplayNameTranslations);
        Assert.Null(dto.DescriptionTranslations);
    }

    [Fact]
    public void ToSummaryDto_WithoutLang_ReturnsMultilingual()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            EntityName = "Customer",
            EntityRoute = "customer",
            DisplayName = new Dictionary<string, string?>
            {
                { "zh", "客户" },
                { "ja", "顧客" }
            }
        };

        // Act
        var dto = entity.ToSummaryDto(lang: null);

        // Assert
        Assert.Null(dto.DisplayName);
        Assert.NotNull(dto.DisplayNameTranslations);
        Assert.Equal("客户", dto.DisplayNameTranslations!["zh"]);
        Assert.Equal("顧客", dto.DisplayNameTranslations!["ja"]);
    }

    [Fact]
    public void ToFieldDto_WithDisplayNameKey_UsesLocalization()
    {
        // Arrange
        var field = new FieldMetadata
        {
            PropertyName = "Code",
            DisplayNameKey = "LBL_FIELD_CODE",
            DataType = "String"
        };

        var mockLoc = new Mock<ILocalization>(MockBehavior.Strict);
        mockLoc.Setup(l => l.T("LBL_FIELD_CODE", "zh")).Returns("编码");

        // Act
        var dto = field.ToFieldDto(mockLoc.Object, "zh");

        // Assert
        Assert.Equal("编码", dto.DisplayName);
        Assert.Null(dto.DisplayNameTranslations);
        Assert.Equal("LBL_FIELD_CODE", dto.DisplayNameKey);
        mockLoc.Verify(l => l.T("LBL_FIELD_CODE", "zh"), Times.Once);
    }

    [Fact]
    public void ToFieldDto_WithDisplayNameDict_UsesResolve()
    {
        // Arrange
        var field = new FieldMetadata
        {
            PropertyName = "CustomField",
            DisplayName = new Dictionary<string, string?>
            {
                { "zh", "自定义字段" },
                { "ja", "カスタムフィールド" }
            },
            DataType = "String"
        };

        var mockLoc = new Mock<ILocalization>(MockBehavior.Strict);

        // Act
        var dto = field.ToFieldDto(mockLoc.Object, "ja");

        // Assert
        Assert.Equal("カスタムフィールド", dto.DisplayName);
        Assert.Null(dto.DisplayNameTranslations);
        mockLoc.Verify(l => l.T(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ToFieldDto_WithoutLang_ReturnsMultilingual()
    {
        // Arrange
        var field = new FieldMetadata
        {
            PropertyName = "Name",
            DisplayName = new Dictionary<string, string?>
            {
                { "zh", "名称" },
                { "en", "Name" }
            },
            DataType = "String"
        };

        var mockLoc = new Mock<ILocalization>(MockBehavior.Strict);

        // Act
        var dto = field.ToFieldDto(mockLoc.Object, lang: null);

        // Assert
        Assert.Null(dto.DisplayName);
        Assert.NotNull(dto.DisplayNameTranslations);
        Assert.Equal("名称", dto.DisplayNameTranslations!["zh"]);
        Assert.Equal("Name", dto.DisplayNameTranslations!["en"]);
        mockLoc.Verify(l => l.T(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ToFieldDto_WithNoDisplayName_ReturnsFallback()
    {
        // Arrange
        var field = new FieldMetadata
        {
            PropertyName = "UnknownField",
            DisplayName = null,
            DataType = "String"
        };

        var mockLoc = new Mock<ILocalization>(MockBehavior.Strict);

        // Act
        var dto = field.ToFieldDto(mockLoc.Object, "zh");

        // Assert
        Assert.Equal("UnknownField", dto.DisplayName);
        Assert.Null(dto.DisplayNameTranslations);
        mockLoc.Verify(l => l.T(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ToSummaryDto_SingleLanguageMode_ReducesResponseSize()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            EntityName = "Customer",
            EntityRoute = "customer",
            ApiEndpoint = "/api/customers",
            DisplayName = new Dictionary<string, string?>
            {
                { "zh", "客户管理系统实体定义" },
                { "ja", "顧客管理システムエンティティ定義" },
                { "en", "Customer Management System Entity Definition" }
            },
            Description = new Dictionary<string, string?>
            {
                { "zh", "用于管理客户信息的核心业务实体" },
                { "ja", "顧客情報を管理するためのコアビジネスエンティティ" },
                { "en", "Core business entity for managing customer information" }
            }
        };

        // Act
        var multiLangDto = entity.ToSummaryDto(null);
        var singleLangDto = entity.ToSummaryDto("zh");

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var multiLangJson = JsonSerializer.Serialize(multiLangDto, options);
        var singleLangJson = JsonSerializer.Serialize(singleLangDto, options);

        // Assert
        Assert.True(singleLangJson.Length < multiLangJson.Length,
            $"单语模式应该减少响应体积。多语: {multiLangJson.Length} bytes, 单语: {singleLangJson.Length} bytes");

        var reduction = 1.0 - ((double)singleLangJson.Length / multiLangJson.Length);

        Assert.True(reduction >= 0.5,
            $"预期至少减少 50%，实际减少: {reduction:P}");
    }

}
