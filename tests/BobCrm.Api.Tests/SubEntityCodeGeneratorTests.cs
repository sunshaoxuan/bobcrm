using Xunit;

using BobCrm.Api.Services;

using BobCrm.Api.Base.Models;

using Microsoft.Extensions.Logging;

using Moq;

namespace BobCrm.Api.Tests;

/// <summary>
/// 子实体代码生成器单元测试
/// 测试C#代码生成的正确性
/// </summary>

public class SubEntityCodeGeneratorTests

{

    private readonly SubEntityCodeGenerator _generator;

    public SubEntityCodeGeneratorTests()

    {

        var mockLogger = new Mock<ILogger<SubEntityCodeGenerator>>();

        _generator = new SubEntityCodeGenerator(mockLogger.Object);

    }

    private EntityDefinition CreateMainEntity(string name = "Order")

    {

        return new EntityDefinition

        {

            Id = Guid.NewGuid(),

            Namespace = "BobCrm.Base.Sales",

            EntityName = name,

            DisplayName = new Dictionary<string, string?> { ["zh"] = "订单" }

        };

    }

    private SubEntityDefinition CreateSubEntity(Guid entityId, string code = "Lines")

    {

        return new SubEntityDefinition

        {

            Id = Guid.NewGuid(),

            EntityDefinitionId = entityId,

            Code = code,

            DisplayName = new Dictionary<string, string?> { ["zh"] = "明细" },

            Description = new Dictionary<string, string?> { ["zh"] = "订单明细行" }

        };

    }

    [Fact]

    public void GenerateSubEntityClass_BasicStructure_GeneratesCorrectly()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        Assert.NotNull(code);

        Assert.Contains("namespace BobCrm.Base.Sales", code);

        Assert.Contains("public class Lines", code);

        Assert.Contains("明细（子实体）", code);

        Assert.Contains("订单明细行", code);

