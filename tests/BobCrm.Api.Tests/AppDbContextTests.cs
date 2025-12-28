using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Tests;

/// <summary>
/// AppDbContext 测试
/// 验证 EF Core 映射正确性
/// </summary>
public class AppDbContextTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    #region Entity Mapping Tests

    [Fact]
    public async Task EntityDefinition_ShouldPersistAndRetrieve()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft
        };

        // Act
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        // Assert
        var retrieved = await ctx.EntityDefinitions.FindAsync(entity.Id);
        retrieved.Should().NotBeNull();
        retrieved!.EntityName.Should().Be("TestEntity");
        retrieved.Namespace.Should().Be("Test");
    }

    [Fact]
    public async Task FieldMetadata_ShouldPersistWithEntityDefinition()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft
        };
        var field = new FieldMetadata
        {
            PropertyName = "TestField",
            DataType = FieldDataType.String,
            Source = FieldSource.Custom,
            EntityDefinition = entity
        };
        entity.Fields.Add(field);

        // Act
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        // Assert
        var retrieved = await ctx.EntityDefinitions
            .Include(e => e.Fields)
            .FirstOrDefaultAsync(e => e.Id == entity.Id);
        retrieved!.Fields.Should().HaveCount(1);
        retrieved.Fields.First().PropertyName.Should().Be("TestField");
    }

    [Fact]
    public async Task Customer_ShouldPersistAndRetrieve()
    {
        // Arrange
        await using var ctx = CreateContext();
        var customer = new Customer
        {
            Code = "CUST001",
            Name = "Test Customer"
        };

        // Act
        ctx.Customers.Add(customer);
        await ctx.SaveChangesAsync();

        // Assert
        var retrieved = await ctx.Customers.FindAsync(customer.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Code.Should().Be("CUST001");
        retrieved.Name.Should().Be("Test Customer");
    }

    [Fact]
    public async Task FormTemplate_ShouldPersistAndRetrieve()
    {
        // Arrange
        await using var ctx = CreateContext();
        var template = new FormTemplate
        {
            Name = "TestTemplate",
            EntityType = "Test.TestEntity",
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.Detail
        };

        // Act
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();

        // Assert
        var retrieved = await ctx.FormTemplates.FindAsync(template.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("TestTemplate");
    }

    [Fact]
    public async Task RoleProfile_ShouldPersistAndRetrieve()
    {
        // Arrange
        await using var ctx = CreateContext();
        var role = new RoleProfile
        {
            Code = "TEST.ROLE",
            Name = "Test Role"
        };

        // Act
        ctx.RoleProfiles.Add(role);
        await ctx.SaveChangesAsync();

        // Assert
        var retrieved = await ctx.RoleProfiles.FindAsync(role.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Code.Should().Be("TEST.ROLE");
    }

    [Fact]
    public async Task FunctionNode_ShouldPersistAndRetrieve()
    {
        // Arrange
        await using var ctx = CreateContext();
        var function = new FunctionNode
        {
            Code = "TEST.FUNCTION",
            Name = "Test Function"
        };

        // Act
        ctx.FunctionNodes.Add(function);
        await ctx.SaveChangesAsync();

        // Assert
        var retrieved = await ctx.FunctionNodes.FindAsync(function.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Code.Should().Be("TEST.FUNCTION");
    }

    #endregion

    #region Relationship Tests

    [Fact]
    public async Task EntityDefinition_ParentChild_ShouldWorkCorrectly()
    {
        // Arrange
        await using var ctx = CreateContext();
        var parent = new EntityDefinition
        {
            EntityName = "ParentEntity",
            Namespace = "Test",
            FullTypeName = "Test.ParentEntity",
            Status = EntityStatus.Draft
        };
        var child = new EntityDefinition
        {
            EntityName = "ChildEntity",
            Namespace = "Test",
            FullTypeName = "Test.ChildEntity",
            Status = EntityStatus.Draft,
            ParentEntityId = parent.Id
        };

        // Act
        ctx.EntityDefinitions.AddRange(parent, child);
        await ctx.SaveChangesAsync();

        // Assert
        var retrievedChild = await ctx.EntityDefinitions.FindAsync(child.Id);
        retrievedChild!.ParentEntityId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task RoleProfile_FunctionPermissions_ShouldWorkCorrectly()
    {
        // Arrange
        await using var ctx = CreateContext();
        var function = new FunctionNode
        {
            Code = "TEST.FUNCTION",
            Name = "Test Function"
        };
        var role = new RoleProfile
        {
            Code = "TEST.ROLE",
            Name = "Test Role"
        };
        ctx.FunctionNodes.Add(function);
        ctx.RoleProfiles.Add(role);
        await ctx.SaveChangesAsync();

        var permission = new RoleFunctionPermission
        {
            RoleId = role.Id,
            FunctionId = function.Id
        };
        ctx.RoleFunctionPermissions.Add(permission);
        await ctx.SaveChangesAsync();

        // Assert
        var retrieved = await ctx.RoleFunctionPermissions
            .FirstOrDefaultAsync(p => p.RoleId == role.Id && p.FunctionId == function.Id);
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task TemplateStateBinding_ShouldWorkCorrectly()
    {
        // Arrange
        await using var ctx = CreateContext();
        var template = new FormTemplate
        {
            Name = "TestTemplate",
            EntityType = "Test.TestEntity",
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.Detail
        };
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();

        var binding = new TemplateStateBinding
        {
            TemplateId = template.Id,
            EntityType = "Test.TestEntity",
            ViewState = "DetailView",
            Priority = 1
        };
        ctx.TemplateStateBindings.Add(binding);
        await ctx.SaveChangesAsync();

        // Assert
        var retrieved = await ctx.TemplateStateBindings
            .Include(b => b.Template)
            .FirstOrDefaultAsync(b => b.Id == binding.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Template.Should().NotBeNull();
        retrieved.Template!.Name.Should().Be("TestTemplate");
    }

    #endregion

    #region IsDeleted Property Tests

    [Fact]
    public async Task FieldMetadata_IsDeletedProperty_ShouldPersist()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft
        };
        var field = new FieldMetadata
        {
            PropertyName = "TestField",
            DataType = FieldDataType.String,
            Source = FieldSource.Custom,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            EntityDefinition = entity
        };
        entity.Fields.Add(field);

        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        // Act
        var retrievedField = await ctx.FieldMetadatas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == field.Id);

        // Assert
        retrievedField.Should().NotBeNull();
        retrievedField!.IsDeleted.Should().BeTrue();
        retrievedField.DeletedAt.Should().NotBeNull();
    }

    #endregion

    #region DbSet Existence Tests

    [Fact]
    public void DbContext_ShouldHaveAllRequiredDbSets()
    {
        // Arrange
        using var ctx = CreateContext();

        // Assert - Core entities
        ctx.EntityDefinitions.Should().NotBeNull();
        ctx.FieldMetadatas.Should().NotBeNull();
        ctx.EntityInterfaces.Should().NotBeNull();
        
        // Assert - Template entities
        ctx.FormTemplates.Should().NotBeNull();
        ctx.TemplateStateBindings.Should().NotBeNull();
        
        // Assert - Access entities
        ctx.RoleProfiles.Should().NotBeNull();
        ctx.FunctionNodes.Should().NotBeNull();
        ctx.RoleFunctionPermissions.Should().NotBeNull();
        ctx.RoleDataScopes.Should().NotBeNull();
        ctx.RoleAssignments.Should().NotBeNull();
        
        // Assert - Customer entities
        ctx.Customers.Should().NotBeNull();
        
        // Assert - Infrastructure entities
        ctx.RefreshTokens.Should().NotBeNull();
        ctx.DDLScripts.Should().NotBeNull();
        ctx.LocalizationResources.Should().NotBeNull();
        ctx.AuditLogs.Should().NotBeNull();
    }

    #endregion

    #region Audit Fields Tests

    [Fact]
    public async Task EntityDefinition_ShouldTrackCreatedAt()
    {
        // Arrange
        await using var ctx = CreateContext();
        var beforeCreate = DateTime.UtcNow;
        var entity = new EntityDefinition
        {
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft
        };

        // Act
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();
        var afterCreate = DateTime.UtcNow;

        // Assert
        entity.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        entity.CreatedAt.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public async Task Customer_ShouldPersistVersion()
    {
        // Arrange
        await using var ctx = CreateContext();
        var customer = new Customer
        {
            Code = "CUST001",
            Name = "Test Customer",
            Version = 1
        };

        // Act
        ctx.Customers.Add(customer);
        await ctx.SaveChangesAsync();

        // Assert
        var retrieved = await ctx.Customers.FindAsync(customer.Id);
        retrieved!.Version.Should().Be(1);
    }

    #endregion

    #region Enum Mapping Tests

    [Fact]
    public async Task EntityDefinition_Status_ShouldMapCorrectly()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Published
        };

        // Act
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        // Assert
        var retrieved = await ctx.EntityDefinitions.FindAsync(entity.Id);
        retrieved!.Status.Should().Be(EntityStatus.Published);
    }

    [Fact]
    public async Task FieldMetadata_DataType_ShouldMapCorrectly()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft
        };
        var field = new FieldMetadata
        {
            PropertyName = "TestField",
            DataType = FieldDataType.DateTime,
            Source = FieldSource.Custom,
            EntityDefinition = entity
        };
        entity.Fields.Add(field);

        // Act
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        // Assert
        var retrieved = await ctx.FieldMetadatas.FindAsync(field.Id);
        retrieved!.DataType.Should().Be(FieldDataType.DateTime);
    }

    #endregion
}
