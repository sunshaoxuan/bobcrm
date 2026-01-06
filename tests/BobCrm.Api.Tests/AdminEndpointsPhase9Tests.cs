using System.Net;
using FluentAssertions;
using Xunit;

namespace BobCrm.Api.Tests;

public class AdminEndpointsPhase9Tests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public AdminEndpointsPhase9Tests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DbHealth_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/admin/db/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("provider").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RegenerateDefaultTemplatesForEntity_WhenEntityMissing_ShouldReturn404()
    {
        var response = await _client.PostAsync($"/api/admin/templates/{Guid.NewGuid():N}/regenerate", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("ENTITY_NOT_FOUND");
    }

    [Fact]
    public async Task ResetTemplates_WhenEntityMissing_ShouldReturn404WithDetails()
    {
        var response = await _client.PostAsync($"/api/admin/templates/{Guid.NewGuid():N}/reset", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("ENTITY_NOT_FOUND");
        root.TryGetProperty("details", out _).Should().BeTrue();
    }
}

