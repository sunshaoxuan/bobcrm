using System.Net.Http.Headers;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.Requests.Template;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using BobCrm.Application.Templates;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace BobCrm.Api.Tests;

public class TemplateRuntimeServiceTests
{
    private static WebApplicationFactory<Program> CreateFactory(
        Mock<IDefaultTemplateService> defaultTemplates,
        Mock<IReflectionPersistenceService> persistence)
        => new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll(typeof(IDefaultTemplateService));
                services.RemoveAll(typeof(IReflectionPersistenceService));
                services.AddSingleton(defaultTemplates.Object);
                services.AddSingleton(persistence.Object);
            });
        });

    private static async Task<(HttpClient client, string userId)> CreateAuthenticatedClientAndUserIdAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var scope = factory.Services.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
        var admin = await um.FindByNameAsync("admin");
        admin.Should().NotBeNull();
        return (client, admin!.Id);
    }

    [Fact]
    public async Task BuildRuntimeContextAsync_WhenBindingExists_ShouldReturnContext()
    {
        var defaultTemplates = new Mock<IDefaultTemplateService>(MockBehavior.Strict);
        defaultTemplates
            .Setup(s => s.EnsureTemplatesAsync(It.IsAny<EntityDefinition>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DefaultTemplateGenerationResult());
        defaultTemplates
            .Setup(s => s.GetDefaultTemplateAsync(It.IsAny<EntityDefinition>(), It.IsAny<FormTemplateUsageType>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException());

        var persistence = new Mock<IReflectionPersistenceService>(MockBehavior.Strict);

        using var factory = CreateFactory(defaultTemplates, persistence);
        var (_, userId) = await CreateAuthenticatedClientAndUserIdAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var template = new FormTemplate
            {
                Name = "RuntimeTemplate",
                EntityType = "customer",
                UserId = "system",
                LayoutJson = "{\"a\":1}",
                UsageType = FormTemplateUsageType.Detail
            };
            db.FormTemplates.Add(template);
            await db.SaveChangesAsync();

            db.TemplateBindings.Add(new TemplateBinding
            {
                EntityType = "customer",
                UsageType = FormTemplateUsageType.Detail,
                TemplateId = template.Id,
                IsSystem = false,
                UpdatedBy = "system",
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var runtime = scope.ServiceProvider.GetRequiredService<TemplateRuntimeService>();

            var response = await runtime.BuildRuntimeContextAsync(
                userId,
                "customer",
                new TemplateRuntimeRequest(UsageType: FormTemplateUsageType.Detail));

            response.Binding.EntityType.Should().Be("customer");
            response.Template.LayoutJson.Should().Contain("\"a\":1");
        }
    }

    [Fact]
    public async Task BuildRuntimeContextAsync_WhenBindingMissing_ShouldAttemptRegenerate()
    {
        var defaultTemplates = new Mock<IDefaultTemplateService>(MockBehavior.Strict);
        defaultTemplates
            .Setup(s => s.EnsureTemplatesAsync(It.IsAny<EntityDefinition>(), It.IsAny<string?>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DefaultTemplateGenerationResult());
        defaultTemplates
            .Setup(s => s.GetDefaultTemplateAsync(It.IsAny<EntityDefinition>(), It.IsAny<FormTemplateUsageType>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException());

        var persistence = new Mock<IReflectionPersistenceService>(MockBehavior.Strict);

        using var factory = CreateFactory(defaultTemplates, persistence);
        var (_, userId) = await CreateAuthenticatedClientAndUserIdAsync(factory);

        var entityType = $"phase8_{Guid.NewGuid():N}";

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.EntityDefinitions.Add(new EntityDefinition
            {
                Namespace = "BobCrm.Tests.Dynamic",
                EntityName = entityType,
                EntityRoute = entityType,
                FullTypeName = $"BobCrm.Tests.Dynamic.{entityType}",
                ApiEndpoint = $"/api/{entityType}",
                Status = EntityStatus.Published,
                StructureType = EntityStructureType.Single,
                IsEnabled = true
            });
            await db.SaveChangesAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var runtime = scope.ServiceProvider.GetRequiredService<TemplateRuntimeService>();

            var act = async () => await runtime.BuildRuntimeContextAsync(
                userId,
                entityType,
                new TemplateRuntimeRequest(UsageType: FormTemplateUsageType.Detail));

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Template binding not found*");
        }

    }

    [Fact]
    public async Task BuildRuntimeContextAsync_WhenPolymorphicBindingMatches_ShouldSwitchTemplate()
    {
        var defaultTemplates = new Mock<IDefaultTemplateService>(MockBehavior.Loose);
        defaultTemplates
            .Setup(s => s.EnsureTemplatesAsync(It.IsAny<EntityDefinition>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DefaultTemplateGenerationResult());

        var persistence = new Mock<IReflectionPersistenceService>(MockBehavior.Strict);

        using var factory = CreateFactory(defaultTemplates, persistence);
        var (_, userId) = await CreateAuthenticatedClientAndUserIdAsync(factory);

        int baseTemplateId;
        int polymorphicTemplateId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var baseTemplate = new FormTemplate
            {
                Name = "Base",
                EntityType = "customer",
                UserId = "system",
                LayoutJson = "{\"a\":1}",
                UsageType = FormTemplateUsageType.List
            };
            var polyTemplate = new FormTemplate
            {
                Name = "Poly",
                EntityType = "customer",
                UserId = "system",
                LayoutJson = "{\"b\":1}",
                UsageType = FormTemplateUsageType.List
            };

            db.FormTemplates.AddRange(baseTemplate, polyTemplate);
            await db.SaveChangesAsync();
            baseTemplateId = baseTemplate.Id;
            polymorphicTemplateId = polyTemplate.Id;

            db.TemplateBindings.Add(new TemplateBinding
            {
                EntityType = "customer",
                UsageType = FormTemplateUsageType.List,
                TemplateId = baseTemplateId,
                IsSystem = false,
                UpdatedBy = "system",
                UpdatedAt = DateTime.UtcNow
            });

            db.TemplateStateBindings.Add(new TemplateStateBinding
            {
                EntityType = "customer",
                ViewState = "List",
                TemplateId = baseTemplateId,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-1)
            });
            db.TemplateStateBindings.Add(new TemplateStateBinding
            {
                EntityType = "customer",
                ViewState = "List",
                TemplateId = polymorphicTemplateId,
                IsDefault = false,
                MatchFieldName = "Status",
                MatchFieldValue = "Draft",
                Priority = 10,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var runtime = scope.ServiceProvider.GetRequiredService<TemplateRuntimeService>();

            var response = await runtime.BuildRuntimeContextAsync(
                userId,
                "customer",
                new TemplateRuntimeRequest(
                    UsageType: FormTemplateUsageType.List,
                    EntityData: System.Text.Json.JsonSerializer.SerializeToElement(new { Status = "Draft" })));

            response.Binding.TemplateId.Should().Be(polymorphicTemplateId);
            response.Template.Id.Should().Be(polymorphicTemplateId);
        }
    }

    [Fact]
    public async Task BuildRuntimeContextAsync_WhenFunctionOverrideDenied_ShouldThrowUnauthorizedAccess()
    {
        var defaultTemplates = new Mock<IDefaultTemplateService>(MockBehavior.Loose);
        defaultTemplates
            .Setup(s => s.EnsureTemplatesAsync(It.IsAny<EntityDefinition>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DefaultTemplateGenerationResult());

        var persistence = new Mock<IReflectionPersistenceService>(MockBehavior.Strict);

        using var factory = CreateFactory(defaultTemplates, persistence);
        var (_, userId) = await CreateAuthenticatedClientAndUserIdAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var template = new FormTemplate
            {
                Name = "Template",
                EntityType = "customer",
                UserId = "system",
                LayoutJson = "{\"a\":1}",
                UsageType = FormTemplateUsageType.Detail
            };
            db.FormTemplates.Add(template);
            await db.SaveChangesAsync();

            db.TemplateBindings.Add(new TemplateBinding
            {
                EntityType = "customer",
                UsageType = FormTemplateUsageType.Detail,
                TemplateId = template.Id,
                IsSystem = false,
                UpdatedBy = "system",
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var runtime = scope.ServiceProvider.GetRequiredService<TemplateRuntimeService>();

            var act = async () => await runtime.BuildRuntimeContextAsync(
                userId,
                "customer",
                new TemplateRuntimeRequest(UsageType: FormTemplateUsageType.Detail, FunctionCodeOverride: "NO.SUCH.PERMISSION"));

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }
    }
}
