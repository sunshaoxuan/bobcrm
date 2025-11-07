using Xunit;
using BobCrm.Api.Services.CodeGeneration;
using BobCrm.Api.Domain.Models;

namespace BobCrm.Api.Tests;

/// <summary>
/// AggVO代码生成器单元测试
/// 测试动态生成AggVO类和VO类的逻辑
/// </summary>
public class AggVOCodeGeneratorTests
{
    private readonly AggVOCodeGenerator _generator;

    public AggVOCodeGeneratorTests()
    {
        _generator = new AggVOCodeGenerator();
    }

    [Fact]
    public void GenerateVOClass_SimpleEntity_GeneratesCorrectCode()
    {
        // Arrange
        var entity = CreateTestEntity("Order", "BobCrm.Domain.Orders", new[]
        {
            CreateField("Id", "Integer", isRequired: true),
            CreateField("OrderNo", "String", length: 50, isRequired: true),
            CreateField("TotalAmount", "Decimal", precision: 18, scale: 2, isRequired: true),
            CreateField("OrderDate", "DateTime", isRequired: true),
            CreateField("Notes", "Text", isRequired: false)
        });

        // Act
        var code = _generator.GenerateVOClass(entity);

        // Assert
        Assert.NotNull(code);
        Assert.Contains("namespace BobCrm.Domain.Orders", code);
        Assert.Contains("public class OrderVO", code);
        Assert.Contains("public int Id { get; set; }", code);
        Assert.Contains("public string OrderNo { get; set; }", code);
        Assert.Contains("public decimal TotalAmount { get; set; }", code);
        Assert.Contains("public DateTime OrderDate { get; set; }", code);
        Assert.Contains("public string? Notes { get; set; }", code); // 可空字段应该有?标记
    }

    [Fact]
    public void GenerateAggVOClass_MasterDetail_GeneratesCorrectStructure()
    {
        // Arrange
        var master = CreateTestEntity("Order", "BobCrm.Domain.Orders", new[]
        {
            CreateField("Id", "Integer", isRequired: true),
            CreateField("OrderNo", "String", length: 50, isRequired: true)
        });

        var detail = CreateTestEntity("OrderLine", "BobCrm.Domain.Orders", new[]
        {
            CreateField("Id", "Integer", isRequired: true),
            CreateField("OrderId", "Integer", isRequired: true),
            CreateField("ProductName", "String", length: 100, isRequired: true),
            CreateField("Quantity", "Integer", isRequired: true),
            CreateField("UnitPrice", "Decimal", precision: 18, scale: 2, isRequired: true)
        });

        detail.ParentEntityId = master.Id;
        detail.ParentForeignKeyField = "OrderId";
        detail.ParentCollectionProperty = "OrderLines";

        // Act
        var code = _generator.GenerateAggVOClass(master, new List<EntityDefinition> { detail });

        // Assert
        Assert.NotNull(code);
        Assert.Contains("public class OrderAggVO : AggBaseVO", code);
        Assert.Contains("public OrderVO HeadVO { get; set; }", code);
        Assert.Contains("public List<OrderLineVO> OrderLineVOs { get; set; }", code);
        Assert.Contains("public override Type GetHeadEntityType()", code);
        Assert.Contains("typeof(Order)", code);
        Assert.Contains("public override List<Type> GetSubEntityTypes()", code);
        Assert.Contains("typeof(OrderLine)", code);
        Assert.Contains("public override Task<int> SaveAsync()", code);
        Assert.Contains("public override Task LoadAsync(int id)", code);
        Assert.Contains("public override Task DeleteAsync()", code);
    }

    [Fact]
    public void GenerateAggVOClass_MultipleChildren_GeneratesAllCollectionProperties()
    {
        // Arrange
        var master = CreateTestEntity("Order", "BobCrm.Domain.Orders", new[]
        {
            CreateField("Id", "Integer", isRequired: true),
            CreateField("OrderNo", "String", length: 50, isRequired: true)
        });

        var orderLine = CreateTestEntity("OrderLine", "BobCrm.Domain.Orders", new[]
        {
            CreateField("Id", "Integer", isRequired: true),
            CreateField("OrderId", "Integer", isRequired: true)
        });
        orderLine.ParentEntityId = master.Id;
        orderLine.ParentForeignKeyField = "OrderId";
        orderLine.ParentCollectionProperty = "OrderLines";

        var orderComment = CreateTestEntity("OrderComment", "BobCrm.Domain.Orders", new[]
        {
            CreateField("Id", "Integer", isRequired: true),
            CreateField("OrderId", "Integer", isRequired: true)
        });
        orderComment.ParentEntityId = master.Id;
        orderComment.ParentForeignKeyField = "OrderId";
        orderComment.ParentCollectionProperty = "Comments";

        // Act
        var code = _generator.GenerateAggVOClass(master, new List<EntityDefinition> { orderLine, orderComment });

        // Assert
        Assert.Contains("public List<OrderLineVO> OrderLineVOs { get; set; }", code);
        Assert.Contains("public List<OrderCommentVO> CommentVOs { get; set; }", code);
        Assert.Contains("typeof(OrderLine)", code);
        Assert.Contains("typeof(OrderComment)", code);
    }

