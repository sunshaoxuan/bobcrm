using System.Net;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using BobCrm.Application.Templates;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BobCrm.Api.Tests;

public class AdminEndpointsFinalSprintTests
{
    [Fact]
    public async Task RegenerateForEntity_ShouldReturnOk()
    {
        using var factory = new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDefaultTemplateService>();
                services.AddScoped<IDefaultTemplateService, DbBackedDefaultTemplateService>();
            });
        });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityRoute = await db.EntityDefinitions.Select(e => e.EntityRoute).FirstAsync(e => e != null);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Lang", "zh");

        var resp = await client.PostAsync($"/api/admin/templates/{entityRoute}/regenerate", content: null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ResetTemplatesForEntity_ShouldDeleteSystemTemplatesAndBindings_AndRecreate()
    {
        using var factory = new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDefaultTemplateService>();
                services.AddScoped<IDefaultTemplateService, DbBackedDefaultTemplateService>();
            });
        });

        string entityType;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            entityType = await db.EntityDefinitions.Select(e => e.EntityRoute).FirstAsync(e => e != null);

            var sysTemplate = new FormTemplate
            {
                Name = "Sys",
                EntityType = entityType,
                UserId = "admin",
                IsSystemDefault = true,
                UsageType = FormTemplateUsageType.List,
                LayoutJson = "{\"items\":{\"a\":1}}"
            };
            var userTemplate = new FormTemplate
            {
                Name = "User",
                EntityType = entityType,
                UserId = "user",
                IsUserDefault = true,
                UsageType = FormTemplateUsageType.List,
                LayoutJson = "{\"items\":{\"a\":1}}"
            };
            db.FormTemplates.AddRange(sysTemplate, userTemplate);
            await db.SaveChangesAsync();

            db.TemplateStateBindings.Add(new TemplateStateBinding
            {
                EntityType = entityType,
                ViewState = "List",
                TemplateId = sysTemplate.Id,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
            db.TemplateBindings.Add(new TemplateBinding
            {
                EntityType = entityType,
                UsageType = FormTemplateUsageType.List,
                TemplateId = sysTemplate.Id,
                IsSystem = true,
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedBy = "admin"
            });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Lang", "zh");

        var resp = await client.PostAsync($"/api/admin/templates/{entityType}/reset", content: null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());

        using var scope2 = factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db2.FormTemplates.CountAsync(t => t.EntityType == entityType)).Should().BeGreaterThan(0);
        (await db2.TemplateStateBindings.CountAsync(b => b.EntityType == entityType)).Should().Be(0);
    }

    [Fact]
    public async Task ResetAllTemplates_ShouldReturnOk()
    {
        using var factory = new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDefaultTemplateService>();
                services.AddSingleton<IDefaultTemplateService>(sp => new MinimalDefaultTemplateService());
            });
        });

        var client = factory.CreateClient();
        var resp = await client.PostAsync("/api/admin/templates/reset-all", content: null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class DbBackedDefaultTemplateService : IDefaultTemplateService
    {
        private readonly AppDbContext _db;

        public DbBackedDefaultTemplateService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<DefaultTemplateGenerationResult> EnsureTemplatesAsync(
            EntityDefinition entityDefinition,
            string? updatedBy,
            bool force = false,
            CancellationToken ct = default)
        {
            var result = new DefaultTemplateGenerationResult();
            var entityType = entityDefinition.EntityRoute;
            var existing = await _db.FormTemplates.FirstOrDefaultAsync(
                t => t.EntityType == entityType && t.IsSystemDefault,
                ct);

            if (existing != null)
            {
                existing.Name = $"{entityType}-List";
                existing.UserId = updatedBy ?? "system";
                existing.UsageType = FormTemplateUsageType.List;
                existing.LayoutJson = "{\"items\":{\"a\":1}}";
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                result.Templates["List"] = existing;
                result.Updated.Add(existing);
                return result;
            }

            var created = new FormTemplate
            {
                Name = $"{entityType}-List",
                EntityType = entityType,
                UserId = updatedBy ?? "system",
                IsSystemDefault = true,
                UsageType = FormTemplateUsageType.List,
                LayoutJson = "{\"items\":{\"a\":1}}"
            };
            _db.FormTemplates.Add(created);
            await _db.SaveChangesAsync(ct);
            result.Templates["List"] = created;
            result.Created.Add(created);
            return result;
        }

        public Task<FormTemplate> GetDefaultTemplateAsync(
            EntityDefinition entityDefinition,
            FormTemplateUsageType usageType,
            string? requestedBy = null,
            CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class MinimalDefaultTemplateService : IDefaultTemplateService
    {
        public Task<DefaultTemplateGenerationResult> EnsureTemplatesAsync(
            EntityDefinition entityDefinition,
            string? updatedBy,
            bool force = false,
            CancellationToken ct = default)
        {
            var result = new DefaultTemplateGenerationResult();
            result.Created.Add(new FormTemplate { Name = "X", EntityType = entityDefinition.EntityRoute, UserId = "system" });
            result.Templates["List"] = result.Created[0];
            return Task.FromResult(result);
        }

        public Task<FormTemplate> GetDefaultTemplateAsync(
            EntityDefinition entityDefinition,
            FormTemplateUsageType usageType,
            string? requestedBy = null,
            CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
