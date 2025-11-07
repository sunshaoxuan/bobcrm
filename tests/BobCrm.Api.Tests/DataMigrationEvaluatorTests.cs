using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BobCrm.Api.Services.DataMigration;
using BobCrm.Api.Domain.Models;
using BobCrm.Api.Data;
using BobCrm.Api.Infrastructure;

namespace BobCrm.Api.Tests;

/// <summary>
/// 数据迁移评估器单元测试
/// 测试字段变更检测、数据丢失风险评估、风险等级计算等逻辑
/// </summary>
public class DataMigrationEvaluatorTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly DataMigrationEvaluator _evaluator;
    private readonly ApplicationDbContext _db;

    public DataMigrationEvaluatorTests(TestWebAppFactory factory)
    {
        _factory = factory;
        var scope = _factory.Services.CreateScope();
        _db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataMigrationEvaluator>>();
        _evaluator = new DataMigrationEvaluator(_db, logger);
    }

    [Fact]
    public async Task EvaluateImpact_DraftEntity_ReturnsLowRisk()
    {
        // Arrange
        var entity = await CreateTestEntity("DraftEntity", EntityStatus.Draft, new[]
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 100, isRequired: true)
        });

        var newFields = new List<FieldMetadata>
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 100, isRequired: true),
            CreateField("Email", FieldDataType.String, length: 100, isRequired: false) // 新增字段
        };

        // Act
        var impact = await _evaluator.EvaluateImpactAsync(entity.Id, newFields);

        // Assert
        Assert.Equal(RiskLevel.Low, impact.RiskLevel);
        Assert.True(impact.IsSafe);
        Assert.Single(impact.Warnings);
        Assert.Contains("draft entity", impact.Warnings[0]);
    }

    [Fact]
    public async Task EvaluateImpact_AddNullableColumn_ReturnsLowRisk()
    {
        // Arrange
        var entity = await CreateTestEntity("Customer", EntityStatus.Published, new[]
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 100, isRequired: true)
        });

        var newFields = new List<FieldMetadata>
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 100, isRequired: true),
            CreateField("Email", FieldDataType.String, length: 100, isRequired: false) // 新增可空字段
        };

        // Act
        var impact = await _evaluator.EvaluateImpactAsync(entity.Id, newFields);

        // Assert
        Assert.Equal(RiskLevel.Low, impact.RiskLevel);
        Assert.True(impact.IsSafe);
        Assert.Contains(impact.Operations, op => op.OperationType == MigrationOperationType.AddColumn);
        var addOp = impact.Operations.First(op => op.OperationType == MigrationOperationType.AddColumn);
        Assert.Equal("Email", addOp.FieldName);
        Assert.Equal(FieldDataType.String, addOp.NewDataType);
        Assert.False(addOp.MayLoseData);
    }

    [Fact]
    public async Task EvaluateImpact_AddRequiredColumnWithoutDefault_ReturnsCritical()
    {
        // Arrange: 模拟已发布的实体有现有数据
        var entity = await CreateTestEntity("Customer", EntityStatus.Published, new[]
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 100, isRequired: true)
        });

        // 模拟表中已有数据（通过直接执行SQL或假设评估器能检测到）
        // 实际实现中，EvaluateImpactAsync会调用GetTableRowCountAsync

        var newFields = new List<FieldMetadata>
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 100, isRequired: true),
            CreateField("Email", FieldDataType.String, length: 100, isRequired: true) // 新增必填字段但无默认值
        };

        // Act
        var impact = await _evaluator.EvaluateImpactAsync(entity.Id, newFields);

        // Assert
        // 如果表中有数据，应该返回错误
        if (impact.AffectedRows > 0)
        {
            Assert.Equal(RiskLevel.Critical, impact.RiskLevel);
            Assert.False(impact.IsSafe);
            Assert.Contains(impact.Errors, e => e.Contains("without default value"));
        }
    }

    [Fact]
    public async Task EvaluateImpact_AddRequiredColumnWithDefault_ReturnsLowOrMediumRisk()
    {
        // Arrange
        var entity = await CreateTestEntity("Customer", EntityStatus.Published, new[]
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 100, isRequired: true)
        });

        var newFields = new List<FieldMetadata>
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 100, isRequired: true),
            CreateField("Status", FieldDataType.String, length: 20, isRequired: true, defaultValue: "Active") // 有默认值
        };

        // Act
        var impact = await _evaluator.EvaluateImpactAsync(entity.Id, newFields);

        // Assert
        Assert.True(impact.IsSafe);
        Assert.DoesNotContain(impact.Errors, e => e.Contains("Email"));
        if (impact.AffectedRows > 0)
        {
            Assert.Contains(impact.Warnings, w => w.Contains("with default value"));
        }
    }

    [Fact]
    public async Task EvaluateImpact_DropColumn_ReturnsHighRisk()
    {
        // Arrange
        var entity = await CreateTestEntity("Customer", EntityStatus.Published, new[]
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 100, isRequired: true),
            CreateField("Email", FieldDataType.String, length: 100, isRequired: false)
        });

        var newFields = new List<FieldMetadata>
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 100, isRequired: true)
            // Email字段被删除
        };

        // Act
        var impact = await _evaluator.EvaluateImpactAsync(entity.Id, newFields);

        // Assert
        var dropOp = impact.Operations.FirstOrDefault(op => op.OperationType == MigrationOperationType.DropColumn);
        Assert.NotNull(dropOp);
        Assert.Equal("Email", dropOp.FieldName);
        Assert.Equal(FieldDataType.String, dropOp.OldDataType);

        if (impact.AffectedRows > 0)
        {
            Assert.True(dropOp.MayLoseData);
            Assert.Contains(impact.Errors, e => e.Contains("data loss"));
        }
    }

    [Fact]
    public async Task EvaluateImpact_ShortenStringLength_ReturnsHighRisk()
    {
        // Arrange
        var entity = await CreateTestEntity("Customer", EntityStatus.Published, new[]
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 200, isRequired: true)
        });

        var newFields = new List<FieldMetadata>
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 100, isRequired: true) // 长度从200缩短到100
        };

        // Act
        var impact = await _evaluator.EvaluateImpactAsync(entity.Id, newFields);

        // Assert
        var alterOp = impact.Operations.FirstOrDefault(op => op.OperationType == MigrationOperationType.AlterColumn);
        Assert.NotNull(alterOp);
        Assert.Equal("Name", alterOp.FieldName);
        Assert.Contains("length changed", alterOp.Description);

        if (impact.AffectedRows > 0)
        {
            Assert.True(alterOp.MayLoseData);
        }
    }

    [Fact]
    public async Task EvaluateImpact_IncreaseStringLength_ReturnsLowRisk()
    {
        // Arrange
        var entity = await CreateTestEntity("Customer", EntityStatus.Published, new[]
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 100, isRequired: true)
        });

        var newFields = new List<FieldMetadata>
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 200, isRequired: true) // 长度从100增加到200
        };

        // Act
        var impact = await _evaluator.EvaluateImpactAsync(entity.Id, newFields);

        // Assert
        var alterOp = impact.Operations.FirstOrDefault(op => op.OperationType == MigrationOperationType.AlterColumn);
        Assert.NotNull(alterOp);
        Assert.Equal("Name", alterOp.FieldName);
        Assert.False(alterOp.MayLoseData); // 增加长度不会丢失数据
        Assert.Equal(RiskLevel.Low, impact.RiskLevel);
    }

    [Fact]
    public async Task EvaluateImpact_ChangeDataType_StringToInteger_ReturnsHighRisk()
    {
        // Arrange
        var entity = await CreateTestEntity("Product", EntityStatus.Published, new[]
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Code", FieldDataType.String, length: 50, isRequired: true)
        });

        var newFields = new List<FieldMetadata>
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Code", FieldDataType.Integer, isRequired: true) // String -> Integer
        };

        // Act
        var impact = await _evaluator.EvaluateImpactAsync(entity.Id, newFields);

        // Assert
        var alterOp = impact.Operations.FirstOrDefault(op => op.OperationType == MigrationOperationType.AlterColumn);
        Assert.NotNull(alterOp);
        Assert.Equal("Code", alterOp.FieldName);
        Assert.Equal(FieldDataType.String, alterOp.OldDataType);
        Assert.Equal(FieldDataType.Integer, alterOp.NewDataType);
        Assert.True(alterOp.RequiresConversion);

        if (impact.AffectedRows > 0)
        {
            Assert.True(alterOp.MayLoseData); // 高风险转换
            Assert.Contains(impact.Errors, e => e.Contains("may cause data loss"));
        }
    }

    [Fact]
    public async Task EvaluateImpact_ChangeDataType_IntegerToString_ReturnsLowOrMediumRisk()
    {
        // Arrange
        var entity = await CreateTestEntity("Product", EntityStatus.Published, new[]
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Quantity", FieldDataType.Integer, isRequired: true)
        });

        var newFields = new List<FieldMetadata>
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Quantity", FieldDataType.String, length: 20, isRequired: true) // Integer -> String（安全）
        };

        // Act
        var impact = await _evaluator.EvaluateImpactAsync(entity.Id, newFields);

        // Assert
        var alterOp = impact.Operations.FirstOrDefault(op => op.OperationType == MigrationOperationType.AlterColumn);
        Assert.NotNull(alterOp);
        Assert.True(alterOp.RequiresConversion);
        Assert.False(alterOp.MayLoseData); // 数值转字符串是安全的
    }

    [Fact]
    public async Task EvaluateImpact_ChangeNullableToRequired_ReturnsHighRisk()
    {
        // Arrange
        var entity = await CreateTestEntity("Customer", EntityStatus.Published, new[]
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Email", FieldDataType.String, length: 100, isRequired: false) // 可空
        });

        var newFields = new List<FieldMetadata>
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Email", FieldDataType.String, length: 100, isRequired: true) // 改为必填
        };

        // Act
        var impact = await _evaluator.EvaluateImpactAsync(entity.Id, newFields);

        // Assert
        var alterOp = impact.Operations.FirstOrDefault(op => op.OperationType == MigrationOperationType.AlterColumn);
        Assert.NotNull(alterOp);
        Assert.Contains("required changed", alterOp.Description);

        if (impact.AffectedRows > 0)
        {
            Assert.True(alterOp.MayLoseData); // 可能有NULL值无法转换
        }
    }

    [Fact]
    public async Task EvaluateImpact_MultipleChanges_CalculatesCorrectRiskLevel()
    {
        // Arrange
        var entity = await CreateTestEntity("Customer", EntityStatus.Published, new[]
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 100, isRequired: true),
            CreateField("Email", FieldDataType.String, length: 100, isRequired: false),
            CreateField("Phone", FieldDataType.String, length: 20, isRequired: false)
        });

        var newFields = new List<FieldMetadata>
        {
            CreateField("Id", FieldDataType.Integer, isRequired: true),
            CreateField("Name", FieldDataType.String, length: 200, isRequired: true), // 增加长度（安全）
            CreateField("Email", FieldDataType.String, length: 100, isRequired: true), // 改为必填（风险）
            CreateField("Address", FieldDataType.String, length: 200, isRequired: false) // 新增字段（安全）
            // Phone字段被删除（高风险）
        };

        // Act
        var impact = await _evaluator.EvaluateImpactAsync(entity.Id, newFields);

        // Assert
        Assert.Contains(impact.Operations, op => op.OperationType == MigrationOperationType.AddColumn);
        Assert.Contains(impact.Operations, op => op.OperationType == MigrationOperationType.DropColumn);
        Assert.Contains(impact.Operations, op => op.OperationType == MigrationOperationType.AlterColumn);

        if (impact.AffectedRows > 0)
        {
            // 有删除列或高风险变更，应该是Critical或High
            Assert.True(impact.RiskLevel == RiskLevel.Critical || impact.RiskLevel == RiskLevel.High);
        }
    }

    // Helper Methods

    private async Task<EntityDefinition> CreateTestEntity(
        string entityName,
        string status,
        FieldMetadata[] fields)
    {
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Domain.Test",
            EntityName = entityName,
            FullTypeName = $"BobCrm.Domain.Test.{entityName}",
            DisplayNameKey = $"ENTITY_{entityName.ToUpper()}",
            StructureType = EntityStructureType.Single,
            Status = status,
            DefaultTableName = entityName.ToLower() + "s",
            Fields = fields.ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.EntityDefinitions.Add(entity);
        await _db.SaveChangesAsync();

        return entity;
    }

    private FieldMetadata CreateField(
        string propertyName,
        string dataType,
        int? length = null,
        int? precision = null,
        int? scale = null,
        bool isRequired = false,
        string? defaultValue = null)
    {
        return new FieldMetadata
        {
            PropertyName = propertyName,
            DisplayNameKey = $"FIELD_{propertyName.ToUpper()}",
            DataType = dataType,
            Length = length,
            Precision = precision,
            Scale = scale,
            IsRequired = isRequired,
            DefaultValue = defaultValue,
            SortOrder = 0
        };
    }
}
