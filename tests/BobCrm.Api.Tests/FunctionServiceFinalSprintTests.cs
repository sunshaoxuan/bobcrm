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

public class FunctionServiceFinalSprintTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static FunctionService CreateService(AppDbContext db)
    {
        var multilingual = new MultilingualFieldService(db, NullLogger<MultilingualFieldService>.Instance);
        return new FunctionService(db, multilingual);
    }

    [Fact]
    public async Task CreateFunctionAsync_WithStateBindingId_ShouldResolveDirectly()
    {
        await using var db = CreateContext();
        db.TemplateStateBindings.Add(new TemplateStateBinding
        {
            Id = 11,
            EntityType = "customer",
            ViewState = "List",
            TemplateId = 1,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateFunctionAsync(new CreateFunctionRequest
        {
            Code = "FN.TEST",
            Name = "Test",
            TemplateStateBindingId = 11,
            IsMenu = true,
            SortOrder = 1
        });

        created.TemplateStateBindingId.Should().Be(11);
    }

    [Fact]
    public async Task CreateFunctionAsync_WithTemplateId_ShouldResolveToDefaultStateBinding()
    {
        await using var db = CreateContext();
        db.FormTemplates.Add(new FormTemplate { Id = 21, Name = "T", EntityType = "customer", UserId = "system", LayoutJson = "{\"items\":{\"a\":1}}" });
        db.TemplateStateBindings.Add(new TemplateStateBinding
        {
            Id = 22,
            EntityType = "customer",
            ViewState = "DetailView",
            TemplateId = 21,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateFunctionAsync(new CreateFunctionRequest
        {
            Code = "FN.TEMPLATE",
            Name = "Template",
            TemplateId = 21,
            IsMenu = false,
            SortOrder = 2
        });

        created.TemplateStateBindingId.Should().Be(22);
    }

    [Fact]
    public async Task CreateFunctionAsync_WithLegacyBindingId_ShouldResolveViaUsageMapping()
    {
        await using var db = CreateContext();
        db.FormTemplates.Add(new FormTemplate { Id = 31, Name = "List", EntityType = "customer", UserId = "system", UsageType = FormTemplateUsageType.List, LayoutJson = "{\"items\":{\"a\":1}}" });
        db.TemplateStateBindings.Add(new TemplateStateBinding
        {
            Id = 32,
            EntityType = "customer",
            ViewState = "List",
            TemplateId = 31,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        });
        db.TemplateBindings.Add(new TemplateBinding
        {
            Id = 33,
            EntityType = "customer",
            UsageType = FormTemplateUsageType.List,
            TemplateId = 31,
            IsSystem = true
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var created = await service.CreateFunctionAsync(new CreateFunctionRequest
        {
            Code = "FN.LEGACY",
            Name = "Legacy",
            TemplateId = 33,
            IsMenu = true,
            SortOrder = 3
        });

        created.TemplateStateBindingId.Should().Be(32);
    }

    [Fact]
    public async Task UpdateFunctionAsync_WhenClearTemplate_ShouldNullifyBinding()
    {
        await using var db = CreateContext();
        var node = new FunctionNode
        {
            Code = "FN.UPDATE",
            Name = "Update",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "更新" },
            IsMenu = true,
            SortOrder = 1,
            TemplateStateBindingId = 1
        };
        db.FunctionNodes.Add(node);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var updated = await service.UpdateFunctionAsync(node.Id, new UpdateFunctionRequest { ClearTemplate = true });

        updated.TemplateStateBindingId.Should().BeNull();
        updated.TemplateStateBinding.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFunctionAsync_WhenHasChildren_ShouldThrow()
    {
        await using var db = CreateContext();
        var parent = new FunctionNode { Code = "FN.PARENT", Name = "Parent", DisplayName = new Dictionary<string, string?> { ["zh"] = "父" } };
        var child = new FunctionNode { Code = "FN.CHILD", Name = "Child", ParentId = parent.Id, DisplayName = new Dictionary<string, string?> { ["zh"] = "子" } };
        db.FunctionNodes.AddRange(parent, child);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteFunctionAsync(parent.Id));
    }

    [Fact]
    public async Task DeleteFunctionAsync_WhenReferenced_ShouldThrow()
    {
        await using var db = CreateContext();
        var node = new FunctionNode { Code = "FN.REF", Name = "Ref", DisplayName = new Dictionary<string, string?> { ["zh"] = "引用" } };
        db.FunctionNodes.Add(node);
        var role = new RoleProfile { Id = Guid.NewGuid(), Code = "R1", Name = "R1", IsEnabled = true };
        db.RoleProfiles.Add(role);
        db.RoleFunctionPermissions.Add(new RoleFunctionPermission { RoleId = role.Id, FunctionId = node.Id });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteFunctionAsync(node.Id));
    }
}
