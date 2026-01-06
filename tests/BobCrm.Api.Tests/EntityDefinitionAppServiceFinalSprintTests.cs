using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Requests.Entity;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BobCrm.Api.Tests;

public class EntityDefinitionAppServiceFinalSprintTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static EntityDefinitionAppService CreateService(AppDbContext context)
    {
        var mockLoc = new Mock<ILocalization>();
        mockLoc.Setup(l => l.T(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((key, lang) => key);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(new DefaultHttpContext());

        return new EntityDefinitionAppService(
            context,
            mockLoc.Object,
            NullLogger<EntityDefinitionAppService>.Instance,
            mockHttpContextAccessor.Object);
    }

    private static MultilingualText Mlt(string zh)
    {
        var text = new MultilingualText();
        text["zh"] = zh;
        return text;
    }

    [Fact]
    public async Task CreateEntityDefinitionAsync_WithInterfaces_ShouldAddInterfaceFields()
    {
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        var dto = new CreateEntityDefinitionDto
        {
            Namespace = "BobCrm.Base.Custom",
            EntityName = "HasAudit",
            DisplayName = Mlt("含审计"),
            Interfaces = new List<string> { InterfaceType.Audit }
        };

        var created = await service.CreateEntityDefinitionAsync("user1", "zh", dto);

        var stored = await ctx.EntityDefinitions
            .Include(e => e.Fields)
            .Include(e => e.Interfaces)
            .FirstAsync(e => e.Id == created.Id);

        stored.Interfaces.Select(i => i.InterfaceType).Should().Contain(InterfaceType.Audit);
        stored.Fields.Should().Contain(f => f.PropertyName == "CreatedAt" && f.Source == FieldSource.Interface);
        stored.Fields.Should().Contain(f => f.PropertyName == "UpdatedAt" && f.Source == FieldSource.Interface);
    }

    [Fact]
    public async Task UpdateEntityDefinitionAsync_WhenEnumChangedToNonEnum_ShouldClearEnumConfig()
    {
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        var entity = new EntityDefinition
        {
            Namespace = "BobCrm.Base.Custom",
            EntityName = "EnumEntity",
            FullTypeName = "BobCrm.Base.Custom.EnumEntity",
            Status = EntityStatus.Draft,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "枚举实体" }
        };

        var enumField = new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Status",
            DataType = FieldDataType.Enum,
            EnumDefinitionId = Guid.NewGuid(),
            IsMultiSelect = true,
            SortOrder = 1,
            Source = FieldSource.Custom
        };
        entity.Fields.Add(enumField);

        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        await service.UpdateEntityDefinitionAsync(entity.Id, "user1", "zh", new UpdateEntityDefinitionDto
        {
            Fields = new List<UpdateFieldMetadataDto>
            {
                new()
                {
                    Id = enumField.Id,
                    PropertyName = enumField.PropertyName,
                    DataType = FieldDataType.String
                }
            }
        });

        var stored = await ctx.FieldMetadatas.FirstAsync(f => f.Id == enumField.Id);
        stored.DataType.Should().Be(FieldDataType.String);
        stored.EnumDefinitionId.Should().BeNull();
        stored.IsMultiSelect.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateEntityDefinitionAsync_WhenEnumConfigProvided_ShouldApply()
    {
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        var entity = new EntityDefinition
        {
            Namespace = "BobCrm.Base.Custom",
            EntityName = "EnumEntity2",
            FullTypeName = "BobCrm.Base.Custom.EnumEntity2",
            Status = EntityStatus.Draft,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "枚举实体2" }
        };

        var enumField = new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Status",
            DataType = FieldDataType.Enum,
            EnumDefinitionId = null,
            IsMultiSelect = false,
            SortOrder = 1,
            Source = FieldSource.Custom
        };
        entity.Fields.Add(enumField);

        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var enumId = Guid.NewGuid();
        await service.UpdateEntityDefinitionAsync(entity.Id, "user1", "zh", new UpdateEntityDefinitionDto
        {
            Fields = new List<UpdateFieldMetadataDto>
            {
                new()
                {
                    Id = enumField.Id,
                    PropertyName = enumField.PropertyName,
                    DataType = FieldDataType.Enum,
                    EnumDefinitionId = enumId,
                    IsMultiSelect = true
                }
            }
        });

        var stored = await ctx.FieldMetadatas.FirstAsync(f => f.Id == enumField.Id);
        stored.EnumDefinitionId.Should().Be(enumId);
        stored.IsMultiSelect.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateEntityDefinitionAsync_WhenInterfacesRemoved_ShouldSyncInterfaces()
    {
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        var entity = new EntityDefinition
        {
            Namespace = "BobCrm.Base.Custom",
            EntityName = "IfaceEntity",
            FullTypeName = "BobCrm.Base.Custom.IfaceEntity",
            Status = EntityStatus.Draft,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "接口实体" }
        };
        entity.Interfaces.Add(new EntityInterface { InterfaceType = InterfaceType.Audit, IsEnabled = true });

        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        await service.UpdateEntityDefinitionAsync(entity.Id, "user1", "zh", new UpdateEntityDefinitionDto
        {
            Interfaces = new List<string>()
        });

        var stored = await ctx.EntityDefinitions.Include(e => e.Interfaces).FirstAsync(e => e.Id == entity.Id);
        stored.Interfaces.Should().BeEmpty();
    }
}

