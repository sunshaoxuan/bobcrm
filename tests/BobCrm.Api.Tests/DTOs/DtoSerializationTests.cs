using System.Text.Json;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Responses.Entity;
using Xunit;

namespace BobCrm.Api.Tests.DTOs;

/// <summary>
/// DTO 序列化/反序列化双模式测试
/// 验证单语/多语字段的互斥序列化与兼容性
/// </summary>
public class DtoSerializationTests
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void EntitySummaryDto_SingleLanguage_SerializesOnlySingleField()
    {
        var dto = new EntitySummaryDto
        {
            EntityName = "Customer",
            DisplayName = "客户",
            Description = "客户描述",
            DisplayNameTranslations = null,
            DescriptionTranslations = null
        };

        var json = JsonSerializer.Serialize(dto, CamelCaseOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("displayName", out var dn));
        Assert.Equal("客户", dn.GetString());
        Assert.False(root.TryGetProperty("displayNameTranslations", out _));
        Assert.True(root.TryGetProperty("description", out var desc));
        Assert.Equal("客户描述", desc.GetString());
        Assert.False(root.TryGetProperty("descriptionTranslations", out _));
    }

    [Fact]
    public void EntitySummaryDto_Multilingual_SerializesOnlyTranslations()
    {
        var dto = new EntitySummaryDto
        {
            EntityName = "Customer",
            DisplayName = null,
            Description = null,
            DisplayNameTranslations = new MultilingualText { { "zh", "客户" } },
            DescriptionTranslations = new MultilingualText { { "zh", "描述" } }
        };

        var json = JsonSerializer.Serialize(dto, CamelCaseOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.False(root.TryGetProperty("displayName", out _));
        Assert.True(root.TryGetProperty("displayNameTranslations", out var dnTrans));
        Assert.Equal("客户", dnTrans.GetProperty("zh").GetString());
        Assert.False(root.TryGetProperty("description", out _));
        Assert.True(root.TryGetProperty("descriptionTranslations", out var descTrans));
        Assert.Equal("描述", descTrans.GetProperty("zh").GetString());
    }

    [Fact]
    public void FieldMetadataDto_SerializesDisplayNameKeyAndSingleLang()
    {
        var dto = new FieldMetadataDto
        {
            PropertyName = "Code",
            DisplayNameKey = "LBL_FIELD_CODE",
            DisplayName = "编码",
            DisplayNameTranslations = null
        };

        var json = JsonSerializer.Serialize(dto, CamelCaseOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("displayNameKey", out var keyProp));
        Assert.Equal("LBL_FIELD_CODE", keyProp.GetString());
        Assert.True(root.TryGetProperty("displayName", out var dnProp));
        Assert.Equal("编码", dnProp.GetString());
        Assert.False(root.TryGetProperty("displayNameTranslations", out _));
    }

    [Fact]
    public void EntitySummaryDto_DeserializesSingleAndMultiFormats()
    {
        var singleJson = "{\"displayName\":\"客户\",\"description\":\"描述\"}";
        var multiJson = "{\"displayNameTranslations\":{\"zh\":\"客户\"},\"descriptionTranslations\":{\"zh\":\"描述\"}}";

        var singleDto = JsonSerializer.Deserialize<EntitySummaryDto>(singleJson, CamelCaseOptions);
        var multiDto = JsonSerializer.Deserialize<EntitySummaryDto>(multiJson, CamelCaseOptions);

        Assert.Equal("客户", singleDto!.DisplayName);
        Assert.Null(singleDto.DisplayNameTranslations);
        Assert.Equal("描述", singleDto.Description);

        Assert.Null(multiDto!.DisplayName);
        Assert.NotNull(multiDto.DisplayNameTranslations);
        Assert.Equal("客户", multiDto.DisplayNameTranslations!["zh"]);
        Assert.NotNull(multiDto.DescriptionTranslations);
        Assert.Equal("描述", multiDto.DescriptionTranslations!["zh"]);
    }

    [Fact]
    public void EntitySummaryDto_SingleLanguage_ReducesPayloadByHalf()
    {
        var multi = new EntitySummaryDto
        {
            EntityName = "Customer",
            DisplayNameTranslations = new MultilingualText
            {
                { "zh", "客户管理系统实体定义" },
                { "ja", "顧客管理システムエンティティ定義" },
                { "en", "Customer Management System Entity Definition" },
                { "fr", "Définition de l'entité du système de gestion des clients" },
                { "de", "Entitätsdefinition des Kundenverwaltungssystems" }
            },
            DescriptionTranslations = new MultilingualText
            {
                { "zh", "用于管理客户信息的核心业务实体" },
                { "ja", "顧客情報を管理するためのコアビジネスエンティティ" },
                { "en", "Core business entity for managing customer information" },
                { "fr", "Entité métier centrale pour gérer les informations client" },
                { "de", "Zentrale Geschäftseinheit zur Verwaltung von Kundeninformationen" }
            }
        };

        var single = new EntitySummaryDto
        {
            EntityName = "Customer",
            DisplayName = "客户",
            Description = "描述"
        };

        var multiJson = JsonSerializer.Serialize(multi, CamelCaseOptions);
        var singleJson = JsonSerializer.Serialize(single, CamelCaseOptions);
        var reduction = 1.0 - ((double)singleJson.Length / multiJson.Length);

        Assert.True(singleJson.Length < multiJson.Length);
        Assert.True(reduction >= 0.5, $"Expect >=50% reduction, actual: {reduction:P}");
    }

    [Fact]
    public void EntitySummaryDto_NullFields_NotSerialized()
    {
        var dto = new EntitySummaryDto
        {
            EntityName = "Customer",
            DisplayName = null,
            Description = null,
            DisplayNameTranslations = null,
            DescriptionTranslations = null
        };

        var json = JsonSerializer.Serialize(dto, CamelCaseOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.False(root.TryGetProperty("displayName", out _));
        Assert.False(root.TryGetProperty("displayNameTranslations", out _));
        Assert.False(root.TryGetProperty("description", out _));
        Assert.False(root.TryGetProperty("descriptionTranslations", out _));
    }
}
