using System.Net;
using System.Net.Http.Json;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Services;
using BobCrm.Application.Templates;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BobCrm.Api.Tests;

public class AdminEndpointsPhase10Tests
{
    [Fact]
    public async Task DebugUsers_ShouldReturnUserList()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/api/debug/users");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await resp.ReadDataAsJsonAsync();
        data.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
        data.EnumerateArray().Select(e => e.GetProperty("username").GetString()).Should().Contain("admin");
    }

    [Fact]
    public async Task DebugResetSetup_ShouldDeleteAdmin_ThenReportNotFound()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Lang", "zh");

        var first = await client.PostAsync("/api/debug/reset-setup", content: null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsync("/api/debug/reset-setup", content: null);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminResetPassword_ShouldReturnOk()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Lang", "zh");

        var resp = await client.PostAsJsonAsync("/api/admin/reset-password", new ResetPasswordDto("Admin@12345X"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegenerateDefaultTemplates_ShouldReturnUpdatedCount()
    {
        using var factory = new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDefaultTemplateService>();
                services.AddSingleton<IDefaultTemplateService>(new FakeDefaultTemplateService());
            });
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Lang", "zh");

        var resp = await client.PostAsync("/api/admin/templates/regenerate-defaults", content: null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await resp.ReadDataAsJsonAsync();
        data.GetProperty("updated").GetInt32().Should().BeGreaterThan(0);
        data.GetProperty("entities").GetInt32().Should().BeGreaterThan(0);
    }

    private sealed class FakeDefaultTemplateService : IDefaultTemplateService
    {
        public Task<DefaultTemplateGenerationResult> EnsureTemplatesAsync(
            EntityDefinition entityDefinition,
            string? updatedBy,
            bool force = false,
            CancellationToken ct = default)
        {
            var result = new DefaultTemplateGenerationResult();
            var template = new FormTemplate
            {
                Name = $"{entityDefinition.EntityRoute}-List",
                EntityType = entityDefinition.FullName,
                UserId = updatedBy ?? "system",
                UsageType = FormTemplateUsageType.List
            };

            result.Templates["List"] = template;
            result.Created.Add(template);
            return Task.FromResult(result);
        }

        public Task<FormTemplate> GetDefaultTemplateAsync(
            EntityDefinition entityDefinition,
            FormTemplateUsageType usageType,
            string? requestedBy = null,
            CancellationToken ct = default)
        {
            return Task.FromResult(new FormTemplate
            {
                Name = $"{entityDefinition.EntityRoute}-{usageType}",
                EntityType = entityDefinition.FullName,
                UserId = requestedBy ?? "system",
                UsageType = usageType
            });
        }
    }
}
