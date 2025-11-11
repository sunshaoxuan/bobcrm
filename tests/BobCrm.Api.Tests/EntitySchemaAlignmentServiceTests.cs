using BobCrm.Api.Domain.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BobCrm.Api.Tests;

/// <summary>
/// EntitySchemaAlignmentService 完整测试套件
/// </summary>
public class EntitySchemaAlignmentServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<DDLExecutionService> _mockDDLService;
    private readonly Mock<ILogger<EntitySchemaAlignmentService>> _mockLogger;
    private readonly EntitySchemaAlignmentService _service;

    public EntitySchemaAlignmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _db = new AppDbContext(options);

        var mockDDLLogger = new Mock<ILogger<DDLExecutionService>>();
        _mockDDLService = new Mock<DDLExecutionService>(_db, mockDDLLogger.Object);
        _mockLogger = new Mock<ILogger<EntitySchemaAlignmentService>>();

        _service = new EntitySchemaAlignmentService(
            _db,
            _mockDDLService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task AlignAllPublishedEntitiesAsync_ShouldSkip_WhenNoPublishedEntities()
    {
        // Arrange - 添加一个草稿状态的实体
        var draftEntity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "Test",
            EntityName = "Draft",
            Status = EntityStatus.Draft,
            Source = EntitySource.Custom,
            Fields = new List<FieldMetadata>()
        };
        await _db.EntityDefinitions.AddAsync(draftEntity);
        await _db.SaveChangesAsync();

        // Act
        await _service.AlignAllPublishedEntitiesAsync();

        // Assert - TableExistsAsync 不应该被调用（因为没有已发布的实体）
        _mockDDLService.Verify(
            x => x.TableExistsAsync(It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AlignAllPublishedEntitiesAsync_ShouldProcess_OnlyPublishedCustomEntities()
    {
        // Arrange
        var publishedCustom = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "Custom",
            EntityName = "Product",
            Status = EntityStatus.Published,
            Source = EntitySource.Custom,
            Fields = new List<FieldMetadata>()
        };

        var publishedSystem = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "System",
            EntityName = "Customer",
            Status = EntityStatus.Published,
            Source = EntitySource.System,
            Fields = new List<FieldMetadata>()
        };

        await _db.EntityDefinitions.AddRangeAsync(publishedCustom, publishedSystem);
        await _db.SaveChangesAsync();

        _mockDDLService.Setup(x => x.TableExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockDDLService.Setup(x => x.GetTableColumnsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<TableColumnInfo>());

        // Act
        await _service.AlignAllPublishedEntitiesAsync();

        // Assert - 只处理自定义实体，不处理系统实体
        _mockDDLService.Verify(
            x => x.TableExistsAsync("Products"),
            Times.Once
        );
        _mockDDLService.Verify(
            x => x.TableExistsAsync("Customers"),
            Times.Never
        );
    }

    [Fact]
    public async Task AlignEntitySchemaAsync_ShouldCreateTable_WhenTableDoesNotExist()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "Test",
            EntityName = "Product",
            Status = EntityStatus.Published,
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
                },
                new FieldMetadata
                {
                    PropertyName = "Price",
                    DataType = "Decimal",
                    Precision = 10,
                    Scale = 2,
                    IsRequired = true,
                    SortOrder = 2
                }
            }
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        _mockDDLService.Setup(x => x.TableExistsAsync("Products"))
            .ReturnsAsync(false);

        var capturedScript = "";
        _mockDDLService.Setup(x => x.ExecuteDDLAsync(
                It.IsAny<Guid>(),
                DDLScriptType.Create,
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<Guid, string, string, string>((id, type, script, user) => capturedScript = script)
            .ReturnsAsync(new DDLScript
            {
                Id = Guid.NewGuid(),
                Status = DDLScriptStatus.Success
            });

        // Act
        var result = await _service.AlignEntitySchemaAsync(entity);

        // Assert
        result.Should().Be(AlignmentResult.Aligned);
        _mockDDLService.Verify(
            x => x.ExecuteDDLAsync(
                entity.Id,
                DDLScriptType.Create,
                It.IsAny<string>(),
                "System"),
            Times.Once
        );

        // 验证 CREATE TABLE SQL 包含必要的列
        capturedScript.Should().Contain("CREATE TABLE");
        capturedScript.Should().Contain("\"Products\"");
        capturedScript.Should().Contain("\"Id\" uuid PRIMARY KEY");
        capturedScript.Should().Contain("\"Name\" varchar(100) NOT NULL");
        capturedScript.Should().Contain("\"Price\" numeric(10,2) NOT NULL");
        capturedScript.Should().Contain("\"CreatedAt\"");
        capturedScript.Should().Contain("\"UpdatedAt\"");
    }

    [Fact]
    public async Task AlignEntitySchemaAsync_ShouldReturnAlreadyAligned_WhenTableMatchesDefinition()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "Test",
            EntityName = "Product",
            Status = EntityStatus.Published,
            Source = EntitySource.Custom,
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata { PropertyName = "Name", DataType = "String", SortOrder = 1 }
            }
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        _mockDDLService.Setup(x => x.TableExistsAsync("Products"))
            .ReturnsAsync(true);

        // 表中的列与定义完全匹配
        _mockDDLService.Setup(x => x.GetTableColumnsAsync("Products"))
            .ReturnsAsync(new List<TableColumnInfo>
            {
                new TableColumnInfo { ColumnName = "Id", DataType = "uuid" },
                new TableColumnInfo { ColumnName = "CreatedAt", DataType = "timestamp without time zone" },
                new TableColumnInfo { ColumnName = "UpdatedAt", DataType = "timestamp without time zone" },
                new TableColumnInfo { ColumnName = "Name", DataType = "text" }
            });

        // Act
        var result = await _service.AlignEntitySchemaAsync(entity);

        // Assert
        result.Should().Be(AlignmentResult.AlreadyAligned);
        _mockDDLService.Verify(
            x => x.ExecuteDDLAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AlignEntitySchemaAsync_ShouldAddMissingColumns_WhenColumnsAreMissing()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "Test",
            EntityName = "Product",
            Status = EntityStatus.Published,
            Source = EntitySource.Custom,
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata { PropertyName = "Name", DataType = "String", SortOrder = 1 },
                new FieldMetadata { PropertyName = "Price", DataType = "Decimal", Precision = 10, Scale = 2, SortOrder = 2 },
                new FieldMetadata { PropertyName = "Stock", DataType = "Int32", SortOrder = 3 }
            }
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        _mockDDLService.Setup(x => x.TableExistsAsync("Products"))
            .ReturnsAsync(true);

        // 表中只有 Name，缺少 Price 和 Stock
        _mockDDLService.Setup(x => x.GetTableColumnsAsync("Products"))
            .ReturnsAsync(new List<TableColumnInfo>
            {
                new TableColumnInfo { ColumnName = "Id", DataType = "uuid" },
                new TableColumnInfo { ColumnName = "Name", DataType = "text" }
            });

        _mockDDLService.Setup(x => x.ExecuteDDLBatchAsync(
                It.IsAny<Guid>(),
                It.IsAny<List<(string, string)>>(),
                It.IsAny<string>()))
            .ReturnsAsync(new List<DDLScript>());

        // Act
        var result = await _service.AlignEntitySchemaAsync(entity);

        // Assert
        result.Should().Be(AlignmentResult.Aligned);

        // 验证调用了 ExecuteDDLBatchAsync
        // 注意：每个字段可能生成多个SQL (ADD COLUMN + UPDATE + SET NOT NULL)，所以不检查具体数量
        _mockDDLService.Verify(
            x => x.ExecuteDDLBatchAsync(
                entity.Id,
                It.Is<List<(string, string)>>(scripts =>
                    scripts.Count >= 2 && // 至少2个语句（每个字段至少1个ADD COLUMN）
                    scripts.Any(s => s.Item2.Contains("ADD COLUMN \"Price\"")) &&
                    scripts.Any(s => s.Item2.Contains("ADD COLUMN \"Stock\""))
                ),
                "System"),
            Times.Once
        );
    }

    [Fact]
    public async Task AlignEntitySchemaAsync_ShouldWarnAboutExtraColumns_ButNotDeleteThem()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "Test",
            EntityName = "Product",
            Status = EntityStatus.Published,
            Source = EntitySource.Custom,
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata { PropertyName = "Name", DataType = "String", SortOrder = 1 }
            }
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        _mockDDLService.Setup(x => x.TableExistsAsync("Products"))
            .ReturnsAsync(true);

        // 表中有额外的列 "OldField"
        _mockDDLService.Setup(x => x.GetTableColumnsAsync("Products"))
            .ReturnsAsync(new List<TableColumnInfo>
            {
                new TableColumnInfo { ColumnName = "Id", DataType = "uuid" },
                new TableColumnInfo { ColumnName = "Name", DataType = "text" },
                new TableColumnInfo { ColumnName = "OldField", DataType = "text" } // 多余的列
            });

        // Act
        var result = await _service.AlignEntitySchemaAsync(entity);

        // Assert
        result.Should().Be(AlignmentResult.AlreadyAligned); // 不执行修改

        // 验证记录了警告日志
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("extra columns")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );

        // 验证没有执行任何 DDL（不删除列）
        _mockDDLService.Verify(
            x => x.ExecuteDDLAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public void MapDataTypeToSQL_ShouldMapStringTypes_Correctly()
    {
        // Arrange & Act
        var varchar100 = MapDataType(new FieldMetadata { DataType = "String", Length = 100, IsRequired = true });
        var text = MapDataType(new FieldMetadata { DataType = "String", IsRequired = false });

        // Assert
        varchar100.Should().Be("varchar(100) NOT NULL");
        text.Should().Be("text");
    }

    [Fact]
    public void MapDataTypeToSQL_ShouldMapNumericTypes_Correctly()
    {
        // Arrange & Act
        var int32 = MapDataType(new FieldMetadata { DataType = "Int32", IsRequired = true });
        var int64 = MapDataType(new FieldMetadata { DataType = "Int64", IsRequired = false });
        var decimal1 = MapDataType(new FieldMetadata { DataType = "Decimal", Precision = 10, Scale = 2, IsRequired = true });
        var decimal2 = MapDataType(new FieldMetadata { DataType = "Decimal", IsRequired = false });

        // Assert
        int32.Should().Be("integer NOT NULL");
        int64.Should().Be("bigint");
        decimal1.Should().Be("numeric(10,2) NOT NULL");
        decimal2.Should().Be("numeric");
    }

    [Fact]
    public void MapDataTypeToSQL_ShouldMapOtherTypes_Correctly()
    {
        // Arrange & Act
        var boolean = MapDataType(new FieldMetadata { DataType = "Boolean", IsRequired = true });
        var datetime = MapDataType(new FieldMetadata { DataType = "DateTime", IsRequired = false });
        var guid = MapDataType(new FieldMetadata { DataType = "Guid", IsRequired = true });
        var json = MapDataType(new FieldMetadata { DataType = "Json", IsRequired = false });

        // Assert
        boolean.Should().Be("boolean NOT NULL");
        datetime.Should().Be("timestamp without time zone");
        guid.Should().Be("uuid NOT NULL");
        json.Should().Be("jsonb");
    }

    [Fact]
    public async Task AlignAllPublishedEntitiesAsync_ShouldHandleMultipleEntities()
    {
        // Arrange
        var entity1 = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "Test",
            EntityName = "Product",
            Status = EntityStatus.Published,
            Source = EntitySource.Custom,
            Fields = new List<FieldMetadata>()
        };

        var entity2 = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "Test",
            EntityName = "Order",
            Status = EntityStatus.Published,
            Source = EntitySource.Custom,
            Fields = new List<FieldMetadata>()
        };

        await _db.EntityDefinitions.AddRangeAsync(entity1, entity2);
        await _db.SaveChangesAsync();

        _mockDDLService.Setup(x => x.TableExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockDDLService.Setup(x => x.GetTableColumnsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<TableColumnInfo>());

        // Act
        await _service.AlignAllPublishedEntitiesAsync();

        // Assert
        _mockDDLService.Verify(x => x.TableExistsAsync("Products"), Times.Once);
        _mockDDLService.Verify(x => x.TableExistsAsync("Orders"), Times.Once);
    }

    [Fact]
    public async Task DeleteFieldAsync_ShouldRemoveMetadata_WhenLogicalDelete()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();

        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "Product",
            Status = EntityStatus.Published,
            Source = EntitySource.Custom,
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    Id = fieldId,
                    PropertyName = "OldField",
                    DataType = "String",
                    SortOrder = 1
                }
            }
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        // Act - 逻辑删除（不删除数据库列）
        var result = await _service.DeleteFieldAsync(entityId, fieldId, physicalDelete: false);

        // Assert
        result.Success.Should().BeTrue();
        result.LogicalDeleteCompleted.Should().BeTrue();
        result.PhysicalDeleteCompleted.Should().BeFalse();

        // 验证元数据已删除
        var updatedEntity = await _db.EntityDefinitions
            .Include(e => e.Fields)
            .FirstOrDefaultAsync(e => e.Id == entityId);
        updatedEntity!.Fields.Should().NotContain(f => f.Id == fieldId);

        // 验证没有执行 DDL
        _mockDDLService.Verify(
            x => x.ExecuteDDLAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task DeleteFieldAsync_ShouldRemoveColumn_WhenPhysicalDelete()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();

        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "Product",
            Status = EntityStatus.Published,
            Source = EntitySource.Custom,
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    Id = fieldId,
                    PropertyName = "OldField",
                    DataType = "String",
                    SortOrder = 1
                }
            }
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        _mockDDLService.Setup(x => x.TableExistsAsync("Products"))
            .ReturnsAsync(true);

        _mockDDLService.Setup(x => x.ExecuteDDLAsync(
                entityId,
                DDLScriptType.Alter,
                It.Is<string>(sql => sql.Contains("DROP COLUMN")),
                It.IsAny<string>()))
            .ReturnsAsync(new DDLScript
            {
                Id = Guid.NewGuid(),
                Status = DDLScriptStatus.Success
            });

        // Act - 物理删除（删除数据库列）
        var result = await _service.DeleteFieldAsync(entityId, fieldId, physicalDelete: true, performedBy: "admin");

        // Assert
        result.Success.Should().BeTrue();
        result.LogicalDeleteCompleted.Should().BeTrue();
        result.PhysicalDeleteCompleted.Should().BeTrue();

        // 验证元数据已删除
        var updatedEntity = await _db.EntityDefinitions
            .Include(e => e.Fields)
            .FirstOrDefaultAsync(e => e.Id == entityId);
        updatedEntity!.Fields.Should().NotContain(f => f.Id == fieldId);

        // 验证执行了 DROP COLUMN DDL
        _mockDDLService.Verify(
            x => x.ExecuteDDLAsync(
                entityId,
                DDLScriptType.Alter,
                It.Is<string>(sql => sql.Contains("DROP COLUMN") && sql.Contains("OldField")),
                "admin"),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteFieldAsync_ShouldFail_WhenEntityNotFound()
    {
        // Arrange
        var nonExistentEntityId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteFieldAsync(nonExistentEntityId, fieldId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteFieldAsync_ShouldFail_WhenFieldNotFound()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "Product",
            Fields = new List<FieldMetadata>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        var nonExistentFieldId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteFieldAsync(entityId, nonExistentFieldId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public void GetDefaultValueForDataType_ShouldReturnCorrectDefaults()
    {
        // Arrange & Act
        var stringDefault = GetDefaultValue(new FieldMetadata { DataType = "String" });
        var intDefault = GetDefaultValue(new FieldMetadata { DataType = "Int32" });
        var decimalDefault = GetDefaultValue(new FieldMetadata { DataType = "Decimal" });
        var boolDefault = GetDefaultValue(new FieldMetadata { DataType = "Boolean" });
        var datetimeDefault = GetDefaultValue(new FieldMetadata { DataType = "DateTime" });
        var guidDefault = GetDefaultValue(new FieldMetadata { DataType = "Guid" });
        var jsonDefault = GetDefaultValue(new FieldMetadata { DataType = "Json" });

        // Assert
        stringDefault.Should().Be("''");
        intDefault.Should().Be("0");
        decimalDefault.Should().Be("0.0");
        boolDefault.Should().Be("FALSE");
        datetimeDefault.Should().Be("NOW()");
        guidDefault.Should().Be("gen_random_uuid()");
        jsonDefault.Should().Be("'{}'::jsonb");
    }

    [Fact]
    public void GetDefaultValueForDataType_ShouldUseFieldDefaultValue_WhenProvided()
    {
        // Arrange & Act
        var customString = GetDefaultValue(new FieldMetadata { DataType = "String", DefaultValue = "Test" });
        var customInt = GetDefaultValue(new FieldMetadata { DataType = "Int32", DefaultValue = "100" });
        var customBool = GetDefaultValue(new FieldMetadata { DataType = "Boolean", DefaultValue = "true" });

        // Assert
        customString.Should().Be("'Test'");
        customInt.Should().Be("100");
        customBool.Should().Be("TRUE");
    }

    // 辅助方法：调用 private 方法 MapDataTypeToSQL
    private string MapDataType(FieldMetadata field)
    {
        var method = typeof(EntitySchemaAlignmentService)
            .GetMethod("MapDataTypeToSQL", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (string)method!.Invoke(_service, new object[] { field })!;
    }

    // 辅助方法：调用 private 方法 GetDefaultValueForDataType
    private string? GetDefaultValue(FieldMetadata field)
    {
        var method = typeof(EntitySchemaAlignmentService)
            .GetMethod("GetDefaultValueForDataType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (string?)method!.Invoke(_service, new object[] { field });
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
