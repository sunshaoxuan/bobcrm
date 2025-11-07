using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BobCrm.Api.Tests;

/// <summary>
/// 集成测试示例 - 测试EntityDefinition和DynamicEntity的API端点
/// 注意：这些测试需要完整的应用程序上下文和数据库
/// </summary>
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact(Skip = "Integration test - requires full application context")]
    public async Task EntityDefinitions_GetAll_ShouldReturn200()
    {
        // Arrange
        var (accessToken, _) = await _client.LoginAsAdminAsync();
        _client.UseBearer(accessToken);

        // Act
        var response = await _client.GetAsync("/api/entity-definitions");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = "Integration test - requires full application context")]
    public async Task EntityDefinitions_Create_ShouldReturn201()
    {
        // Arrange
        var (accessToken, _) = await _client.LoginAsAdminAsync();
        _client.UseBearer(accessToken);

        var createRequest = new
        {
            @namespace = "BobCrm.Test",
            entityName = "TestProduct",
            displayNameKey = "ENTITY_TEST_PRODUCT",
            structureType = "Single",
            interfaces = new[] { "Base", "Archive" },
            fields = new[]
            {
                new
                {
                    propertyName = "Price",
                    displayNameKey = "FIELD_PRICE",
                    dataType = "Decimal",
                    precision = 10,
                    scale = 2,
                    isRequired = true,
                    sortOrder = 10
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/entity-definitions", createRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
    }

    [Fact(Skip = "Integration test - requires full application context")]
    public async Task EntityDefinitions_PublishNewEntity_ShouldReturn200()
    {
        // This test would require:
        // 1. Creating an entity definition
        // 2. Getting its ID
        // 3. Publishing it
        // 4. Verifying the table was created in the database

        // Arrange
        var (accessToken, _) = await _client.LoginAsAdminAsync();
        _client.UseBearer(accessToken);

        // Create entity first (implementation omitted for brevity)
        var entityId = Guid.NewGuid(); // Would come from create response

        // Act
        var response = await _client.PostAsync($"/api/entity-definitions/{entityId}/publish", null);

        // Assert
        // Would verify successful publish
    }

    [Fact(Skip = "Integration test - requires full application context")]
    public async Task EntityDefinitions_GenerateCode_ShouldReturnCode()
    {
        // Arrange
        var (accessToken, _) = await _client.LoginAsAdminAsync();
        _client.UseBearer(accessToken);

        var entityId = Guid.NewGuid(); // Would come from a published entity

        // Act
        var response = await _client.GetAsync($"/api/entity-definitions/{entityId}/generate-code");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CodeGenerationResponse>();
        result.Should().NotBeNull();
        result!.Code.Should().NotBeNullOrEmpty();
        result.Code.Should().Contain("public class");
    }

    [Fact(Skip = "Integration test - requires full application context")]
    public async Task EntityDefinitions_Compile_ShouldReturnSuccess()
    {
        // Arrange
        var (accessToken, _) = await _client.LoginAsAdminAsync();
        _client.UseBearer(accessToken);

        var entityId = Guid.NewGuid(); // Would come from a published entity

        // Act
        var response = await _client.PostAsync($"/api/entity-definitions/{entityId}/compile", null);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CompilationResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.LoadedTypes.Should().NotBeEmpty();
    }

    [Fact(Skip = "Integration test - requires full application context")]
    public async Task DynamicEntities_Query_ShouldReturnData()
    {
        // Arrange
        var (accessToken, _) = await _client.LoginAsAdminAsync();
        _client.UseBearer(accessToken);

        var fullTypeName = "BobCrm.Test.Product"; // Would be a compiled entity
        var queryRequest = new
        {
            filters = new object[] { },
            orderBy = "Id",
            orderByDescending = false,
            skip = 0,
            take = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/dynamic-entities/{fullTypeName}/query",
            queryRequest
        );

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<QueryResponse>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
    }

    [Fact(Skip = "Integration test - requires full application context")]
    public async Task DynamicEntities_Create_ShouldReturn201()
    {
        // Arrange
        var (accessToken, _) = await _client.LoginAsAdminAsync();
        _client.UseBearer(accessToken);

        var fullTypeName = "BobCrm.Test.Product";
        var createData = new Dictionary<string, object>
        {
            ["code"] = "P001",
            ["name"] = "Test Product",
            ["price"] = 99.99
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/dynamic-entities/{fullTypeName}",
            createData
        );

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
    }

    [Fact(Skip = "Integration test - requires full application context")]
    public async Task DynamicEntities_Update_ShouldReturn200()
    {
        // Arrange
        var (accessToken, _) = await _client.LoginAsAdminAsync();
        _client.UseBearer(accessToken);

        var fullTypeName = "BobCrm.Test.Product";
        var entityId = 1; // Would be ID of an existing entity
        var updateData = new Dictionary<string, object>
        {
            ["price"] = 149.99
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/dynamic-entities/{fullTypeName}/{entityId}",
            updateData
        );

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact(Skip = "Integration test - requires full application context")]
    public async Task DynamicEntities_Delete_ShouldReturn204()
    {
        // Arrange
        var (accessToken, _) = await _client.LoginAsAdminAsync();
        _client.UseBearer(accessToken);

        var fullTypeName = "BobCrm.Test.Product";
        var entityId = 1; // Would be ID of an existing entity

        // Act
        var response = await _client.DeleteAsync(
            $"/api/dynamic-entities/{fullTypeName}/{entityId}"
        );

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    [Fact(Skip = "Integration test - requires full application context")]
    public async Task FullWorkflow_CreatePublishCompileAndQuery_ShouldSucceed()
    {
        // This test demonstrates the complete workflow:
        // 1. Create entity definition
        // 2. Publish (create table)
        // 3. Compile (load into memory)
        // 4. Create data
        // 5. Query data

        // Arrange
        var (accessToken, _) = await _client.LoginAsAdminAsync();
        _client.UseBearer(accessToken);

        // Step 1: Create entity definition
        var createEntityRequest = new
        {
            @namespace = "BobCrm.Test",
            entityName = "WorkflowTest",
            displayNameKey = "ENTITY_WORKFLOW_TEST",
            structureType = "Single",
            interfaces = new[] { "Base", "Archive" },
            fields = new[]
            {
                new
                {
                    propertyName = "Value",
                    displayNameKey = "FIELD_VALUE",
                    dataType = "Integer",
                    isRequired = true,
                    sortOrder = 10
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/entity-definitions", createEntityRequest);
        createResponse.EnsureSuccessStatusCode();

        // Get entity ID from response
        // var entityId = ...; (parse from response)

        // Step 2: Publish entity
        // var publishResponse = await _client.PostAsync($"/api/entity-definitions/{entityId}/publish", null);
        // publishResponse.EnsureSuccessStatusCode();

        // Step 3: Compile entity
        // var compileResponse = await _client.PostAsync($"/api/entity-definitions/{entityId}/compile", null);
        // compileResponse.EnsureSuccessStatusCode();

        // Step 4: Create data
        // Step 5: Query data

        // This is a demonstration of the workflow structure
        // Actual implementation would need proper ID extraction and error handling
    }

    // Response DTOs for testing
    private record CodeGenerationResponse(string Code);
    private record CompilationResponse(bool Success, List<string> LoadedTypes);
    private record QueryResponse(List<object> Data, int Total);
}
