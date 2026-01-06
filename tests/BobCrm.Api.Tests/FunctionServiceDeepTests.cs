using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.Requests.Access;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BobCrm.Api.Tests;

public class FunctionServiceDeepTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static FunctionService CreateService(AppDbContext context)
    {
        var multilingual = new MultilingualFieldService(context, NullLogger<MultilingualFieldService>.Instance);
        return new FunctionService(context, multilingual);
    }

    [Fact]
    public async Task GetMyFunctionsAsync_ShouldIncludeAncestorsAndRoot()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var root = new FunctionNode { Code = "APP.ROOT", Name = "Root", SortOrder = 0 };
        var parent = new FunctionNode { Code = "CRM.CORE", Name = "Core", ParentId = root.Id, SortOrder = 10 };
        var leaf = new FunctionNode
        {
            Code = "CRM.CORE.PRODUCT",
            Name = "Product",
            ParentId = parent.Id,
            SortOrder = 20,
            DisplayNameKey = "MENU_CRM_CORE_PRODUCT"
        };

        db.FunctionNodes.AddRange(root, parent, leaf);
        db.LocalizationResources.Add(new LocalizationResource
        {
            Key = "MENU_CRM_CORE_PRODUCT",
            Translations = new Dictionary<string, string> { ["zh"] = "产品", ["en"] = "Products", ["ja"] = "製品" }
        });

        var role = new RoleProfile { Code = "R.TEST", Name = "Test Role", IsEnabled = true };
        db.RoleProfiles.Add(role);
        db.RoleAssignments.Add(new RoleAssignment { RoleId = role.Id, UserId = "u1" });
        db.RoleFunctionPermissions.Add(new RoleFunctionPermission { RoleId = role.Id, FunctionId = leaf.Id });
        await db.SaveChangesAsync();

        var tree = await service.GetMyFunctionsAsync("u1", "en");

        tree.Should().ContainSingle();
        tree[0].Code.Should().Be("APP.ROOT");
        tree[0].Children.Should().ContainSingle(c => c.Code == "CRM.CORE");
        tree[0].Children[0].Children.Should().ContainSingle(c => c.Code == "CRM.CORE.PRODUCT");
        tree[0].Children[0].Children[0].DisplayName.Should().Be("Products");
        tree[0].Children[0].Children[0].DisplayNameTranslations.Should().BeNull();
    }

    [Fact]
    public async Task ReorderFunctionsAsync_WhenMoveUnderDescendant_ShouldThrow()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var a = new FunctionNode { Code = "A", Name = "A", SortOrder = 0 };
        var b = new FunctionNode { Code = "B", Name = "B", ParentId = a.Id, SortOrder = 1 };
        var c = new FunctionNode { Code = "C", Name = "C", ParentId = b.Id, SortOrder = 2 };
        db.FunctionNodes.AddRange(a, b, c);
        await db.SaveChangesAsync();

        var act = async () => await service.ReorderFunctionsAsync(
            [new FunctionOrderUpdate(a.Id, c.Id, 100)]);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*descendant*");
    }

    [Fact]
    public async Task ReorderFunctionsAsync_WhenParentDoesNotExist_ShouldThrow()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var node = new FunctionNode { Code = "A", Name = "A", SortOrder = 0 };
        db.FunctionNodes.Add(node);
        await db.SaveChangesAsync();

        var act = async () => await service.ReorderFunctionsAsync(
            [new FunctionOrderUpdate(node.Id, Guid.NewGuid(), 1)]);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*non-existent parent*");
    }

    [Fact]
    public async Task ImportFunctionsAsync_WhenConflictsAndNotReplace_ShouldThrowConflictException()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        db.FunctionNodes.Add(new FunctionNode { Code = "EXIST", Name = "Existing" });
        await db.SaveChangesAsync();

        var request = new MenuImportRequest
        {
            Functions =
            [
                new MenuImportNode { Code = "EXIST", Name = "New" }
            ],
            MergeStrategy = "skip"
        };

        var act = async () => await service.ImportFunctionsAsync(request);
        var ex = await act.Should().ThrowAsync<FunctionService.ConflictException>();
        ex.Which.Conflicts.Should().ContainSingle("EXIST");
    }

    [Fact]
    public async Task ImportFunctionsAsync_WithReplace_ShouldUpdateExistingAndImportChildren()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var existing = new FunctionNode { Code = "EXIST", Name = "Old", IsMenu = false, SortOrder = 1 };
        db.FunctionNodes.Add(existing);
        await db.SaveChangesAsync();

        var request = new MenuImportRequest
        {
            MergeStrategy = "replace",
            Functions =
            [
                new MenuImportNode
                {
                    Code = "EXIST",
                    Name = "New",
                    IsMenu = true,
                    SortOrder = 99,
                    Children =
                    [
                        new MenuImportNode { Code = "EXIST.CHILD", Name = "Child", SortOrder = 1 }
                    ]
                }
            ]
        };

        var (imported, skipped) = await service.ImportFunctionsAsync(request);
        imported.Should().Be(2);
        skipped.Should().Be(0);

        var updated = await db.FunctionNodes.SingleAsync(f => f.Code == "EXIST");
        updated.Name.Should().Be("New");
        updated.IsMenu.Should().BeTrue();
        updated.SortOrder.Should().Be(99);

        var child = await db.FunctionNodes.SingleAsync(f => f.Code == "EXIST.CHILD");
        child.ParentId.Should().Be(updated.Id);
    }

    [Fact]
    public async Task EnsureEntityMenuAsync_ShouldCreateNodesAndGrantAdminRoleTemplateBindings()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var template = new FormTemplate { Id = 1, Name = "List", EntityType = "product", UsageType = FormTemplateUsageType.List, IsSystemDefault = true };
        var stateBinding = new TemplateStateBinding { Id = 10, EntityType = "product", ViewState = "List", TemplateId = 1, Template = template, IsDefault = true };
        var legacyBinding = new TemplateBinding { Id = 100, EntityType = "product", UsageType = FormTemplateUsageType.List, TemplateId = 1, Template = template, IsSystem = true };

        db.FormTemplates.Add(template);
        db.TemplateStateBindings.Add(stateBinding);
        db.TemplateBindings.Add(legacyBinding);

        var adminRole = new RoleProfile { Code = "R.ADMIN", Name = "Admin", IsSystem = true };
        db.RoleProfiles.Add(adminRole);
        await db.SaveChangesAsync();

        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityRoute = "product",
            EntityName = "Product",
            ApiEndpoint = "/api/products",
            Order = 1,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "产品" }
        };

        var bindings = new Dictionary<string, TemplateStateBinding>
        {
            ["List"] = stateBinding
        };

        var nodes = await service.EnsureEntityMenuAsync(entity, bindings);

        nodes.Should().ContainKey("List");
        nodes["List"].Code.Should().Be("CRM.CORE.PRODUCT");
        nodes["List"].Route.Should().Be("/products");
        nodes["List"].TemplateStateBindingId.Should().Be(stateBinding.Id);

        var permission = await db.RoleFunctionPermissions.SingleAsync(p => p.RoleId == adminRole.Id && p.FunctionId == nodes["List"].Id);
        permission.TemplateBindingId.Should().Be(legacyBinding.Id);
    }
}
