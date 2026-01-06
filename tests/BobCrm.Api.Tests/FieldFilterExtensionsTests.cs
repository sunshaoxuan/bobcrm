using System.Security.Claims;
using System.Text.Json;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Services;
using BobCrm.Api.Utils;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BobCrm.Api.Tests;

public class FieldFilterExtensionsTests
{
    private static readonly IServiceProvider ResultServices = new ServiceCollection()
        .AddLogging()
        .AddOptions()
        .ConfigureHttpJsonOptions(_ => { })
        .BuildServiceProvider();

    private static ClaimsPrincipal CreateUser(string userId = "u1")
        => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test"));

    private static async Task<(int StatusCode, JsonElement? Json)> ExecuteJsonAsync(IResult result)
    {
        var http = new DefaultHttpContext();
        http.RequestServices = ResultServices;
        http.Response.Body = new MemoryStream();

        await result.ExecuteAsync(http);

        http.Response.Body.Position = 0;
        var body = await new StreamReader(http.Response.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            return (http.Response.StatusCode, null);
        }

        using var doc = JsonDocument.Parse(body);
        return (http.Response.StatusCode, doc.RootElement.Clone());
    }

    private static FieldFilterService CreateFilterService(Mock<IFieldPermissionService> permissionService)
    {
        var logger = new Mock<ILogger<FieldFilterService>>();
        return new FieldFilterService(permissionService.Object, logger.Object);
    }

    [Fact]
    public async Task FilteredOkAsync_WhenDataNull_ShouldReturnOkNull()
    {
        var permission = new Mock<IFieldPermissionService>();
        var service = CreateFilterService(permission);

        var result = await service.FilteredOkAsync(CreateUser(), "customer", data: null);

        var (status, json) = await ExecuteJsonAsync(result);
        status.Should().Be(StatusCodes.Status200OK);
        if (json != null)
        {
            json.Value.ValueKind.Should().Be(JsonValueKind.Null);
        }
    }

    [Fact]
    public async Task FilteredOkAsync_WhenReadableFieldsRestricted_ShouldFilterObject()
    {
        var permission = new Mock<IFieldPermissionService>();
        permission.Setup(p => p.GetReadableFieldsAsync("u1", "customer"))
            .ReturnsAsync(["a"]);

        var service = CreateFilterService(permission);

        var result = await service.FilteredOkAsync(CreateUser(), "customer", new { a = 1, b = 2 });

        var (status, json) = await ExecuteJsonAsync(result);
        status.Should().Be(StatusCodes.Status200OK);
        json.Should().NotBeNull();
        json!.Value.TryGetProperty("a", out var a).Should().BeTrue();
        a.GetInt32().Should().Be(1);
        json.Value.TryGetProperty("b", out _).Should().BeFalse();
    }

    [Fact]
    public async Task FilteredOkArrayAsync_WhenDataNullOrEmpty_ShouldReturnEmptyArray()
    {
        var permission = new Mock<IFieldPermissionService>();
        var service = CreateFilterService(permission);

        var resultNull = await service.FilteredOkArrayAsync(CreateUser(), "customer", data: (IEnumerable<object>?)null);
        var (statusNull, jsonNull) = await ExecuteJsonAsync(resultNull);
        statusNull.Should().Be(StatusCodes.Status200OK);
        jsonNull.Should().NotBeNull();
        jsonNull!.Value.ValueKind.Should().Be(JsonValueKind.Array);
        jsonNull.Value.GetArrayLength().Should().Be(0);

        var resultEmpty = await service.FilteredOkArrayAsync(CreateUser(), "customer", Array.Empty<object>());
        var (statusEmpty, jsonEmpty) = await ExecuteJsonAsync(resultEmpty);
        statusEmpty.Should().Be(StatusCodes.Status200OK);
        jsonEmpty.Should().NotBeNull();
        jsonEmpty!.Value.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task FilteredOkArrayAsync_WhenReadableFieldsRestricted_ShouldFilterEachObject()
    {
        var permission = new Mock<IFieldPermissionService>();
        permission.Setup(p => p.GetReadableFieldsAsync("u1", "customer"))
            .ReturnsAsync(["a"]);

        var service = CreateFilterService(permission);

        var result = await service.FilteredOkArrayAsync(CreateUser(), "customer", new[]
        {
            new { a = 1, b = 2 },
            new { a = 3, b = 4 }
        });

        var (status, json) = await ExecuteJsonAsync(result);
        status.Should().Be(StatusCodes.Status200OK);
        json.Should().NotBeNull();
        json!.Value.ValueKind.Should().Be(JsonValueKind.Array);
        json.Value.GetArrayLength().Should().Be(2);

        foreach (var item in json.Value.EnumerateArray())
        {
            item.TryGetProperty("a", out _).Should().BeTrue();
            item.TryGetProperty("b", out _).Should().BeFalse();
        }
    }

    [Fact]
    public async Task ValidateWritePermissionsAsync_WhenUnauthorizedFields_ShouldReturn403ErrorResult()
    {
        var permission = new Mock<IFieldPermissionService>();
        permission.Setup(p => p.GetWritableFieldsAsync("u1", "customer"))
            .ReturnsAsync(["allowed"]);

        var service = CreateFilterService(permission);

        var (isValid, error) = await service.ValidateWritePermissionsAsync(CreateUser(), "customer", new { allowed = 1, forbidden = 2 });

        isValid.Should().BeFalse();
        error.Should().NotBeNull();

        var (status, json) = await ExecuteJsonAsync(error!);
        status.Should().Be(StatusCodes.Status403Forbidden);
        json.Should().NotBeNull();
        json!.Value.GetProperty("unauthorizedFields").EnumerateArray().Select(x => x.GetString()).Should().Contain("forbidden");
    }

    [Fact]
    public async Task FilterWriteFieldsAsync_ShouldReturnWritableSubset()
    {
        var permission = new Mock<IFieldPermissionService>();
        permission.Setup(p => p.GetWritableFieldsAsync("u1", "customer"))
            .ReturnsAsync(["a"]);

        var service = CreateFilterService(permission);

        var filtered = await service.FilterWriteFieldsAsync(CreateUser(), "customer", new Dictionary<string, object?>
        {
            ["a"] = 1,
            ["b"] = 2
        });

        filtered.Should().ContainKey("a");
        filtered.Should().NotContainKey("b");
    }

    [Fact]
    public async Task ResultExtensions_OrElse_ShouldShortCircuitOnError()
    {
        var error = Results.BadRequest(new { code = "X" });
        var validation = (IsValid: false, ErrorResult: (IResult?)error);

        var result = validation.OrElse(() => Results.Ok(new { ok = true }));

        var (status, json) = await ExecuteJsonAsync(result);
        status.Should().Be(StatusCodes.Status400BadRequest);
        json.Should().NotBeNull();
        json!.Value.GetProperty("code").GetString().Should().Be("X");
    }

    [Fact]
    public async Task ResultExtensions_OrElseAsync_ShouldRunOnSuccess()
    {
        var validation = (IsValid: true, ErrorResult: (IResult?)Results.BadRequest());

        var result = await validation.OrElseAsync(() => Task.FromResult<IResult>(Results.Ok(new { ok = true })));

        var (status, json) = await ExecuteJsonAsync(result);
        status.Should().Be(StatusCodes.Status200OK);
        json.Should().NotBeNull();
        json!.Value.GetProperty("ok").GetBoolean().Should().BeTrue();
    }
}
