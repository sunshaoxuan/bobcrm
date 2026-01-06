using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Endpoints;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace BobCrm.Api.Tests;

public class FieldPermissionEndpointsTests
{
    private static WebApplicationFactory<Program> CreateFactory(Mock<IFieldPermissionService> service)
        => new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll(typeof(IFieldPermissionService));
                services.AddSingleton(service.Object);
            });
        });

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    [Fact]
    public async Task ReadableFields_WithoutAuth_ShouldReturn401()
    {
        var service = new Mock<IFieldPermissionService>(MockBehavior.Strict);
        using var factory = CreateFactory(service);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/field-permissions/user/entity/customer/readable-fields");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReadableFields_WithAuth_ShouldReturnList()
    {
        var service = new Mock<IFieldPermissionService>();
        service.Setup(s => s.GetReadableFieldsAsync(It.IsAny<string>(), "customer"))
            .ReturnsAsync(["a", "b"]);

        using var factory = CreateFactory(service);
        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/field-permissions/user/entity/customer/readable-fields");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
        data.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task CanRead_WithAuth_ShouldReturnAllowedFlag()
    {
        var service = new Mock<IFieldPermissionService>();
        service.Setup(s => s.CanUserReadFieldAsync(It.IsAny<string>(), "customer", "Code"))
            .ReturnsAsync(true);

        using var factory = CreateFactory(service);
        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/field-permissions/user/entity/customer/field/Code/can-read");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("allowed").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpsertPermission_WithAuth_ShouldReturnPermission()
    {
        var roleId = Guid.NewGuid();
        var service = new Mock<IFieldPermissionService>();
        service.Setup(s => s.UpsertPermissionAsync(roleId, "customer", "Code", true, false, "r", It.IsAny<string>()))
            .ReturnsAsync(new FieldPermission
            {
                Id = 123,
                RoleId = roleId,
                EntityType = "customer",
                FieldName = "Code",
                CanRead = true,
                CanWrite = false
            });

        using var factory = CreateFactory(service);
        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsJsonAsync($"/api/field-permissions/role/{roleId}/entity/customer/field/Code",
            new UpsertFieldPermissionRequest(true, false, "r"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("id").GetInt32().Should().Be(123);
        data.GetProperty("fieldName").GetString().Should().Be("Code");
    }
}
