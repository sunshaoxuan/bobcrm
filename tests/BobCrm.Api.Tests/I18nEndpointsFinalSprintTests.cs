using System.Net;
using System.Net.Http.Headers;
using BobCrm.Api.Base;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BobCrm.Api.Tests;

public class I18nEndpointsFinalSprintTests
{
    private static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Lang", "zh");
        return client;
    }

    [Fact]
    public async Task I18n_VersionAndLanguages_ShouldReturnOk()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        (await client.GetAsync("/api/i18n/version")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/i18n/languages")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/i18n/zh")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task I18n_Resources_ShouldRequireAuth_AndSupportEtag304()
    {
        using var factory = new TestWebAppFactory();

        var anon = factory.CreateClient();
        (await anon.GetAsync("/api/i18n/resources")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var client = await CreateAuthenticatedClientAsync(factory);
        var resp = await client.GetAsync("/api/i18n/resources");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.ETag.Should().NotBeNull();

        var etag = resp.Headers.ETag!.Tag;
        var second = new HttpRequestMessage(HttpMethod.Get, "/api/i18n/resources");
        second.Headers.TryAddWithoutValidation("If-None-Match", etag);
        var resp2 = await client.SendAsync(second);
        resp2.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task I18n_LanguageDictionary_ShouldSupportEtag304()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/api/i18n/zh");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.ETag.Should().NotBeNull();

        var etag = resp.Headers.ETag!.Tag;
        var second = new HttpRequestMessage(HttpMethod.Get, "/api/i18n/zh");
        second.Headers.TryAddWithoutValidation("If-None-Match", etag);
        var resp2 = await client.SendAsync(second);
        resp2.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task I18n_WhenNoLanguagesConfigured_ShouldFallbackToEn()
    {
        using var factory = new TestWebAppFactory();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.LocalizationLanguages.RemoveRange(db.LocalizationLanguages);
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/i18n");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.ETag.Should().NotBeNull();
        resp.Headers.ETag!.Tag.Should().Contain("_en");
    }
}
