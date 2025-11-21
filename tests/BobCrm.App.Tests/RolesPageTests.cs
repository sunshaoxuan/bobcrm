using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AntDesign;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using BobCrm.App.Components.Pages;
using BobCrm.App.Models;
using BobCrm.App.Services;
using BobCrm.App.Services.Multilingual;
using BobCrm.App.Services.Widgets.Rendering;

namespace BobCrm.App.Tests;

public class RolesPageTests : TestContext
{
    private readonly FakeRoleService _roleService = new();

    public RolesPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<string?>("localStorage.getItem", "accessToken").SetResult("token");

        var httpClient = new HttpClient(new PassthroughHandler())
        {
            BaseAddress = new Uri("http://localhost")
        };
        var httpFactory = new SimpleHttpClientFactory(httpClient);

        Services.AddSingleton<IHttpClientFactory>(httpFactory);
        Services.AddScoped(sp => new AuthService(httpFactory, sp.GetRequiredService<IJSRuntime>()));
        Services.AddScoped(sp => new I18nService(httpFactory, sp.GetRequiredService<AuthService>(), sp.GetRequiredService<IJSRuntime>()));
        Services.AddSingleton<ILanguageContext>(new TestLanguageContext());
        Services.AddLogging();
        Services.AddScoped<IMultilingualTextResolver, MultilingualTextResolver>();
        Services.AddAntDesign();
        Services.AddScoped<NavigationManager, FakeNavigationManager>();
        Services.AddScoped<IRoleService>(_ => _roleService);
        Services.AddScoped<IRuntimeWidgetRenderer, RuntimeWidgetRenderer>();
    }

    [Fact]
    public void LoadFunctionTree_RendersPermissionNodes()
    {
        var cut = RenderComponent<Roles>();
        SelectFirstRole(cut);

        cut.WaitForAssertion(() =>
        {
            var permissionTree = cut.Find("[data-testid='permission-tree']");
            Assert.Contains("Customers", permissionTree.TextContent);
        });
    }

    [Fact]
    public void SelectingTemplateAndSavingPermissions_PersistsTemplateBinding()
    {
        var cut = RenderComponent<Roles>();
        SelectFirstRole(cut);

        var dropdown = cut.Find("select.role-template-dropdown");
        var selectedBinding = _roleService.TemplateOptions.Last().BindingId;
        dropdown.Change(selectedBinding.ToString());

        cut.Find("[data-testid='save-permissions-button']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(_roleService.RoleId, _roleService.LastPermissionsRoleId);
            Assert.NotNull(_roleService.LastPermissionsRequest);
            var selection = _roleService.LastPermissionsRequest!.FunctionPermissions
                .Single(fp => fp.FunctionId == _roleService.FunctionId);
            Assert.Equal(selectedBinding, selection.TemplateBindingId);
        });
    }

    private static void SelectFirstRole(IRenderedComponent<Roles> cut)
    {
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".role-card")));
        cut.Find(".role-card").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".permission-node")));
    }

    private sealed class FakeRoleService : IRoleService
    {
        private readonly List<RoleProfileDto> _roles;
        private readonly RoleProfileDto _roleDetail;
        private readonly List<FunctionMenuNode> _tree;
        private readonly Guid _roleId;

        public Guid RoleId => _roleId;
        public Guid FunctionId { get; } = Guid.NewGuid();
        public IReadOnlyList<FunctionTemplateOption> TemplateOptions { get; }
        public UpdatePermissionsRequestDto? LastPermissionsRequest { get; private set; }
        public Guid? LastPermissionsRoleId { get; private set; }
        private const string TreeVersion = "v1";

        public FakeRoleService()
        {
            _roleId = Guid.NewGuid();
            TemplateOptions = new List<FunctionTemplateOption>
            {
                new()
                {
                    BindingId = 10,
                    TemplateId = 1,
                    TemplateName = "Default",
                    EntityType = "Customer",
                    UsageType = TemplateUsageType.Detail,
                    IsDefault = true
                },
                new()
                {
                    BindingId = 20,
                    TemplateId = 2,
                    TemplateName = "Custom",
                    EntityType = "Customer",
                    UsageType = TemplateUsageType.Detail
                }
            };

            _tree = new List<FunctionMenuNode>
            {
                new()
                {
                    Id = FunctionId,
                    Code = "CRM.CUSTOMERS",
                    Name = "Customers",
                    IsMenu = true,
                    TemplateOptions = TemplateOptions.ToList(),
                    Children = new List<FunctionMenuNode>()
                }
            };

            _roleDetail = new RoleProfileDto
            {
                Id = _roleId,
                Code = "CRM.MANAGER",
                Name = "CRM Manager",
                Description = "Manages customer permissions",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Functions = new List<RoleFunctionDto>
                {
                    new()
                    {
                        RoleId = _roleId,
                        FunctionId = FunctionId,
                        TemplateBindingId = null
                    }
                },
                DataScopes = new List<RoleDataScopeDto>()
            };

            _roles = new List<RoleProfileDto>
            {
                new()
                {
                    Id = _roleId,
                    Code = _roleDetail.Code,
                    Name = _roleDetail.Name,
                    Description = _roleDetail.Description,
                    IsEnabled = _roleDetail.IsEnabled,
                    CreatedAt = _roleDetail.CreatedAt,
                    UpdatedAt = _roleDetail.UpdatedAt,
                    Functions = new List<RoleFunctionDto>(),
                    DataScopes = new List<RoleDataScopeDto>()
                }
            };
        }

        public Task<List<RoleProfileDto>> GetRolesAsync(CancellationToken ct = default)
            => Task.FromResult(_roles.Select(CloneRole).ToList());

        public Task<RoleProfileDto?> GetRoleAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(id == _roleDetail.Id ? CloneRole(_roleDetail) : null);

        public Task<RoleProfileDto?> CreateRoleAsync(CreateRoleRequestDto request, CancellationToken ct = default)
            => Task.FromResult<RoleProfileDto?>(null);

        public Task<bool> UpdateRoleAsync(Guid id, UpdateRoleRequestDto request, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<bool> UpdatePermissionsAsync(Guid id, UpdatePermissionsRequestDto request, CancellationToken ct = default)
        {
            LastPermissionsRoleId = id;
            LastPermissionsRequest = request;
            return Task.FromResult(true);
        }

        public Task<FunctionTreeResponse> GetFunctionTreeAsync(bool forceRefresh = false, CancellationToken ct = default)
            => Task.FromResult(new FunctionTreeResponse(_tree.Select(CloneNode).ToList(), TreeVersion));

        public Task<string?> GetFunctionTreeVersionAsync(CancellationToken ct = default)
            => Task.FromResult<string?>(TreeVersion);

        public void InvalidateFunctionTreeCache()
        {
        }

        private static RoleProfileDto CloneRole(RoleProfileDto source) => new()
        {
            Id = source.Id,
            Code = source.Code,
            Name = source.Name,
            Description = source.Description,
            IsEnabled = source.IsEnabled,
            IsSystem = source.IsSystem,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            Functions = source.Functions.Select(f => new RoleFunctionDto
            {
                RoleId = f.RoleId,
                FunctionId = f.FunctionId,
                TemplateBindingId = f.TemplateBindingId
            }).ToList(),
            DataScopes = source.DataScopes.Select(ds => new RoleDataScopeDto
            {
                Id = ds.Id,
                EntityName = ds.EntityName,
                ScopeType = ds.ScopeType,
                FilterExpression = ds.FilterExpression
            }).ToList()
        };

        private static FunctionMenuNode CloneNode(FunctionMenuNode node) => new()
        {
            Id = node.Id,
            Code = node.Code,
            Name = node.Name,
            IsMenu = node.IsMenu,
            TemplateOptions = node.TemplateOptions.Select(option => new FunctionTemplateOption
            {
                BindingId = option.BindingId,
                TemplateId = option.TemplateId,
                TemplateName = option.TemplateName,
                EntityType = option.EntityType,
                UsageType = option.UsageType,
                IsSystem = option.IsSystem,
                IsDefault = option.IsDefault
            }).ToList(),
            Children = node.Children.Select(CloneNode).ToList()
        };
    }

    private sealed class SimpleHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public SimpleHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class PassthroughHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private sealed class TestLanguageContext : ILanguageContext
    {
        public string CurrentLanguage => "ja";
        public string[] FallbackLanguages => new[] { "en", "zh" };
    }
}
