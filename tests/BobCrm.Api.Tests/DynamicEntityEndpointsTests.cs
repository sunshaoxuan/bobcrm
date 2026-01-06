using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BobCrm.Api.Tests;

public class DynamicEntityEndpointsTests
{
    [Fact]
    public async Task QueryDynamicEntities_WithoutLang_ReturnsTranslationsMode_AndIgnoresAcceptLanguage()
    {
        using var factory = CreateFactoryWithFakePersistence();
        var fullTypeName = await SeedEntityDefinitionAsync(factory.Services);

        var client = await CreateAuthenticatedClientAsync(factory);
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ja"));

        var response = await client.PostAsJsonAsync(
            $"/api/dynamic-entities/{fullTypeName}/query",
            new { filters = Array.Empty<object>(), orderBy = "Id", orderByDescending = false, skip = 0, take = 10 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var root = await response.ReadAsJsonAsync();
        var payload = root.GetProperty("data");
        Assert.True(payload.TryGetProperty("meta", out var meta));
        Assert.True(meta.TryGetProperty("fields", out var fields));
        Assert.Equal(JsonValueKind.Array, fields.ValueKind);

        var codeField = FindField(fields, "Code");
        Assert.Equal("LBL_FIELD_CODE", codeField.GetProperty("displayNameKey").GetString());
        Assert.False(codeField.TryGetProperty("displayName", out _));
        Assert.False(codeField.TryGetProperty("displayNameTranslations", out _));

        var customField = FindField(fields, "CustomField");
        Assert.False(customField.TryGetProperty("displayName", out _));
        Assert.True(customField.TryGetProperty("displayNameTranslations", out var translations));
        Assert.Equal("自定义字段", translations.GetProperty("zh").GetString());
    }

    [Fact]
    public async Task QueryDynamicEntities_WithLang_ReturnsSingleLanguageMode()
    {
        using var factory = CreateFactoryWithFakePersistence();
        var fullTypeName = await SeedEntityDefinitionAsync(factory.Services);

        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsJsonAsync(
            $"/api/dynamic-entities/{fullTypeName}/query?lang=zh",
            new { filters = Array.Empty<object>(), orderBy = "Id", orderByDescending = false, skip = 0, take = 10 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var root = await response.ReadAsJsonAsync();
        var fields = root.GetProperty("data").GetProperty("meta").GetProperty("fields");

        var codeField = FindField(fields, "Code");
        Assert.Equal("LBL_FIELD_CODE", codeField.GetProperty("displayNameKey").GetString());
        Assert.Equal("编码", codeField.GetProperty("displayName").GetString());
        Assert.False(codeField.TryGetProperty("displayNameTranslations", out _));

        var customField = FindField(fields, "CustomField");
        Assert.Equal("自定义字段", customField.GetProperty("displayName").GetString());
        Assert.False(customField.TryGetProperty("displayNameTranslations", out _));
    }

    [Fact]
    public async Task QueryDynamicEntities_WithLang_IncludesDisplayForEnumField()
    {
        using var factory = CreateFactoryWithFakePersistence();
        var fullTypeName = await SeedEntityDefinitionAsync(factory.Services);

        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsJsonAsync(
            $"/api/dynamic-entities/{fullTypeName}/query?lang=zh",
            new { filters = Array.Empty<object>(), orderBy = "Id", orderByDescending = false, skip = 0, take = 10 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var root = await response.ReadAsJsonAsync();
        var dto = root.GetProperty("data");
        var rows = dto.GetProperty("data");
        Assert.True(rows.GetArrayLength() > 0);

        var first = rows[0];
        Assert.True(first.TryGetProperty("__display", out var display));
        string? statusLabel = null;
        foreach (var prop in display.EnumerateObject())
        {
            if (string.Equals(prop.Name, "status", StringComparison.OrdinalIgnoreCase))
            {
                statusLabel = prop.Value.GetString();
                break;
            }
        }
        Assert.Equal("草稿", statusLabel);
    }

    [Fact]
    public async Task QueryDynamicEntities_IncludeMetaFalse_OmitsMeta()
    {
        using var factory = CreateFactoryWithFakePersistence();
        var fullTypeName = await SeedEntityDefinitionAsync(factory.Services);

        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsJsonAsync(
            $"/api/dynamic-entities/{fullTypeName}/query?includeMeta=false",
            new { filters = Array.Empty<object>(), orderBy = "Id", orderByDescending = false, skip = 0, take = 10 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var root = await response.ReadAsJsonAsync();
        var payload = root.GetProperty("data");
        Assert.False(payload.TryGetProperty("meta", out _));
    }

    [Fact]
    public async Task GetDynamicEntityById_Default_ReturnsRawEntityObject()
    {
        using var factory = CreateFactoryWithFakePersistence();
        var fullTypeName = await SeedEntityDefinitionAsync(factory.Services);

        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync($"/api/dynamic-entities/{fullTypeName}/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var root = await response.ReadAsJsonAsync();
        var payload = root.GetProperty("data");
        Assert.False(payload.TryGetProperty("meta", out _));
        Assert.Equal(1, payload.GetProperty("data").GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task GetDynamicEntityById_IncludeMeta_WithoutLang_ReturnsWrapperTranslationsMode()
    {
        using var factory = CreateFactoryWithFakePersistence();
        var fullTypeName = await SeedEntityDefinitionAsync(factory.Services);

        var client = await CreateAuthenticatedClientAsync(factory);
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));

        var response = await client.GetAsync($"/api/dynamic-entities/{fullTypeName}/1?includeMeta=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var root = await response.ReadAsJsonAsync();
        var payload = root.GetProperty("data");
        Assert.True(payload.TryGetProperty("meta", out var meta));
        var data = payload.GetProperty("data");
        Assert.Equal(1, data.GetProperty("id").GetInt32());

        var fields = meta.GetProperty("fields");
        var codeField = FindField(fields, "Code");
        Assert.Equal("LBL_FIELD_CODE", codeField.GetProperty("displayNameKey").GetString());
        Assert.False(codeField.TryGetProperty("displayName", out _));
        Assert.False(codeField.TryGetProperty("displayNameTranslations", out _));
    }

    [Fact]
    public async Task GetDynamicEntityById_IncludeMeta_WithLang_ReturnsSingleLanguageMode()
    {
        using var factory = CreateFactoryWithFakePersistence();
        var fullTypeName = await SeedEntityDefinitionAsync(factory.Services);

        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync($"/api/dynamic-entities/{fullTypeName}/1?includeMeta=true&lang=zh");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var root = await response.ReadAsJsonAsync();
        var fields = root.GetProperty("data").GetProperty("meta").GetProperty("fields");

        var codeField = FindField(fields, "Code");
        Assert.Equal("编码", codeField.GetProperty("displayName").GetString());
        Assert.False(codeField.TryGetProperty("displayNameTranslations", out _));
    }

    private static WebApplicationFactory<Program> CreateFactoryWithFakePersistence()
    {
        var fake = new FakeReflectionPersistenceService();
        return new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll(typeof(IReflectionPersistenceService));
                services.AddSingleton<IReflectionPersistenceService>(fake);
            });
        });
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.UseBearer(accessToken);
        return client;
    }

    private static async Task<string> SeedEntityDefinitionAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var enumDef = new EnumDefinition
        {
            Id = Guid.NewGuid(),
            Code = $"order_status_{Guid.NewGuid():N}",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "订单状态", ["en"] = "Order Status", ["ja"] = "注文ステータス" }
        };
        enumDef.Options.Add(new EnumOption
        {
            Id = Guid.NewGuid(),
            EnumDefinitionId = enumDef.Id,
            Value = "DRAFT",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "草稿", ["en"] = "Draft", ["ja"] = "下書き" },
            SortOrder = 1,
            IsEnabled = true
        });
        enumDef.Options.Add(new EnumOption
        {
            Id = Guid.NewGuid(),
            EnumDefinitionId = enumDef.Id,
            Value = "SUBMITTED",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "已提交", ["en"] = "Submitted", ["ja"] = "提出済み" },
            SortOrder = 2,
            IsEnabled = true
        });
        db.EnumDefinitions.Add(enumDef);

        var entityName = $"DynamicTest_{Guid.NewGuid():N}";
        var fullTypeName = $"BobCrm.Test.{entityName}";

        var definition = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = entityName,
            FullTypeName = fullTypeName,
            EntityRoute = entityName.ToLowerInvariant(),
            ApiEndpoint = $"/api/{entityName.ToLowerInvariant()}",
            StructureType = "Single",
            Status = EntityStatus.Published,
            Source = "Custom",
            IsEnabled = true,
            IsRootEntity = true,
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
            IsRequired = false,
            SortOrder = 2,
            Source = FieldSource.Custom
        });

        definition.Fields.Add(new FieldMetadata
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = definition.Id,
            PropertyName = "Status",
            DisplayNameKey = null,
            DisplayName = new Dictionary<string, string?>
            {
                ["zh"] = "状态",
                ["en"] = "Status",
                ["ja"] = "状態"
            },
            DataType = FieldDataType.Enum,
            IsRequired = false,
            SortOrder = 3,
            Source = FieldSource.Custom,
            EnumDefinitionId = enumDef.Id,
            IsMultiSelect = false
        });

