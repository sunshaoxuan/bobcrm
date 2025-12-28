using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using BobCrm.Api.Contracts.Requests.Template;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Infrastructure.Ef;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BobCrm.Api.Tests;

/// <summary>
/// TemplateService 测试
/// 覆盖模板管理核心逻辑
/// </summary>
public class TemplateServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static TemplateService CreateService(AppDbContext context)
    {
        var repo = new EfRepository<FormTemplate>(context);
        var uow = new EfUnitOfWork(context);
        var mockI18n = new Mock<II18nService>();
        mockI18n.Setup(i => i.T(It.IsAny<string>()))
            .Returns<string>(key => key);

        return new TemplateService(
            repo,
            uow,
            mockI18n.Object,
            NullLogger<TemplateService>.Instance);
    }

    #region GetTemplatesAsync Tests

    [Fact]
    public async Task GetTemplatesAsync_ShouldReturnUserAndSystemTemplates()
    {
        // Arrange
        await using var ctx = CreateContext();
        ctx.FormTemplates.AddRange(
            new FormTemplate { Name = "User Template", EntityType = "Customer", UserId = "user1", LayoutJson = "{}", UsageType = FormTemplateUsageType.Detail },
            new FormTemplate { Name = "System Template", EntityType = "Customer", IsSystemDefault = true, LayoutJson = "{}", UsageType = FormTemplateUsageType.Detail }
        );
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.GetTemplatesAsync("user1");

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTemplatesAsync_WithEntityTypeFilter_ShouldFilterByEntityType()
    {
        // Arrange
        await using var ctx = CreateContext();
        ctx.FormTemplates.AddRange(
            new FormTemplate { Name = "Customer Template", EntityType = "Customer", UserId = "user1", LayoutJson = "{}", UsageType = FormTemplateUsageType.Detail },
            new FormTemplate { Name = "Order Template", EntityType = "Order", UserId = "user1", LayoutJson = "{}", UsageType = FormTemplateUsageType.Detail }
        );
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.GetTemplatesAsync("user1", entityType: "Customer");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items!.First().EntityType.Should().Be("Customer");
    }

    [Fact]
    public async Task GetTemplatesAsync_GroupByEntity_ShouldGroupCorrectly()
    {
        // Arrange
        await using var ctx = CreateContext();
        ctx.FormTemplates.AddRange(
            new FormTemplate { Name = "Customer 1", EntityType = "Customer", UserId = "user1", LayoutJson = "{}", UsageType = FormTemplateUsageType.Detail },
            new FormTemplate { Name = "Customer 2", EntityType = "Customer", UserId = "user1", LayoutJson = "{}", UsageType = FormTemplateUsageType.Detail },
            new FormTemplate { Name = "Order 1", EntityType = "Order", UserId = "user1", LayoutJson = "{}", UsageType = FormTemplateUsageType.Detail }
        );
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.GetTemplatesAsync("user1", groupBy: "entity");

        // Assert
        result.GroupBy.Should().Be("entity");
        result.GroupsByEntity.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTemplatesAsync_ByTemplateType_ShouldFilterSystemTemplates()
    {
        // Arrange
        await using var ctx = CreateContext();
        ctx.FormTemplates.AddRange(
            new FormTemplate { Name = "User Template", EntityType = "Customer", UserId = "user1", LayoutJson = "{}", UsageType = FormTemplateUsageType.Detail },
            new FormTemplate { Name = "System Template", EntityType = "Customer", IsSystemDefault = true, LayoutJson = "{}", UsageType = FormTemplateUsageType.Detail }
        );
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.GetTemplatesAsync("user1", templateType: "system");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items!.First().IsSystemDefault.Should().BeTrue();
    }

    #endregion

    #region GetTemplateByIdAsync Tests

    [Fact]
    public async Task GetTemplateByIdAsync_WithValidId_ShouldReturnTemplate()
    {
        // Arrange
        await using var ctx = CreateContext();
        var template = new FormTemplate
        {
            Name = "Test Template",
            EntityType = "Customer",
            UserId = "user1",
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.Detail
        };
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.GetTemplateByIdAsync(template.Id, "user1");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Template");
    }

    [Fact]
    public async Task GetTemplateByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        // Act
        var result = await service.GetTemplateByIdAsync(9999, "user1");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTemplateByIdAsync_OtherUserTemplate_ShouldReturnNull()
    {
        // Arrange
        await using var ctx = CreateContext();
        var template = new FormTemplate
        {
            Name = "Test Template",
            EntityType = "Customer",
            UserId = "other-user",
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.Detail
        };
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.GetTemplateByIdAsync(template.Id, "user1");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTemplateByIdAsync_SystemTemplate_ShouldBeAccessibleByAnyUser()
    {
        // Arrange
        await using var ctx = CreateContext();
        var template = new FormTemplate
        {
            Name = "System Template",
            EntityType = "Customer",
            IsSystemDefault = true,
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.Detail
        };
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.GetTemplateByIdAsync(template.Id, "any-user");

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region CreateTemplateAsync Tests

    [Fact]
    public async Task CreateTemplateAsync_WithValidRequest_ShouldCreateTemplate()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var request = new CreateTemplateRequest(
            Name: "New Template",
            EntityType: "Customer",
            IsUserDefault: false,
            LayoutJson: "{}",
            Description: null
        );

        // Act
        var result = await service.CreateTemplateAsync("user1", request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Template");
        result.UserId.Should().Be("user1");
    }

    [Fact]
    public async Task CreateTemplateAsync_SetAsUserDefault_ShouldClearExistingDefaults()
    {
        // Arrange
        await using var ctx = CreateContext();
        var existingDefault = new FormTemplate
        {
            Name = "Existing Default",
            EntityType = "Customer",
            UserId = "user1",
            IsUserDefault = true,
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.Detail
        };
        ctx.FormTemplates.Add(existingDefault);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new CreateTemplateRequest(
            Name: "New Default",
            EntityType: "Customer",
            IsUserDefault: true,
            LayoutJson: "{}",
            Description: null
        );

        // Act
        var result = await service.CreateTemplateAsync("user1", request);

        // Assert
        result.IsUserDefault.Should().BeTrue();
        var oldDefault = await ctx.FormTemplates.FindAsync(existingDefault.Id);
        oldDefault!.IsUserDefault.Should().BeFalse();
    }

    #endregion

    #region UpdateTemplateAsync Tests

    [Fact]
    public async Task UpdateTemplateAsync_WithValidRequest_ShouldUpdateTemplate()
    {
        // Arrange
        await using var ctx = CreateContext();
        var template = new FormTemplate
        {
            Name = "Original Name",
            EntityType = "Customer",
            UserId = "user1",
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.Detail
        };
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new UpdateTemplateRequest(
            Name: "Updated Name",
            EntityType: null,
            IsUserDefault: null,
            LayoutJson: null,
            Description: null
        );

        // Act
        var result = await service.UpdateTemplateAsync(template.Id, "user1", request);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateTemplateAsync_OtherUserTemplate_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var template = new FormTemplate
        {
            Name = "Other User Template",
            EntityType = "Customer",
            UserId = "other-user",
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.Detail
        };
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new UpdateTemplateRequest(
            Name: "Hacked",
            EntityType: null,
            IsUserDefault: null,
            LayoutJson: null,
            Description: null
        );

        // Act & Assert - service throws when template not found for user
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateTemplateAsync(template.Id, "user1", request));
    }

    #endregion

    #region DeleteTemplateAsync Tests

    [Fact]
    public async Task DeleteTemplateAsync_WithValidId_ShouldDeleteTemplate()
    {
        // Arrange
        await using var ctx = CreateContext();
        var template = new FormTemplate
        {
            Name = "Template to Delete",
            EntityType = "Customer",
            UserId = "user1",
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.Detail
        };
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        await service.DeleteTemplateAsync(template.Id, "user1");

        // Assert
        var deleted = await ctx.FormTemplates.FindAsync(template.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTemplateAsync_OtherUserTemplate_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var template = new FormTemplate
        {
            Name = "Other User Template",
            EntityType = "Customer",
            UserId = "other-user",
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.Detail
        };
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act & Assert - service throws when template not found for user
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.DeleteTemplateAsync(template.Id, "user1"));
    }

    [Fact]
    public async Task DeleteTemplateAsync_InUseTemplate_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var template = new FormTemplate
        {
            Name = "In Use Template",
            EntityType = "Customer",
            UserId = "user1",
            IsInUse = true,
            LayoutJson = "{}",
            UsageType = FormTemplateUsageType.Detail
        };
        ctx.FormTemplates.Add(template);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteTemplateAsync(template.Id, "user1"));
    }

    #endregion
}
