using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using BobCrm.Application.Templates;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BobCrm.Api.Tests;

public class EntityPublishingServiceTemplateMenuTests
{
    [Fact]
    public async Task PublishNewEntityAsync_ShouldGenerateTemplatesBindingsAndMenus()
    {
        await using var db = await CreateSqliteContextAsync();

        var entity = NewDraftEntity(entityName: $"P9_{Guid.NewGuid():N}", route: $"p9_{Guid.NewGuid():N}");
        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Name",
            DataType = FieldDataType.String,
            SortOrder = 1,
            Source = FieldSource.Custom
        });
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        // Pre-create a default TemplateStateBinding so EnsureTemplateStateBindingAsync hits the update branch too.
        db.FormTemplates.Add(new FormTemplate
        {
            Id = 999,
            Name = "PreExisting",
            EntityType = entity.EntityRoute,
            UserId = "system",
            UsageType = FormTemplateUsageType.List,
            LayoutJson = "{\"items\":{\"a\":1}}",
            IsSystemDefault = true
        });
        await db.SaveChangesAsync();

        db.TemplateStateBindings.Add(new TemplateStateBinding
        {
            EntityType = entity.EntityRoute,
            ViewState = "List",
            TemplateId = 999,
            IsDefault = true,
            RequiredPermission = "OLD",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var ddl = new StubDdlExecutionService(db) { AlwaysSucceed = true, TableExists = false };
        var defaultTemplates = new DbBackedDefaultTemplateService(db);
        var service = CreateService(db, ddl, defaultTemplates);

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeTrue(result.ErrorMessage);
        result.Templates.Should().HaveCount(4);
        result.TemplateBindings.Should().HaveCount(4);
        result.MenuNodes.Should().NotBeEmpty();

        var persistedBindings = await db.TemplateStateBindings.AsNoTracking()
            .Where(b => b.EntityType == entity.EntityRoute && b.IsDefault)
            .ToListAsync();
        persistedBindings.Select(b => b.ViewState).Should().Contain(new[] { "List", "DetailView", "DetailEdit", "Create" });
    }

    private static EntityPublishingService CreateService(
        AppDbContext db,
        DDLExecutionService ddl,
        IDefaultTemplateService defaultTemplateService)
    {
        var templateBindingService = new TemplateBindingService(db, NullLogger<TemplateBindingService>.Instance);
        var multilingual = new MultilingualFieldService(db, NullLogger<MultilingualFieldService>.Instance);
        var functionService = new FunctionService(db, multilingual);
        var locks = new EntityLockService(db, NullLogger<EntityLockService>.Instance);
        var cfg = new ConfigurationBuilder().AddInMemoryCollection().Build();

        return new EntityPublishingService(
            db,
            new PostgreSQLDDLGenerator(),
            ddl,
            locks,
            templateBindingService,
            functionService,
            defaultTemplateService,
            cfg,
            NullLogger<EntityPublishingService>.Instance);
    }

    private static EntityDefinition NewDraftEntity(string entityName, string route)
    {
        return new EntityDefinition
        {
            Namespace = "BobCrm.Tests.Dynamic",
            EntityName = entityName,
            FullTypeName = $"BobCrm.Tests.Dynamic.{entityName}",
            EntityRoute = route,
            ApiEndpoint = $"/api/{route}s",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Draft,
            Source = EntitySource.Custom,
            IsEnabled = true
        };
    }

    private static async Task<AppDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private sealed class StubDdlExecutionService : DDLExecutionService
    {
        public bool TableExists { get; set; }
        public bool AlwaysSucceed { get; set; } = true;

        public StubDdlExecutionService(AppDbContext db)
            : base(db, NullLogger<DDLExecutionService>.Instance)
        {
        }

        public override Task<bool> TableExistsAsync(string tableName) => Task.FromResult(TableExists);

        public override async Task<DDLScript> ExecuteDDLAsync(Guid entityDefinitionId, string scriptType, string sqlScript, string? createdBy = null)
        {
            var script = new DDLScript
            {
                EntityDefinitionId = entityDefinitionId,
                ScriptType = scriptType,
                SqlScript = sqlScript,
                Status = AlwaysSucceed ? DDLScriptStatus.Success : DDLScriptStatus.Failed,
                CreatedAt = DateTime.UtcNow,
                ExecutedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                ErrorMessage = AlwaysSucceed ? null : "DDL failed (stub)"
            };

            await _db.DDLScripts.AddAsync(script);
            await _db.SaveChangesAsync();

            if (AlwaysSucceed && string.Equals(scriptType, DDLScriptType.Create, StringComparison.OrdinalIgnoreCase))
            {
                TableExists = true;
            }

            return script;
        }
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
            var entityType = entityDefinition.EntityRoute;
            var userId = string.IsNullOrWhiteSpace(updatedBy) ? "system" : updatedBy!;

            var result = new DefaultTemplateGenerationResult();
            foreach (var viewState in new[] { "List", "DetailView", "DetailEdit", "Create" })
            {
                var usage = viewState switch
                {
                    "List" => FormTemplateUsageType.List,
                    "DetailEdit" => FormTemplateUsageType.Edit,
                    "Create" => FormTemplateUsageType.Combined,
                    _ => FormTemplateUsageType.Detail
                };

                var template = new FormTemplate
                {
                    Name = $"{entityType}-{viewState}",
                    EntityType = entityType,
                    UserId = userId,
                    UsageType = usage,
                    LayoutJson = "{\"items\":{\"a\":1}}",
                    IsSystemDefault = true
                };

                _db.FormTemplates.Add(template);
                await _db.SaveChangesAsync(ct);

                result.Templates[viewState] = template;
                result.Created.Add(template);
            }

            return result;
        }

        public Task<FormTemplate> GetDefaultTemplateAsync(
            EntityDefinition entityDefinition,
            FormTemplateUsageType usageType,
            string? requestedBy = null,
            CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}
