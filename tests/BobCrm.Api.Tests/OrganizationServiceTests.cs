using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Requests.Organization;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Tests;

public class OrganizationServiceTests
{
    [Fact]
    public async Task CreateRoot_ShouldSucceedOnce_AndAssignPathCode()
    {
        await using var ctx = CreateDbContext();
        var service = new OrganizationService(ctx);

        var root = await service.CreateAsync(new CreateOrganizationRequest
        {
            Code = "BASE",
            Name = "总部"
        });

        root.PathCode.Should().Be("01");
        root.Level.Should().Be(0);

        var act = async () => await service.CreateAsync(new CreateOrganizationRequest
        {
            Code = "SECOND",
            Name = "第二根节点"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Root organization already exists.");
    }

    [Fact]
    public async Task CreateChild_ShouldGenerateIncrementalPathCodes()
    {
        await using var ctx = CreateDbContext();
        var service = new OrganizationService(ctx);
        var root = await service.CreateAsync(new CreateOrganizationRequest { Code = "ROOT", Name = "Root" });

        var child1 = await service.CreateAsync(new CreateOrganizationRequest
        {
            ParentId = root.Id,
            Code = "HR",
            Name = "人力"
        });

        var child2 = await service.CreateAsync(new CreateOrganizationRequest
        {
            ParentId = root.Id,
            Code = "FIN",
            Name = "财务"
        });

        child1.PathCode.Should().Be("01.01");
        child2.PathCode.Should().Be("01.02");
    }

    [Fact]
    public async Task CreateOrUpdate_ShouldEnforceSiblingCodeUniqueness()
    {
        await using var ctx = CreateDbContext();
        var service = new OrganizationService(ctx);
        var root = await service.CreateAsync(new CreateOrganizationRequest { Code = "ROOT", Name = "Root" });
        var branch = await service.CreateAsync(new CreateOrganizationRequest { ParentId = root.Id, Code = "BR", Name = "分支" });

        var duplicateAct = async () => await service.CreateAsync(new CreateOrganizationRequest
        {
            ParentId = root.Id,
            Code = "BR",
            Name = "重复"
        });

        await duplicateAct.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Code already exists for the selected parent.");

        var updateAct = async () => await service.UpdateAsync(branch.Id, new UpdateOrganizationRequest
        {
            Code = "BR",
            Name = "更新"
        });

        await updateAct.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Delete_WithChildren_ShouldThrow()
    {
        await using var ctx = CreateDbContext();
        var service = new OrganizationService(ctx);
        var root = await service.CreateAsync(new CreateOrganizationRequest { Code = "ROOT", Name = "Root" });
        var child = await service.CreateAsync(new CreateOrganizationRequest { ParentId = root.Id, Code = "HR", Name = "人力" });

        var deleteRoot = async () => await service.DeleteAsync(root.Id);
        await deleteRoot.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete organization that has children.");

        await service.DeleteAsync(child.Id);
        (await ctx.OrganizationNodes.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DeleteRootWithoutChildren_ShouldStillThrow()
    {
        await using var ctx = CreateDbContext();
        var service = new OrganizationService(ctx);
        var root = await service.CreateAsync(new CreateOrganizationRequest { Code = "ROOT", Name = "Root" });

        var deleteRoot = async () => await service.DeleteAsync(root.Id);
        await deleteRoot.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Root organization cannot be deleted.");
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
