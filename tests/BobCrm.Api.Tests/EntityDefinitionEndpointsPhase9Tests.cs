using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Emit;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Responses.System;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BobCrm.Api.Tests;

public class EntityDefinitionEndpointsPhase9Tests
{
    private static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Lang", "zh");
        return client;
    }

    private static async Task<EntityDefinition> InsertEntityAsync(IServiceProvider services, string status, string entityRoute)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var id = Guid.NewGuid();
        var ns = "BobCrm.Base.Custom";
        var entityName = $"Phase9_{Guid.NewGuid():N}";

        var entity = new EntityDefinition
        {
            Id = id,
            Namespace = ns,
            EntityName = entityName,
            FullTypeName = $"{ns}.{entityName}",
            EntityRoute = entityRoute,
            ApiEndpoint = $"/api/{entityRoute}",
            Status = status,
            IsEnabled = true,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "Phase9 实体", ["en"] = "Phase9 Entity", ["ja"] = "Phase9 エンティティ" },
            Description = new Dictionary<string, string?> { ["zh"] = "描述", ["en"] = "Desc", ["ja"] = "説明" },
            CreatedAt = DateTime.UtcNow.AddMinutes(-20),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-20),
            Source = "Custom"
        };

        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    [Fact]
    public async Task Entities_List_ShouldAllowAnonymous_AndReturnPublishedEnabledEntities()
    {
        using var factory = new TestWebAppFactory();
        await InsertEntityAsync(factory.Services, EntityStatus.Published, entityRoute: $"phase9entities{Guid.NewGuid():N}");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Lang", "zh");

        var response = await client.GetAsync("/api/entities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
        data.EnumerateArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task Entities_All_WithoutAuth_ShouldReturn401()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/entities/all");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Entities_All_WithAuth_ShouldUseFullTypeNameAsEntityType()
    {
        using var factory = new TestWebAppFactory();
        var entityRoute = $"phase9drafts{Guid.NewGuid():N}";
        var entity = await InsertEntityAsync(factory.Services, EntityStatus.Draft, entityRoute);

        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/entities/all?lang=zh");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();

        data.EnumerateArray()
            .Single(e => e.GetProperty("entityRoute").GetString() == entityRoute)
            .GetProperty("entityType")
            .GetString()
            .Should()
            .Be(entity.FullTypeName);
    }

    [Fact]
    public async Task Entities_ValidateRoute_WhenMissing_ShouldReturnIsValidFalse()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/entities/missing-{Guid.NewGuid():N}/validate");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("isValid").GetBoolean().Should().BeFalse();
        data.GetProperty("entity").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
    }

    [Fact]
    public async Task Entities_GetDefinition_ByEntityCandidates_ShouldResolvePluralAndEntityPrefix()
    {
        using var factory = new TestWebAppFactory();
        var singular = $"phase9customer{Guid.NewGuid():N}";
        var pluralRoute = $"{singular}s";
        var entity = await InsertEntityAsync(factory.Services, EntityStatus.Published, entityRoute: pluralRoute);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Lang", "zh");

        var bySingularWithPrefix = await client.GetAsync($"/api/entities/entity_{singular}/definition?lang=zh");
        bySingularWithPrefix.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto1 = await bySingularWithPrefix.ReadDataAsJsonAsync();
        dto1.GetProperty("id").GetGuid().Should().Be(entity.Id);

        var byPlural = await client.GetAsync($"/api/entities/{pluralRoute}/definition?lang=zh");
        byPlural.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto2 = await byPlural.ReadDataAsJsonAsync();
        dto2.GetProperty("id").GetGuid().Should().Be(entity.Id);
    }

    [Fact]
    public async Task DdlHistory_ShouldReturnScriptPreview_WithEllipsisWhenLong()
    {
        using var factory = new TestWebAppFactory();
        var entity = await InsertEntityAsync(factory.Services, EntityStatus.Draft, entityRoute: $"phase9ddl{Guid.NewGuid():N}");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.DDLScripts.Add(new DDLScript
            {
                EntityDefinitionId = entity.Id,
                ScriptType = DDLScriptType.Create,
                SqlScript = new string('x', 250),
                Status = DDLScriptStatus.Success,
                CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                CreatedBy = "tester"
            });
            await db.SaveChangesAsync();
        }

        var client = await CreateAuthenticatedClientAsync(factory);
        var response = await client.GetAsync($"/api/entity-definitions/{entity.Id}/ddl-history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
        var first = data.EnumerateArray().First();
        var preview = first.GetProperty("scriptPreview").GetString();
        preview.Should().NotBeNull();
        preview!.Length.Should().Be(203);
        preview.Should().EndWith("...");
    }

    [Fact]
    public async Task PublishEndpoints_ShouldReturnOkOnSuccess_AndBadRequestOnFailure()
    {
        var jobs = new FakeBackgroundJobClient();
        var publish = new FakeEntityPublishingService
        {
            PublishNew = (_, _) => Task.FromResult(new PublishResult
            {
                Success = true,
                ScriptId = Guid.NewGuid(),
                DDLScript = "CREATE TABLE ...",
                ChangeAnalysis = null,
                ErrorMessage = null
            }),
            PublishChanges = (_, _) => Task.FromResult(new PublishResult
            {
                Success = false,
                ErrorMessage = "boom"
            }),
            Withdraw = (_, _) => Task.FromResult(new WithdrawResult { Success = false, ErrorMessage = "nope" })
        };

        using var factory = new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IBackgroundJobClient>();
                services.AddSingleton<IBackgroundJobClient>(jobs);

                services.RemoveAll<IEntityPublishingService>();
                services.AddSingleton<IEntityPublishingService>(publish);
            });
        });

        var client = await CreateAuthenticatedClientAsync(factory);
        var id = Guid.NewGuid();

        var publishOk = await client.PostAsync($"/api/entity-definitions/{id}/publish", content: null);
        publishOk.StatusCode.Should().Be(HttpStatusCode.OK);

        var publishChangesFail = await client.PostAsync($"/api/entity-definitions/{id}/publish-changes", content: null);
        publishChangesFail.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await publishChangesFail.ReadAsJsonAsync()).GetProperty("code").GetString().Should().Be("PUBLISH_FAILED");

        var withdrawFail = await client.PostAsync($"/api/entity-definitions/{id}/withdraw", content: null);
        withdrawFail.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await withdrawFail.ReadAsJsonAsync()).GetProperty("code").GetString().Should().Be("WITHDRAW_FAILED");

        jobs.Completed.Count.Should().Be(1);
        jobs.Failed.Count.Should().Be(2);
    }

    [Fact]
    public async Task DynamicEntityEndpoints_ShouldSupportCompile_LoadedEntities_TypeInfo_Unload_AndRecompile()
    {
        using var factory = new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<RoslynCompiler>();
                services.AddSingleton<RoslynCompiler>(sp => new EmitRoslynCompiler());
            });
        });

        var entity = await InsertEntityAsync(factory.Services, EntityStatus.Draft, entityRoute: $"phase9dyn{Guid.NewGuid():N}");
        var client = await CreateAuthenticatedClientAsync(factory);

        var compile = await client.PostAsync($"/api/entity-definitions/{entity.Id}/compile", content: null);
        compile.StatusCode.Should().Be(HttpStatusCode.OK);

        var loaded = await client.GetAsync("/api/entity-definitions/loaded-entities");
        loaded.StatusCode.Should().Be(HttpStatusCode.OK);
        var loadedData = await loaded.ReadDataAsJsonAsync();
        loadedData.GetProperty("entities").EnumerateArray().Select(e => e.GetString()).Should().Contain(entity.FullTypeName);

        var typeInfo = await client.GetAsync($"/api/entity-definitions/type-info/{entity.FullTypeName}");
        typeInfo.StatusCode.Should().Be(HttpStatusCode.OK);
        var infoData = await typeInfo.ReadDataAsJsonAsync();
        infoData.GetProperty("fullName").GetString().Should().Be(entity.FullTypeName);

        var unload = await client.DeleteAsync($"/api/entity-definitions/loaded-entities/{entity.FullTypeName}");
        unload.StatusCode.Should().Be(HttpStatusCode.OK);

        var recompile = await client.PostAsync($"/api/entity-definitions/{entity.Id}/recompile", content: null);
        recompile.StatusCode.Should().Be(HttpStatusCode.OK);

        var validateOk = await client.GetAsync($"/api/entity-definitions/{entity.Id}/validate-code");
        validateOk.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class FakeEntityPublishingService : IEntityPublishingService
    {
        public Func<Guid, string?, Task<PublishResult>> PublishNew { get; init; } =
            (_, _) => Task.FromResult(new PublishResult { Success = true });

        public Func<Guid, string?, Task<PublishResult>> PublishChanges { get; init; } =
            (_, _) => Task.FromResult(new PublishResult { Success = true });

        public Func<Guid, string?, Task<WithdrawResult>> Withdraw { get; init; } =
            (_, _) => Task.FromResult(new WithdrawResult { Success = true });

        public Task<PublishResult> PublishNewEntityAsync(Guid entityDefinitionId, string? publishedBy = null) =>
            PublishNew(entityDefinitionId, publishedBy);

        public Task<PublishResult> PublishEntityChangesAsync(Guid entityDefinitionId, string? publishedBy = null) =>
            PublishChanges(entityDefinitionId, publishedBy);

        public Task<WithdrawResult> WithdrawAsync(Guid entityDefinitionId, string? withdrawnBy = null) =>
            Withdraw(entityDefinitionId, withdrawnBy);
    }

    private sealed class FakeBackgroundJobClient : IBackgroundJobClient
    {
        public List<Guid> Completed { get; } = new();
        public List<Guid> Failed { get; } = new();

        public Task<PagedResponse<BackgroundJobDto>> GetRecentJobsAsync(int page, int pageSize, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<BackgroundJobDto?> GetJobAsync(Guid id, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<BackgroundJobLogDto>> GetJobLogsAsync(Guid id, int limit = 500, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> RequestCancelAsync(Guid id, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Guid StartJob(string name, string category, string? actorId, string? actorName, bool canCancel) => Guid.NewGuid();

        public void AppendLog(Guid jobId, string level, string message) { }

        public void SetProgress(Guid jobId, int progressPercent) { }

        public void Complete(Guid jobId) => Completed.Add(jobId);

        public void Fail(Guid jobId, string errorMessage) => Failed.Add(jobId);
    }

    private sealed class EmitRoslynCompiler : RoslynCompiler
    {
        public EmitRoslynCompiler() : base(new Microsoft.Extensions.Logging.Abstractions.NullLogger<RoslynCompiler>()) { }

        public override CompilationResult Compile(string sourceCode, string assemblyName)
        {
            var fullTypeName = TryParseFullTypeName(sourceCode) ?? "BobCrm.Api.Tests.Dynamic.EmitFallback";

            var assembly = CreateAssemblyWithType(assemblyName, fullTypeName);
            return new CompilationResult
            {
                Success = true,
                AssemblyName = assemblyName,
                Assembly = assembly,
                LoadedTypes = new List<string> { fullTypeName }
            };
        }

        public override CompilationResult CompileMultiple(Dictionary<string, string> sources, string assemblyName)
        {
            var first = sources.Values.Select(TryParseFullTypeName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ??
                        "BobCrm.Api.Tests.Dynamic.EmitBatchFallback";

            var assembly = CreateAssemblyWithType(assemblyName, first);
            return new CompilationResult
            {
                Success = true,
                AssemblyName = assemblyName,
                Assembly = assembly,
                LoadedTypes = new List<string> { first }
            };
        }

        public override ValidationResult ValidateSyntax(string sourceCode) => new() { IsValid = true };

        private static string? TryParseFullTypeName(string sourceCode)
        {
            string? ns = null;
            string? name = null;

            foreach (var line in sourceCode.Split('\n'))
            {
                var trimmed = line.Trim();
                if (ns == null && trimmed.StartsWith("namespace ", StringComparison.Ordinal))
                {
                    ns = trimmed["namespace ".Length..].Trim().TrimEnd('{').Trim();
                    continue;
                }

                if (name == null && trimmed.StartsWith("public class ", StringComparison.Ordinal))
                {
                    var rest = trimmed["public class ".Length..];
                    var parts = rest.Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
                    name = parts.FirstOrDefault();
                }

                if (ns != null && name != null) break;
            }

            return ns != null && name != null ? $"{ns}.{name}" : null;
        }

        private static Assembly CreateAssemblyWithType(string assemblyName, string fullTypeName)
        {
            var lastDot = fullTypeName.LastIndexOf('.');
            var ns = lastDot >= 0 ? fullTypeName[..lastDot] : string.Empty;
            var typeName = lastDot >= 0 ? fullTypeName[(lastDot + 1)..] : fullTypeName;

            var an = new AssemblyName(assemblyName);
            var ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            var module = ab.DefineDynamicModule($"{assemblyName}.dll");
            var tb = module.DefineType(string.IsNullOrWhiteSpace(ns) ? typeName : $"{ns}.{typeName}",
                TypeAttributes.Public | TypeAttributes.Class);

            tb.DefineDefaultConstructor(MethodAttributes.Public);
            tb.CreateTypeInfo();

            return ab;
        }
    }
}
