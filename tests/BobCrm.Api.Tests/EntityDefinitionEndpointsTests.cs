using System.Net;
using System.Text.Json;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace BobCrm.Api.Tests;

public class EntityDefinitionEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public EntityDefinitionEndpointsTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.UseBearer(accessToken);
        return client;
    }

    private async Task<(Guid entityId, string entityName)> SeedEntityDefinitionAsync()
    {
        var entityId = Guid.NewGuid();
        var entityName = $"TestEntity_{Guid.NewGuid():N}";

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var definition = new EntityDefinition
        {
            Id = entityId,
            Namespace = "BobCrm.Test",
            EntityName = entityName,
            FullTypeName = $"BobCrm.Test.{entityName}",
            EntityRoute = entityName.ToLowerInvariant(),
            ApiEndpoint = $"/api/{entityName.ToLowerInvariant()}",
            StructureType = "Single",
            Status = EntityStatus.Draft,
            Source = "Custom",
            IsEnabled = true,
            IsRootEntity = true,
            DisplayName = new Dictionary<string, string?>
            {
                ["zh"] = "测试实体",
                ["ja"] = "テストエンティティ",
                ["en"] = "Test Entity"
            },
            Description = new Dictionary<string, string?>
            {
                ["zh"] = "描述",
                ["en"] = "Description"
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        definition.Fields.Add(new FieldMetadata
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = definition.Id,
            PropertyName = "Code",
            DisplayNameKey = "LBL_FIELD_CODE",
            DisplayName = null,
            DataType = FieldDataType.String,
            Length = 64,
            IsRequired = true,
            SortOrder = 1,
            Source = FieldSource.Interface
        });

        definition.Fields.Add(new FieldMetadata
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = definition.Id,
            PropertyName = "CustomField",
            DisplayNameKey = null,
            DisplayName = new Dictionary<string, string?>
            {
                ["zh"] = "自定义字段",
                ["en"] = "Custom Field"
            },
            DataType = FieldDataType.String,
            Length = 100,
            IsRequired = false,
            SortOrder = 2,
            Source = FieldSource.Custom
        });

        db.EntityDefinitions.Add(definition);
        await db.SaveChangesAsync();

        return (definition.Id, entityName);
    }

    [Fact]
    public async Task GetEntityDefinitions_WithoutLang_ReturnsTranslationsMode()
    {
        var (_, entityName) = await SeedEntityDefinitionAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/entity-definitions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = (await response.ReadAsJsonAsync()).UnwrapData();
        Assert.Equal(JsonValueKind.Array, data.ValueKind);

        var item = data.EnumerateArray().FirstOrDefault(e => e.GetProperty("entityName").GetString() == entityName);
        Assert.True(item.ValueKind == JsonValueKind.Object);

        Assert.False(item.TryGetProperty("displayName", out _));
        Assert.True(item.TryGetProperty("displayNameTranslations", out var displayNameTranslations));
        Assert.Equal("测试实体", displayNameTranslations.GetProperty("zh").GetString());
    }

    [Fact]
    public async Task GetEntityDefinitions_WithLang_ReturnsSingleLanguageMode()
    {
        var (_, entityName) = await SeedEntityDefinitionAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/entity-definitions?lang=zh");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = (await response.ReadAsJsonAsync()).UnwrapData();
        Assert.Equal(JsonValueKind.Array, data.ValueKind);

        var item = data.EnumerateArray().FirstOrDefault(e => e.GetProperty("entityName").GetString() == entityName);
        Assert.True(item.ValueKind == JsonValueKind.Object);

        Assert.True(item.TryGetProperty("displayName", out var displayName));
        Assert.Equal("测试实体", displayName.GetString());
        Assert.False(item.TryGetProperty("displayNameTranslations", out _));
    }

    [Fact]
    public async Task GetEntityDefinitionById_WithoutLang_ReturnsFieldTranslations()
    {
        var (entityId, _) = await SeedEntityDefinitionAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/entity-definitions/{entityId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = (await response.ReadAsJsonAsync()).UnwrapData();

        Assert.False(dto.TryGetProperty("displayName", out _));
        Assert.True(dto.TryGetProperty("displayNameTranslations", out _));

        var fields = dto.GetProperty("fields");
        Assert.Equal(JsonValueKind.Array, fields.ValueKind);

        var codeField = fields.EnumerateArray().First(f => f.GetProperty("propertyName").GetString() == "Code");
        Assert.Equal("LBL_FIELD_CODE", codeField.GetProperty("displayNameKey").GetString());
        Assert.False(codeField.TryGetProperty("displayName", out _));
        Assert.False(codeField.TryGetProperty("displayNameTranslations", out _));

        var customField = fields.EnumerateArray().First(f => f.GetProperty("propertyName").GetString() == "CustomField");
        Assert.True(customField.TryGetProperty("displayNameTranslations", out var customTranslations));
        Assert.Equal("自定义字段", customTranslations.GetProperty("zh").GetString());
        Assert.False(customField.TryGetProperty("displayName", out _));
    }

    [Fact]
    public async Task GetEntityDefinitionById_WithLang_ResolvesInterfaceAndCustomFieldDisplayName()
    {
        var (entityId, _) = await SeedEntityDefinitionAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/entity-definitions/{entityId}?lang=zh");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = (await response.ReadAsJsonAsync()).UnwrapData();

        Assert.True(dto.TryGetProperty("displayName", out var displayName));
        Assert.Equal("测试实体", displayName.GetString());
        Assert.False(dto.TryGetProperty("displayNameTranslations", out _));

        var fields = dto.GetProperty("fields");

        var codeField = fields.EnumerateArray().First(f => f.GetProperty("propertyName").GetString() == "Code");
        Assert.Equal("LBL_FIELD_CODE", codeField.GetProperty("displayNameKey").GetString());
        Assert.Equal("编码", codeField.GetProperty("displayName").GetString());
        Assert.False(codeField.TryGetProperty("displayNameTranslations", out _));

        var customField = fields.EnumerateArray().First(f => f.GetProperty("propertyName").GetString() == "CustomField");
        Assert.Equal("自定义字段", customField.GetProperty("displayName").GetString());
        Assert.False(customField.TryGetProperty("displayNameTranslations", out _));
    }
}

