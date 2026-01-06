using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.Requests.Template;
using BobCrm.Api.Contracts.Responses.Template;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace BobCrm.Api.Tests;

public class TemplateEndpointsFinalSprintTests
{
    private static WebApplicationFactory<Program> CreateFactory(Mock<ITemplateService>? templateService = null)
        => new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                if (templateService != null)
                {
                    services.RemoveAll(typeof(ITemplateService));
                    services.AddSingleton(templateService.Object);
                }
            });
        });

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Lang", "zh");
        return client;
    }

    [Fact]
    public async Task GetTemplate_WhenNotFound_ShouldReturn404()
    {
        var service = new Mock<ITemplateService>(MockBehavior.Strict);
        service.Setup(s => s.GetTemplateByIdAsync(999, It.IsAny<string>()))
            .ReturnsAsync((FormTemplate?)null);

        using var factory = CreateFactory(service);
        var client = await CreateAuthenticatedClientAsync(factory);

        var resp = await client.GetAsync("/api/templates/999");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTemplates_ShouldReturnOk()
    {
        var service = new Mock<ITemplateService>(MockBehavior.Strict);
        service.Setup(s => s.GetTemplatesAsync(It.IsAny<string>(), "customer", null, null, "none"))
            .ReturnsAsync(new TemplateQueryResponseDto
            {
                GroupBy = "none",
                Items =
                [
                    new TemplateSummaryDto
                    {
                        Id = 1,
                        Name = "T",
                        EntityType = "customer",
                        UsageType = FormTemplateUsageType.List
                    }
                ]
            });

        using var factory = CreateFactory(service);
        var client = await CreateAuthenticatedClientAsync(factory);

        var resp = await client.GetAsync("/api/templates?entityType=customer&groupBy=none");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateTemplate_ShouldReturn201()
    {
        var service = new Mock<ITemplateService>(MockBehavior.Strict);
        service.Setup(s => s.CreateTemplateAsync(It.IsAny<string>(), It.IsAny<CreateTemplateRequest>()))
            .ReturnsAsync(new FormTemplate { Id = 123, Name = "New", EntityType = "customer", LayoutJson = "{\"a\":1}" });

        using var factory = CreateFactory(service);
        var client = await CreateAuthenticatedClientAsync(factory);

        var resp = await client.PostAsJsonAsync("/api/templates",
            new CreateTemplateRequest("New", "customer", false, "{\"a\":1}", null));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task DeleteTemplate_ShouldReturnOk()
    {
        var service = new Mock<ITemplateService>(MockBehavior.Strict);
        service.Setup(s => s.DeleteTemplateAsync(123, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        using var factory = CreateFactory(service);
        var client = await CreateAuthenticatedClientAsync(factory);

        var resp = await client.DeleteAsync("/api/templates/123");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetEffectiveTemplate_WhenNotFound_ShouldReturn404()
    {
        var service = new Mock<ITemplateService>(MockBehavior.Strict);
        service.Setup(s => s.GetEffectiveTemplateAsync("customer", It.IsAny<string>()))
            .ReturnsAsync((FormTemplate?)null);

        using var factory = CreateFactory(service);
        var client = await CreateAuthenticatedClientAsync(factory);

        var resp = await client.GetAsync("/api/templates/effective/customer");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CopyTemplate_ShouldReturn201()
    {
        var service = new Mock<ITemplateService>(MockBehavior.Strict);
        service.Setup(s => s.CopyTemplateAsync(1, It.IsAny<string>(), It.IsAny<CopyTemplateRequest>()))
            .ReturnsAsync(new FormTemplate { Id = 2, Name = "Copy", EntityType = "customer", LayoutJson = "{\"a\":1}" });

        using var factory = CreateFactory(service);
        var client = await CreateAuthenticatedClientAsync(factory);

        var resp = await client.PostAsJsonAsync("/api/templates/1/copy", new CopyTemplateRequest("Copy", null, null, null));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ApplyTemplate_ShouldReturnOk()
    {
        var service = new Mock<ITemplateService>(MockBehavior.Strict);
        service.Setup(s => s.ApplyTemplateAsync(1, It.IsAny<string>()))
            .ReturnsAsync(new FormTemplate { Id = 1, Name = "T", EntityType = "customer", IsUserDefault = true, LayoutJson = "{\"a\":1}" });

        using var factory = CreateFactory(service);
        var client = await CreateAuthenticatedClientAsync(factory);

        var resp = await client.PutAsync("/api/templates/1/apply", content: null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TemplateBindings_WhenMissing_ShouldReturn404()
    {
        using var factory = CreateFactory();
        var client = await CreateAuthenticatedClientAsync(factory);

        var resp = await client.GetAsync("/api/templates/bindings/no-such-entity?usageType=List");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpsertTemplateBinding_ShouldReturnOk()
    {
        using var factory = CreateFactory();

        string entityType;
        int templateId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var template = new FormTemplate
            {
                Name = "T",
                EntityType = "customer",
                UserId = "admin",
                IsSystemDefault = false,
                IsUserDefault = false,
                UsageType = FormTemplateUsageType.List,
                LayoutJson = "{\"items\":{\"a\":1}}"
            };
            db.FormTemplates.Add(template);
            await db.SaveChangesAsync();
            entityType = template.EntityType!;
            templateId = template.Id;
        }

        var client = await CreateAuthenticatedClientAsync(factory);
        var resp = await client.PutAsJsonAsync("/api/templates/bindings",
            new UpsertTemplateBindingRequest(entityType, FormTemplateUsageType.List, templateId, true, null));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope2 = factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db2.TemplateBindings.CountAsync(b => b.EntityType == entityType && b.UsageType == FormTemplateUsageType.List))
            .Should().BeGreaterThan(0);
    }
}
