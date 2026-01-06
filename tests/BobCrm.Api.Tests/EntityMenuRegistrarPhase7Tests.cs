using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Tests;

public class EntityMenuRegistrarPhase7Tests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RegisterAsync_WhenNodesExistButInWrongState_ShouldNormalizeHierarchyAndBindingPermission()
    {
        await using var db = CreateContext();

        var root = new FunctionNode { Code = "APP.ROOT", Name = "root", IsMenu = true, SortOrder = 0 };
        var domain = new FunctionNode { Code = "CRM", Name = "crm", IsMenu = true, SortOrder = 30, ParentId = null };
        var module = new FunctionNode { Code = "CRM.ENTITY", Name = "wrong", Icon = "x", IsMenu = true, SortOrder = 0, ParentId = null };
        db.FunctionNodes.AddRange(root, domain, module);

        var template = new FormTemplate
        {
            Name = "Customer List",
            EntityType = "customer",
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.List,
            IsSystemDefault = true
        };
        db.FormTemplates.Add(template);
        await db.SaveChangesAsync();

        var binding = new TemplateStateBinding
        {
            EntityType = "customer",
            ViewState = "List",
            TemplateId = template.Id,
            IsDefault = true,
            Priority = 1,
            RequiredPermission = "OLD.PERMISSION"
        };
        db.TemplateStateBindings.Add(binding);

        var entity = new EntityDefinition
        {
            Namespace = "CRM",
            EntityName = "Customer",
            FullTypeName = "CRM.Customer",
            EntityRoute = "customer",
            ApiEndpoint = "/api/customers",
            Status = EntityStatus.Published,
            StructureType = EntityStructureType.Single,
            Source = EntitySource.System,
            Category = "CRM",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "客户" }
        };
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var registrar = new EntityMenuRegistrar(db, NullLogger<EntityMenuRegistrar>.Instance);

        var result = await registrar.RegisterAsync(entity, "admin");

        result.Success.Should().BeTrue();
        result.FunctionCode.Should().Be("CRM.ENTITY.CUSTOMER");
        result.TemplateBindingId.Should().Be(binding.Id);

        db.ChangeTracker.Clear();

        var storedDomain = await db.FunctionNodes.AsNoTracking().SingleAsync(x => x.Code == "CRM");
        storedDomain.ParentId.Should().Be(root.Id);

        var storedModule = await db.FunctionNodes.AsNoTracking().SingleAsync(x => x.Code == "CRM.ENTITY");
        storedModule.ParentId.Should().Be(storedDomain.Id);
        storedModule.Name.Should().Be("业务实体");
        storedModule.Icon.Should().Be("database");
        storedModule.SortOrder.Should().Be(storedDomain.SortOrder + 1);

        var storedEntityNode = await db.FunctionNodes.AsNoTracking().SingleAsync(x => x.Code == "CRM.ENTITY.CUSTOMER");
        storedEntityNode.ParentId.Should().Be(storedModule.Id);
        storedEntityNode.TemplateStateBindingId.Should().Be(binding.Id);

        var storedBinding = await db.TemplateStateBindings.AsNoTracking().SingleAsync(x => x.Id == binding.Id);
        storedBinding.RequiredPermission.Should().Be("CRM.ENTITY.CUSTOMER");
    }

    [Fact]
    public async Task RegisterAsync_WhenDomainNotExists_ShouldUseEntityDomainNameAndDefaultSortOrder()
    {
        await using var db = CreateContext();

        db.FunctionNodes.Add(new FunctionNode { Code = "APP.ROOT", Name = "root", IsMenu = true, SortOrder = 0 });
        db.EntityDomains.Add(new EntityDomain
        {
            Code = "SCM",
            Name = new Dictionary<string, string?> { ["ja"] = "サプライチェーン", ["zh"] = "供应链" },
            SortOrder = 0,
            IsEnabled = true
        });

        var template = new FormTemplate
        {
            Name = "Product List",
            EntityType = "product",
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.List,
            IsSystemDefault = true
        };
        db.FormTemplates.Add(template);
        await db.SaveChangesAsync();

        db.TemplateStateBindings.Add(new TemplateStateBinding
        {
            EntityType = "product",
            ViewState = "List",
            TemplateId = template.Id,
            IsDefault = true
        });

        var entity = new EntityDefinition
        {
            Namespace = "SCM",
            EntityName = "Product",
            FullTypeName = "SCM.Product",
            EntityRoute = "product",
            ApiEndpoint = "/api/products",
            Status = EntityStatus.Published,
            StructureType = EntityStructureType.Single,
            Source = EntitySource.Custom,
            Category = "SCM",
            DisplayName = new Dictionary<string, string?> { ["ja"] = "商品" }
        };
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var registrar = new EntityMenuRegistrar(db, NullLogger<EntityMenuRegistrar>.Instance);

        var result = await registrar.RegisterAsync(entity, "admin");

        result.Success.Should().BeTrue();
        result.DomainCode.Should().Be("SCM");

        db.ChangeTracker.Clear();
        var domainNode = await db.FunctionNodes.AsNoTracking().SingleAsync(x => x.Code == "SCM");
        domainNode.Name.Should().Be("供应链");
        domainNode.SortOrder.Should().Be(100);
    }
}
