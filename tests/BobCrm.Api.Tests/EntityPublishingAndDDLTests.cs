using System.Text.Json;
using System.Threading;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
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
    private readonly DefaultTemplateGenerator _templateGenerator;
    private readonly TemplateBindingService _bindingService;
    private readonly AccessService _accessService;
    private readonly FunctionService _functionService;
    private readonly Mock<IDefaultTemplateService> _defaultTemplateService;
    private readonly EntityMenuRegistrar _menuRegistrar;
    private readonly IConfiguration _configuration;

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

        var templateLogger = new Mock<ILogger<DefaultTemplateGenerator>>();
        _templateGenerator = new DefaultTemplateGenerator(_db, templateLogger.Object);

        var bindingLogger = new Mock<ILogger<TemplateBindingService>>();
        _bindingService = new TemplateBindingService(_db, bindingLogger.Object);

        var multilingualLogger = Mock.Of<ILogger<MultilingualFieldService>>();
        var multilingual = new MultilingualFieldService(_db, multilingualLogger);
        _functionService = new FunctionService(_db, multilingual);
        var roleService = new RoleService(_db);
        _accessService = new AccessService(_db, CreateUserManager(_db), CreateRoleManager(_db), multilingual, _functionService, roleService, TimeProvider.System);

        var menuRegistrarLogger = new Mock<ILogger<EntityMenuRegistrar>>();
        _menuRegistrar = new EntityMenuRegistrar(_db, menuRegistrarLogger.Object);

        _defaultTemplateService = new Mock<IDefaultTemplateService>();
        _defaultTemplateService
            .Setup(s => s.EnsureTemplatesAsync(
                It.IsAny<EntityDefinition>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns<EntityDefinition, string?, bool, CancellationToken>((entity, _, force, ct) =>
                _templateGenerator.EnsureTemplatesAsync(entity, force, ct));

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EntityPublishing:WithdrawalMode"] = "Logical"
            })
            .Build();

        _db.RoleProfiles.Add(new RoleProfile
        {
            Code = "SYS.ADMIN",
            Name = "System Administrator",
            IsSystem = true,
            IsEnabled = true
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task PublishNewEntityAsync_ShouldFail_WhenEntityNotFound()
    {
        // Arrange
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object, tableExists: true);
        var service = CreatePublishingService(ddlExecutor);

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
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(ddlExecutor);

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
    public async Task PublishNewEntityAsync_ShouldInvokeDefaultTemplateService()
    {
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(ddlExecutor);

        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "Test",
            EntityName = "Order",
            EntityRoute = "order",
            ApiEndpoint = "/api/orders",
            Status = EntityStatus.Draft,
            Fields = new List<FieldMetadata>
            {
                new()
                {
                    PropertyName = "Name",
                    DataType = FieldDataType.String,
                    SortOrder = 0,
                    IsRequired = true,
                    DisplayName = new Dictionary<string, string?> { ["zh"] = "名称" }
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id, "publisher");

        result.Success.Should().BeTrue();
        _defaultTemplateService.Verify(
            s => s.EnsureTemplatesAsync(
                It.Is<EntityDefinition>(e => e.Id == entity.Id),
                "publisher",
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task PublishNewEntityAsync_ShouldFail_WhenLookupEntityNotFound()
    {
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(ddlExecutor);

        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "Test",
            EntityName = "Order",
            EntityRoute = "order",
            ApiEndpoint = "/api/orders",
            Status = EntityStatus.Draft,
            Fields = new List<FieldMetadata>
            {
                new()
                {
                    PropertyName = "CustomerId",
                    DataType = FieldDataType.Integer,
                    SortOrder = 0,
                    LookupEntityName = "Customer",
                    ForeignKeyAction = ForeignKeyAction.Restrict
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Lookup referenced entities");
    }

    [Fact]
    public async Task PublishNewEntityAsync_ShouldFail_WhenLookupSetNullButNotNull()
    {
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(ddlExecutor);

        await _db.EntityDefinitions.AddAsync(new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "Test",
            EntityName = "Customer",
            Status = EntityStatus.Published,
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        });
        await _db.SaveChangesAsync();

        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "Test",
            EntityName = "Order",
            EntityRoute = "order",
            ApiEndpoint = "/api/orders",
            Status = EntityStatus.Draft,
            Fields = new List<FieldMetadata>
            {
                new()
                {
                    PropertyName = "CustomerId",
                    DataType = FieldDataType.Integer,
                    SortOrder = 0,
                    IsRequired = true,
                    LookupEntityName = "Customer",
                    ForeignKeyAction = ForeignKeyAction.SetNull
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        var result = await service.PublishNewEntityAsync(entity.Id);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ForeignKeyAction=SetNull");
    }

    [Fact]
    public async Task PublishEntityChangesAsync_ShouldFail_WhenEntityNotFound()
    {
        // Arrange
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(ddlExecutor);

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
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(ddlExecutor);

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
        result.Templates.Should().BeEmpty();
        result.TemplateBindings.Should().BeEmpty();
        result.MenuNodes.Should().BeEmpty();
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
        var service = CreatePublishingService(mockDDLExecutor.Object);

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
        result.Templates.Should().HaveCount(3);
        result.TemplateBindings.Should().HaveCount(3);
        result.MenuNodes.Should().NotBeEmpty();
        result.TemplateBindings.Should().Contain(b => b.UsageType == FormTemplateUsageType.List && b.RequiredFunctionCode == "CRM.CORE.PRODUCT");
        result.MenuNodes.Should().Contain(m => m.UsageType == FormTemplateUsageType.List && m.Route == "/products");

        // 验证实体状态已更新
        var updatedEntity = await _db.EntityDefinitions.FindAsync(entityId);
        updatedEntity!.Status.Should().Be(EntityStatus.Published);
        updatedEntity.UpdatedBy.Should().Be("test-user");

        var templates = await _db.FormTemplates
            .Where(t => t.EntityType == entity.EntityRoute)
            .ToListAsync();
        templates.Should().HaveCount(4); // Detail (user), Detail (auto), Edit (auto), List (auto)

        foreach (var template in templates)
        {
            AssertTemplateContainsField(template, "Name");
        }

        var detailTemplate = templates.First(t => t.UsageType == FormTemplateUsageType.Detail);
        using (var detailDoc = JsonDocument.Parse(detailTemplate.LayoutJson!))
        {
            detailDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
            detailDoc.RootElement.GetArrayLength().Should().BeGreaterThan(0);
        }

        var listTemplate = templates.First(t => t.UsageType == FormTemplateUsageType.List);
        using (var listDoc = JsonDocument.Parse(listTemplate.LayoutJson!))
        {
            listDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
            listDoc.RootElement.EnumerateArray()
                .Any(e => e.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "datagrid")
                .Should().BeTrue();
        }

        var bindings = await _db.TemplateBindings
            .Where(b => b.EntityType == entity.EntityRoute)
            .ToListAsync();
        bindings.Should().HaveCount(3);

        var nodes = await _db.FunctionNodes
            .Where(n => n.Code.StartsWith("CRM.CORE.PRODUCT"))
            .ToListAsync();
        nodes.Should().NotBeEmpty();

        var rolePermissions = await _db.RoleFunctionPermissions.ToListAsync();
        var listBinding = bindings.First(b => b.UsageType == FormTemplateUsageType.List);
        rolePermissions.Should().Contain(r => r.TemplateBindingId == listBinding.Id);

        // 验证调用了锁定服务
        _mockLockService.Verify(
            x => x.LockEntityAsync(entityId, It.IsAny<string>()),
            Times.Once);

        _defaultTemplateService.Verify(
            x => x.EnsureTemplatesAsync(
                It.Is<EntityDefinition>(e => e.Id == entityId),
                "test-user",
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishEntityChangesAsync_ShouldSucceed_WithValidModifiedEntity()
    {
        // Arrange
        var mockDDLExecutor = new Mock<DDLExecutionService>(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(mockDDLExecutor.Object);

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
        result.Templates.Should().NotBeEmpty();
        result.TemplateBindings.Should().HaveCount(3);
        result.MenuNodes.Should().NotBeEmpty();

        // 验证实体状态已更新
        var updatedEntity = await _db.EntityDefinitions.FindAsync(entityId);
        updatedEntity!.Status.Should().Be(EntityStatus.Published);
        updatedEntity.UpdatedBy.Should().Be("test-user");

        var templates = await _db.FormTemplates
            .Where(t => t.EntityType == entity.EntityRoute)
            .ToListAsync();
        templates.Should().HaveCount(4); // Detail (user), Detail (auto), Edit (auto), List (auto)

        foreach (var template in templates)
        {
            AssertTemplateContainsField(template, "Name");

            // Only system-generated templates should have NewField (user template was created before field was added)
            if (template.IsSystemDefault)
            {
                AssertTemplateContainsField(template, "NewField");
            }
        }

        var bindings = await _db.TemplateBindings
            .Where(b => b.EntityType == entity.EntityRoute)
            .ToListAsync();
        bindings.Should().HaveCount(3);

        var nodes = await _db.FunctionNodes
            .Where(n => n.Code.StartsWith("CRM.CORE.PRODUCT"))
            .ToListAsync();
        nodes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PublishNewEntityAsync_ShouldFail_WhenTableAlreadyExists()
    {
        // Arrange
        var mockDDLExecutor = new Mock<DDLExecutionService>(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(mockDDLExecutor.Object);

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

    private sealed class NoOpDDLExecutionService : DDLExecutionService
    {
        private readonly bool _tableExists;

        public NoOpDDLExecutionService(AppDbContext db, ILogger<DDLExecutionService> logger, bool tableExists = false)
            : base(db, logger)
        {
            _tableExists = tableExists;
        }

        public override Task<DDLScript> ExecuteDDLAsync(
            Guid entityDefinitionId,
            string scriptType,
            string sqlScript,
            string? createdBy = null)
        {
            var script = new DDLScript
            {
                EntityDefinitionId = entityDefinitionId,
                ScriptType = scriptType,
                SqlScript = sqlScript,
                Status = DDLScriptStatus.Success,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                ExecutedAt = DateTime.UtcNow
            };

            return Task.FromResult(script);
        }

        public override Task<bool> TableExistsAsync(string tableName)
            => Task.FromResult(_tableExists);
    }

    private sealed class MapDDLExecutionService : DDLExecutionService
    {
        private readonly Dictionary<string, bool> _tableExistsMap;
        public List<(Guid EntityDefinitionId, string ScriptType, string SqlScript)> ExecutedScripts { get; } = new();

        public MapDDLExecutionService(
            AppDbContext db,
            ILogger<DDLExecutionService> logger,
            Dictionary<string, bool> tableExistsMap)
            : base(db, logger)
        {
            _tableExistsMap = tableExistsMap;
        }

        public override Task<bool> TableExistsAsync(string tableName)
            => Task.FromResult(_tableExistsMap.TryGetValue(tableName, out var exists) && exists);

        public override Task<DDLScript> ExecuteDDLAsync(Guid entityDefinitionId, string scriptType, string sqlScript, string? createdBy = null)
        {
            ExecutedScripts.Add((entityDefinitionId, scriptType, sqlScript));
            var script = new DDLScript
            {
                EntityDefinitionId = entityDefinitionId,
                ScriptType = scriptType,
                SqlScript = sqlScript,
                Status = DDLScriptStatus.Success,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                ExecutedAt = DateTime.UtcNow
            };

            return Task.FromResult(script);
        }
    }

    private EntityPublishingService CreatePublishingService(DDLExecutionService ddlExecutor)
        => new(
            _db,
            _ddlGenerator,
            ddlExecutor,
            _mockLockService.Object,
            _bindingService,
            _functionService,
            _defaultTemplateService.Object,
            _configuration,
            _mockPublishLogger.Object);

    private async Task<TemplateBinding> SeedTemplateBindingAsync(EntityDefinition entity)
    {
        var template = new FormTemplate
        {
            Name = $"{entity.EntityName} Detail",
            EntityType = entity.EntityRoute ?? entity.EntityName.ToLowerInvariant(),
            UserId = "system",
            UsageType = FormTemplateUsageType.Detail,
            LayoutJson = "[{\"id\":\"Name_detail\",\"type\":\"text\",\"label\":\"Name\",\"dataField\":\"Name\",\"required\":false,\"w\":6}]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _db.FormTemplates.AddAsync(template);
        await _db.SaveChangesAsync();

        var binding = new TemplateBinding
        {
            EntityType = template.EntityType,
            UsageType = FormTemplateUsageType.Detail,
            TemplateId = template.Id,
            IsSystem = true,
            UpdatedBy = "seed",
            UpdatedAt = DateTime.UtcNow
        };
        await _db.TemplateBindings.AddAsync(binding);
        await _db.SaveChangesAsync();

        return binding;
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
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            normalizer,
            describer,
            services,
            logger);
    }

    private static RoleManager<IdentityRole> CreateRoleManager(AppDbContext context)
    {
        var store = new RoleStore<IdentityRole>(context);
        var roleValidators = new List<IRoleValidator<IdentityRole>> { new RoleValidator<IdentityRole>() };
        var normalizer = new UpperInvariantLookupNormalizer();
        var describer = new IdentityErrorDescriber();

        var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        var logger = services.GetRequiredService<ILogger<RoleManager<IdentityRole>>>();

        return new RoleManager<IdentityRole>(store, roleValidators, normalizer, describer, logger);
    }

    private static void AssertTemplateContainsField(FormTemplate template, string fieldName)
    {
        template.LayoutJson.Should().NotBeNullOrWhiteSpace();
        using var layoutDoc = JsonDocument.Parse(template.LayoutJson!);
        var root = layoutDoc.RootElement;
        root.ValueKind.Should().Be(JsonValueKind.Array);

        if (template.UsageType == FormTemplateUsageType.List)
        {
            var datagrid = root.EnumerateArray()
                .FirstOrDefault(e => e.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "datagrid");

            datagrid.ValueKind.Should().NotBe(JsonValueKind.Undefined, "list templates should include a datagrid widget");

            var columnsJson = datagrid.GetProperty("columnsJson").GetString();
            columnsJson.Should().NotBeNullOrWhiteSpace();
            using var columnsDoc = JsonDocument.Parse(columnsJson!);
            var targetField = fieldName.ToLowerInvariant();

            columnsDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
            columnsDoc.RootElement.EnumerateArray()
                .Any(col =>
                    col.TryGetProperty("field", out var fieldProp) &&
                    string.Equals(fieldProp.GetString(), targetField, StringComparison.OrdinalIgnoreCase))
                .Should().BeTrue($"list template should include column '{fieldName}'");
        }
        else
        {
            root.EnumerateArray()
                .Any(widget =>
                    widget.TryGetProperty("dataField", out var dataField) &&
                    string.Equals(dataField.GetString(), fieldName, StringComparison.OrdinalIgnoreCase))
                .Should().BeTrue($"template should include field '{fieldName}'");
        }
    }

    #region Enum Validation Tests

    [Fact]
    public async Task PublishNewEntityAsync_ShouldFail_WhenEnumFieldHasNullEnumDefinitionId()
    {
        // Arrange
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(ddlExecutor);

        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "ProductWithInvalidEnum",
            Status = "Draft",
            Fields = new List<FieldMetadata>
            {
                new()
                {
                    PropertyName = "ProductType",
                    DataType = FieldDataType.Enum,
                    EnumDefinitionId = null, // Invalid: null
                    IsRequired = true
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        // Act
        var result = await service.PublishNewEntityAsync(entity.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ProductType");
        result.ErrorMessage.Should().Contain("EnumDefinitionId is null");
    }

    [Fact]
    public async Task PublishNewEntityAsync_ShouldFail_WhenEnumFieldReferencesNonExistentEnum()
    {
        // Arrange
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(ddlExecutor);

        var nonExistentEnumId = Guid.NewGuid();
        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "ProductWithMissingEnum",
            Status = "Draft",
            Fields = new List<FieldMetadata>
            {
                new()
                {
                    PropertyName = "ProductType",
                    DataType = FieldDataType.Enum,
                    EnumDefinitionId = nonExistentEnumId, // Does not exist
                    IsRequired = true
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        // Act
        var result = await service.PublishNewEntityAsync(entity.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ProductType");
        result.ErrorMessage.Should().Contain("does not exist");
    }

    [Fact]
    public async Task PublishNewEntityAsync_ShouldFail_WhenEnumFieldReferencesDisabledEnum()
    {
        // Arrange
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(ddlExecutor);

        // Create a disabled enum
        var disabledEnum = new EnumDefinition
        {
            Code = $"disabled_enum_{Guid.NewGuid():N}",
            DisplayName = new() { { "zh", "禁用枚举" } },
            IsEnabled = false,
            IsSystem = false
        };
        await _db.EnumDefinitions.AddAsync(disabledEnum);
        await _db.SaveChangesAsync();

        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "ProductWithDisabledEnum",
            Status = "Draft",
            Fields = new List<FieldMetadata>
            {
                new()
                {
                    PropertyName = "ProductType",
                    DataType = FieldDataType.Enum,
                    EnumDefinitionId = disabledEnum.Id,
                    IsRequired = true
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        // Act
        var result = await service.PublishNewEntityAsync(entity.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ProductType");
        result.ErrorMessage.Should().Contain("disabled");
    }

    [Fact]
    public async Task PublishNewEntityAsync_ShouldSucceed_WhenEnumFieldReferencesValidEnum()
    {
        // Arrange
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(ddlExecutor);

        // Create a valid enabled enum
        var validEnum = new EnumDefinition
        {
            Code = $"valid_enum_{Guid.NewGuid():N}",
            DisplayName = new() { { "zh", "有效枚举" } },
            IsEnabled = true,
            IsSystem = false,
            Options = new List<EnumOption>
            {
                new() { Value = "OPT1", DisplayName = new() { { "zh", "选项1" } }, SortOrder = 0 }
            }
        };
        await _db.EnumDefinitions.AddAsync(validEnum);
        await _db.SaveChangesAsync();

        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "ProductWithValidEnum",
            Status = "Draft",
            Fields = new List<FieldMetadata>
            {
                new()
                {
                    PropertyName = "ProductType",
                    DataType = FieldDataType.Enum,
                    EnumDefinitionId = validEnum.Id,
                    IsRequired = true,
                    Source = "Custom"
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        // Act
        var result = await service.PublishNewEntityAsync(entity.Id);

        // Assert
        result.Success.Should().BeTrue($"enum validation should pass for valid enum: {result.ErrorMessage}");
    }

    [Fact]
    public async Task PublishEntityChangesAsync_ShouldFail_WhenAddingInvalidEnumField()
    {
        // Arrange
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(ddlExecutor);

        // Create a published entity first
        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "ExistingProduct",
            Status = "Published",
            Fields = new List<FieldMetadata>
            {
                new()
                {
                    PropertyName = "Name",
                    DataType = FieldDataType.String,
                    IsRequired = true,
                    Source = "Custom"
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        // Now modify it by adding an invalid enum field
        entity.Status = "Modified";
        entity.Fields.Add(new FieldMetadata
        {
            PropertyName = "Category",
            DataType = FieldDataType.Enum,
            EnumDefinitionId = null, // Invalid
            IsRequired = false,
            Source = "Custom"
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await service.PublishEntityChangesAsync(entity.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Category");
        result.ErrorMessage.Should().Contain("EnumDefinitionId is null");
    }

    [Fact]
    public async Task PublishNewEntityAsync_ShouldValidateMultipleEnumFields()
    {
        // Arrange
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(ddlExecutor);

        // Create valid enums
        var enum1 = new EnumDefinition
        {
            Code = $"enum1_{Guid.NewGuid():N}",
            DisplayName = new() { { "zh", "枚举1" } },
            IsEnabled = true
        };
        var enum2 = new EnumDefinition
        {
            Code = $"enum2_{Guid.NewGuid():N}",
            DisplayName = new() { { "zh", "枚举2" } },
            IsEnabled = true
        };
        await _db.EnumDefinitions.AddRangeAsync(enum1, enum2);
        await _db.SaveChangesAsync();

        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "ProductWithMultipleEnums",
            Status = "Draft",
            Fields = new List<FieldMetadata>
            {
                new()
                {
                    PropertyName = "Type",
                    DataType = FieldDataType.Enum,
                    EnumDefinitionId = enum1.Id,
                    IsRequired = true,
                    Source = "Custom"
                },
                new()
                {
                    PropertyName = "Category",
                    DataType = FieldDataType.Enum,
                    EnumDefinitionId = enum2.Id,
                    IsRequired = false,
                    Source = "Custom"
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        // Act
        var result = await service.PublishNewEntityAsync(entity.Id);

        // Assert
        result.Success.Should().BeTrue("all enum fields reference valid enums");
    }

    #endregion

    #region Cascade Publishing & Withdrawal Tests

    [Fact]
    public async Task PublishNewEntityAsync_ShouldCascadePublish_DraftLookupDependency_AndSkipTemplatesForChild()
    {
        // Arrange
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);

        var ensuredEntities = new List<Guid>();
        _defaultTemplateService
            .Setup(s => s.EnsureTemplatesAsync(It.IsAny<EntityDefinition>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<EntityDefinition, string?, bool, CancellationToken>((entity, _, _, _) => ensuredEntities.Add(entity.Id))
            .Returns<EntityDefinition, string?, bool, CancellationToken>((entity, _, force, ct) =>
                _templateGenerator.EnsureTemplatesAsync(entity, force, ct));

        var service = CreatePublishingService(ddlExecutor);

        var child = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "ChildEntity",
            EntityRoute = "child",
            ApiEndpoint = "/api/childs",
            Status = EntityStatus.Draft,
            Fields = new List<FieldMetadata>
            {
                new()
                {
                    PropertyName = "Name",
                    DataType = FieldDataType.String,
                    IsRequired = true,
                    Source = "Custom"
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        var parent = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "ParentEntity",
            EntityRoute = "parent",
            ApiEndpoint = "/api/parents",
            Status = EntityStatus.Draft,
            Fields = new List<FieldMetadata>
            {
                new()
                {
                    PropertyName = "ChildId",
                    DataType = FieldDataType.Guid,
                    LookupEntityName = child.EntityName,
                    IsRequired = false,
                    Source = "Custom"
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddRangeAsync(child, parent);
        await _db.SaveChangesAsync();

        // Act
        var result = await service.PublishNewEntityAsync(parent.Id, "tester");

        // Assert
        result.Success.Should().BeTrue(result.ErrorMessage);

        var reloadedChild = await _db.EntityDefinitions.FirstAsync(e => e.Id == child.Id);
        reloadedChild.Status.Should().Be(BobCrm.Api.Base.Models.EntityStatus.Published);
        reloadedChild.Source.Should().Be(BobCrm.Api.Base.Models.EntitySource.Custom);

        ensuredEntities.Should().Contain(parent.Id);
        ensuredEntities.Should().NotContain(child.Id);
    }

    [Fact]
    public async Task PublishNewEntityAsync_ShouldRepublish_WithdrawnLookupDependency_WithoutDDL()
    {
        // Arrange
        var tableExists = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        var child = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "WithdrawnChild",
            EntityRoute = "withdrawn-child",
            ApiEndpoint = "/api/withdrawnchilds",
            Status = EntityStatus.Withdrawn,
            IsEnabled = false,
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        var parent = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "WithdrawnParent",
            EntityRoute = "withdrawn-parent",
            ApiEndpoint = "/api/withdrawnparents",
            Status = EntityStatus.Draft,
            Fields = new List<FieldMetadata>
            {
                new()
                {
                    PropertyName = "ChildId",
                    DataType = FieldDataType.Guid,
                    LookupEntityName = child.EntityName,
                    IsRequired = false,
                    Source = "Custom"
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddRangeAsync(child, parent);
        await _db.SaveChangesAsync();

        tableExists[child.DefaultTableName] = true;  // logical withdraw: table still exists
        tableExists[parent.DefaultTableName] = false;

        var ddlExecutor = new MapDDLExecutionService(_db, _mockDDLLogger.Object, tableExists);

        var ensuredEntities = new List<Guid>();
        _defaultTemplateService
            .Setup(s => s.EnsureTemplatesAsync(It.IsAny<EntityDefinition>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<EntityDefinition, string?, bool, CancellationToken>((entity, _, _, _) => ensuredEntities.Add(entity.Id))
            .Returns<EntityDefinition, string?, bool, CancellationToken>((entity, _, force, ct) =>
                _templateGenerator.EnsureTemplatesAsync(entity, force, ct));

        var service = CreatePublishingService(ddlExecutor);

        // Act
        var result = await service.PublishNewEntityAsync(parent.Id, "tester");

        // Assert
        result.Success.Should().BeTrue(result.ErrorMessage);

        var reloadedChild = await _db.EntityDefinitions.FirstAsync(e => e.Id == child.Id);
        reloadedChild.Status.Should().Be(BobCrm.Api.Base.Models.EntityStatus.Published);
        reloadedChild.Source.Should().Be(BobCrm.Api.Base.Models.EntitySource.System);

        ddlExecutor.ExecutedScripts.Any(s => s.EntityDefinitionId == child.Id).Should().BeFalse();
        ddlExecutor.ExecutedScripts.Count(s => s.EntityDefinitionId == parent.Id && s.ScriptType == DDLScriptType.Create).Should().Be(1);

        ensuredEntities.Should().Contain(parent.Id);
        ensuredEntities.Should().NotContain(child.Id);
    }

    [Fact]
    public async Task WithdrawAsync_ShouldFail_WhenReferencedByOtherPublishedEntity()
    {
        // Arrange
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(ddlExecutor);

        var referenced = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "ReferencedEntity",
            EntityRoute = "referenced",
            ApiEndpoint = "/api/referenceds",
            Status = EntityStatus.Published,
            IsEnabled = true,
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        var referrer = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "ReferrerEntity",
            EntityRoute = "referrer",
            ApiEndpoint = "/api/referrers",
            Status = EntityStatus.Published,
            IsEnabled = true,
            Fields = new List<FieldMetadata>
            {
                new()
                {
                    PropertyName = "ReferencedId",
                    DataType = FieldDataType.Guid,
                    LookupEntityName = referenced.EntityName,
                    IsRequired = false,
                    Source = "Custom"
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddRangeAsync(referenced, referrer);
        await _db.SaveChangesAsync();

        // Act
        var result = await service.WithdrawAsync(referenced.Id, "tester");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("referenced");

        var reloaded = await _db.EntityDefinitions.FirstAsync(e => e.Id == referenced.Id);
        reloaded.Status.Should().Be(EntityStatus.Published);
        reloaded.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task WithdrawAsync_ShouldSetStatusWithdrawn_AndDisableEntity_WhenLogicalMode()
    {
        // Arrange
        var ddlExecutor = new NoOpDDLExecutionService(_db, _mockDDLLogger.Object);
        var service = CreatePublishingService(ddlExecutor);

        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "StandaloneEntity",
            EntityRoute = "standalone",
            ApiEndpoint = "/api/standalones",
            Status = EntityStatus.Published,
            IsEnabled = true,
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        // Act
        var result = await service.WithdrawAsync(entity.Id, "tester");

        // Assert
        result.Success.Should().BeTrue(result.ErrorMessage);
        result.Mode.Should().Be("Logical");

        var reloaded = await _db.EntityDefinitions.FirstAsync(e => e.Id == entity.Id);
        reloaded.Status.Should().Be(BobCrm.Api.Base.Models.EntityStatus.Withdrawn);
        reloaded.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task WithdrawAsync_ShouldExecuteDropDDL_WhenPhysicalMode()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EntityPublishing:WithdrawalMode"] = "Physical"
            })
            .Build();

        var tableExists = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var ddlExecutor = new MapDDLExecutionService(_db, _mockDDLLogger.Object, tableExists);

        var service = new EntityPublishingService(
            _db,
            _ddlGenerator,
            ddlExecutor,
            _mockLockService.Object,
            _bindingService,
            _functionService,
            _defaultTemplateService.Object,
            config,
            _mockPublishLogger.Object);

        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "PhysicalWithdrawEntity",
            EntityRoute = "physical-withdraw",
            ApiEndpoint = "/api/physicalwithdraws",
            Status = EntityStatus.Published,
            IsEnabled = true,
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        // Act
        var result = await service.WithdrawAsync(entity.Id, "tester");

        // Assert
        result.Success.Should().BeTrue(result.ErrorMessage);
        result.Mode.Should().Be("Physical");
        ddlExecutor.ExecutedScripts.Any(s => s.EntityDefinitionId == entity.Id && s.ScriptType == DDLScriptType.Drop)
            .Should().BeTrue();
    }

    #endregion
}
