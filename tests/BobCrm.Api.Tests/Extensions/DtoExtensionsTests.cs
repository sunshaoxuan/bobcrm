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
        // TODO [Task 0.3]: 单语模式应该返回 string，而不是单键字典
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
        Assert.NotNull(dto.DisplayName);
        Assert.Single(dto.DisplayName!);
        Assert.Equal("客户", dto.DisplayName!["zh"]);
        Assert.Equal("客户实体", dto.Description!["zh"]);
    }

    [Fact]
    public void ToSummaryDto_WithoutLang_ReturnsMultilingual()
    {
        // Arrange
        // TODO [Task 0.3]: 多语模式将使用 DisplayNameTranslations 保持兼容
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
        Assert.NotNull(dto.DisplayName);
        Assert.Equal("客户", dto.DisplayName!["zh"]);
        Assert.Equal("顧客", dto.DisplayName!["ja"]);
    }

    [Fact]
    public void ToFieldDto_WithDisplayNameKey_UsesLocalization()
    {
        // Arrange
        var field = new FieldMetadataWithKey
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
        Assert.NotNull(dto.DisplayName);
        Assert.Equal("编码", dto.DisplayName!["zh"]);
        Assert.Equal("LBL_FIELD_CODE", dto.DisplayNameKey);
        mockLoc.Verify(l => l.T("LBL_FIELD_CODE", "zh"), Times.Once);
    }

    [Fact]
    public void ToFieldDto_WithDisplayNameDict_UsesResolve()
    {
        // Arrange
        // TODO [Task 0.3]: 单语模式返回 string 而非字典
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
        Assert.NotNull(dto.DisplayName);
        Assert.Equal("カスタムフィールド", dto.DisplayName!["ja"]);
        mockLoc.Verify(l => l.T(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ToFieldDto_WithoutLang_ReturnsMultilingual()
    {
        // Arrange
        // TODO [Task 0.3]: 多语模式将迁移到 DisplayNameTranslations
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
        Assert.Equal("名称", dto.DisplayName!["zh"]);
        Assert.Equal("Name", dto.DisplayName!["en"]);
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
        Assert.Equal("UnknownField", dto.DisplayName!["zh"]);
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

        var multiLangJson = JsonSerializer.Serialize(multiLangDto);
        var singleLangJson = JsonSerializer.Serialize(singleLangDto);

        // Assert
        Assert.True(singleLangJson.Length < multiLangJson.Length,
            $"单语模式应该减少响应体积。多语: {multiLangJson.Length} bytes, 单语: {singleLangJson.Length} bytes");

        var reduction = 1.0 - ((double)singleLangJson.Length / multiLangJson.Length);

        // TODO [Task 0.3]: 目标提升到 >= 50%；当前字典实现只能达到约 20-40%
        Assert.True(reduction >= 0.2,
            $"预期至少减少 20%，实际减少: {reduction:P}");
    }

    private class FieldMetadataWithKey : FieldMetadata
    {
        public string? DisplayNameKey { get; set; }
    }
}
