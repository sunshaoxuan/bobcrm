using System.Text.Json;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BobCrm.Api.Tests;

public class TemplateRuntimeServicePolymorphicTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SqliteConnection _connection;

    public TemplateRuntimeServicePolymorphicTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task BuildRuntimeContextAsync_ShouldSelectMatchedTemplate_WhenEntityDataMatches()
    {
        await SeedTemplatesAndBindingsAsync();

        var bindingService = new TemplateBindingService(_db, Mock.Of<ILogger<TemplateBindingService>>());
        var accessService = CreateAccessService(_db);
        var runtimeService = CreateRuntimeService(bindingService, accessService);

        var data = JsonSerializer.Deserialize<JsonElement>("{\"Status\":\"Draft\"}");

        var context = await runtimeService.BuildRuntimeContextAsync(
            userId: "u1",
            entityType: "order",
            request: new BobCrm.Api.Contracts.Requests.Template.TemplateRuntimeRequest(
                UsageType: FormTemplateUsageType.Detail,
                EntityData: data),
            ct: default);

        context.Template.Id.Should().Be(2);
    }

    [Fact]
    public async Task BuildRuntimeContextAsync_ShouldFallbackToDefaultTemplate_WhenNoRuleMatches()
    {
        await SeedTemplatesAndBindingsAsync();

        var bindingService = new TemplateBindingService(_db, Mock.Of<ILogger<TemplateBindingService>>());
        var accessService = CreateAccessService(_db);
        var runtimeService = CreateRuntimeService(bindingService, accessService);

        var data = JsonSerializer.Deserialize<JsonElement>("{\"Status\":\"Approved\"}");

        var context = await runtimeService.BuildRuntimeContextAsync(
            userId: "u1",
            entityType: "order",
            request: new BobCrm.Api.Contracts.Requests.Template.TemplateRuntimeRequest(
                UsageType: FormTemplateUsageType.Detail,
                EntityData: data),
            ct: default);

        context.Template.Id.Should().Be(1);
    }

    private async Task SeedTemplatesAndBindingsAsync()
    {
        _db.FormTemplates.AddRange(
            new FormTemplate
            {
                Id = 1,
                Name = "Order Default",
                EntityType = "order",
                LayoutJson = "[{\"id\":\"Name_detail\",\"type\":\"text\",\"label\":\"Name\"}]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsSystemDefault = true
            },
            new FormTemplate
            {
                Id = 2,
                Name = "Order Draft",
                EntityType = "order",
                LayoutJson = "[{\"id\":\"Name_detail\",\"type\":\"text\",\"label\":\"Name\"}]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsSystemDefault = true
            });

        _db.TemplateStateBindings.AddRange(
            new TemplateStateBinding
            {
                EntityType = "order",
                ViewState = "DetailView",
                TemplateId = 1,
                IsDefault = true,
                Priority = -1,
                CreatedAt = DateTime.UtcNow
            },
            new TemplateStateBinding
            {
                EntityType = "order",
                ViewState = "DetailView",
                TemplateId = 2,
                MatchFieldName = "Status",
                MatchFieldValue = "Draft",
                Priority = 10,
                IsDefault = false,
                CreatedAt = DateTime.UtcNow
            });

        await _db.SaveChangesAsync();
    }

    private TemplateRuntimeService CreateRuntimeService(TemplateBindingService bindingService, AccessService accessService)
    {
        var defaultTemplateService = new Mock<IDefaultTemplateService>();
        var persistence = new Mock<IReflectionPersistenceService>();
        var logger = Mock.Of<ILogger<TemplateRuntimeService>>();
        return new TemplateRuntimeService(bindingService, accessService, _db, defaultTemplateService.Object, persistence.Object, logger);
    }

    private static AccessService CreateAccessService(AppDbContext context)
    {
        var multilingualLogger = Mock.Of<ILogger<MultilingualFieldService>>();
        var multilingual = new MultilingualFieldService(context, multilingualLogger);
        return new AccessService(context, CreateUserManager(context), CreateRoleManager(context), multilingual);
    }

    private static UserManager<IdentityUser> CreateUserManager(AppDbContext context)
    {
        var store = new UserStore<IdentityUser>(context);
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<IdentityUser>();
        var userValidators = new List<IUserValidator<IdentityUser>> { new UserValidator<IdentityUser>() };
        var passwordValidators = new List<IPasswordValidator<IdentityUser>> { new PasswordValidator<IdentityUser>() };
        var normalizer = new UpperInvariantLookupNormalizer();
        var describer = new IdentityErrorDescriber();

        var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        var logger = services.GetRequiredService<ILogger<UserManager<IdentityUser>>>();

        return new UserManager<IdentityUser>(
            store, options, passwordHasher, userValidators, passwordValidators, normalizer, describer,
            services, logger);
    }

    private static RoleManager<IdentityRole> CreateRoleManager(AppDbContext context)
    {
        var store = new RoleStore<IdentityRole>(context);
        var roleValidators = new List<IRoleValidator<IdentityRole>> { new RoleValidator<IdentityRole>() };

        var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        var logger = services.GetRequiredService<ILogger<RoleManager<IdentityRole>>>();

        return new RoleManager<IdentityRole>(store, roleValidators, new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), logger);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
