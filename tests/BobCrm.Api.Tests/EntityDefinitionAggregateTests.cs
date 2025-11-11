using Xunit;
using BobCrm.Api.Domain.Aggregates;
using BobCrm.Api.Domain.Models;

namespace BobCrm.Api.Tests;

/// <summary>
/// 实体定义聚合根单元测试
/// 测试聚合根的业务逻辑和验证规则
/// </summary>
public class EntityDefinitionAggregateTests
{
    private EntityDefinition CreateTestEntity(string name = "Order")
    {
        return new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Domain.Test",
            EntityName = name,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "测试实体" },
            Status = EntityStatus.Draft
        };
    }

    [Fact]
    public void Constructor_WithValidRoot_CreatesAggregate()
    {
        // Arrange
        var root = CreateTestEntity();

        // Act
        var aggregate = new EntityDefinitionAggregate(root);

        // Assert
        Assert.NotNull(aggregate);
        Assert.Equal(root, aggregate.Root);
        Assert.Empty(aggregate.SubEntities);
    }

    [Fact]
    public void Constructor_WithNullRoot_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EntityDefinitionAggregate(null!));
    }

    [Fact]
    public void AddSubEntity_WithValidData_AddsSubEntity()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var displayName = new Dictionary<string, string?> { ["zh"] = "订单明细" };

        // Act
        var subEntity = aggregate.AddSubEntity("Lines", displayName);

        // Assert
        Assert.Single(aggregate.SubEntities);
        Assert.Equal("Lines", subEntity.Code);
        Assert.Equal(displayName, subEntity.DisplayName);
        Assert.Equal(root.Id, subEntity.EntityDefinitionId);
    }

    [Fact]
    public void AddSubEntity_WithDuplicateCode_ThrowsDomainException()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var displayName = new Dictionary<string, string?> { ["zh"] = "明细" };

        aggregate.AddSubEntity("Lines", displayName);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            aggregate.AddSubEntity("Lines", displayName));
        Assert.Contains("已存在", exception.Message);
    }

    [Fact]
    public void AddSubEntity_WithInvalidCode_ThrowsDomainException()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var displayName = new Dictionary<string, string?> { ["zh"] = "明细" };

        // Act & Assert - 小写字母开头
        var exception1 = Assert.Throws<DomainException>(() =>
            aggregate.AddSubEntity("lines", displayName));
        Assert.Contains("格式无效", exception1.Message);

        // Act & Assert - 包含特殊字符
        var exception2 = Assert.Throws<DomainException>(() =>
            aggregate.AddSubEntity("Lines-1", displayName));
        Assert.Contains("格式无效", exception2.Message);
    }

    [Fact]
    public void RemoveSubEntity_ExistingSubEntity_RemovesSuccessfully()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });

        // Act
        aggregate.RemoveSubEntity(subEntity.Id);

        // Assert
        Assert.Empty(aggregate.SubEntities);
    }

    [Fact]
    public void AddFieldToSubEntity_WithValidData_AddsField()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });
        var fieldDisplayName = new Dictionary<string, string?> { ["zh"] = "产品编码" };

        // Act
        var field = aggregate.AddFieldToSubEntity(
            subEntity.Id,
            "ProductCode",
            fieldDisplayName,
            FieldDataType.String,
            isRequired: true,
            length: 100);

        // Assert
        Assert.Single(subEntity.Fields);
        Assert.Equal("ProductCode", field.PropertyName);
        Assert.Equal(FieldDataType.String, field.DataType);
        Assert.True(field.IsRequired);
        Assert.Equal(100, field.Length);
        Assert.Equal(root.Id, field.EntityDefinitionId);
        Assert.Equal(subEntity.Id, field.SubEntityDefinitionId);
    }

    [Fact]
    public void AddFieldToSubEntity_WithNonExistentSubEntity_ThrowsDomainException()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            aggregate.AddFieldToSubEntity(
                nonExistentId,
                "ProductCode",
                new Dictionary<string, string?> { ["zh"] = "产品" },
                FieldDataType.String));
        Assert.Contains("不存在", exception.Message);
    }

    [Fact]
    public void AddFieldToSubEntity_WithDuplicateFieldName_ThrowsDomainException()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });
        var fieldDisplayName = new Dictionary<string, string?> { ["zh"] = "产品" };

        aggregate.AddFieldToSubEntity(subEntity.Id, "ProductCode", fieldDisplayName, FieldDataType.String);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            aggregate.AddFieldToSubEntity(subEntity.Id, "ProductCode", fieldDisplayName, FieldDataType.String));
        Assert.Contains("已存在", exception.Message);
    }

    [Fact]
    public void UpdateFieldInSubEntity_WithValidData_UpdatesField()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });
        var field = aggregate.AddFieldToSubEntity(
            subEntity.Id,
            "ProductCode",
            new Dictionary<string, string?> { ["zh"] = "产品" },
            FieldDataType.String);

        var newDisplayName = new Dictionary<string, string?> { ["zh"] = "商品编码" };

        // Act
        aggregate.UpdateFieldInSubEntity(
            subEntity.Id,
            field.Id,
            "ProductCode",
            newDisplayName,
            FieldDataType.String,
            true,
            200);

        // Assert
        Assert.Equal(newDisplayName, field.DisplayName);
        Assert.True(field.IsRequired);
        Assert.Equal(200, field.Length);
    }

    [Fact]
    public void UpdateFieldInSubEntity_ChangingToExistingName_ThrowsDomainException()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });

        var field1 = aggregate.AddFieldToSubEntity(
            subEntity.Id, "Field1", new Dictionary<string, string?> { ["zh"] = "字段1" }, FieldDataType.String);
        var field2 = aggregate.AddFieldToSubEntity(
            subEntity.Id, "Field2", new Dictionary<string, string?> { ["zh"] = "字段2" }, FieldDataType.String);

        // Act & Assert - 尝试将field2重命名为field1
        var exception = Assert.Throws<DomainException>(() =>
            aggregate.UpdateFieldInSubEntity(
                subEntity.Id,
                field2.Id,
                "Field1", // 重复的名称
                new Dictionary<string, string?> { ["zh"] = "字段2" },
                FieldDataType.String,
                false));
        Assert.Contains("已存在", exception.Message);
    }

    [Fact]
    public void RemoveFieldFromSubEntity_ExistingField_RemovesSuccessfully()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });
        var field = aggregate.AddFieldToSubEntity(
            subEntity.Id,
            "ProductCode",
            new Dictionary<string, string?> { ["zh"] = "产品" },
            FieldDataType.String);

        // Act
        aggregate.RemoveFieldFromSubEntity(subEntity.Id, field.Id);

        // Assert
        Assert.Empty(subEntity.Fields);
    }

    [Fact]
    public void Validate_ValidAggregate_ReturnsValid()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });
        aggregate.AddFieldToSubEntity(
            subEntity.Id,
            "ProductCode",
            new Dictionary<string, string?> { ["zh"] = "产品" },
            FieldDataType.String,
            isRequired: true);

        // Act
        var result = aggregate.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RootWithoutEntityName_ReturnsError()
    {
        // Arrange
        var root = CreateTestEntity();
        root.EntityName = ""; // 无效的实体名
        var aggregate = new EntityDefinitionAggregate(root);

        // Act
        var result = aggregate.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyPath == "Root.EntityName");
    }

    [Fact]
    public void Validate_SubEntityWithDuplicateFields_ReturnsError()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });

        // 直接添加重复字段（绕过AddFieldToSubEntity的检查）
        subEntity.Fields.Add(new FieldMetadata
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = root.Id,
            SubEntityDefinitionId = subEntity.Id,
            PropertyName = "Field1",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "字段1" },
            DataType = FieldDataType.String
        });
        subEntity.Fields.Add(new FieldMetadata
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = root.Id,
            SubEntityDefinitionId = subEntity.Id,
            PropertyName = "Field1", // 重复
            DisplayName = new Dictionary<string, string?> { ["zh"] = "字段1" },
            DataType = FieldDataType.String
        });

        // Act
        var result = aggregate.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("Field1"));
    }

    [Fact]
    public void Validate_SubEntityWithInvalidCode_ReturnsError()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = new SubEntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = root.Id,
            Code = "invalid-code", // 无效格式
            DisplayName = new Dictionary<string, string?> { ["zh"] = "明细" }
        };

        var aggregateWithInvalidSubEntity = new EntityDefinitionAggregate(root, new List<SubEntityDefinition> { subEntity });

        // Act
        var result = aggregateWithInvalidSubEntity.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("格式无效"));
    }

    [Fact]
    public void Validate_ReferenceConsistency_EntityDefinitionIdMismatch_ReturnsError()
    {
        // Arrange
        var root = CreateTestEntity();
        var wrongEntityId = Guid.NewGuid();
        var subEntity = new SubEntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = wrongEntityId, // 错误的EntityDefinitionId
            Code = "Lines",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "明细" }
        };

        var aggregate = new EntityDefinitionAggregate(root, new List<SubEntityDefinition> { subEntity });

        // Act
        var result = aggregate.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("EntityDefinitionId") && e.Message.Contains("不一致"));
    }

    [Fact]
    public void Validate_RequiredFieldWithoutDataType_ReturnsError()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });

        // 直接添加无效字段
        subEntity.Fields.Add(new FieldMetadata
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = root.Id,
            SubEntityDefinitionId = subEntity.Id,
            PropertyName = "Field1",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "字段1" },
            DataType = "", // 空的数据类型
            IsRequired = true
        });

        // Act
        var result = aggregate.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("数据类型"));
    }

    [Fact]
    public void GetSubEntity_ExistingId_ReturnsSubEntity()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });

        // Act
        var retrieved = aggregate.GetSubEntity(subEntity.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(subEntity.Id, retrieved.Id);
    }

    [Fact]
    public void GetSubEntity_NonExistentId_ReturnsNull()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);

        // Act
        var retrieved = aggregate.GetSubEntity(Guid.NewGuid());

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public void UpdateSubEntity_ExistingSubEntity_UpdatesSuccessfully()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });

        var newDisplayName = new Dictionary<string, string?> { ["zh"] = "订单明细行" };
        var newDescription = new Dictionary<string, string?> { ["zh"] = "订单的明细行" };

        // Act
        aggregate.UpdateSubEntity(subEntity.Id, newDisplayName, newDescription, 10);

        // Assert
        Assert.Equal(newDisplayName, subEntity.DisplayName);
        Assert.Equal(newDescription, subEntity.Description);
        Assert.Equal(10, subEntity.SortOrder);
        Assert.NotNull(subEntity.UpdatedAt);
    }

    [Fact]
    public void UpdateSubEntity_NonExistentSubEntity_ThrowsDomainException()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            aggregate.UpdateSubEntity(
                Guid.NewGuid(),
                new Dictionary<string, string?> { ["zh"] = "明细" }));
        Assert.Contains("不存在", exception.Message);
    }
}
