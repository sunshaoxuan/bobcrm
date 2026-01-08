using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

public class EntityPublishingServicePhase8Tests
{
    [Fact]
    public async Task PublishNewEntityAsync_WhenEntityNotFound_ShouldFail()
    {
        await using var db = CreateInMemoryContext();
        var ddl = new StubDdlExecutionService(db);
        var lockService = new Mock<IEntityLockService>(MockBehavior.Strict);
        var service = CreateService(db, ddl, lockService.Object);

        var result = await service.PublishNewEntityAsync(Guid.NewGuid(), "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
        ddl.ExecuteCount.Should().Be(0);
        lockService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishNewEntityAsync_WhenStatusNotDraft_ShouldFailBeforeDDL()
    {
        await using var db = CreateInMemoryContext();
        var ddl = new StubDdlExecutionService(db);
        var lockService = new Mock<IEntityLockService>(MockBehavior.Strict);
        var service = CreateService(db, ddl, lockService.Object);

        var entity = NewEntity(EntityStatus.Published);
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expected Draft");
        ddl.ExecuteCount.Should().Be(0);
        lockService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishNewEntityAsync_WhenTableExists_ShouldFailBeforeDDL()
    {
        await using var db = CreateInMemoryContext();
        var ddl = new StubDdlExecutionService(db) { TableExists = true };
        var lockService = new Mock<IEntityLockService>(MockBehavior.Strict);
        var service = CreateService(db, ddl, lockService.Object);

        var entity = NewEntity(EntityStatus.Draft);
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
        ddl.ExecuteCount.Should().Be(0);
        lockService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishNewEntityAsync_WhenDdlFails_ShouldKeepDraftAndNotLock()
    {
        await using var db = CreateInMemoryContext();
        var ddl = new StubDdlExecutionService(db) { TableExists = false, AlwaysSucceed = false };
        var lockService = new Mock<IEntityLockService>(MockBehavior.Strict);
        var service = CreateService(db, ddl, lockService.Object);

        var entity = NewEntity(EntityStatus.Draft);
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("DDL failed");
        ddl.ExecuteCount.Should().Be(1);

        var stored = await db.EntityDefinitions.AsNoTracking().SingleAsync(e => e.Id == entity.Id);
        stored.Status.Should().Be(EntityStatus.Draft);
        lockService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task WithdrawAsync_WhenDraft_ShouldFail()
    {
        await using var db = CreateInMemoryContext();
        var ddl = new StubDdlExecutionService(db) { AlwaysSucceed = true, TableExists = true };
        var lockService = new Mock<IEntityLockService>(MockBehavior.Strict);
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["EntityPublishing:WithdrawalMode"] = "Logical" })
            .Build();
        var service = CreateService(db, ddl, lockService.Object, configuration: cfg);

        var entity = NewEntity(EntityStatus.Draft);
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var result = await service.WithdrawAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cannot be withdrawn");
        ddl.ExecuteCount.Should().Be(0);
        lockService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishNewEntityAsync_WhenLookupDependencyDraft_ShouldCascadePublishDependency()
    {
        await using var db = CreateInMemoryContext();
        var ddl = new StubDdlExecutionService(db) { AlwaysSucceed = true, TableExists = false };
        var lockService = new EntityLockService(db, NullLogger<EntityLockService>.Instance);

        var service = CreateService(db, ddl, lockService);

        var dependency = NewEntity(EntityStatus.Draft);
        dependency.EntityName = "Dep";
        dependency.EntityRoute = "dep";
        dependency.FullTypeName = "BobCrm.Tests.Dynamic.Dep";
        dependency.ApiEndpoint = "/api/deps";

        var entity = NewEntity(EntityStatus.Draft);
        entity.EntityName = "Main";
        entity.EntityRoute = "main";
        entity.FullTypeName = "BobCrm.Tests.Dynamic.Main";
        entity.ApiEndpoint = "/api/mains";
        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "DepId",
            DataType = FieldDataType.Guid,
            LookupEntityName = "Dep",
            ForeignKeyAction = ForeignKeyAction.Restrict,
            IsRequired = false,
            SortOrder = 2,
            Source = FieldSource.Custom
        });

        db.EntityDefinitions.AddRange(dependency, entity);
        await db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeTrue();

        var depStored = await db.EntityDefinitions.AsNoTracking().SingleAsync(e => e.Id == dependency.Id);
        depStored.Status.Should().Be(EntityStatus.Published);
        depStored.Source.Should().Be(EntitySource.Custom);
    }

    [Fact]
    public async Task PublishNewEntityAsync_WhenLookupDependencyMissing_ShouldFail()
    {
        await using var db = CreateInMemoryContext();
        var ddl = new StubDdlExecutionService(db) { AlwaysSucceed = true, TableExists = false };
        var lockService = new EntityLockService(db, NullLogger<EntityLockService>.Instance);
        var service = CreateService(db, ddl, lockService);

        var entity = NewEntity(EntityStatus.Draft);
        entity.EntityName = "Main";
        entity.EntityRoute = "main";
        entity.FullTypeName = "BobCrm.Tests.Dynamic.Main";
        entity.ApiEndpoint = "/api/mains";
        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "MissingId",
            DataType = FieldDataType.Guid,
            LookupEntityName = "Missing",
            ForeignKeyAction = ForeignKeyAction.Restrict,
            IsRequired = false,
            SortOrder = 2,
            Source = FieldSource.Custom
        });
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Lookup referenced entities not found");
    }

