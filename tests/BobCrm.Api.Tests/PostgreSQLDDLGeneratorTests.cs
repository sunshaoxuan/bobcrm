using BobCrm.Api.Base.Models;
using BobCrm.Api.Services;
using FluentAssertions;

namespace BobCrm.Api.Tests;

public class PostgreSQLDDLGeneratorTests
{
    private readonly PostgreSQLDDLGenerator _generator;

    public PostgreSQLDDLGeneratorTests()
    {
        _generator = new PostgreSQLDDLGenerator();
    }

    [Fact]
    public void GenerateCreateTableScript_ShouldGenerateValidDDL_WithBasicFields()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "Product",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_PRODUCT" } },
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
                },
                new FieldMetadata
                {
                    PropertyName = "Price",
                    DisplayName = new Dictionary<string, string?> { { "en", "FIELD_PRICE" } },
                    DataType = FieldDataType.Decimal,
                    Precision = 10,
                    Scale = 2,
                    IsRequired = true,
                    SortOrder = 3
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        // Act
        var ddl = _generator.GenerateCreateTableScript(entity);

        // Assert
        ddl.Should().Contain("CREATE TABLE IF NOT EXISTS \"Products\"");
        ddl.Should().Contain("\"Id\" INTEGER NOT NULL");
        ddl.Should().Contain("\"Name\" VARCHAR(100) NOT NULL");
        ddl.Should().Contain("\"Price\" NUMERIC(10,2) NOT NULL");
        ddl.Should().Contain("ALTER TABLE \"Products\" ADD PRIMARY KEY (\"Id\")");
    }

    [Fact]
    public void GenerateCreateTableScript_ShouldGenerateUniqueIndex_WhenArchiveInterfaceEnabled()
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
                },
                new FieldMetadata
                {
                    PropertyName = "Code",
                    DataType = FieldDataType.String,
                    Length = 64,
                    IsRequired = true,
                    SortOrder = 10
                }
            },
            Interfaces = new List<EntityInterface>
            {
                new EntityInterface
                {
                    InterfaceType = EntityInterfaceType.Archive,
                    IsEnabled = true
                }
            }
        };

        // Act
        var ddl = _generator.GenerateCreateTableScript(entity);

        // Assert
        ddl.Should().Contain("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_Customers_Code\"");
    }

    [Fact]
    public void GenerateCreateTableScript_ShouldGenerateDefaultValue_ForAllTypes()
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
                    PropertyName = "Status",
                    DataType = FieldDataType.String,
                    Length = 50,
                    DefaultValue = "Active",
                    SortOrder = 2
                },
                new FieldMetadata
                {
                    PropertyName = "Count",
                    DataType = FieldDataType.Integer,
                    DefaultValue = "0",
                    SortOrder = 3
                },
                new FieldMetadata
                {
                    PropertyName = "IsActive",
                    DataType = FieldDataType.Boolean,
                    DefaultValue = "true",
                    SortOrder = 4
                },
                new FieldMetadata
                {
                    PropertyName = "CreatedAt",
                    DataType = FieldDataType.DateTime,
                    DefaultValue = "NOW",
                    SortOrder = 5
                }
            },
            Interfaces = new List<EntityInterface>()
        };

        // Act
        var ddl = _generator.GenerateCreateTableScript(entity);

        // Assert
        ddl.Should().Contain("\"Status\" VARCHAR(50) NULL DEFAULT 'Active'");
        ddl.Should().Contain("\"Count\" INTEGER NULL DEFAULT 0");
        ddl.Should().Contain("\"IsActive\" BOOLEAN NULL DEFAULT TRUE");
        ddl.Should().Contain("\"CreatedAt\" TIMESTAMP WITHOUT TIME ZONE NULL DEFAULT CURRENT_TIMESTAMP");
    }

    [Fact]
    public void GenerateCreateTableScript_ShouldIncludeOrganizationIdOnly()
    {
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestEntity",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_TEST" } },
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata { PropertyName = "OrganizationId", DataType = FieldDataType.Guid, IsRequired = true, TableName = "OrganizationNodes", IsEntityRef = true, SortOrder = 1 }
            },
            Interfaces = new List<EntityInterface>
            {
                new EntityInterface { InterfaceType = EntityInterfaceType.Organization, IsEnabled = true }
            }
        };

        var ddl = _generator.GenerateCreateTableScript(entity);

        ddl.Should().Contain("\"OrganizationId\" UUID NOT NULL");
        ddl.Should().Contain("IX_TestEntitys_OrganizationId");
        ddl.Should().NotContain("OrganizationCode");
        ddl.Should().NotContain("OrganizationPathCode");
    }

    [Fact]
    public void GenerateAlterTableAddColumns_ShouldGenerateValidDDL()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "Product",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_PRODUCT" } }
        };

        var newFields = new List<FieldMetadata>
        {
            new FieldMetadata
            {
                PropertyName = "Description",
                DisplayName = new Dictionary<string, string?> { { "en", "FIELD_DESCRIPTION" } },
                DataType = FieldDataType.Text,
                IsRequired = false,
                SortOrder = 10
            },
            new FieldMetadata
            {
                PropertyName = "Stock",
                DisplayName = new Dictionary<string, string?> { { "en", "FIELD_STOCK" } },
                DataType = FieldDataType.Integer,
                IsRequired = true,
                DefaultValue = "0",
                SortOrder = 11
            }
        };

        // Act
        var ddl = _generator.GenerateAlterTableAddColumns(entity, newFields);

        // Assert
        ddl.Should().Contain("ALTER TABLE \"Products\" ADD COLUMN IF NOT EXISTS \"Description\" TEXT NULL");
        ddl.Should().Contain("ALTER TABLE \"Products\" ADD COLUMN IF NOT EXISTS \"Stock\" INTEGER NOT NULL DEFAULT 0");
    }

    [Fact]
    public void GenerateAlterTableModifyColumns_ShouldGenerateValidDDL()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "Product",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_PRODUCT" } }
        };

        var field1 = new FieldMetadata
        {
            PropertyName = "Name",
            DataType = FieldDataType.String,
            Length = 100
        };

        var field2 = new FieldMetadata
        {
            PropertyName = "Description",
            DataType = FieldDataType.String,
            Length = 500
        };

        var fieldLengthChanges = new Dictionary<FieldMetadata, int>
        {
            { field1, 200 },
            { field2, 1000 }
        };

        // Act
        var ddl = _generator.GenerateAlterTableModifyColumns(entity, fieldLengthChanges);

        // Assert
        ddl.Should().Contain("ALTER TABLE \"Products\" ALTER COLUMN \"Name\" TYPE VARCHAR(200)");
        ddl.Should().Contain("ALTER TABLE \"Products\" ALTER COLUMN \"Description\" TYPE VARCHAR(1000)");
    }

    [Fact]
    public void GenerateDropTableScript_ShouldGenerateValidDDL()
    {
        // Arrange
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "Product",
            DisplayName = new Dictionary<string, string?> { { "en", "ENTITY_PRODUCT" } }
        };

        // Act
        var ddl = _generator.GenerateDropTableScript(entity);

        // Assert
        ddl.Should().Contain("DROP TABLE IF EXISTS \"Products\" CASCADE");
    }

    [Fact]
    public void GenerateInterfaceFields_ShouldGenerateBaseFields()
    {
        // Arrange
        var entityInterface = new EntityInterface
        {
            InterfaceType = EntityInterfaceType.Base,
            IsEnabled = true
        };

        // Act
        var fields = _generator.GenerateInterfaceFields(entityInterface);

        // Assert
        fields.Should().HaveCount(1);
        fields[0].PropertyName.Should().Be("Id");
        fields[0].DataType.Should().Be(FieldDataType.Integer);
        fields[0].IsRequired.Should().BeTrue();
    }

    [Fact]
    public void GenerateInterfaceFields_ShouldGenerateArchiveFields()
    {
        // Arrange
        var entityInterface = new EntityInterface
        {
            InterfaceType = EntityInterfaceType.Archive,
            IsEnabled = true
        };

        // Act
        var fields = _generator.GenerateInterfaceFields(entityInterface);

        // Assert
        fields.Should().HaveCount(2);
        fields.Should().Contain(f => f.PropertyName == "Code");
        fields.Should().Contain(f => f.PropertyName == "Name");
    }

    [Fact]
    public void GenerateInterfaceFields_ShouldGenerateAuditFields()
    {
        // Arrange
        var entityInterface = new EntityInterface
        {
            InterfaceType = EntityInterfaceType.Audit,
            IsEnabled = true
        };

        // Act
        var fields = _generator.GenerateInterfaceFields(entityInterface);

        // Assert
        fields.Should().HaveCount(5);
        fields.Should().Contain(f => f.PropertyName == "CreatedAt");
        fields.Should().Contain(f => f.PropertyName == "CreatedBy");
        fields.Should().Contain(f => f.PropertyName == "UpdatedAt");
        fields.Should().Contain(f => f.PropertyName == "UpdatedBy");
        fields.Should().Contain(f => f.PropertyName == "Version");
    }

    [Fact]
    public void GenerateInterfaceFields_ShouldGenerateVersionFields()
    {
        // Arrange
        var entityInterface = new EntityInterface
        {
            InterfaceType = EntityInterfaceType.Version,
            IsEnabled = true
        };

        // Act
        var fields = _generator.GenerateInterfaceFields(entityInterface);

        // Assert
        fields.Should().HaveCount(1);
        fields[0].PropertyName.Should().Be("Version");
        fields[0].DefaultValue.Should().Be("1");
    }

    [Fact]
    public void GenerateInterfaceFields_ShouldGenerateTimeVersionFields()
    {
        // Arrange
        var entityInterface = new EntityInterface
        {
            InterfaceType = EntityInterfaceType.TimeVersion,
            IsEnabled = true
        };

        // Act
        var fields = _generator.GenerateInterfaceFields(entityInterface);

        // Assert
        fields.Should().HaveCount(3);
        fields.Should().Contain(f => f.PropertyName == "ValidFrom");
        fields.Should().Contain(f => f.PropertyName == "ValidTo");
        fields.Should().Contain(f => f.PropertyName == "VersionNo");
    }

    [Fact]
    public void GenerateCreateTableScript_ShouldHandleAllDataTypes()
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
                new FieldMetadata { PropertyName = "Id", DataType = FieldDataType.Integer, IsRequired = true, SortOrder = 1 },
                new FieldMetadata { PropertyName = "StringField", DataType = FieldDataType.String, Length = 100, SortOrder = 2 },
                new FieldMetadata { PropertyName = "IntField", DataType = FieldDataType.Integer, SortOrder = 3 },
                new FieldMetadata { PropertyName = "LongField", DataType = FieldDataType.Long, SortOrder = 4 },
                new FieldMetadata { PropertyName = "DecimalField", DataType = FieldDataType.Decimal, Precision = 18, Scale = 2, SortOrder = 5 },
                new FieldMetadata { PropertyName = "BoolField", DataType = FieldDataType.Boolean, SortOrder = 6 },
                new FieldMetadata { PropertyName = "DateTimeField", DataType = FieldDataType.DateTime, SortOrder = 7 },
                new FieldMetadata { PropertyName = "DateField", DataType = FieldDataType.Date, SortOrder = 8 },
                new FieldMetadata { PropertyName = "TextField", DataType = FieldDataType.Text, SortOrder = 9 },
                new FieldMetadata { PropertyName = "GuidField", DataType = FieldDataType.Guid, SortOrder = 10 }
            },
            Interfaces = new List<EntityInterface>()
        };

        // Act
        var ddl = _generator.GenerateCreateTableScript(entity);

        // Assert
        ddl.Should().Contain("\"StringField\" VARCHAR(100) NULL");
        ddl.Should().Contain("\"IntField\" INTEGER NULL");
        ddl.Should().Contain("\"LongField\" BIGINT NULL");
        ddl.Should().Contain("\"DecimalField\" NUMERIC(18,2) NULL");
        ddl.Should().Contain("\"BoolField\" BOOLEAN NULL");
        ddl.Should().Contain("\"DateTimeField\" TIMESTAMP WITHOUT TIME ZONE NULL");
        ddl.Should().Contain("\"DateField\" DATE NULL");
        ddl.Should().Contain("\"TextField\" TEXT NULL");
        ddl.Should().Contain("\"GuidField\" UUID NULL");
    }
}
