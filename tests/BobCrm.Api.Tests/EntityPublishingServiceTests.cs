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
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BobCrm.Api.Tests;

public class EntityPublishingServiceTests
{
    [Fact]
    public async Task PublishNewEntityAsync_WhenEnumFieldMissingEnumDefinitionId_ShouldFailBeforeDDL()
    {
        await using var db = CreateInMemoryContext();
        var ddl = new StubDdlExecutionService(db);
        var service = CreateService(db, ddl);

        var entity = NewDraftEntity("Order", "order");
        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Status",
            DataType = FieldDataType.Enum
        });
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("EnumDefinitionId is null");
        ddl.ExecuteCount.Should().Be(0);

        var stored = await db.EntityDefinitions.AsNoTracking().SingleAsync(e => e.Id == entity.Id);
        stored.Status.Should().Be(EntityStatus.Draft);
    }

    [Fact]
    public async Task PublishNewEntityAsync_WhenEnumDefinitionDisabled_ShouldFailBeforeDDL()
    {
        await using var db = CreateInMemoryContext();
        var ddl = new StubDdlExecutionService(db);
        var service = CreateService(db, ddl);

        var disabledEnum = new EnumDefinition
        {
            Code = "order_status",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "订单状态" },
            Description = new Dictionary<string, string?>(),
            IsEnabled = false
        };
        db.EnumDefinitions.Add(disabledEnum);

        var entity = NewDraftEntity("Order", "order");
        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Status",
            DataType = FieldDataType.Enum,
            EnumDefinitionId = disabledEnum.Id
        });
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("disabled");
        ddl.ExecuteCount.Should().Be(0);
    }

    [Fact]
    public async Task PublishNewEntityAsync_WhenLookupSetNullButNotNull_ShouldFailBeforeDDL()
    {
        await using var db = CreateInMemoryContext();
        var ddl = new StubDdlExecutionService(db);
        var service = CreateService(db, ddl);

        var referenced = NewDraftEntity("Customer", "customer");
        referenced.Status = EntityStatus.Published;
        db.EntityDefinitions.Add(referenced);

        var entity = NewDraftEntity("Order", "order");
        var field = new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "CustomerId",
            DataType = FieldDataType.Guid,
            LookupEntityName = "Customer",
            ForeignKeyAction = ForeignKeyAction.SetNull
        };
        field.IsRequired = true;
        entity.Fields.Add(field);
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var persisted = await db.FieldMetadatas.AsNoTracking().SingleAsync(f => f.EntityDefinitionId == entity.Id);
        persisted.ForeignKeyAction.Should().Be(ForeignKeyAction.SetNull);
        persisted.IsRequired.Should().BeTrue();

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ForeignKeyAction=SetNull");
        ddl.ExecuteCount.Should().Be(0);
    }

    [Fact]
    public async Task PublishNewEntityAsync_Success_ShouldTransitionDraftToPublishedAndLock()
    {
        await using var db = CreateInMemoryContext();

        var ddl = new StubDdlExecutionService(db) { TableExists = false, AlwaysSucceed = true };
        var lockService = new EntityLockService(db, NullLogger<EntityLockService>.Instance);
        var service = CreateService(db, ddl, lockService: lockService);

        var entity = NewDraftEntity("Order", "order");
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeTrue();
        ddl.ExecuteCount.Should().Be(1);

        var stored = await db.EntityDefinitions.AsNoTracking().SingleAsync(e => e.Id == entity.Id);
        stored.Status.Should().Be(EntityStatus.Published);
        stored.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task PublishNewEntityAsync_WhenLockThrows_ShouldRollbackToDraft_InRelationalTransaction()
    {
        await using var db = await CreateSqliteContextAsync();

        var ddl = new StubDdlExecutionService(db) { TableExists = false, AlwaysSucceed = true };
        var lockMock = new Mock<IEntityLockService>(MockBehavior.Strict);
        lockMock.Setup(x => x.LockEntityAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("lock failed"));
        lockMock.Setup(x => x.LockEntityHierarchyAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(0);
        lockMock.Setup(x => x.UnlockEntityAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        lockMock.Setup(x => x.IsEntityLockedAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);
        lockMock.Setup(x => x.CanModifyPropertyAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((true, (string?)null));
        lockMock.Setup(x => x.GetLockInfoAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new NotSupportedException("not needed"));
        lockMock.Setup(x => x.ValidateModificationAsync(It.IsAny<Guid>(), It.IsAny<BobCrm.Api.Services.EntityLocking.EntityDefinitionUpdateRequest>()))
            .ThrowsAsync(new NotSupportedException("not needed"));

        var service = CreateService(db, ddl, lockService: lockMock.Object);

        var entity = NewDraftEntity("Order", "order");
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("lock failed");

        var stored = await db.EntityDefinitions.AsNoTracking().SingleAsync(e => e.Id == entity.Id);
        stored.Status.Should().Be(EntityStatus.Draft);
    }

    [Fact]
    public async Task PublishNewEntityAsync_WhenAlreadyPublished_ShouldRejectSecondAttempt()
    {
        await using var db = CreateInMemoryContext();
        var ddl = new StubDdlExecutionService(db) { TableExists = false, AlwaysSucceed = true };
        var lockService = new EntityLockService(db, NullLogger<EntityLockService>.Instance);
        var service = CreateService(db, ddl, lockService: lockService);

        var entity = NewDraftEntity("Order", "order");
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        (await service.PublishNewEntityAsync(entity.Id, "tester")).Success.Should().BeTrue();
        var second = await service.PublishNewEntityAsync(entity.Id, "tester");
        second.Success.Should().BeFalse();
        second.ErrorMessage.Should().Contain("expected Draft");
    }

    [Fact]
    public async Task WithdrawAsync_WhenPublished_ShouldMarkWithdrawn()
    {
        await using var db = CreateInMemoryContext();
        var ddl = new StubDdlExecutionService(db) { AlwaysSucceed = true, TableExists = true };
        var lockService = new EntityLockService(db, NullLogger<EntityLockService>.Instance);
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["EntityPublishing:WithdrawalMode"] = "Logical" })
            .Build();

        var service = CreateService(db, ddl, lockService: lockService, configuration: cfg);

        var entity = NewDraftEntity("Order", "order");
        entity.Status = EntityStatus.Published;
        entity.IsEnabled = true;
        entity.IsLocked = true;
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var result = await service.WithdrawAsync(entity.Id, "tester");

        result.Success.Should().BeTrue();

        var stored = await db.EntityDefinitions.AsNoTracking().SingleAsync(e => e.Id == entity.Id);
        stored.Status.Should().Be(EntityStatus.Withdrawn);
        stored.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task PublishEntityChangesAsync_WhenTemplateGenerationFails_ShouldRollbackStatusInRelationalDb()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var entity = NewDraftEntity("Order", "order");
        entity.Status = EntityStatus.Modified;
        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Name",
            DataType = FieldDataType.String,
            SortOrder = 1
        });
        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Description",
            DataType = FieldDataType.String,
            SortOrder = 2
        });
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var ddl = new StubDdlExecutionService(db)
        {
            TableExists = true,
            AlwaysSucceed = true,
            Columns =
            [
                new TableColumnInfo { ColumnName = "id", DataType = "integer" },
                new TableColumnInfo { ColumnName = "name", DataType = "text" }
            ]
        };

        var defaultTemplateService = new Mock<IDefaultTemplateService>(MockBehavior.Strict);
        defaultTemplateService
            .Setup(x => x.EnsureTemplatesAsync(It.IsAny<EntityDefinition>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("template failed"));
        defaultTemplateService
            .Setup(x => x.GetDefaultTemplateAsync(It.IsAny<EntityDefinition>(), It.IsAny<FormTemplateUsageType>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException("not needed by this test"));

        var service = CreateService(db, ddl, defaultTemplateService: defaultTemplateService.Object);

        var result = await service.PublishEntityChangesAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("template failed");

        var stored = await db.EntityDefinitions.AsNoTracking().SingleAsync(e => e.Id == entity.Id);
        stored.Status.Should().Be(EntityStatus.Modified);
    }

    [Fact]
    public async Task PublishNewEntityAsync_WhenLookupDependenciesFormCycle_ShouldFailWithCyclicDependencyError()
    {
        await using var db = CreateInMemoryContext();
        var ddl = new StubDdlExecutionService(db);
        var service = CreateService(db, ddl);

        var entityA = NewDraftEntity("A", "a");
        entityA.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entityA.Id,
            PropertyName = "BId",
            DataType = FieldDataType.Guid,
            LookupEntityName = "B",
            ForeignKeyAction = ForeignKeyAction.Restrict
        });

        var entityB = NewDraftEntity("B", "b");
        entityB.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entityB.Id,
            PropertyName = "AId",
            DataType = FieldDataType.Guid,
            LookupEntityName = "A",
            ForeignKeyAction = ForeignKeyAction.Restrict
        });

        db.EntityDefinitions.AddRange(entityA, entityB);
        await db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entityA.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cyclic publish dependency detected");
        ddl.ExecuteCount.Should().Be(0);
    }

    [Fact]
    public async Task PublishEntityChangesAsync_WhenLockedAndHasDestructiveChanges_ShouldRejectBeforeDDL()
    {
        await using var db = CreateInMemoryContext();
        var ddl = new StubDdlExecutionService(db)
        {
            TableExists = true,
            AlwaysSucceed = true,
            Columns =
            [
                new TableColumnInfo { ColumnName = "id", DataType = "integer" },
                new TableColumnInfo { ColumnName = "name", DataType = "text" },
                new TableColumnInfo { ColumnName = "obsolete", DataType = "text" }
            ]
        };
        var service = CreateService(db, ddl);

        var entity = NewDraftEntity("Order", "order");
        entity.Status = EntityStatus.Modified;
        entity.IsLocked = true;
        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Name",
            DataType = FieldDataType.String,
            SortOrder = 1
        });
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var result = await service.PublishEntityChangesAsync(entity.Id, "tester");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Entity is locked");
        ddl.ExecuteCount.Should().Be(0);
    }

    private static EntityPublishingService CreateService(
        AppDbContext db,
        DDLExecutionService ddl,
        IEntityLockService? lockService = null,
        IConfiguration? configuration = null,
        IDefaultTemplateService? defaultTemplateService = null)
    {
        var templateBindingService = new TemplateBindingService(db, NullLogger<TemplateBindingService>.Instance);
        var multilingual = new MultilingualFieldService(db, NullLogger<MultilingualFieldService>.Instance);
        var functionService = new FunctionService(db, multilingual);

        var defaultTemplates = defaultTemplateService ?? CreateDefaultTemplateServiceMock();
        var cfg = configuration ?? new ConfigurationBuilder().AddInMemoryCollection().Build();
        var locks = lockService ?? new EntityLockService(db, NullLogger<EntityLockService>.Instance);

        return new EntityPublishingService(
            db,
            new PostgreSQLDDLGenerator(),
            ddl,
            locks,
            templateBindingService,
            functionService,
            defaultTemplates,
            CreateDynamicEntityServiceMock(),
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

    private static IDynamicEntityService CreateDynamicEntityServiceMock()
    {
        var mock = new Mock<IDynamicEntityService>(MockBehavior.Loose);
        mock.Setup(x => x.CompileEntityAsync(It.IsAny<Guid>())).ReturnsAsync(new CompilationResult { Success = true });
        mock.Setup(x => x.RecompileEntityAsync(It.IsAny<Guid>())).ReturnsAsync(new CompilationResult { Success = true });
        return mock.Object;
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

    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
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
        public int ExecuteCount { get; private set; }
        public List<TableColumnInfo> Columns { get; set; } = new();

        public StubDdlExecutionService(AppDbContext db)
            : base(db, NullLogger<DDLExecutionService>.Instance)
        {
        }

        public override Task<bool> TableExistsAsync(string tableName) => Task.FromResult(TableExists);

        public override Task<List<TableColumnInfo>> GetTableColumnsAsync(string tableName)
            => Task.FromResult(Columns);

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

            if (AlwaysSucceed && string.Equals(scriptType, DDLScriptType.Create, StringComparison.OrdinalIgnoreCase))
            {
                TableExists = true;
            }

            return script;
        }
    }
}
