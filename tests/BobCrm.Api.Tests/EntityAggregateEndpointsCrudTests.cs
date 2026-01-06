using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Base.Aggregates;
using BobCrm.Api.Endpoints;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BobCrm.Api.Tests;

public class EntityAggregateEndpointsCrudTests
{
    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll(typeof(ISubEntityCodeGenerator));
                services.AddSingleton<ISubEntityCodeGenerator>(new SafeSubEntityCodeGenerator());
            });
        });
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static SaveEntityDefinitionAggregateRequest CreateValidRequest(Guid id)
    {
        return new SaveEntityDefinitionAggregateRequest
        {
            Id = id,
            Namespace = "BobCrm.Test",
            EntityName = $"Agg_{Guid.NewGuid():N}",
            DisplayName = new Dictionary<string, string?>
            {
                ["zh"] = "聚合测试",
                ["en"] = "Aggregate Test",
                ["ja"] = "集約テスト"
            },
            SubEntities =
            [
                new SubEntityDto
                {
                    Id = Guid.Empty,
                    Code = "Lines",
                    SortOrder = 1,
                    DisplayName = new Dictionary<string, string?> { ["zh"] = "明细", ["en"] = "Lines" },
                    Fields =
                    [
                        new FieldMetadataDto
                        {
                            Id = Guid.Empty,
                            PropertyName = "Name",
                            DisplayName = new Dictionary<string, string?> { ["zh"] = "名称", ["en"] = "Name" },
                            DataType = FieldDataType.String,
                            IsRequired = true,
                            SortOrder = 1
                        }
                    ]
                }
            ]
        };
    }

    [Fact]
    public async Task GetEntityAggregate_WhenNotFound_ShouldReturn404()
    {
        using var factory = CreateFactory();
        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync($"/api/entity-aggregates/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("ENTITY_NOT_FOUND");
    }

    [Fact]
    public async Task ValidateEntityAggregate_WithValidRequest_ShouldReturnIsValidTrue()
    {
        using var factory = CreateFactory();
        var client = await CreateAuthenticatedClientAsync(factory);

        var request = CreateValidRequest(Guid.Empty);
        var response = await client.PostAsJsonAsync("/api/entity-aggregates/validate", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("isValid").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SaveEntityAggregate_New_ShouldPersistAndSupportPreviews()
    {
        using var factory = CreateFactory();
        var client = await CreateAuthenticatedClientAsync(factory);

        var request = CreateValidRequest(Guid.Empty);
        var save = await client.PostAsJsonAsync("/api/entity-aggregates", request);

        save.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await save.ReadDataAsJsonAsync();
        var id = saved.GetProperty("master").GetProperty("id").GetGuid();
        id.Should().NotBeEmpty();

        var load = await client.GetAsync($"/api/entity-aggregates/{id}");
        load.StatusCode.Should().Be(HttpStatusCode.OK);

        var metaPreview = await client.GetAsync($"/api/entity-aggregates/{id}/metadata-preview");
        metaPreview.StatusCode.Should().Be(HttpStatusCode.OK);

        var codePreview = await client.GetAsync($"/api/entity-aggregates/{id}/code-preview");
        codePreview.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SaveEntityAggregate_UpdateAndDeleteSubEntity_ShouldWork()
    {
        using var factory = CreateFactory();
        var client = await CreateAuthenticatedClientAsync(factory);

        var request = CreateValidRequest(Guid.Empty);
        var save = await client.PostAsJsonAsync("/api/entity-aggregates", request);
        save.StatusCode.Should().Be(HttpStatusCode.OK);

        var saved = await save.ReadDataAsJsonAsync();
        var id = saved.GetProperty("master").GetProperty("id").GetGuid();
        var subEntityId = saved.GetProperty("subEntities").EnumerateArray().Single().GetProperty("id").GetGuid();

        var update = CreateValidRequest(id);
        update.EntityName = saved.GetProperty("master").GetProperty("entityName").GetString() ?? update.EntityName;
        update.SubEntities = new List<SubEntityDto>(); // remove sub entity

        var updatedRes = await client.PostAsJsonAsync("/api/entity-aggregates", update);
        updatedRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteSub = await client.DeleteAsync($"/api/entity-aggregates/sub-entities/{subEntityId}");
        deleteSub.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class SafeSubEntityCodeGenerator : ISubEntityCodeGenerator
    {
        private readonly SubEntityCodeGenerator _inner = new(NullLogger<SubEntityCodeGenerator>.Instance);

        public Task GenerateSubEntitiesAsync(EntityDefinitionAggregate aggregate, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public string GenerateSubEntityClass(EntityDefinition mainEntity, SubEntityDefinition subEntity)
            => _inner.GenerateSubEntityClass(mainEntity, subEntity);

        public string GenerateAggregateVoClass(EntityDefinition mainEntity, List<SubEntityDefinition> subEntities)
            => _inner.GenerateAggregateVoClass(mainEntity, subEntities);
    }
}
