using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BobCrm.Api.Tests;

public class TemplateEndpointsCrudTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public TemplateEndpointsCrudTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    [Fact]
    public async Task GetTemplates_WithoutAuth_ShouldReturn401()
    {
        var resp = await _client.GetAsync("/api/templates");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task TemplateCrud_Copy_Apply_Effective_ShouldWork()
    {
        var client = await GetAuthenticatedClientAsync();
        var entityType = $"customer_{Guid.NewGuid():N}";

        // Create
        var create = await client.PostAsJsonAsync("/api/templates", new
        {
            name = "T1",
            entityType,
            isUserDefault = false,
            layoutJson = "{\"widgets\":[]}",
            description = "d"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.ReadDataAsJsonAsync();
        var templateId = created.GetProperty("id").GetInt32();
        Assert.True(templateId > 0);

        // Get
        var get = await client.GetAsync($"/api/templates/{templateId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        // Update
        var update = await client.PutAsJsonAsync($"/api/templates/{templateId}", new
        {
            name = "T1-Updated",
            entityType = (string?)null,
            isUserDefault = false,
            layoutJson = "{\"widgets\":[{\"type\":\"text\"}]}",
            description = "d2"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("T1-Updated", (await update.ReadDataAsJsonAsync()).GetProperty("name").GetString());

        // Copy
        var copy = await client.PostAsJsonAsync($"/api/templates/{templateId}/copy", new
        {
            name = "T1-Copy",
            entityType,
            usageType = 0,
            description = "copy"
        });
        Assert.Equal(HttpStatusCode.Created, copy.StatusCode);
        var copied = await copy.ReadDataAsJsonAsync();
        var copyId = copied.GetProperty("id").GetInt32();
        Assert.True(copyId > 0);
        Assert.NotEqual(templateId, copyId);

        // Apply copy as user default
        var apply = await client.PutAsync($"/api/templates/{copyId}/apply", content: null);
        Assert.Equal(HttpStatusCode.OK, apply.StatusCode);
        var applied = (await apply.ReadDataAsJsonAsync()).GetProperty("template");
        Assert.Equal(copyId, applied.GetProperty("id").GetInt32());
        Assert.True(applied.GetProperty("isUserDefault").GetBoolean());

        // Effective should return user default
        var effective = await client.GetAsync($"/api/templates/effective/{entityType}");
        Assert.Equal(HttpStatusCode.OK, effective.StatusCode);
        var eff = await effective.ReadDataAsJsonAsync();
        Assert.Equal(copyId, eff.GetProperty("id").GetInt32());

        // Delete original (not default, not in use)
        var del = await client.DeleteAsync($"/api/templates/{templateId}");
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);
    }

    [Fact]
    public async Task Apply_SystemTemplate_ShouldCreateUserCopyAndSetDefault()
    {
        var entityType = $"customer_{Guid.NewGuid():N}";

        int systemTemplateId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var systemTemplate = new FormTemplate
            {
                Name = "SYS-T",
                EntityType = entityType,
                UserId = "system",
                IsUserDefault = false,
                IsSystemDefault = true,
                UsageType = FormTemplateUsageType.Detail,
                LayoutJson = "{\"widgets\":[]}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.FormTemplates.Add(systemTemplate);
            await db.SaveChangesAsync();
            systemTemplateId = systemTemplate.Id;
        }

        var client = await GetAuthenticatedClientAsync();
        var apply = await client.PutAsync($"/api/templates/{systemTemplateId}/apply", content: null);
        if (apply.StatusCode != HttpStatusCode.OK)
        {
            var body = await apply.Content.ReadAsStringAsync();
            Assert.Fail($"Expected 200 OK but got {(int)apply.StatusCode} {apply.StatusCode}. Body: {body}");
        }

        var applied = (await apply.ReadDataAsJsonAsync()).GetProperty("template");
        Assert.True(applied.GetProperty("isUserDefault").GetBoolean());
        Assert.False(applied.GetProperty("isSystemDefault").GetBoolean());

        var effective = await client.GetAsync($"/api/templates/effective/{entityType}");
        Assert.Equal(HttpStatusCode.OK, effective.StatusCode);
        var eff = await effective.ReadDataAsJsonAsync();
        Assert.Equal(applied.GetProperty("id").GetInt32(), eff.GetProperty("id").GetInt32());
    }
}
