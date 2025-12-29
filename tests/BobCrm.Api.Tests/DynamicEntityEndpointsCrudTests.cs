using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BobCrm.Api.Tests;

public class DynamicEntityEndpointsCrudTests
{
    [Fact]
    public async Task Create_ShouldInjectAuditFields_WhenKeysPresent()
    {
        var fake = new CapturingReflectionPersistenceService();
        using var factory = CreateFactory(fake);
        var fullTypeName = await DynamicEntityEndpointsTests_SeedDefinitionAsync(factory.Services);

        var client = await CreateAuthenticatedClientAsync(factory);
        var adminId = await GetAdminUserIdAsync(factory.Services);

        var response = await client.PostAsJsonAsync(
            $"/api/dynamic-entities/{fullTypeName}",
            new Dictionary<string, object>
            {
                ["CreatedBy"] = "should-be-overwritten",
                ["CreatedAt"] = "should-be-overwritten",
                ["Code"] = "C001"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(fake.LastCreatedData);
        Assert.Equal(adminId, fake.LastCreatedData!["CreatedBy"]?.ToString());
        Assert.IsType<DateTime>(fake.LastCreatedData["CreatedAt"]);
    }

    [Fact]
    public async Task Update_ShouldInjectAuditFields_WhenKeysPresent()
    {
        var fake = new CapturingReflectionPersistenceService
        {
            UpdateResult = new LocalDynamicEntity { Id = 1, Code = "C002" }
        };
        using var factory = CreateFactory(fake);
        var fullTypeName = await DynamicEntityEndpointsTests_SeedDefinitionAsync(factory.Services);

        var client = await CreateAuthenticatedClientAsync(factory);
        var adminId = await GetAdminUserIdAsync(factory.Services);

        var response = await client.PutAsJsonAsync(
            $"/api/dynamic-entities/{fullTypeName}/1",
            new Dictionary<string, object>
            {
                ["UpdatedBy"] = "should-be-overwritten",
                ["UpdatedAt"] = "should-be-overwritten",
                ["Code"] = "C002"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(fake.LastUpdatedData);
        Assert.Equal(adminId, fake.LastUpdatedData!["UpdatedBy"]?.ToString());
        Assert.IsType<DateTime>(fake.LastUpdatedData["UpdatedAt"]);
    }

    [Fact]
    public async Task Update_WhenNotFound_ShouldReturn404()
    {
        var fake = new CapturingReflectionPersistenceService { UpdateResult = null };
        using var factory = CreateFactory(fake);
        var fullTypeName = await DynamicEntityEndpointsTests_SeedDefinitionAsync(factory.Services);

        var client = await CreateAuthenticatedClientAsync(factory);
        var response = await client.PutAsJsonAsync(
            $"/api/dynamic-entities/{fullTypeName}/999",
            new Dictionary<string, object> { ["Code"] = "X" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ShouldReturn404()
    {
        var fake = new CapturingReflectionPersistenceService { DeleteResult = false };
        using var factory = CreateFactory(fake);
        var fullTypeName = await DynamicEntityEndpointsTests_SeedDefinitionAsync(factory.Services);

        var client = await CreateAuthenticatedClientAsync(factory);
        var response = await client.DeleteAsync($"/api/dynamic-entities/{fullTypeName}/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(IReflectionPersistenceService persistence)
    {
        return new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll(typeof(IReflectionPersistenceService));
                services.AddSingleton(persistence);
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

    private static async Task<string> GetAdminUserIdAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var admin = await um.FindByNameAsync("admin");
        Assert.NotNull(admin);
        return admin!.Id;
    }

    private static async Task<string> DynamicEntityEndpointsTests_SeedDefinitionAsync(IServiceProvider services)
    {
        var method = typeof(DynamicEntityEndpointsTests)
            .GetMethod("SeedEntityDefinitionAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        var task = (Task<string>)method!.Invoke(null, new object[] { services })!;
        return await task;
    }

    private sealed class CapturingReflectionPersistenceService : IReflectionPersistenceService
    {
        public Dictionary<string, object>? LastCreatedData { get; private set; }
        public Dictionary<string, object>? LastUpdatedData { get; private set; }

        public object CreateResult { get; set; } = new LocalDynamicEntity { Id = 1, Code = "C001" };
        public object? UpdateResult { get; set; } = new LocalDynamicEntity { Id = 1, Code = "C002" };
        public bool DeleteResult { get; set; } = true;

        public Task<List<object>> QueryAsync(string fullTypeName, QueryOptions? options = null) =>
            Task.FromResult(new List<object>());

        public Task<object?> GetByIdAsync(string fullTypeName, int id) =>
            Task.FromResult<object?>(null);

        public Task<object> CreateAsync(string fullTypeName, Dictionary<string, object> data)
        {
            LastCreatedData = data;
            return Task.FromResult(CreateResult);
        }

        public Task<object?> UpdateAsync(string fullTypeName, int id, Dictionary<string, object> data)
        {
            LastUpdatedData = data;
            return Task.FromResult(UpdateResult);
        }

        public Task<bool> DeleteAsync(string fullTypeName, int id, string? deletedBy = null) =>
            Task.FromResult(DeleteResult);

        public Task<int> CountAsync(string fullTypeName, List<FilterCondition>? filters = null) =>
            Task.FromResult(0);

        public Task<List<Dictionary<string, object?>>> QueryRawAsync(string tableName, QueryOptions? options = null) =>
            Task.FromResult(new List<Dictionary<string, object?>>());
    }

    private sealed class LocalDynamicEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }
}
