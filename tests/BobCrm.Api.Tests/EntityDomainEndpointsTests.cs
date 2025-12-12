using System.Net;
using System.Net.Http.Json;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.Responses.Entity;
using BobCrm.Api.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BobCrm.Api.Tests;

public class EntityDomainEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public EntityDomainEndpointsTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.UseBearer(accessToken);
        return client;
    }

    private async Task<Guid> SeedEntityDomainAsync(string? code = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var domain = new EntityDomain
        {
            Code = code ?? $"TEST_DOMAIN_{Guid.NewGuid():N}",
            Name = new Dictionary<string, string?>
            {
                ["zh"] = "测试领域",
                ["ja"] = "テスト領域",
                ["en"] = "Test Domain"
            },
            SortOrder = 999,
            IsSystem = false,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.EntityDomains.Add(domain);
        await db.SaveChangesAsync();
        return domain.Id;
    }

    [Fact]
    public async Task GetEntityDomains_WithoutLang_ReturnsTranslationsMode()
    {
        var domainId = await SeedEntityDomainAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/entity-domains");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var domains = await response.Content.ReadFromJsonAsync<List<EntityDomainDto>>();
        Assert.NotNull(domains);
        Assert.NotEmpty(domains);

        var domain = domains!.First(d => d.Id == domainId);
        Assert.Null(domain.Name);
        Assert.NotNull(domain.NameTranslations);
        Assert.Equal("测试领域", domain.NameTranslations!["zh"]);
        Assert.Equal("テスト領域", domain.NameTranslations!["ja"]);
    }

    [Fact]
    public async Task GetEntityDomains_WithoutLang_IgnoresAcceptLanguageHeader()
    {
        var domainId = await SeedEntityDomainAsync();
        var client = await CreateAuthenticatedClientAsync();
        client.DefaultRequestHeaders.Add("Accept-Language", "ja");

        var response = await client.GetAsync("/api/entity-domains");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var domains = await response.Content.ReadFromJsonAsync<List<EntityDomainDto>>();
        Assert.NotNull(domains);
        Assert.NotEmpty(domains);

        var domain = domains!.First(d => d.Id == domainId);
        Assert.Null(domain.Name);
        Assert.NotNull(domain.NameTranslations);
    }

    [Fact]
    public async Task GetEntityDomains_WithLang_ReturnsSingleLanguageMode()
    {
        var domainId = await SeedEntityDomainAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/entity-domains?lang=zh");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var domains = await response.Content.ReadFromJsonAsync<List<EntityDomainDto>>();
        Assert.NotNull(domains);
        Assert.NotEmpty(domains);

        var domain = domains!.First(d => d.Id == domainId);
        Assert.Equal("测试领域", domain.Name);
        Assert.Null(domain.NameTranslations);
    }

    [Fact]
    public async Task GetEntityDomainById_WithoutLang_ReturnsTranslationsMode()
    {
        var domainId = await SeedEntityDomainAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/entity-domains/{domainId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var domain = await response.Content.ReadFromJsonAsync<EntityDomainDto>();
        Assert.NotNull(domain);
        Assert.Equal(domainId, domain!.Id);
        Assert.Null(domain.Name);
        Assert.NotNull(domain.NameTranslations);
        Assert.Equal("测试领域", domain.NameTranslations!["zh"]);
    }

    [Fact]
    public async Task GetEntityDomainById_WithLang_ReturnsSingleLanguageMode()
    {
        var domainId = await SeedEntityDomainAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/entity-domains/{domainId}?lang=zh");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var domain = await response.Content.ReadFromJsonAsync<EntityDomainDto>();
        Assert.NotNull(domain);
        Assert.Equal(domainId, domain!.Id);
        Assert.Equal("测试领域", domain.Name);
        Assert.Null(domain.NameTranslations);
    }
}

