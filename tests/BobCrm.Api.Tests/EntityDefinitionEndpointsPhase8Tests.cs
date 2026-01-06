using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.Requests.Entity;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BobCrm.Api.Tests;

public class EntityDefinitionEndpointsPhase8Tests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public EntityDefinitionEndpointsPhase8Tests(TestWebAppFactory factory)
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

    private async Task<EntityDefinition> CreateEntityDefinitionAsync(string status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var id = Guid.NewGuid();
        var namespaceName = "BobCrm.Base.Custom";
        var entityName = $"Phase8Entity_{Guid.NewGuid():N}";
        var entityRoute = entityName.ToLowerInvariant();

        var entity = new EntityDefinition
        {
            Id = id,
            Namespace = namespaceName,
            EntityName = entityName,
            FullTypeName = $"{namespaceName}.{entityName}",
            EntityRoute = entityRoute,
            ApiEndpoint = $"/api/{entityRoute}",
            Status = status,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-10),
            DisplayName = new Dictionary<string, string?>
            {
                ["zh"] = "Phase8 实体",
                ["en"] = "Phase8 Entity",
                ["ja"] = "Phase8 エンティティ"
            }
        };

        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = id,
            PropertyName = "Code",
            DataType = "String",
            Length = 64,
            SortOrder = 1,
            DisplayName = new Dictionary<string, string?>
            {
                ["zh"] = "编码",
                ["en"] = "Code",
                ["ja"] = "コード"
            },
            Source = "Custom",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-10)
        });

        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        return entity;
    }

    [Fact]
    public async Task CheckEntityReferenced_WhenReferenced_ShouldReturnTrue()
    {
        var client = await GetAuthenticatedClientAsync();
        var entity = await CreateEntityDefinitionAsync(EntityStatus.Draft);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var template = new FormTemplate
            {
                Name = "Phase8 Template",
                EntityType = entity.FullName,
                UserId = "admin"
            };
            db.FormTemplates.Add(template);
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/entity-definitions/{entity.Id}/referenced");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("isReferenced").GetBoolean().Should().BeTrue();
        data.GetProperty("referenceCount").GetInt32().Should().BeGreaterThan(0);
        data.GetProperty("details").GetProperty("formTemplates").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteEntityDefinition_WhenPublished_ShouldReturnBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();
        var entity = await CreateEntityDefinitionAsync(EntityStatus.Published);

        var response = await client.DeleteAsync($"/api/entity-definitions/{entity.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("ENTITY_PUBLISHED");
    }

    [Fact]
    public async Task DeleteEntityDefinition_WhenReferenced_ShouldReturnBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();
        var entity = await CreateEntityDefinitionAsync(EntityStatus.Draft);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.FormTemplates.Add(new FormTemplate
            {
                Name = "Ref Template",
                EntityType = entity.FullName,
                UserId = "admin"
            });
            await db.SaveChangesAsync();
        }

        var response = await client.DeleteAsync($"/api/entity-definitions/{entity.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("ENTITY_REFERENCED");
    }

    [Fact]
    public async Task PreviewDdl_ForDraft_ShouldReturnCreateTableScript()
    {
        var client = await GetAuthenticatedClientAsync();
        var entity = await CreateEntityDefinitionAsync(EntityStatus.Draft);

        var response = await client.GetAsync($"/api/entity-definitions/{entity.Id}/preview-ddl");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("status").GetString().Should().Be(EntityStatus.Draft);
        data.GetProperty("ddlScript").GetString().Should().Contain("CREATE TABLE");
    }

    [Fact]
    public async Task PreviewDdl_ForModifiedWithNewFields_ShouldReturnAlterTableScript()
    {
        var client = await GetAuthenticatedClientAsync();
        var entity = await CreateEntityDefinitionAsync(EntityStatus.Modified);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.FieldMetadatas.Add(new FieldMetadata
            {
                EntityDefinitionId = entity.Id,
                PropertyName = "NewField",
                DataType = "Int32",
                SortOrder = 2,
                Source = "Custom",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/entity-definitions/{entity.Id}/preview-ddl");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("status").GetString().Should().Be(EntityStatus.Modified);
        data.GetProperty("ddlScript").GetString().Should().Contain("ALTER TABLE");
    }

    [Fact]
    public async Task GenerateCode_WhenEntityNotFound_ShouldReturnBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/entity-definitions/{Guid.NewGuid()}/generate-code");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("CODE_GENERATION_FAILED");
    }

    [Fact]
    public async Task Compile_WhenEntityNotFound_ShouldReturnBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.PostAsync($"/api/entity-definitions/{Guid.NewGuid()}/compile", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("COMPILE_FAILED");
    }

    [Fact]
    public async Task CompileBatch_WithNoIds_ShouldReturnBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/entity-definitions/compile-batch", new CompileBatchDto
        {
            EntityIds = new List<Guid>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("COMPILE_FAILED");
    }

    [Fact]
    public async Task ValidateCode_WhenEntityNotFound_ShouldReturnBadRequest()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/entity-definitions/{Guid.NewGuid()}/validate-code");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task GetEntityTypeInfo_WhenNotLoaded_ShouldReturnNotFound()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/entity-definitions/type-info/BobCrm.Base.Custom.NotLoaded");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("TYPE_NOT_LOADED");
    }

    [Fact]
    public async Task GetLoadedEntities_AndUnload_ShouldSucceed()
    {
        var client = await GetAuthenticatedClientAsync();

        var list = await client.GetAsync("/api/entity-definitions/loaded-entities");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var listData = await list.ReadDataAsJsonAsync();
        listData.GetProperty("count").GetInt32().Should().BeGreaterOrEqualTo(0);
        listData.GetProperty("entities").ValueKind.Should().Be(JsonValueKind.Array);

        var unload = await client.DeleteAsync("/api/entity-definitions/loaded-entities/BobCrm.Base.Custom.NotLoaded");
        unload.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