    private static EntityPublishingService CreateService(
        AppDbContext db,
        DDLExecutionService ddl,
        IEntityLockService lockService,
        IConfiguration? configuration = null,
        IDefaultTemplateService? defaultTemplateService = null)
    {
        var templateBindingService = new TemplateBindingService(db, NullLogger<TemplateBindingService>.Instance);
        var multilingual = new MultilingualFieldService(db, NullLogger<MultilingualFieldService>.Instance);
        var functionService = new FunctionService(db, multilingual);

        var templates = defaultTemplateService ?? CreateDefaultTemplateServiceMock();
        var cfg = configuration ?? new ConfigurationBuilder().AddInMemoryCollection().Build();

        return new EntityPublishingService(
            db,
            new PostgreSQLDDLGenerator(),
            ddl,
            lockService,
            templateBindingService,
            functionService,
            templates,
            cfg,
            NullLogger<EntityPublishingService>.Instance);
    }

    private static IDefaultTemplateService CreateDefaultTemplateServiceMock()
    {
        var mock = new Mock<IDefaultTemplateService>(MockBehavior.Strict);
        mock.Setup(x => x.EnsureTemplatesAsync(It.IsAny<EntityDefinition>(), It.IsAny<string?>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DefaultTemplateGenerationResult());
        mock.Setup(x => x.GetDefaultTemplateAsync(It.IsAny<EntityDefinition>(), It.IsAny<FormTemplateUsageType>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException("not needed by these tests"));
        return mock.Object;
    }

    private static EntityDefinition NewEntity(string status)
    {
        return new EntityDefinition
        {
            Namespace = "BobCrm.Tests.Dynamic",
            EntityName = $"Entity_{Guid.NewGuid():N}",
            FullTypeName = $"BobCrm.Tests.Dynamic.Entity_{Guid.NewGuid():N}",
            EntityRoute = $"entity_{Guid.NewGuid():N}",
            ApiEndpoint = "/api/entities",
            StructureType = EntityStructureType.Single,
            Status = status,
            Source = EntitySource.Custom,
            IsEnabled = true,
            Fields = new List<FieldMetadata>
            {
                new()
                {
                    PropertyName = "Id",
                    DataType = FieldDataType.Integer,
                    IsRequired = true,
                    SortOrder = 1,
                    Source = FieldSource.System
                }
            },
            Interfaces = new List<EntityInterface>()
        };
    }

    private static AppDbContext CreateInMemoryContext()
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
