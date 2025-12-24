using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AntDesign;
using BobCrm.App.Components.Pages;
using BobCrm.App.Models;
using BobCrm.App.Services;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.JSInterop;
using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BobCrm.App.Tests;

public class TemplateBindingsTests : TestContext
{
    public TemplateBindingsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<string?>("localStorage.getItem", "accessToken").SetResult("token");

        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost") };
        var httpFactory = new SimpleHttpClientFactory(httpClient);

        Services.AddSingleton<IHttpClientFactory>(httpFactory);
        Services.AddScoped(sp => new AuthService(httpFactory, sp.GetRequiredService<IJSRuntime>()));
        Services.AddScoped(sp => new I18nService(httpFactory, sp.GetRequiredService<AuthService>(), sp.GetRequiredService<IJSRuntime>()));
        Services.AddLogging();
        Services.AddAntDesign();
    }

    [Fact]
    public void SwitchingToUserTemplateSelectsUserBindingAndSaves()
    {
        var bindingService = new StubTemplateBindingService();
        var runtimeClient = new StubTemplateRuntimeClient();

        Services.AddSingleton<TemplateBindingService>(bindingService);
        Services.AddSingleton<TemplateRuntimeClient>(runtimeClient);

        var cut = RenderComponent<TemplateBindings>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindAll("[data-testid^='template-option-']").Count);
        });

        Assert.True(cut.Find("[data-testid='template-option-1']").HasAttribute("checked"));
        Assert.Contains("active", cut.Find("[data-testid='binding-scope-system']").ClassList);

        cut.Find("[data-testid='binding-scope-user']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Find("[data-testid='template-option-2']").HasAttribute("checked"));
        });

        var functionInput = cut.Find("[data-testid='function-code-input']");
        Assert.Equal("USR.CUSTOM", functionInput.GetAttribute("value"));

        cut.Find("[data-testid='save-binding']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(bindingService.LastUpsert);
            Assert.False(bindingService.LastUpsert!.IsSystem);
            Assert.Equal(2, bindingService.LastUpsert.TemplateId);
        });

        Assert.NotEmpty(runtimeClient.Requests);
    }

    private sealed class StubTemplateBindingService : TemplateBindingService
    {
        private TemplateBindingDto? _binding;
        private readonly List<FormTemplate> _templates;

        public StubTemplateBindingService()
        {
            _templates = new List<FormTemplate>
            {
                new()
                {
                    Id = 1,
                    Name = "系统详情",
                    EntityType = "Customer",
                    UsageType = TemplateUsageType.Detail,
                    IsSystemDefault = true,
                    RequiredFunctionCode = "SYS.TEMPLATE.ASSIGN",
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = 2,
                    Name = "用户详情",
                    EntityType = "Customer",
                    UsageType = TemplateUsageType.Detail,
                    IsSystemDefault = false,
                    RequiredFunctionCode = "USR.CUSTOM",
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _binding = new TemplateBindingDto(
                Id: 100,
                EntityType: "Customer",
                UsageType: TemplateUsageType.Detail,
                TemplateId: 1,
                IsSystem: true,
                RequiredFunctionCode: "SYS.TEMPLATE.ASSIGN",
                UpdatedBy: "tester",
                UpdatedAt: DateTime.UtcNow);
        }

        public TemplateBindingDto? LastUpsert { get; private set; }

        public override Task<bool> HasAssignPermissionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public override Task<IReadOnlyList<FormTemplate>> GetTemplatesAsync(string? entityType, CancellationToken cancellationToken = default)
        {
            IEnumerable<FormTemplate> source = _templates;
            if (!string.IsNullOrWhiteSpace(entityType))
            {
                source = source.Where(t => string.Equals(t.EntityType, entityType, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult<IReadOnlyList<FormTemplate>>(source.ToList());
        }

        public override Task<TemplateBindingDto?> GetBindingAsync(string entityType, TemplateUsageType usageType, CancellationToken cancellationToken = default)
            => Task.FromResult(_binding);

        public override Task<TemplateBindingDto?> UpsertBindingAsync(string entityType, TemplateUsageType usageType, int templateId, bool isSystem, string? requiredFunctionCode, CancellationToken cancellationToken = default)
        {
            LastUpsert = new TemplateBindingDto(
                Id: 200,
                EntityType: entityType,
                UsageType: usageType,
                TemplateId: templateId,
                IsSystem: isSystem,
                RequiredFunctionCode: requiredFunctionCode,
                UpdatedBy: "tester",
                UpdatedAt: DateTime.UtcNow);

            _binding = LastUpsert;
            return Task.FromResult<TemplateBindingDto?>(_binding);
        }
    }

    private sealed class StubTemplateRuntimeClient : TemplateRuntimeClient
    {
        public List<(string EntityType, TemplateUsageType UsageType, int? EntityId)> Requests { get; } = new();

        public override Task<TemplateRuntimeResponse?> GetRuntimeAsync(
            string entityType,
            TemplateUsageType usageType,
            string? functionOverride = null,
            int? entityId = null,
            System.Text.Json.JsonElement? entityData = null,
            CancellationToken cancellationToken = default)
        {
            Requests.Add((entityType, usageType, entityId));
            var binding = new TemplateBindingDto(1, entityType, usageType, 1, true, "SYS.TEMPLATE.ASSIGN", "tester", DateTime.UtcNow);
            var template = new TemplateDescriptorDto(1, "系统详情", entityType, usageType, "{}", Array.Empty<string>(), null);
            return Task.FromResult<TemplateRuntimeResponse?>(new TemplateRuntimeResponse(binding, template, true, new[] { "scope" }));
        }
    }

    private sealed class SimpleHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public SimpleHttpClientFactory(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public HttpClient CreateClient(string name) => _httpClient;
    }
}
