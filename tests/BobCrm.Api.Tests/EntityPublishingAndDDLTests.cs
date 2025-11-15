using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq;
using System.Reflection;

namespace BobCrm.Api.Tests;

/// <summary>
/// 测试EntityPublishingService和DDLExecutionService
/// 由于这些服务涉及数据库DDL操作，主要测试业务逻辑和错误处理
/// </summary>
public class EntityPublishingAndDDLTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PostgreSQLDDLGenerator _ddlGenerator;
    private readonly Mock<ILogger<DDLExecutionService>> _mockDDLLogger;
    private readonly Mock<ILogger<EntityPublishingService>> _mockPublishLogger;
    private readonly Mock<IEntityLockService> _mockLockService;
    private readonly Mock<ILogger<EntityMenuRegistrar>> _mockMenuLogger;
    private readonly EntityMenuRegistrar _menuRegistrar;

    public EntityPublishingAndDDLTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _db = new AppDbContext(options);
        _ddlGenerator = new PostgreSQLDDLGenerator();
        _mockDDLLogger = new Mock<ILogger<DDLExecutionService>>();
        _mockPublishLogger = new Mock<ILogger<EntityPublishingService>>();
        _mockLockService = new Mock<IEntityLockService>();
        _mockMenuLogger = new Mock<ILogger<EntityMenuRegistrar>>();
        _menuRegistrar = new EntityMenuRegistrar(_db, _mockMenuLogger.Object);
    }

    [Fact]
    public async Task PublishNewEntityAsync_ShouldFail_WhenEntityNotFound()
    {
        // Arrange
        var ddlExecutor = new DDLExecutionService(_db, _mockDDLLogger.Object);
        var service = new EntityPublishingService(_db, _ddlGenerator, ddlExecutor, _mockLockService.Object, _menuRegistrar, _mockPublishLogger.Object);

        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.PublishNewEntityAsync(nonExistentId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task PublishNewEntityAsync_ShouldFail_WhenEntityNotDraft()
    {
        // Arrange
        var ddlExecutor = new DDLExecutionService(_db, _mockDDLLogger.Object);
        var service = new EntityPublishingService(_db, _ddlGenerator, ddlExecutor, _mockLockService.Object, _menuRegistrar, _mockPublishLogger.Object);

        var entityId = Guid.NewGuid();
        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "Product",
            Status = EntityStatus.Published, // Already published
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        // Act
        var result = await service.PublishNewEntityAsync(entityId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expected Draft");
    }

    [Fact]
    public async Task PublishEntityChangesAsync_ShouldFail_WhenEntityNotFound()
    {
        // Arrange
        var ddlExecutor = new DDLExecutionService(_db, _mockDDLLogger.Object);
        var service = new EntityPublishingService(_db, _ddlGenerator, ddlExecutor, _mockLockService.Object, _menuRegistrar, _mockPublishLogger.Object);

        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.PublishEntityChangesAsync(nonExistentId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task PublishEntityChangesAsync_ShouldFail_WhenEntityNotModified()
    {
        // Arrange
        var ddlExecutor = new DDLExecutionService(_db, _mockDDLLogger.Object);
        var service = new EntityPublishingService(_db, _ddlGenerator, ddlExecutor, _mockLockService.Object, _menuRegistrar, _mockPublishLogger.Object);

        var entityId = Guid.NewGuid();
        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "Product",
            Status = EntityStatus.Draft, // Not modified
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        // Act
        var result = await service.PublishEntityChangesAsync(entityId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expected Modified");
    }

    [Fact]
    public async Task DDLExecutionService_ShouldCreateScriptRecord()
    {
        // Arrange
        var service = new DDLExecutionService(_db, _mockDDLLogger.Object);
        var entityId = Guid.NewGuid();
        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "TestEntity",
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        var script = "SELECT 1"; // Simple valid SQL

        // Act
        var result = await service.ExecuteDDLAsync(
            entityId,
            DDLScriptType.Create,
            script,
            "test-user"
        );

        // Assert
        result.Should().NotBeNull();
        result.EntityDefinitionId.Should().Be(entityId);
        result.ScriptType.Should().Be(DDLScriptType.Create);
        result.SqlScript.Should().Be(script);
        result.CreatedBy.Should().Be("test-user");
        result.ExecutedAt.Should().NotBeNull();

        // Verify saved to database
        var savedScript = await _db.DDLScripts.FindAsync(result.Id);
        savedScript.Should().NotBeNull();
        savedScript!.Status.Should().BeOneOf(DDLScriptStatus.Success, DDLScriptStatus.Failed);
    }

    [Fact]
    public async Task DDLExecutionService_ShouldHandleInvalidSQL()
    {
        // Arrange
        var service = new DDLExecutionService(_db, _mockDDLLogger.Object);
        var entityId = Guid.NewGuid();
        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "TestEntity",
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        var invalidScript = "INVALID SQL SYNTAX ;;;";

        // Act
        var result = await service.ExecuteDDLAsync(
            entityId,
            DDLScriptType.Create,
            invalidScript
        );

        // Assert
        result.Status.Should().Be(DDLScriptStatus.Failed);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DDLExecutionService_GetDDLHistoryAsync_ShouldReturnScripts()
    {
        // Arrange
        var service = new DDLExecutionService(_db, _mockDDLLogger.Object);
        var entityId = Guid.NewGuid();

        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "TestEntity",
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        // Create some DDL scripts
        await service.ExecuteDDLAsync(entityId, DDLScriptType.Create, "SELECT 1");
        await service.ExecuteDDLAsync(entityId, DDLScriptType.Alter, "SELECT 2");

        // Act
        var history = await service.GetDDLHistoryAsync(entityId);

        // Assert
        history.Should().HaveCountGreaterOrEqualTo(2);
        history.Should().OnlyContain(s => s.EntityDefinitionId == entityId);
    }

    [Fact]
    public void PublishResult_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var result = new PublishResult();

        // Assert
        result.Success.Should().BeFalse();
        result.EntityDefinitionId.Should().Be(Guid.Empty);
        result.ScriptId.Should().Be(Guid.Empty);
        result.DDLScript.Should().BeNullOrEmpty();
        result.ErrorMessage.Should().BeNullOrEmpty();
        result.ChangeAnalysis.Should().BeNull();
    }

    [Fact]
    public void ChangeAnalysis_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var analysis = new ChangeAnalysis
        {
            NewFields = new List<FieldMetadata>
            {
                new FieldMetadata { PropertyName = "NewField", DataType = FieldDataType.String }
            },
            LengthIncreases = new Dictionary<FieldMetadata, int>(),
            HasDestructiveChanges = false
        };

        // Assert
        analysis.NewFields.Should().HaveCount(1);
        analysis.LengthIncreases.Should().BeEmpty();
        analysis.HasDestructiveChanges.Should().BeFalse();
    }

    [Fact]
    public void DDLScriptType_Constants_ShouldBeAccessible()
    {
        // Assert
        DDLScriptType.Create.Should().Be("Create");
        DDLScriptType.Alter.Should().Be("Alter");
        DDLScriptType.Drop.Should().Be("Drop");
        DDLScriptType.Rollback.Should().Be("Rollback");
    }

    [Fact]
    public void DDLScriptStatus_Constants_ShouldBeAccessible()
    {
        // Assert
        DDLScriptStatus.Pending.Should().Be("Pending");
        DDLScriptStatus.Success.Should().Be("Success");
        DDLScriptStatus.Failed.Should().Be("Failed");
        DDLScriptStatus.RolledBack.Should().Be("RolledBack");
    }

    [Fact]
    public void EntityStatus_Constants_ShouldBeAccessible()
    {
        // Assert
        EntityStatus.Draft.Should().Be("Draft");
        EntityStatus.Published.Should().Be("Published");
        EntityStatus.Modified.Should().Be("Modified");
    }

    [Fact]
    public async Task DDLExecutionService_RollbackDDLAsync_ShouldFail_WhenScriptNotFound()
    {
        // Arrange
        var service = new DDLExecutionService(_db, _mockDDLLogger.Object);
        var nonExistentScriptId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RollbackDDLAsync(nonExistentScriptId, "SELECT 1")
        );
    }

    [Fact]
    public void DDLScript_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var script = new DDLScript
        {
            EntityDefinitionId = Guid.NewGuid(),
            ScriptType = DDLScriptType.Create,
            SqlScript = "CREATE TABLE test (id INT)",
            Status = DDLScriptStatus.Pending
        };

        // Assert
        script.Id.Should().NotBe(Guid.Empty);
        script.EntityDefinitionId.Should().NotBe(Guid.Empty);
        script.ScriptType.Should().Be(DDLScriptType.Create);
        script.SqlScript.Should().NotBeNullOrEmpty();
        script.Status.Should().Be(DDLScriptStatus.Pending);
    }

    [Fact]
    public async Task PublishNewEntityAsync_ShouldSucceed_WithValidDraftEntity()
    {
        // Arrange
        var mockDDLExecutor = new Mock<DDLExecutionService>(_db, _mockDDLLogger.Object);
        var service = new EntityPublishingService(
            _db,
            _ddlGenerator,
            mockDDLExecutor.Object,
            _mockLockService.Object,
            _menuRegistrar,
            _mockPublishLogger.Object);

        var entityId = Guid.NewGuid();
        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "Product",
            EntityRoute = "product",
            ApiEndpoint = "/api/products",
            Status = EntityStatus.Draft,
            Source = EntitySource.Custom,
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    PropertyName = "Name",
                    DataType = "String",
                    Length = 100,
                    IsRequired = true,
                    SortOrder = 1
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        var binding = await SeedTemplateBindingAsync(entity);

        // Mock DDL 执行成功
        mockDDLExecutor.Setup(x => x.TableExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        mockDDLExecutor.Setup(x => x.ExecuteDDLAsync(
                It.IsAny<Guid>(),
                DDLScriptType.Create,
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new DDLScript
            {
                Id = Guid.NewGuid(),
                Status = DDLScriptStatus.Success,
                EntityDefinitionId = entityId
            });

        // Act
        var result = await service.PublishNewEntityAsync(entityId, "test-user");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.EntityDefinitionId.Should().Be(entityId);
        result.DDLScript.Should().NotBeNullOrEmpty();
        result.ScriptId.Should().NotBe(Guid.Empty);
        result.MenuRegistration.Should().NotBeNull();
        result.MenuRegistration!.Success.Should().BeTrue();
        result.MenuRegistration.FunctionCode.Should().NotBeNullOrWhiteSpace();

        // 验证实体状态已更新
        var updatedEntity = await _db.EntityDefinitions.FindAsync(entityId);
        updatedEntity!.Status.Should().Be(EntityStatus.Published);
        updatedEntity.UpdatedBy.Should().Be("test-user");

        var functionNode = await _db.FunctionNodes.FirstOrDefaultAsync(f => f.Code == result.MenuRegistration.FunctionCode);
        functionNode.Should().NotBeNull();
        functionNode!.Route.Should().Be($"/dynamic-entity/{entity.FullTypeName}");

        var refreshedBinding = await _db.TemplateBindings.FindAsync(binding.Id);
        refreshedBinding.Should().NotBeNull();
        refreshedBinding!.RequiredFunctionCode.Should().Be(result.MenuRegistration.FunctionCode);

        var nodes = await _db.FunctionNodes
            .AsNoTracking()
            .OrderBy(f => f.SortOrder)
            .ToListAsync();

        var buildTreeMethod = typeof(BobCrm.Api.Endpoints.AccessEndpoints)
            .GetMethod("BuildTree", BindingFlags.NonPublic | BindingFlags.Static);
        buildTreeMethod.Should().NotBeNull();

        var tree = (List<FunctionNodeDto>)buildTreeMethod!
            .Invoke(null, new object[] { nodes })!;
        tree.Should().NotBeNull();

        FlattenTree(tree)
            .Any(n => string.Equals(n.Code, result.MenuRegistration.FunctionCode, StringComparison.Ordinal))
            .Should().BeTrue("菜单 API 应包含新实体节点");

        // 验证调用了锁定服务
        _mockLockService.Verify(
            x => x.LockEntityAsync(entityId, It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishEntityChangesAsync_ShouldSucceed_WithValidModifiedEntity()
    {
        // Arrange
        var mockDDLExecutor = new Mock<DDLExecutionService>(_db, _mockDDLLogger.Object);
        var service = new EntityPublishingService(
            _db,
            _ddlGenerator,
            mockDDLExecutor.Object,
            _mockLockService.Object,
            _menuRegistrar,
            _mockPublishLogger.Object);

        var entityId = Guid.NewGuid();
        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "Product",
            EntityRoute = "product",
            ApiEndpoint = "/api/products",
            Status = EntityStatus.Modified, // 已修改状态
            Source = EntitySource.Custom,
            IsLocked = false,
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    PropertyName = "Name",
                    DataType = "String",
                    SortOrder = 1
                },
                new FieldMetadata
                {
                    PropertyName = "NewField", // 新增字段
                    DataType = "Int32",
                    SortOrder = 2
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        await SeedTemplateBindingAsync(entity);

        // Mock 表已存在
        mockDDLExecutor.Setup(x => x.TableExistsAsync("Products"))
            .ReturnsAsync(true);

        // Mock 表列信息（只有 Name，缺少 NewField）
        mockDDLExecutor.Setup(x => x.GetTableColumnsAsync("Products"))
            .ReturnsAsync(new List<TableColumnInfo>
            {
                new TableColumnInfo { ColumnName = "Id", DataType = "uuid" },
                new TableColumnInfo { ColumnName = "Name", DataType = "text" }
            });

        // Mock DDL 执行成功
        mockDDLExecutor.Setup(x => x.ExecuteDDLAsync(
                It.IsAny<Guid>(),
                DDLScriptType.Alter,
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new DDLScript
            {
                Id = Guid.NewGuid(),
                Status = DDLScriptStatus.Success,
                EntityDefinitionId = entityId
            });

        // Act
        var result = await service.PublishEntityChangesAsync(entityId, "test-user");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.EntityDefinitionId.Should().Be(entityId);
        result.DDLScript.Should().NotBeNullOrEmpty();
        result.DDLScript.Should().Contain("ALTER TABLE");
        result.MenuRegistration.Should().NotBeNull();

        // 验证实体状态已更新
        var updatedEntity = await _db.EntityDefinitions.FindAsync(entityId);
        updatedEntity!.Status.Should().Be(EntityStatus.Published);
        updatedEntity.UpdatedBy.Should().Be("test-user");
    }

    [Fact]
    public async Task PublishNewEntityAsync_ShouldFail_WhenTableAlreadyExists()
    {
        // Arrange
        var mockDDLExecutor = new Mock<DDLExecutionService>(_db, _mockDDLLogger.Object);
        var service = new EntityPublishingService(
            _db,
            _ddlGenerator,
            mockDDLExecutor.Object,
            _mockLockService.Object,
            _menuRegistrar,
            _mockPublishLogger.Object);

        var entityId = Guid.NewGuid();
        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "Product",
            Status = EntityStatus.Draft,
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        // Mock 表已存在
        mockDDLExecutor.Setup(x => x.TableExistsAsync("Products"))
            .ReturnsAsync(true);

        // Act
        var result = await service.PublishNewEntityAsync(entityId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task MenuRegistrar_ShouldBeIdempotent()
    {
        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "Order",
            EntityRoute = "order",
            ApiEndpoint = "/api/orders",
            Status = EntityStatus.Draft,
            Source = EntitySource.Custom,
            Category = "CRM",
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();
        await SeedTemplateBindingAsync(entity);

        var first = await _menuRegistrar.RegisterAsync(entity, "tester");
        var second = await _menuRegistrar.RegisterAsync(entity, "tester");

        first.Success.Should().BeTrue();
        second.Success.Should().BeTrue();
        first.FunctionNodeId.Should().Be(second.FunctionNodeId);

        var nodes = await _db.FunctionNodes
            .Where(f => f.Code == first.FunctionCode)
            .ToListAsync();
        nodes.Should().HaveCount(1);
    }

    public void Dispose()
    {
        _db?.Dispose();
    }

    private static IEnumerable<FunctionNodeDto> FlattenTree(IEnumerable<FunctionNodeDto> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            if (node.Children?.Count > 0)
            {
                foreach (var child in FlattenTree(node.Children))
                {
                    yield return child;
                }
            }
        }
    }

    private async Task<TemplateBinding> SeedTemplateBindingAsync(EntityDefinition entity)
    {
        var entityType = entity.EntityRoute ?? entity.EntityName;
        var template = new FormTemplate
        {
            Name = $"{entity.EntityName} Default",
            EntityType = entityType,
            UserId = "__system__",
            IsSystemDefault = true,
            UsageType = FormTemplateUsageType.Detail,
            LayoutJson = "{}"
        };

        var binding = new TemplateBinding
        {
            EntityType = entityType,
            UsageType = FormTemplateUsageType.Detail,
            Template = template,
            IsSystem = true,
            UpdatedBy = "system",
            UpdatedAt = DateTime.UtcNow
        };

        await _db.FormTemplates.AddAsync(template);
        await _db.TemplateBindings.AddAsync(binding);
        await _db.SaveChangesAsync();

        return binding;
    }
}

/// <summary>
/// DDL脚本类型常量
/// </summary>
public static class DDLScriptType
{
    public const string Create = "Create";
    public const string Alter = "Alter";
    public const string Drop = "Drop";
    public const string Rollback = "Rollback";
}

/// <summary>
/// DDL脚本状态常量
/// </summary>
public static class DDLScriptStatus
{
    public const string Pending = "Pending";
    public const string Success = "Success";
    public const string Failed = "Failed";
    public const string RolledBack = "RolledBack";
}

/// <summary>
/// 实体状态常量
/// </summary>
public static class EntityStatus
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Modified = "Modified";
}
