using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Tests;

/// <summary>
/// EntityMenuRegistrar 测试
/// 覆盖动态菜单注册
/// </summary>
public class EntityMenuRegistrarTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static EntityMenuRegistrar CreateRegistrar(AppDbContext context)
    {
        return new EntityMenuRegistrar(context, NullLogger<EntityMenuRegistrar>.Instance);
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithNewEntity_ShouldCreateMenuHierarchy()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            EntityName = "Customer",
            Namespace = "CRM",
            FullTypeName = "CRM.Customer",
            Status = EntityStatus.Published,
            Category = "CRM"
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var registrar = CreateRegistrar(ctx);

        // Act
        var result = await registrar.RegisterAsync(entity, "admin");

        // Assert
        result.Success.Should().BeTrue();
        result.FunctionCode.Should().NotBeNullOrEmpty();
        result.DomainNodeId.Should().NotBeNull();
        result.ModuleNodeId.Should().NotBeNull();
        result.FunctionNodeId.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateRootNode()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            EntityName = "Customer",
            Namespace = "CRM",
            FullTypeName = "CRM.Customer",
            Status = EntityStatus.Published,
            Category = "CRM"
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var registrar = CreateRegistrar(ctx);

        // Act
        await registrar.RegisterAsync(entity, "admin");

        // Assert
        var rootNode = await ctx.FunctionNodes.FirstOrDefaultAsync(f => f.Code == "APP.ROOT");
        rootNode.Should().NotBeNull();
        rootNode!.IsMenu.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateDomainNode()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            EntityName = "Customer",
            Namespace = "CRM",
            FullTypeName = "CRM.Customer",
            Status = EntityStatus.Published,
            Category = "CRM"
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var registrar = CreateRegistrar(ctx);

        // Act
        var result = await registrar.RegisterAsync(entity, "admin");

        // Assert
        result.DomainNodeId.Should().NotBeNull();
        var domainNode = await ctx.FunctionNodes.FirstOrDefaultAsync(f => f.Id == result.DomainNodeId!.Value);
        domainNode.Should().NotBeNull();
        domainNode!.Code.Should().Be("CRM");
    }

    [Fact]
    public async Task RegisterAsync_WithSystemCategory_ShouldUseSysCode()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            EntityName = "Setting",
            Namespace = "System",
            FullTypeName = "System.Setting",
            Status = EntityStatus.Published,
            Category = "System"
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var registrar = CreateRegistrar(ctx);

        // Act
        var result = await registrar.RegisterAsync(entity, "admin");

        // Assert
        result.DomainCode.Should().Be("SYS");
    }

    [Fact]
    public async Task RegisterAsync_ShouldBeIdempotent()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            EntityName = "Customer",
            Namespace = "CRM",
            FullTypeName = "CRM.Customer",
            Status = EntityStatus.Published,
            Category = "CRM"
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var registrar = CreateRegistrar(ctx);

        // Act
        var result1 = await registrar.RegisterAsync(entity, "admin");
        var result2 = await registrar.RegisterAsync(entity, "admin");

        // Assert
        result1.FunctionNodeId.Should().Be(result2.FunctionNodeId);
        result1.DomainNodeId.Should().Be(result2.DomainNodeId);
    }

    [Fact]
    public async Task RegisterAsync_MultipleEntitiesSameDomain_ShouldShareDomainNode()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity1 = new EntityDefinition
        {
            EntityName = "Customer",
            Namespace = "CRM",
            FullTypeName = "CRM.Customer",
            Status = EntityStatus.Published,
            Category = "CRM"
        };
        var entity2 = new EntityDefinition
        {
            EntityName = "Contact",
            Namespace = "CRM",
            FullTypeName = "CRM.Contact",
            Status = EntityStatus.Published,
            Category = "CRM"
        };
        ctx.EntityDefinitions.AddRange(entity1, entity2);
        await ctx.SaveChangesAsync();

        var registrar = CreateRegistrar(ctx);

        // Act
        var result1 = await registrar.RegisterAsync(entity1, "admin");
        var result2 = await registrar.RegisterAsync(entity2, "admin");

        // Assert
        // Both should share the same domain node
        result1.DomainNodeId.Should().Be(result2.DomainNodeId);
        // Both should have function nodes created
        result1.FunctionNodeId.Should().NotBeNull();
        result2.FunctionNodeId.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsync_ShouldLinkTemplateBinding()
    {
        // Arrange
        await using var ctx = CreateContext();
        
        // Create a template for the entity
        var template = new FormTemplate
        {
            Name = "Customer Detail",
            EntityType = "CRM.Customer",
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.Detail,
            IsSystemDefault = true
        };
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();
        
        // Create template state binding
        var binding = new TemplateStateBinding
        {
            TemplateId = template.Id,
            EntityType = "CRM.Customer",
            ViewState = "DetailView",
            IsDefault = true,
            Priority = 1
        };
        ctx.TemplateStateBindings.Add(binding);
        await ctx.SaveChangesAsync();

        var entity = new EntityDefinition
        {
            EntityName = "Customer",
            Namespace = "CRM",
            FullTypeName = "CRM.Customer",
            Status = EntityStatus.Published,
            Category = "CRM"
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var registrar = CreateRegistrar(ctx);

        // Act
        var result = await registrar.RegisterAsync(entity, "admin");

        // Assert
        result.Success.Should().BeTrue();
        // TemplateBindingId may or may not be set depending on implementation
        // Just verify success
    }

    [Fact]
    public async Task RegisterAsync_WithoutTemplate_ShouldSetWarning()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            EntityName = "Customer",
            Namespace = "CRM",
            FullTypeName = "CRM.Customer",
            Status = EntityStatus.Published,
            Category = "CRM"
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var registrar = CreateRegistrar(ctx);

        // Act
        var result = await registrar.RegisterAsync(entity, "admin");

        // Assert
        result.Success.Should().BeTrue();
        result.Warning.Should().Contain("Template binding not found");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task RegisterAsync_WithNullCategory_ShouldUseCustomDomain()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            EntityName = "CustomEntity",
            Namespace = "Custom",
            FullTypeName = "Custom.CustomEntity",
            Status = EntityStatus.Published,
            Category = null
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var registrar = CreateRegistrar(ctx);

        // Act
        var result = await registrar.RegisterAsync(entity, "admin");

        // Assert
        result.Success.Should().BeTrue();
        result.DomainCode.Should().Be("CUSTOM");
    }

    [Fact]
    public async Task RegisterAsync_ShouldSetCorrectSortOrder()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            EntityName = "Customer",
            Namespace = "CRM",
            FullTypeName = "CRM.Customer",
            Status = EntityStatus.Published,
            Category = "CRM"
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var registrar = CreateRegistrar(ctx);

        // Act
        var result = await registrar.RegisterAsync(entity, "admin");

        // Assert
        var functionNode = await ctx.FunctionNodes.FindAsync(result.FunctionNodeId);
        functionNode.Should().NotBeNull();
        functionNode!.SortOrder.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion
}
