using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using BobCrm.Application.Templates;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BobCrm.Api.Tests;

public class EntityPublishingServiceValidationTests
{
    [Fact]
    public async Task PublishNewEntityAsync_WhenEnumDefinitionIdMissing_ShouldFail()
    {
        await using var db = CreateContext();
        var ddl = new StubDdlExecutionService(db) { AlwaysSucceed = true };
        var service = CreateService(db, ddl);

        var entity = NewDraftEntity("EnumMissing", "enum-missing");
        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Status",
            DataType = FieldDataType.Enum,
            EnumDefinitionId = null,
            SortOrder = 1,
            Source = FieldSource.Custom
        });
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("EnumDefinitionId is null");
        ddl.ExecuteCount.Should().Be(0);
    }

    [Fact]
    public async Task PublishNewEntityAsync_WhenEnumDisabled_ShouldFail()
    {
        await using var db = CreateContext();
        var ddl = new StubDdlExecutionService(db) { AlwaysSucceed = true };
        var service = CreateService(db, ddl);

        var enumDef = new EnumDefinition
        {
            Id = Guid.NewGuid(),
            Code = "E1",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "枚举" },
            IsEnabled = false
        };
        db.EnumDefinitions.Add(enumDef);

        var entity = NewDraftEntity("EnumDisabled", "enum-disabled");
        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Status",
            DataType = FieldDataType.Enum,
            EnumDefinitionId = enumDef.Id,
            SortOrder = 1,
            Source = FieldSource.Custom
        });
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("disabled");
        ddl.ExecuteCount.Should().Be(0);
    }

    [Fact]
    public async Task PublishNewEntityAsync_WhenLookupEntityMissing_ShouldFail()
    {
        await using var db = CreateContext();
        var ddl = new StubDdlExecutionService(db) { AlwaysSucceed = true };
        var service = CreateService(db, ddl);

        var entity = NewDraftEntity("LookupMissing", "lookup-missing");
        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Ref",
            DataType = FieldDataType.Guid,
            LookupEntityName = "NoSuchEntity",
            SortOrder = 1,
            Source = FieldSource.Custom
        });
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Lookup referenced entities not found");
        ddl.ExecuteCount.Should().Be(0);
    }

    [Fact]
    public async Task PublishNewEntityAsync_WhenLookupSetNullButRequired_ShouldFail()
    {
        await using var db = CreateContext();
        var ddl = new StubDdlExecutionService(db) { AlwaysSucceed = true };
        var service = CreateService(db, ddl);

        db.EntityDefinitions.Add(new EntityDefinition
        {
            Namespace = "BobCrm.Tests.Dynamic",
            EntityName = "Any",
            FullTypeName = "BobCrm.Tests.Dynamic.Any",
            EntityRoute = "any",
            ApiEndpoint = "/api/anys",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            Source = EntitySource.Custom,
            IsEnabled = true
        });

        var entity = NewDraftEntity("LookupSetNull", "lookup-setnull");
        var field = new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Ref",
            DataType = FieldDataType.Guid,
            LookupEntityName = "Any",
            ForeignKeyAction = ForeignKeyAction.SetNull,
            IsRequired = true,
            SortOrder = 1,
            Source = FieldSource.Custom
        };
        entity.Fields.Add(field);
        db.EntityDefinitions.Add(entity);
        db.FieldMetadatas.Add(field);
        await db.SaveChangesAsync();

        var reloaded = await db.EntityDefinitions.Include(e => e.Fields).FirstAsync(e => e.Id == entity.Id);
        reloaded.Fields.Single(f => f.PropertyName == "Ref").ForeignKeyAction.Should().Be(ForeignKeyAction.SetNull);
        reloaded.Fields.Single(f => f.PropertyName == "Ref").IsRequired.Should().BeTrue();

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("SetNull");
        ddl.ExecuteCount.Should().Be(0);
    }

    private static EntityPublishingService CreateService(AppDbContext db, DDLExecutionService ddl)
    {
        var templateBindingService = new TemplateBindingService(db, NullLogger<TemplateBindingService>.Instance);
        var multilingual = new MultilingualFieldService(db, NullLogger<MultilingualFieldService>.Instance);
        var functionService = new FunctionService(db, multilingual);
        var locks = new EntityLockService(db, NullLogger<EntityLockService>.Instance);
        var cfg = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var defaultTemplateService = new Mock<IDefaultTemplateService>(MockBehavior.Strict);
        defaultTemplateService.Setup(x => x.EnsureTemplatesAsync(It.IsAny<EntityDefinition>(), It.IsAny<string?>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DefaultTemplateGenerationResult());
        defaultTemplateService.Setup(x => x.GetDefaultTemplateAsync(It.IsAny<EntityDefinition>(), It.IsAny<FormTemplateUsageType>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException());

        var dynamicEntityService = new Mock<IDynamicEntityService>(MockBehavior.Loose);
        dynamicEntityService.Setup(x => x.CompileEntityAsync(It.IsAny<Guid>())).ReturnsAsync(new CompilationResult { Success = true });
        dynamicEntityService.Setup(x => x.RecompileEntityAsync(It.IsAny<Guid>())).ReturnsAsync(new CompilationResult { Success = true });

        return new EntityPublishingService(
            db,
            new PostgreSQLDDLGenerator(),
            ddl,
            locks,
            templateBindingService,
            functionService,
            defaultTemplateService.Object,
            dynamicEntityService.Object,
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

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class StubDdlExecutionService : DDLExecutionService
    {
        public bool TableExists { get; set; }
        public bool AlwaysSucceed { get; set; } = true;
        public int ExecuteCount { get; private set; }

        public StubDdlExecutionService(AppDbContext db)
            : base(db, NullLogger<DDLExecutionService>.Instance)
        {
        }

        public override Task<bool> TableExistsAsync(string tableName) => Task.FromResult(TableExists);

        public override async Task<DDLScript> ExecuteDDLAsync(Guid entityDefinitionId, string scriptType, string sqlScript, string? createdBy = null)
        {
            ExecuteCount++;
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
            return script;
        }
    }
}