        db.EntityDefinitions.Add(definition);
        await db.SaveChangesAsync();

        return fullTypeName;
    }

    private static JsonElement FindField(JsonElement fields, string propertyName)
    {
        return fields.EnumerateArray().First(f =>
            string.Equals(f.GetProperty("propertyName").GetString(), propertyName, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeReflectionPersistenceService : IReflectionPersistenceService
    {
        private static readonly TestDynamicEntity Entity = new()
        {
            Id = 1,
            Code = "C001",
            CustomField = "自定义值",
            Status = "DRAFT"
        };

        public Task<List<object>> QueryAsync(string fullTypeName, QueryOptions? options = null)
        {
            return Task.FromResult(new List<object> { Entity });
        }

        public Task<object?> GetByIdAsync(string fullTypeName, int id)
        {
            return Task.FromResult<object?>(id == 1 ? Entity : null);
        }

        public Task<object> CreateAsync(string fullTypeName, Dictionary<string, object> data)
        {
            return Task.FromResult<object>(Entity);
        }

        public Task<object?> UpdateAsync(string fullTypeName, int id, Dictionary<string, object> data)
        {
            return Task.FromResult<object?>(id == 1 ? Entity : null);
        }

        public Task<bool> DeleteAsync(string fullTypeName, int id, string? deletedBy = null)
        {
            return Task.FromResult(id == 1);
        }

        public Task<int> CountAsync(string fullTypeName, List<FilterCondition>? filters = null)
        {
            return Task.FromResult(1);
        }

        public Task<List<Dictionary<string, object?>>> QueryRawAsync(string tableName, QueryOptions? options = null)
        {
            return Task.FromResult(new List<Dictionary<string, object?>>());
        }
    }

    private sealed class TestDynamicEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? CustomField { get; set; }
        public string? Status { get; set; }
    }
}