        Assert.Contains("[Table(\"Orders_Lines\")]", code);

    }

    [Fact]

    public void GenerateSubEntityClass_WithPrimaryKey_IncludesIdProperty()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        Assert.Contains("/// 主键ID", code);

        Assert.Contains("[Key]", code);

        Assert.Contains("public Guid Id { get; set; } = Guid.NewGuid();", code);

    }

    [Fact]

    public void GenerateSubEntityClass_WithForeignKey_IncludesForeignKeyProperty()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        Assert.Contains("/// 所属Order的ID（外键）", code);

        Assert.Contains("[Required]", code);

        Assert.Contains("public Guid OrderId { get; set; }", code);

    }

    [Fact]

    public void GenerateSubEntityClass_WithCustomForeignKey_UsesCustomName()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        subEntity.ForeignKeyField = "ParentOrderId";

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        Assert.Contains("public Guid ParentOrderId { get; set; }", code);

    }

    [Fact]

    public void GenerateSubEntityClass_WithNavigationProperty_IncludesNavigation()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        Assert.Contains("/// 导航属性：所属Order", code);

        Assert.Contains("public Order? Order { get; set; }", code);

    }

    [Fact]

    public void GenerateSubEntityClass_WithStringField_GeneratesCorrectly()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        subEntity.Fields.Add(new FieldMetadata

        {

            Id = Guid.NewGuid(),

            PropertyName = "ProductCode",

            DisplayName = new Dictionary<string, string?> { ["zh"] = "产品编码" },

            DataType = FieldDataType.String,

            Length = 100,

            IsRequired = true

        });

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        Assert.Contains("/// 产品编码", code);

        Assert.Contains("[Required]", code);

        Assert.Contains("[MaxLength(100)]", code);

        Assert.Contains("public string ProductCode { get; set; } = string.Empty;", code);

    }

    [Fact]

    public void GenerateSubEntityClass_WithOptionalStringField_GeneratesNullable()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        subEntity.Fields.Add(new FieldMetadata

        {

            Id = Guid.NewGuid(),

            PropertyName = "Notes",

            DisplayName = new Dictionary<string, string?> { ["zh"] = "备注" },

            DataType = FieldDataType.String,

            IsRequired = false

        });

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        Assert.Contains("public string Notes { get; set; }", code);

        Assert.DoesNotContain("public string? Notes", code); // string不需要?

    }

    [Fact]

    public void GenerateSubEntityClass_WithNumericFields_GeneratesCorrectly()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        subEntity.Fields.Add(new FieldMetadata

        {

            PropertyName = "Quantity",

            DisplayName = new Dictionary<string, string?> { ["zh"] = "数量" },

            DataType = FieldDataType.Int32,

            IsRequired = true

        });

        subEntity.Fields.Add(new FieldMetadata

        {

            PropertyName = "UnitPrice",

            DisplayName = new Dictionary<string, string?> { ["zh"] = "单价" },

            DataType = FieldDataType.Decimal,

            IsRequired = true

        });

        subEntity.Fields.Add(new FieldMetadata

        {

            PropertyName = "Discount",

            DisplayName = new Dictionary<string, string?> { ["zh"] = "折扣" },

            DataType = FieldDataType.Decimal,

            IsRequired = false

        });

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        Assert.Contains("public int Quantity { get; set; }", code);

        Assert.Contains("public decimal UnitPrice { get; set; }", code);

        Assert.Contains("public decimal? Discount { get; set; }", code); // 可选的值类型应有?

    }

    [Fact]

    public void GenerateSubEntityClass_WithDateTimeField_GeneratesCorrectly()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        subEntity.Fields.Add(new FieldMetadata

        {

            PropertyName = "DeliveryDate",

            DisplayName = new Dictionary<string, string?> { ["zh"] = "交付日期" },

            DataType = FieldDataType.DateTime,

            IsRequired = true

        });

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        Assert.Contains("public DateTime DeliveryDate { get; set; }", code);

    }

    [Fact]

    public void GenerateSubEntityClass_WithDateField_GeneratesDateOnly()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        subEntity.Fields.Add(new FieldMetadata

        {

            PropertyName = "BirthDate",

            DisplayName = new Dictionary<string, string?> { ["zh"] = "出生日期" },

            DataType = FieldDataType.Date,

            IsRequired = true

        });

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        Assert.Contains("public DateOnly BirthDate { get; set; }", code);

    }

    [Fact]

    public void GenerateSubEntityClass_WithDefaultValue_IncludesDefaultValue()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        subEntity.Fields.Add(new FieldMetadata

        {

            PropertyName = "Status",

            DisplayName = new Dictionary<string, string?> { ["zh"] = "状态" },

            DataType = FieldDataType.String,

            IsRequired = true,

            DefaultValue = "Pending"

        });

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        Assert.Contains("public string Status { get; set; } = \"Pending\";", code);

    }

    [Fact]

    public void GenerateSubEntityClass_WithBooleanDefaultValue_GeneratesCorrectly()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        subEntity.Fields.Add(new FieldMetadata

        {

            PropertyName = "IsActive",

            DisplayName = new Dictionary<string, string?> { ["zh"] = "是否激活" },

            DataType = FieldDataType.Boolean,

            IsRequired = true,

            DefaultValue = "True"

        });

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        Assert.Contains("public bool IsActive { get; set; } = true;", code);

    }

    [Fact]

    public void GenerateSubEntityClass_WithMultipleFields_OrdersBySortOrder()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        subEntity.Fields.Add(new FieldMetadata

        {

            PropertyName = "Field3",

            DisplayName = new Dictionary<string, string?> { ["zh"] = "字段3" },

            DataType = FieldDataType.String,

            SortOrder = 3

        });

        subEntity.Fields.Add(new FieldMetadata

        {

            PropertyName = "Field1",

            DisplayName = new Dictionary<string, string?> { ["zh"] = "字段1" },

            DataType = FieldDataType.String,

            SortOrder = 1

        });

        subEntity.Fields.Add(new FieldMetadata

        {

            PropertyName = "Field2",

            DisplayName = new Dictionary<string, string?> { ["zh"] = "字段2" },

            DataType = FieldDataType.String,

            SortOrder = 2

        });

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        var field1Index = code.IndexOf("Field1");

        var field2Index = code.IndexOf("Field2");

        var field3Index = code.IndexOf("Field3");

        Assert.True(field1Index < field2Index);

        Assert.True(field2Index < field3Index);

    }

    [Fact]

    public void GenerateAggregateVoClass_WithSingleSubEntity_GeneratesCorrectly()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        var subEntities = new List<SubEntityDefinition> { subEntity };

        // Act

        var code = _generator.GenerateAggregateVoClass(mainEntity, subEntities);

        // Assert

        Assert.Contains("namespace BobCrm.Base.Sales", code);

        Assert.Contains("public class OrderAggVo", code);

        Assert.Contains("/// 订单聚合VO", code);

        Assert.Contains("public Order Master { get; set; } = null!;", code);

        Assert.Contains("public List<Lines> Lines { get; set; } = new();", code);

    }

    [Fact]

    public void GenerateAggregateVoClass_WithCustomCollectionPropertyName_UsesCustomName()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        subEntity.CollectionPropertyName = "OrderLines";

        var subEntities = new List<SubEntityDefinition> { subEntity };

        // Act

        var code = _generator.GenerateAggregateVoClass(mainEntity, subEntities);

        // Assert

        Assert.Contains("public List<Lines> OrderLines { get; set; } = new();", code);

    }

    [Fact]

    public void GenerateAggregateVoClass_WithMultipleSubEntities_GeneratesAllCollections()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity1 = CreateSubEntity(mainEntity.Id, "Lines");

        subEntity1.SortOrder = 1;

        var subEntity2 = CreateSubEntity(mainEntity.Id, "Comments");

        subEntity2.SortOrder = 2;

        subEntity2.DisplayName = new Dictionary<string, string?> { ["zh"] = "评论" };

        var subEntities = new List<SubEntityDefinition> { subEntity2, subEntity1 }; // 故意不按顺序

        // Act

        var code = _generator.GenerateAggregateVoClass(mainEntity, subEntities);

        // Assert

        Assert.Contains("public List<Lines> Lines { get; set; } = new();", code);

        Assert.Contains("public List<Comments> Comments { get; set; } = new();", code);

        // 验证排序（Lines应该在Comments前面）

        var linesIndex = code.IndexOf("public List<Lines>");

        var commentsIndex = code.IndexOf("public List<Comments>");

        Assert.True(linesIndex < commentsIndex);

    }

    [Fact]

    public void GenerateAggregateVoClass_IncludesTimestamp()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        var subEntities = new List<SubEntityDefinition> { subEntity };

        // Act

        var code = _generator.GenerateAggregateVoClass(mainEntity, subEntities);

        // Assert

        Assert.Contains("自动生成于:", code);

    }

    [Fact]

    public void GenerateSubEntityClass_IncludesUsingStatements()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        Assert.Contains("using System;", code);

        Assert.Contains("using System.ComponentModel.DataAnnotations;", code);

        Assert.Contains("using System.ComponentModel.DataAnnotations.Schema;", code);

    }

    [Fact]

    public void GenerateAggregateVoClass_IncludesUsingStatements()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        var subEntities = new List<SubEntityDefinition> { subEntity };

        // Act

        var code = _generator.GenerateAggregateVoClass(mainEntity, subEntities);

        // Assert

        Assert.Contains("using System;", code);

        Assert.Contains("using System.Collections.Generic;", code);

    }

    [Fact]

    public void GenerateSubEntityClass_WithGuidField_GeneratesCorrectly()

    {

        // Arrange

        var mainEntity = CreateMainEntity();

        var subEntity = CreateSubEntity(mainEntity.Id);

        subEntity.Fields.Add(new FieldMetadata

        {

            PropertyName = "CorrelationId",

            DisplayName = new Dictionary<string, string?> { ["zh"] = "关联ID" },

            DataType = FieldDataType.Guid,

            IsRequired = true

        });

        // Act

        var code = _generator.GenerateSubEntityClass(mainEntity, subEntity);

        // Assert

        Assert.Contains("public Guid CorrelationId { get; set; } = Guid.NewGuid();", code);

    }

}

