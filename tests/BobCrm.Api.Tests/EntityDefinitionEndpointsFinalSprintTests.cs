using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Reflection.Emit;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.Requests.Entity;
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
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BobCrm.Api.Tests;

public class EntityDefinitionEndpointsFinalSprintTests
{
    [Fact]
    public async Task GenerateCode_WhenPublished_ShouldReturnCode()
    {
        using var factory = new TestWebAppFactory();
        var entity = await InsertEntityAsync(factory.Services, status: EntityStatus.Published);

        var client = await CreateAuthenticatedClientAsync(factory);
        var response = await client.GetAsync($"/api/entity-definitions/{entity.Id}/generate-code");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("entityId").GetGuid().Should().Be(entity.Id);
        data.GetProperty("code").GetString().Should().Contain("namespace");
    }

    [Fact]
    public async Task Compile_WhenCompilationFails_ShouldReturnBadRequestWithErrors()
    {
        using var factory = new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<RoslynCompiler>();
                services.AddSingleton<RoslynCompiler>(sp => new FailRoslynCompiler());
            });
        });

        var entity = await InsertEntityAsync(factory.Services, status: EntityStatus.Published);
        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsync($"/api/entity-definitions/{entity.Id}/compile", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("COMPILE_FAILED");
        root.GetProperty("details").GetProperty("Errors")[0].GetString().Should().Contain("CS1001");
    }

    [Fact]
    public async Task CompileBatch_WhenHasPublishedEntities_ShouldReturnOk()
    {
        using var factory = new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<RoslynCompiler>();
                services.AddSingleton<RoslynCompiler>(sp => new EmitRoslynCompiler());
            });
        });

        var e1 = await InsertEntityAsync(factory.Services, status: EntityStatus.Published);
        var e2 = await InsertEntityAsync(factory.Services, status: EntityStatus.Published);

        var client = await CreateAuthenticatedClientAsync(factory);
        var response = await client.PostAsJsonAsync("/api/entity-definitions/compile-batch",
            new CompileBatchDto { EntityIds = new List<Guid> { e1.Id, e2.Id } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("count").GetInt32().Should().BeGreaterThan(0);
        data.GetProperty("loadedTypes").EnumerateArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task PreviewDdl_WhenPublished_ShouldReturnMessage()
    {
        using var factory = new TestWebAppFactory();
        var entity = await InsertEntityAsync(factory.Services, status: EntityStatus.Published);

        var client = await CreateAuthenticatedClientAsync(factory);
        var response = await client.GetAsync($"/api/entity-definitions/{entity.Id}/preview-ddl");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.ReadDataAsJsonAsync();
        data.GetProperty("status").GetString().Should().Be(EntityStatus.Published);
        data.GetProperty("ddlScript").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PublishChanges_And_Withdraw_ShouldMapTemplatesBindingsMenus()
    {
        var jobs = new FakeBackgroundJobClient();
        var publish = new FakeEntityPublishingService
        {
            PublishChanges = (_, _) =>
            {
                var res = new PublishResult
                {
                    Success = true,
                    ScriptId = Guid.NewGuid(),
                    DDLScript = "ALTER TABLE ...",
                    ChangeAnalysis = new ChangeAnalysis
                    {
                        NewFields = new List<FieldMetadata> { new() { PropertyName = "NewField", DataType = FieldDataType.String } }
                    }
                };

                res.Templates.Add(new PublishedTemplateInfo("List", 1, "ListTemplate"));
                res.TemplateBindings.Add(new PublishedTemplateBindingInfo("List", FormTemplateUsageType.List, 2, 1, "FN_LIST"));
                res.MenuNodes.Add(new PublishedMenuInfo("MENU_LIST", Guid.NewGuid(), null, "/x", "List", FormTemplateUsageType.List));
                return Task.FromResult(res);
            },
            Withdraw = (_, _) => Task.FromResult(new WithdrawResult
            {
                Success = true,
                ScriptId = Guid.NewGuid(),
                DDLScript = "DROP TABLE ...",
                Mode = "Logical"
            })
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

        var entity = await InsertEntityAsync(factory.Services, status: EntityStatus.Published);
        var client = await CreateAuthenticatedClientAsync(factory);

        var publishChanges = await client.PostAsync($"/api/entity-definitions/{entity.Id}/publish-changes", content: null);
        publishChanges.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await publishChanges.ReadDataAsJsonAsync();
        payload.GetProperty("ddlScript").GetString().Should().Contain("ALTER TABLE");
        payload.GetProperty("templates").EnumerateArray().Should().NotBeEmpty();
        payload.GetProperty("bindings").EnumerateArray().Should().NotBeEmpty();
        payload.GetProperty("menus").EnumerateArray().Should().NotBeEmpty();
        payload.GetProperty("changeAnalysis").GetProperty("newFieldsCount").GetInt32().Should().BeGreaterThan(0);

        var withdraw = await client.PostAsync($"/api/entity-definitions/{entity.Id}/withdraw", content: null);
        withdraw.StatusCode.Should().Be(HttpStatusCode.OK);
        (await withdraw.ReadDataAsJsonAsync()).GetProperty("mode").GetString().Should().Be("Logical");

        jobs.Completed.Count.Should().Be(2);
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Lang", "zh");
        return client;
    }

    private static async Task<EntityDefinition> InsertEntityAsync(IServiceProvider services, string status)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var id = Guid.NewGuid();
        var ns = "BobCrm.Base.Custom";
        var entityName = $"FinalSprint_{Guid.NewGuid():N}";
        var entityRoute = entityName.ToLowerInvariant();

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
            CreatedAt = DateTime.UtcNow.AddMinutes(-20),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-20),
            Source = "Custom"
        };

        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = id,
            PropertyName = "Name",
            DataType = FieldDataType.String,
            SortOrder = 1,
            Source = FieldSource.Custom,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-20)
        });

        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();
        return entity;
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

        public void Fail(Guid jobId, string errorMessage) { }
    }

    private sealed class FailRoslynCompiler : RoslynCompiler
    {
        public FailRoslynCompiler() : base(NullLogger<RoslynCompiler>.Instance) { }

        public override CompilationResult Compile(string sourceCode, string assemblyName)
        {
            return new CompilationResult
            {
                Success = false,
                AssemblyName = assemblyName,
                Errors = new List<CompilationError>
                {
                    new() { Code = "CS1001", Message = "Identifier expected", Line = 1, Column = 1 }
                }
            };
        }
    }

    private sealed class EmitRoslynCompiler : RoslynCompiler
    {
        public EmitRoslynCompiler() : base(NullLogger<RoslynCompiler>.Instance) { }

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
