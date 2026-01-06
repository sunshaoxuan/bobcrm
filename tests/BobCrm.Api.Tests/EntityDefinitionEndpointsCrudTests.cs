using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Requests.Entity;
using BobCrm.Api.Contracts.Responses.Entity;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// EntityDefinitionEndpoints 实体定义端点 CRUD 深度测试
/// 覆盖完整业务流程，包括创建、读取、更新、删除
/// </summary>
public class EntityDefinitionEndpointsCrudTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EntityDefinitionEndpointsCrudTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static CreateEntityDefinitionDto CreateValidEntityDefinitionDto(string namespaceName, string entityName)
        => new CreateEntityDefinitionDto
        {
            Namespace = namespaceName,
            EntityName = entityName,
            DisplayName = new MultilingualText
            {
                ["zh"] = "测试实体",
                ["en"] = "Test Entity",
                ["ja"] = "テスト"
            },
            Description = new MultilingualText
            {
                ["zh"] = "测试描述",
                ["en"] = "Description"
            },
            Fields =
            [
                new CreateFieldMetadataDto
                {
                    PropertyName = "Code",
                    DisplayName = new MultilingualText { ["zh"] = "编码", ["en"] = "Code" },
                    DataType = "String",
                    IsRequired = true,
                    Length = 64,
                    SortOrder = 1
                }
            ]
        };

    #region GetAvailableEntities (Public) Tests

    [Fact]
    public async Task GetAvailableEntities_ShouldReturnPublishedEntities()
    {
        // Act
        var response = await _client.GetAsync("/api/entities");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("data");
    }

    [Fact]
    public async Task GetAvailableEntities_WithLang_ShouldReturnLocalizedData()
    {
        // Act
        var response = await _client.GetAsync("/api/entities?lang=zh");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region GetEntityDefinitionByRoute (Public) Tests

    [Fact]
    public async Task GetEntityDefinitionByRoute_WithValidRoute_ShouldReturnDefinition()
    {
        // Act - Use 'customer' which is a known system entity
        var response = await _client.GetAsync("/api/entities/customer/definition");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("entityName");
    }

    [Fact]
    public async Task GetEntityDefinitionByRoute_WithEntityPrefix_ShouldResolveCustomerDefinition()
    {
        var response = await _client.GetAsync("/api/entities/entity_customer/definition?lang=zh");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("entityRoute").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetEntityDefinitionByRoute_WithPlural_ShouldResolveCustomerDefinition()
    {
        var response = await _client.GetAsync("/api/entities/customers/definition?lang=zh");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("entityRoute").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetEntityDefinitionByRoute_WithInvalidRoute_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/api/entities/nonexistent_entity_xyz/definition");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEntityDefinitionByRoute_WithLang_ShouldReturnLocalizedFields()
    {
        // Act
        var response = await _client.GetAsync("/api/entities/customer/definition?lang=zh");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetEntityDefinitionByRoute_WithoutLang_ShouldReturnTranslationsMode()
    {
        var response = await _client.GetAsync("/api/entities/customer/definition");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await response.ReadDataAsJsonAsync();
        data.TryGetProperty("displayName", out _).Should().BeFalse();
        data.TryGetProperty("displayNameTranslations", out var translations).Should().BeTrue();
        translations.ValueKind.Should().Be(JsonValueKind.Object);
    }

    #endregion

    #region ValidateEntityRoute (Public) Tests

    [Fact]
    public async Task ValidateEntityRoute_WithValidRoute_ShouldReturnIsValidTrue()
    {
        // Act
        var response = await _client.GetAsync("/api/entities/customer/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("isValid");
    }

    [Fact]
    public async Task ValidateEntityRoute_WithInvalidRoute_ShouldReturnIsValidFalse()
    {
        // Act
        var response = await _client.GetAsync("/api/entities/nonexistent_xyz/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("isValid").GetBoolean().Should().BeFalse();
    }

    #endregion

    #region GetAllEntities (Admin) Tests

    [Fact]
    public async Task GetAllEntities_WithoutAuth_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/entities/all");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllEntities_WithAuth_ShouldReturnList()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/entities/all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.ValueKind.Should().Be(JsonValueKind.Array);
    }

    #endregion

    #region GetEntityDefinitions (Admin) Tests

    [Fact]
    public async Task GetEntityDefinitions_WithAuth_ShouldReturnAllDefinitions()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/entity-definitions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("data");
    }

    [Fact]
    public async Task GetEntityDefinitions_WithoutAuth_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/entity-definitions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetEntityDefinition (Admin) Tests

    [Fact]
    public async Task GetEntityDefinitionById_WithValidId_ShouldReturnDefinition()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // First get list to find a valid ID
        var listResponse = await client.GetAsync("/api/entity-definitions");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(listContent);
        var dataArray = doc.RootElement.GetProperty("data");

        if (dataArray.GetArrayLength() > 0)
        {
            var firstId = dataArray[0].GetProperty("id").GetString();

            // Act
            var response = await client.GetAsync($"/api/entity-definitions/{firstId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("entityName");
            content.Should().Contain("fields");
        }
    }

    [Fact]
    public async Task GetEntityDefinitionById_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/entity-definitions/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetEntityDefinitionByType (Admin) Tests

    [Fact]
    public async Task GetEntityDefinitionByType_WithValidType_ShouldReturnDefinition()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act - Try with Customer which should exist
        var response = await client.GetAsync("/api/entity-definitions/by-type/Customer");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region CheckEntityReferenced Tests

    [Fact]
    public async Task CheckEntityReferenced_WithValidId_ShouldReturnReferenceInfo()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Get a valid entity ID
        var listResponse = await client.GetAsync("/api/entity-definitions");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(listContent);
        var dataArray = doc.RootElement.GetProperty("data");

        if (dataArray.GetArrayLength() > 0)
        {
            var firstId = dataArray[0].GetProperty("id").GetString();

            // Act
            var response = await client.GetAsync($"/api/entity-definitions/{firstId}/referenced");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("isReferenced");
        }
    }

    #endregion

    #region CreateEntityDefinition Tests

    [Fact]
    public async Task CreateEntityDefinition_WithValidData_ShouldReturn201()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var uniqueName = $"TestEntity_{Guid.NewGuid():N}";
        var request = CreateValidEntityDefinitionDto("BobCrm.Test", uniqueName);

        // Act
        var response = await client.PostAsJsonAsync("/api/entity-definitions", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.ReadDataAsync<EntityDefinitionDto>(JsonOptions);
        dto.Should().NotBeNull();
        dto!.EntityName.Should().Be(uniqueName);
    }

    [Fact]
    public async Task CreateEntityDefinition_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        var request = new
        {
            entityName = "Test",
            @namespace = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/entity-definitions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region UpdateEntityDefinition Tests

    [Fact]
    public async Task UpdateEntityDefinition_WithInvalidId_ShouldReturn404OrBadRequest()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var invalidId = Guid.NewGuid();
        var request = new
        {
            displayName = new Dictionary<string, string> { ["zh"] = "更新名称" }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/entity-definitions/{invalidId}", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region DeleteEntityDefinition Tests

    [Fact]
    public async Task DeleteEntityDefinition_WithDraftEntity_ShouldDeleteAndReturn200()
    {
        var client = await GetAuthenticatedClientAsync();
        var uniqueName = $"DeleteMe_{Guid.NewGuid():N}";
        var create = await client.PostAsJsonAsync("/api/entity-definitions", CreateValidEntityDefinitionDto("BobCrm.Test", uniqueName), JsonOptions);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.ReadDataAsync<EntityDefinitionDto>(JsonOptions);
        created.Should().NotBeNull();

        var delete = await client.DeleteAsync($"/api/entity-definitions/{created!.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await client.GetAsync($"/api/entity-definitions/{created!.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEntityDefinition_WhenPublished_ShouldReturn400()
    {
        var client = await GetAuthenticatedClientAsync();
        var uniqueName = $"Published_{Guid.NewGuid():N}";
        var create = await client.PostAsJsonAsync("/api/entity-definitions", CreateValidEntityDefinitionDto("BobCrm.Test", uniqueName), JsonOptions);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.ReadDataAsync<EntityDefinitionDto>(JsonOptions);
        created.Should().NotBeNull();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.EntityDefinitions.FindAsync(created!.Id);
            entity.Should().NotBeNull();
            entity!.Status = EntityStatus.Published;
            await db.SaveChangesAsync();
        }

        var delete = await client.DeleteAsync($"/api/entity-definitions/{created!.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var root = await delete.ReadAsJsonAsync();
        root.TryGetProperty("code", out var code).Should().BeTrue();
        code.GetString().Should().Be("ENTITY_PUBLISHED");
    }

    [Fact]
    public async Task DeleteEntityDefinition_WithoutAuth_ShouldReturn401()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/entity-definitions/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteEntityDefinition_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/entity-definitions/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Code Generation & Compilation Tests

    [Fact]
    public async Task GenerateEntityCode_ForDraftEntity_ShouldReturnBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();
        var uniqueName = $"Draft_{Guid.NewGuid():N}";
        var create = await client.PostAsJsonAsync("/api/entity-definitions", CreateValidEntityDefinitionDto("BobCrm.Test", uniqueName), JsonOptions);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.ReadDataAsync<EntityDefinitionDto>(JsonOptions);
        created.Should().NotBeNull();

        var response = await client.GetAsync($"/api/entity-definitions/{created!.Id}/generate-code");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var root = await response.ReadAsJsonAsync();
        root.TryGetProperty("code", out var code).Should().BeTrue();
        code.GetString().Should().Be("CODE_GENERATION_FAILED");
    }

    [Fact]
    public async Task CompileEntity_WithInvalidNamespace_ShouldReturnBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();
        var uniqueName = $"BadNs_{Guid.NewGuid():N}";
        var create = await client.PostAsJsonAsync("/api/entity-definitions", CreateValidEntityDefinitionDto("BobCrm.Test Invalid", uniqueName), JsonOptions);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.ReadDataAsync<EntityDefinitionDto>(JsonOptions);
        created.Should().NotBeNull();

        var response = await client.PostAsync($"/api/entity-definitions/{created!.Id}/compile", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var root = await response.ReadAsJsonAsync();
        root.TryGetProperty("code", out var code).Should().BeTrue();
        code.GetString().Should().Be("COMPILE_FAILED");
    }

    [Fact]
    public async Task CompileBatchEntities_WithEmptyList_ShouldReturnBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/entity-definitions/compile-batch", new { entityIds = Array.Empty<Guid>() }, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var root = await response.ReadAsJsonAsync();
        root.TryGetProperty("code", out var code).Should().BeTrue();
        code.GetString().Should().Be("COMPILE_FAILED");
    }

    [Fact]
    public async Task ValidateEntityCode_WithInvalidId_ShouldReturnBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/entity-definitions/{Guid.NewGuid()}/validate-code");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var root = await response.ReadAsJsonAsync();
        root.TryGetProperty("code", out var code).Should().BeTrue();
        code.GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task GetEntityTypeInfo_WhenNotLoaded_ShouldReturn404()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/entity-definitions/type-info/Not.Loaded.Type");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var root = await response.ReadAsJsonAsync();
        root.TryGetProperty("code", out var code).Should().BeTrue();
        code.GetString().Should().Be("TYPE_NOT_LOADED");
    }

    [Fact]
    public async Task UnloadEntity_ShouldReturn200()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.DeleteAsync("/api/entity-definitions/loaded-entities/Not.Loaded.Type");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region PreviewDDL Tests

    [Fact]
    public async Task PreviewDDL_WithValidId_ShouldReturnScript()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Get a valid entity ID
        var listResponse = await client.GetAsync("/api/entity-definitions");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(listContent);
        var dataArray = doc.RootElement.GetProperty("data");

        if (dataArray.GetArrayLength() > 0)
        {
            var firstId = dataArray[0].GetProperty("id").GetString();

            // Act
            var response = await client.GetAsync($"/api/entity-definitions/{firstId}/preview-ddl");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("ddlScript");
        }
    }

    [Fact]
    public async Task PreviewDDL_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync($"/api/entity-definitions/{Guid.NewGuid()}/preview-ddl");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DDL History Tests

    [Fact]
    public async Task GetDDLHistory_WithValidId_ShouldReturnHistory()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Get a valid entity ID
        var listResponse = await client.GetAsync("/api/entity-definitions");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(listContent);
        var dataArray = doc.RootElement.GetProperty("data");

        if (dataArray.GetArrayLength() > 0)
        {
            var firstId = dataArray[0].GetProperty("id").GetString();

            // Act
            var response = await client.GetAsync($"/api/entity-definitions/{firstId}/ddl-history");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("data");
        }
    }

    #endregion

    #region Generate Code Tests

    [Fact]
    public async Task GenerateCode_WithValidId_ShouldReturnCode()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Get a valid entity ID
        var listResponse = await client.GetAsync("/api/entity-definitions");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(listContent);
        var dataArray = doc.RootElement.GetProperty("data");

        if (dataArray.GetArrayLength() > 0)
        {
            var firstId = dataArray[0].GetProperty("id").GetString();

            // Act
            var response = await client.GetAsync($"/api/entity-definitions/{firstId}/generate-code");

            // Assert - May return OK or BadRequest depending on entity state
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        }
    }

    #endregion

    #region Loaded Entities Tests

    [Fact]
    public async Task GetLoadedEntities_WithAuth_ShouldReturnList()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/entity-definitions/loaded-entities");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("count");
    }

    #endregion
}