    [Fact]
    public void GenerateAggVOClass_MasterDetailGrandchild_GeneratesNestedAggVO()
    {
        // Arrange
        var master = CreateTestEntity("Order", "BobCrm.Domain.Orders", new[]
        {
            CreateField("Id", "Integer", isRequired: true),
            CreateField("OrderNo", "String", length: 50, isRequired: true)
        });
        master.StructureType = EntityStructureType.MasterDetailGrandchild;

        var detail = CreateTestEntity("OrderLine", "BobCrm.Domain.Orders", new[]
        {
            CreateField("Id", "Integer", isRequired: true),
            CreateField("OrderId", "Integer", isRequired: true)
        });
        detail.ParentEntityId = master.Id;
        detail.ParentForeignKeyField = "OrderId";
        detail.ParentCollectionProperty = "OrderLines";
        detail.StructureType = EntityStructureType.MasterDetail; // 子实体本身也是主实体

        var grandchild = CreateTestEntity("OrderLineAttribute", "BobCrm.Domain.Orders", new[]
        {
            CreateField("Id", "Integer", isRequired: true),
            CreateField("OrderLineId", "Integer", isRequired: true)
        });
        grandchild.ParentEntityId = detail.Id;
        grandchild.ParentForeignKeyField = "OrderLineId";
        grandchild.ParentCollectionProperty = "Attributes";

        // Act
        var masterCode = _generator.GenerateAggVOClass(master, new List<EntityDefinition> { detail });
        var detailCode = _generator.GenerateAggVOClass(detail, new List<EntityDefinition> { grandchild });

        // Assert - 主实体的AggVO
        Assert.Contains("public class OrderAggVO : AggBaseVO", masterCode);
        Assert.Contains("public List<OrderLineAggVO> OrderLineAggVOs { get; set; }", masterCode); // 注意：是AggVO而非VO

        // Assert - 子实体的AggVO
        Assert.Contains("public class OrderLineAggVO : AggBaseVO", detailCode);
        Assert.Contains("public OrderLineVO HeadVO { get; set; }", detailCode);
        Assert.Contains("public List<OrderLineAttributeVO> AttributeVOs { get; set; }", detailCode);
    }

    [Fact]
    public void GenerateVOClass_WithNullableFields_GeneratesCorrectNullability()
    {
        // Arrange
        var entity = CreateTestEntity("Product", "BobCrm.Domain.Products", new[]
        {
            CreateField("Id", "Integer", isRequired: true),
            CreateField("Code", "String", length: 50, isRequired: true),
            CreateField("Name", "String", length: 200, isRequired: true),
            CreateField("Description", "Text", isRequired: false), // 可空
            CreateField("Price", "Decimal", precision: 18, scale: 2, isRequired: false), // 可空
            CreateField("StockQuantity", "Integer", isRequired: false), // 可空
            CreateField("DiscontinuedDate", "DateTime", isRequired: false) // 可空
        });

        // Act
        var code = _generator.GenerateVOClass(entity);

        // Assert
        Assert.Contains("public int Id { get; set; }", code); // 必填，不可空
        Assert.Contains("public string Code { get; set; }", code); // 必填字符串，不可空
        Assert.Contains("public string? Description { get; set; }", code); // 可选字符串，可空
        Assert.Contains("public decimal? Price { get; set; }", code); // 可选decimal，可空
        Assert.Contains("public int? StockQuantity { get; set; }", code); // 可选int，可空
        Assert.Contains("public DateTime? DiscontinuedDate { get; set; }", code); // 可选DateTime，可空
    }

    [Fact]
    public void GenerateAggVOClass_WithCascadeDelete_GeneratesDeleteLogic()
    {
        // Arrange
        var master = CreateTestEntity("Order", "BobCrm.Domain.Orders", new[]
        {
            CreateField("Id", "Integer", isRequired: true)
        });

        var detail = CreateTestEntity("OrderLine", "BobCrm.Domain.Orders", new[]
        {
            CreateField("Id", "Integer", isRequired: true),
            CreateField("OrderId", "Integer", isRequired: true)
        });
        detail.ParentEntityId = master.Id;
        detail.ParentForeignKeyField = "OrderId";
        detail.ParentCollectionProperty = "OrderLines";
        detail.CascadeDeleteBehavior = CascadeDeleteBehavior.Cascade; // 级联删除

        // Act
        var code = _generator.GenerateAggVOClass(master, new List<EntityDefinition> { detail });

        // Assert
        Assert.Contains("public override Task DeleteAsync()", code);
        // 应包含级联删除逻辑的注释或实现
        Assert.Contains("Cascade", code);
    }

    [Fact]
    public void GenerateVOClass_WithDataAnnotations_IncludesValidationAttributes()
    {
        // Arrange
        var entity = CreateTestEntity("Customer", "BobCrm.Domain.Customers", new[]
        {
            CreateField("Id", "Integer", isRequired: true),
            CreateField("Code", "String", length: 50, isRequired: true),
            CreateField("Name", "String", length: 200, isRequired: true),
            CreateField("Email", "String", length: 100, isRequired: false)
        });

        // Act
        var code = _generator.GenerateVOClass(entity);

        // Assert
        Assert.Contains("[Required]", code);
        Assert.Contains("[MaxLength(50)]", code);
        Assert.Contains("[MaxLength(200)]", code);
        Assert.Contains("[MaxLength(100)]", code);
    }

    [Fact]
    public void GenerateAggVOClass_EmptyChildList_ThrowsException()
    {
        // Arrange
        var master = CreateTestEntity("Order", "BobCrm.Domain.Orders", new[]
        {
            CreateField("Id", "Integer", isRequired: true)
        });
        master.StructureType = EntityStructureType.MasterDetail;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            _generator.GenerateAggVOClass(master, new List<EntityDefinition>());
        });

        Assert.Contains("No child entities configured", exception.Message);
    }

    // Helper Methods

    private EntityDefinition CreateTestEntity(string entityName, string namespaceName, FieldMetadata[] fields)
    {
        return new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = namespaceName,
            EntityName = entityName,
            FullTypeName = $"{namespaceName}.{entityName}",
            DisplayNameKey = $"ENTITY_{entityName.ToUpper()}",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            // DefaultTableName 是计算属性，不需要赋值
            Fields = fields.ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
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
