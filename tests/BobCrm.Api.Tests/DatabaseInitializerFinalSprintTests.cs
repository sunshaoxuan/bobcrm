using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BobCrm.Api.Tests;

public class DatabaseInitializerFinalSprintTests
{
    [Fact]
    public async Task InitializeAsync_ShouldCleanupSampleEntities_WorkflowArtifacts_AndTestUsers()
    {
        using var factory = new TestWebAppFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sample = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Base.Test",
            EntityName = "TestSingleEntity",
            FullTypeName = "BobCrm.Base.Test.TestSingleEntity",
            EntityRoute = $"sample_{Guid.NewGuid():N}",
            ApiEndpoint = "/api/sample",
            Status = EntityStatus.Draft,
            Source = EntitySource.Custom,
            IsEnabled = true,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "样例" },
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var wfRoute = $"workflow_{Guid.NewGuid():N}";
        var workflow = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Dynamic",
            EntityName = "WorkflowCleanup",
            FullTypeName = "BobCrm.Dynamic.WorkflowCleanup",
            EntityRoute = wfRoute,
            ApiEndpoint = $"/api/{wfRoute}",
            Status = EntityStatus.Draft,
            Source = EntitySource.Custom,
            IsEnabled = true,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "工作流" },
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        db.EntityDefinitions.AddRange(sample, workflow);

        var template = new FormTemplate
        {
            Name = "WF Template",
            EntityType = wfRoute,
            UserId = "admin",
            IsSystemDefault = true,
            UsageType = FormTemplateUsageType.List,
            LayoutJson = "{\"items\":{\"a\":1}}"
        };
        db.FormTemplates.Add(template);
        await db.SaveChangesAsync();

        var stateBinding = new TemplateStateBinding
        {
            EntityType = wfRoute,
            ViewState = "List",
            TemplateId = template.Id,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        db.TemplateStateBindings.Add(stateBinding);

        var legacyBinding = new TemplateBinding
        {
            EntityType = wfRoute,
            UsageType = FormTemplateUsageType.List,
            TemplateId = template.Id,
            IsSystem = true,
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedBy = "admin"
        };
        db.TemplateBindings.Add(legacyBinding);

        var fn = new FunctionNode
        {
            Code = $"WF.{Guid.NewGuid():N}",
            Name = "Workflow Menu",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "WF" },
            IsMenu = true,
            SortOrder = 1,
            TemplateStateBinding = stateBinding
        };
        db.FunctionNodes.Add(fn);

        var role = new RoleProfile { Code = $"R_{Guid.NewGuid():N}", Name = "Role", IsEnabled = true };
        db.RoleProfiles.Add(role);
        db.RoleFunctionPermissions.Add(new RoleFunctionPermission { RoleId = role.Id, FunctionId = fn.Id });

        var testUser = new IdentityUser { UserName = $"user_{Guid.NewGuid():N}", Email = "u@local", EmailConfirmed = true };
        db.Users.Add(testUser);
        await db.SaveChangesAsync();

        db.UserLayouts.Add(new UserLayout { UserId = testUser.Id, EntityType = UserLayoutScope.ForCustomer(0), LayoutJson = "{\"x\":1}" });
        db.UserPreferences.Add(new UserPreferences { UserId = testUser.Id, Language = "zh", Theme = "light" });
        db.RefreshTokens.Add(new RefreshToken { UserId = testUser.Id, Token = "t", ExpiresAt = DateTime.UtcNow.AddDays(1) });
        await db.SaveChangesAsync();

        await DatabaseInitializer.InitializeAsync(db);

        (await db.EntityDefinitions.IgnoreQueryFilters().AnyAsync(e => e.Id == sample.Id)).Should().BeFalse();
        (await db.EntityDefinitions.IgnoreQueryFilters().AnyAsync(e => e.Id == workflow.Id)).Should().BeFalse();

        (await db.TemplateBindings.AnyAsync(b => b.EntityType == wfRoute)).Should().BeFalse();
        (await db.TemplateStateBindings.AnyAsync(b => b.EntityType == wfRoute)).Should().BeFalse();
        (await db.FormTemplates.AnyAsync(t => t.EntityType == wfRoute)).Should().BeFalse();
        (await db.FunctionNodes.AnyAsync(n => n.Id == fn.Id)).Should().BeFalse();
        (await db.RoleFunctionPermissions.AnyAsync(p => p.FunctionId == fn.Id)).Should().BeFalse();

        (await db.Users.AnyAsync(u => u.Id == testUser.Id)).Should().BeFalse();
        (await db.UserLayouts.AnyAsync(u => u.UserId == testUser.Id)).Should().BeFalse();
        (await db.UserPreferences.AnyAsync(u => u.UserId == testUser.Id)).Should().BeFalse();
        (await db.RefreshTokens.AnyAsync(u => u.UserId == testUser.Id)).Should().BeFalse();
    }
}
