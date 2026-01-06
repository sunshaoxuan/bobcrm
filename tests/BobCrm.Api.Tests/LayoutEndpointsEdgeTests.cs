using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BobCrm.Api.Base;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BobCrm.Api.Tests;

public class LayoutEndpointsEdgeTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public LayoutEndpointsEdgeTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    [Fact]
    public async Task GetFieldTags_WithInvalidTagsJson_ShouldIgnoreAndReturnOk()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.FieldDefinitions.Add(new FieldDefinition { Key = "A", DisplayName = "A", DataType = "String", Tags = "[\"x\",\"y\"]" });
            db.FieldDefinitions.Add(new FieldDefinition { Key = "B", DisplayName = "B", DataType = "String", Tags = "{not-json}" });
            await db.SaveChangesAsync();
        }

        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync("/api/fields/tags");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.ValueKind.Should().Be(JsonValueKind.Array);

        var tags = data.EnumerateArray().Select(e => e.GetProperty("tag").GetString()).ToList();
        tags.Should().Contain("x").And.Contain("y");
    }

    [Fact]
    public async Task DefaultLayoutScope_WhenNonAdmin_ShouldForbidSaveAndDelete()
    {
        var client = _factory.CreateClient();
        var (_, _, accessToken) = await client.CreateAndLoginUserAsync(_factory.Services);
        client.UseBearer(accessToken);

        var save = await client.PostAsJsonAsync("/api/layout?scope=default", new { v = 1 });
        save.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var delete = await client.DeleteAsync("/api/layout?scope=default");
        delete.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserLayout_ShouldCreateUpdateAndDelete()
    {
        var client = await CreateAdminClientAsync();

        var save1 = await client.PostAsJsonAsync("/api/layout", new { v = 1, widgets = new[] { "a" } });
        save1.StatusCode.Should().Be(HttpStatusCode.OK);

        var save2 = await client.PostAsJsonAsync("/api/layout", new { v = 2, widgets = new[] { "b" } });
        save2.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await client.GetAsync("/api/layout");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await get.Content.ReadAsStringAsync();
        content.Should().Contain("\"v\":2");

        var alias = await client.GetAsync("/api/layout/customer");
        alias.StatusCode.Should().Be(HttpStatusCode.OK);

        var del = await client.DeleteAsync("/api/layout");
        del.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
