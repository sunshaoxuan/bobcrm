using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models.Metadata;
using BobCrm.Api.Contracts.Requests.DataSet;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BobCrm.Api.Tests;

public class DataSetEndpointsFinalSprintTests
{
    private static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Lang", "zh");
        return client;
    }

    private static async Task EnsureEntityDataSourceTypeAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var exists = await db.DataSourceTypes.AnyAsync(t => t.Code == "entity");
        if (exists)
        {
            return;
        }

        db.DataSourceTypes.Add(new DataSourceTypeEntry
        {
            Code = "entity",
            HandlerType = "BobCrm.Api.Services.DataSources.EntityDataSourceHandler",
            Category = "General",
            IsSystem = true,
            IsEnabled = true
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task DataSetEndpoints_FullCrudAndRuntime_ShouldReturnExpectedStatusCodes()
    {
        using var factory = new TestWebAppFactory();
        await EnsureEntityDataSourceTypeAsync(factory);
        var client = await CreateAuthenticatedClientAsync(factory);

        var code = $"DS_{Guid.NewGuid():N}";
        var createResp = await client.PostAsJsonAsync("/api/datasets/",
            new CreateDataSetRequest
            {
                Code = code,
                Name = "N",
                DataSourceTypeCode = "entity",
                ConfigJson = "{\"EntityType\":\"customer\"}",
                IsSystem = false,
                IsEnabled = true,
                CreatedBy = "admin"
            });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created, await createResp.Content.ReadAsStringAsync());
        var created = await createResp.ReadDataAsJsonAsync();
        var id = created.GetProperty("id").GetInt32();

        var listResp = await client.GetAsync("/api/datasets/");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var byIdResp = await client.GetAsync($"/api/datasets/{id}");
        byIdResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var byCodeResp = await client.GetAsync($"/api/datasets/by-code/{code}");
        byCodeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateResp = await client.PutAsJsonAsync($"/api/datasets/{id}",
            new UpdateDataSetRequest
            {
                Name = "N2",
                IsEnabled = true,
                UpdatedBy = "admin"
            });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var execResp = await client.PostAsJsonAsync($"/api/datasets/{id}/execute",
            new DataSetExecutionRequest { DataSetId = id, Page = 1 });
        execResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var fieldsResp = await client.GetAsync($"/api/datasets/{id}/fields");
        fieldsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResp = await client.DeleteAsync($"/api/datasets/{id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDataSetById_WhenMissing_ShouldReturn404()
    {
        using var factory = new TestWebAppFactory();
        var client = await CreateAuthenticatedClientAsync(factory);

        var resp = await client.GetAsync("/api/datasets/999999");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
