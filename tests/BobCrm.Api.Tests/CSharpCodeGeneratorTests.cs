using BobCrm.Api.Base.Models;
using BobCrm.Api.Services;
using FluentAssertions;

namespace BobCrm.Api.Tests;

public class CSharpCodeGeneratorTests
{
    private readonly CSharpCodeGenerator _generator;

    public CSharpCodeGeneratorTests()
    {
        _generator = new CSharpCodeGenerator();
    }

    [Fact]
    public void GenerateEntityClass_ShouldGenerateValidCode_WithBasicFields()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "Product",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_PRODUCT" } },
            Description = new Dictionary<string, string?> { { "en", "Product entity description" } },
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    PropertyName = "Id",
                    DisplayName = new Dictionary<string, string?> { { "en", "FIELD_ID" } },
                    DataType = FieldDataType.Integer,
                    IsRequired = true,
                    SortOrder = 1
                },
                new FieldMetadata
                {
                    PropertyName = "Name",
                    DisplayName = new Dictionary<string, string?> { { "en", "FIELD_NAME" } },
                    DataType = FieldDataType.String,
                    Length = 100,
                    IsRequired = true,
                    SortOrder = 2
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        // Act
        var code = _generator.GenerateEntityClass(entity);

        // Assert
        code.Should().Contain("namespace BobCrm.Test");
        code.Should().Contain("public class Product");
        code.Should().Contain("[Table(\"Products\")]");
        code.Should().Contain("public int Id { get; set; }");
        code.Should().Contain("[Required]");
        code.Should().Contain("[MaxLength(100)]");
        code.Should().Contain("public string Name { get; set; }");
    }

    [Fact]
    public void GenerateEntityClass_ShouldIncludeInterfaces()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "Customer",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_CUSTOMER" } },
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    PropertyName = "Id",
                    DataType = FieldDataType.Integer,
                    IsRequired = true,
                    SortOrder = 1
                }
            },
            Interfaces = new List<EntityInterface>
            {
                new EntityInterface { InterfaceType = EntityInterfaceType.Base, IsEnabled = true },
                new EntityInterface { InterfaceType = EntityInterfaceType.Archive, IsEnabled = true },
                new EntityInterface { InterfaceType = EntityInterfaceType.Audit, IsEnabled = true }
            }
        };

        // Act
        var code = _generator.GenerateEntityClass(entity);

        // Assert
        code.Should().Contain("public class Customer : IEntity, IArchive, IAuditable");
    }

    [Fact]
    public void GenerateEntityClass_ShouldGenerateNullableValueTypes()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestEntity",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_TEST" } },
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    PropertyName = "Id",
                    DataType = FieldDataType.Integer,
                    IsRequired = true,
                    SortOrder = 1
                },
                new FieldMetadata
                {
                    PropertyName = "Count",
                    DataType = FieldDataType.Integer,
                    IsRequired = false,
                    SortOrder = 2
                },
                new FieldMetadata
                {
                    PropertyName = "Price",
                    DataType = FieldDataType.Decimal,
                    IsRequired = false,
                    SortOrder = 3
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        // Act
        var code = _generator.GenerateEntityClass(entity);

        // Assert
        code.Should().Contain("public int? Count { get; set; }");
        code.Should().Contain("public decimal? Price { get; set; }");
    }

    [Fact]
    public void GenerateEntityClass_ShouldGenerateDefaultValues()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestEntity",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_TEST" } },
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    PropertyName = "Status",
                    DataType = FieldDataType.String,
                    DefaultValue = "Active",
                    SortOrder = 1
                },
                new FieldMetadata
                {
                    PropertyName = "Count",
                    DataType = FieldDataType.Integer,
                    DefaultValue = "0",
                    SortOrder = 2
                },
                new FieldMetadata
                {
                    PropertyName = "IsActive",
                    DataType = FieldDataType.Boolean,
                    DefaultValue = "true",
                    SortOrder = 3
                },
                new FieldMetadata
                {
                    PropertyName = "Price",
                    DataType = FieldDataType.Decimal,
                    DefaultValue = "99.99",
                    SortOrder = 4
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        // Act
        var code = _generator.GenerateEntityClass(entity);

        // Assert
        code.Should().Contain("public string Status { get; set; } = \"Active\";");
        code.Should().Contain("public int Count { get; set; } = 0;");
        code.Should().Contain("public bool IsActive { get; set; } = true;");
        code.Should().Contain("public decimal Price { get; set; } = 99.99m;");
    }

    [Fact]
    public void GenerateEntityClass_ShouldEmitOrganizationalInterface()
    {
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "OrgBound",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_ORG" } },
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata { PropertyName = "OrganizationId", DataType = FieldDataType.Guid, IsRequired = true, SortOrder = 1 }
            },
            Interfaces = new List<EntityInterface>
            {
                new EntityInterface { InterfaceType = EntityInterfaceType.Organization, IsEnabled = true }
            }
        };

        var code = _generator.GenerateEntityClass(entity);

        code.Should().Contain("IOrganizational");
    }

    [Fact]
    public void GenerateEntityClass_ShouldGenerateSpecialDefaultValues()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestEntity",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_TEST" } },
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    PropertyName = "CreatedAt",
                    DataType = FieldDataType.DateTime,
                    DefaultValue = "NOW",
                    SortOrder = 1
                },
                new FieldMetadata
                {
                    PropertyName = "Date",
                    DataType = FieldDataType.Date,
                    DefaultValue = "TODAY",
                    SortOrder = 2
                },
                new FieldMetadata
                {
                    PropertyName = "UniqueId",
                    DataType = FieldDataType.Guid,
                    DefaultValue = "NEWID",
                    SortOrder = 3
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        // Act
        var code = _generator.GenerateEntityClass(entity);

        // Assert
        code.Should().Contain("public DateTime CreatedAt { get; set; } = DateTime.UtcNow;");
        code.Should().Contain("public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);");
        code.Should().Contain("public Guid UniqueId { get; set; } = Guid.NewGuid();");
    }

    [Fact]
    public void GenerateEntityClass_ShouldGenerateColumnTypeAttribute_ForDecimalAndText()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestEntity",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_TEST" } },
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    PropertyName = "Price",
                    DataType = FieldDataType.Decimal,
                    Precision = 10,
                    Scale = 2,
                    SortOrder = 1
                },
                new FieldMetadata
                {
                    PropertyName = "Description",
                    DataType = FieldDataType.Text,
                    SortOrder = 2
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        // Act
        var code = _generator.GenerateEntityClass(entity);

        // Assert
        code.Should().Contain("[Column(TypeName = \"decimal(10,2)\")]");
        code.Should().Contain("[Column(TypeName = \"text\")]");
    }

    [Fact]
    public void GenerateEntityClass_ShouldHandleAllDataTypes()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestAllTypes",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_TEST_ALL_TYPES" } },
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata { PropertyName = "StringField", DataType = FieldDataType.String, SortOrder = 1 },
                new FieldMetadata { PropertyName = "IntField", DataType = FieldDataType.Integer, SortOrder = 2 },
                new FieldMetadata { PropertyName = "LongField", DataType = FieldDataType.Long, SortOrder = 3 },
                new FieldMetadata { PropertyName = "DecimalField", DataType = FieldDataType.Decimal, SortOrder = 4 },
                new FieldMetadata { PropertyName = "BoolField", DataType = FieldDataType.Boolean, SortOrder = 5 },
                new FieldMetadata { PropertyName = "DateTimeField", DataType = FieldDataType.DateTime, SortOrder = 6 },
                new FieldMetadata { PropertyName = "DateField", DataType = FieldDataType.Date, SortOrder = 7 },
                new FieldMetadata { PropertyName = "TextField", DataType = FieldDataType.Text, SortOrder = 8 },
                new FieldMetadata { PropertyName = "GuidField", DataType = FieldDataType.Guid, SortOrder = 9 }
            },
            Interfaces = new List<EntityInterface>()
        };

        // Act
        var code = _generator.GenerateEntityClass(entity);

        // Assert
        code.Should().Contain("public string StringField { get; set; }");
        code.Should().Contain("public int IntField { get; set; }");
        code.Should().Contain("public long LongField { get; set; }");
        code.Should().Contain("public decimal DecimalField { get; set; }");
        code.Should().Contain("public bool BoolField { get; set; }");
        code.Should().Contain("public DateTime DateTimeField { get; set; }");
        code.Should().Contain("public DateOnly DateField { get; set; }");
        code.Should().Contain("public string TextField { get; set; }");
        code.Should().Contain("public Guid GuidField { get; set; }");
    }

    [Fact]
    public void GenerateEntityClass_ShouldIncludeXmlComments()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "Product",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_PRODUCT" } },
            Description = new Dictionary<string, string?> { { "en", "Product description" } },
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    PropertyName = "Name",
                    DisplayName = new Dictionary<string, string?> { { "en", "FIELD_NAME" } },
                    DataType = FieldDataType.String,
                    SortOrder = 1
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        // Act
        var code = _generator.GenerateEntityClass(entity);

        // Assert
        code.Should().Contain("/// <summary>");
        code.Should().Contain("/// ENTITY_PRODUCT");
        code.Should().Contain("/// Product description");
        code.Should().Contain("/// 自动生成于:");
        code.Should().Contain("/// FIELD_NAME");
    }

    [Fact]
    public void GenerateEntityClass_ShouldGenerateStringEmptyForRequiredStrings()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestEntity",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_TEST" } },
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    PropertyName = "Name",
                    DataType = FieldDataType.String,
                    IsRequired = true,
                    SortOrder = 1
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        // Act
        var code = _generator.GenerateEntityClass(entity);

        // Assert
        code.Should().Contain("public string Name { get; set; } = string.Empty;");
    }

    [Fact]
    public void GenerateInterfaces_ShouldGenerateAllInterfaceDefinitions()
    {
        // Act
        var code = _generator.GenerateInterfaces();

        // Assert
        code.Should().Contain("public interface IEntity");
        code.Should().Contain("public interface IArchive");
        code.Should().Contain("public interface IAuditable");
        code.Should().Contain("public interface IVersioned");
        code.Should().Contain("public interface ITimeVersioned");
        code.Should().Contain("int Id { get; set; }");
        code.Should().Contain("string Code { get; set; }");
        code.Should().Contain("DateTime CreatedAt { get; set; }");
        code.Should().Contain("int Version { get; set; }");
        code.Should().Contain("DateTime ValidFrom { get; set; }");
    }

    [Fact]
    public void GenerateMultipleEntities_ShouldGenerateCodeForAllEntities()
    {
        // Arrange
        var entities = new List<EntityDefinition>
        {
            new EntityDefinition
            {
                Id = Guid.NewGuid(),
                Namespace = "BobCrm.Test",
                EntityName = "Product",
                DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_PRODUCT" } },
                FullTypeName = "BobCrm.Test.Product",
                Fields = new List<FieldMetadata>
                {
                    new FieldMetadata { PropertyName = "Id", DataType = FieldDataType.Integer, SortOrder = 1 }
                },
                Interfaces = new List<EntityInterface>()
            },
            new EntityDefinition
            {
                Id = Guid.NewGuid(),
                Namespace = "BobCrm.Test",
                EntityName = "Customer",
                DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_CUSTOMER" } },
                FullTypeName = "BobCrm.Test.Customer",
                Fields = new List<FieldMetadata>
                {
                    new FieldMetadata { PropertyName = "Id", DataType = FieldDataType.Integer, SortOrder = 1 }
                },
                Interfaces = new List<EntityInterface>()
            }
        };

        // Act
        var result = _generator.GenerateMultipleEntities(entities);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("BobCrm.Test.Product");
        result.Should().ContainKey("BobCrm.Test.Customer");
        result["BobCrm.Test.Product"].Should().Contain("public class Product");
        result["BobCrm.Test.Customer"].Should().Contain("public class Customer");
    }

    [Fact]
    public void GenerateDbContextExtension_ShouldGenerateDbSetProperties()
    {
        // Arrange
        var entities = new List<EntityDefinition>
        {
            new EntityDefinition
            {
                Id = Guid.NewGuid(),
                Namespace = "BobCrm.Test",
                EntityName = "Product",
                Fields = new List<FieldMetadata>(),
                Interfaces = new List<EntityInterface>()
            },
            new EntityDefinition
            {
                Id = Guid.NewGuid(),
                Namespace = "BobCrm.Test",
                EntityName = "Customer",
                Fields = new List<FieldMetadata>(),
                Interfaces = new List<EntityInterface>()
            }
        };

        // Act
        var code = _generator.GenerateDbContextExtension(entities);

        // Assert
        code.Should().Contain("namespace BobCrm.Api.Infrastructure");
        code.Should().Contain("public partial class AppDbContext");
        code.Should().Contain("public DbSet<BobCrm.Test.Product> Products => Set<BobCrm.Test.Product>();");
        code.Should().Contain("public DbSet<BobCrm.Test.Customer> Customers => Set<BobCrm.Test.Customer>();");
    }
}
