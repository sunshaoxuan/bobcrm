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

namespace BobCrm.Api.Tests;

public class EntityDefinitionAppServicePhase7Tests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static EntityDefinitionAppService CreateService(AppDbContext context, Mock<IFieldMetadataCache>? cache = null)
    {
        var loc = new Mock<ILocalization>();
        loc.Setup(l => l.T(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((key, _) => key);

        var http = new Mock<IHttpContextAccessor>();
        http.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());

        return new EntityDefinitionAppService(
            context,
            loc.Object,
            (cache ?? new Mock<IFieldMetadataCache>(MockBehavior.Loose)).Object,
            NullLogger<EntityDefinitionAppService>.Instance,
            http.Object);
    }

    private static MultilingualText Ml(string zh, string? en = null, string? ja = null)
        => new()
        {
            ["zh"] = zh,
            ["en"] = en ?? zh,
            ["ja"] = ja ?? zh
        };

    private static EntityDefinition SeedDefinition(Guid id, string status, bool isLocked = false)
        => new()
        {
            Id = id,
            Namespace = "BobCrm.Test",
            EntityName = "Order",
            FullTypeName = "BobCrm.Test.Order",
            EntityRoute = "order",
            ApiEndpoint = "/api/orders",
            StructureType = EntityStructureType.Single,
            Status = status,
            Source = EntitySource.Custom,
            IsEnabled = true,
            IsLocked = isLocked,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "订单", ["en"] = "Order", ["ja"] = "注文" }
        };

    [Fact]
    public async Task UpdateEntityDefinitionAsync_WhenPublished_ShouldTransitionToModified()
    {
        await using var db = CreateContext();
        var svc = CreateService(db);

        var entity = SeedDefinition(Guid.NewGuid(), EntityStatus.Published);
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var updated = await svc.UpdateEntityDefinitionAsync(entity.Id, "u1", "zh", new UpdateEntityDefinitionDto
        {
            DisplayName = Ml("订单-更新")
        });

        updated.Status.Should().Be(EntityStatus.Modified);
        (await db.EntityDefinitions.AsNoTracking().SingleAsync(e => e.Id == entity.Id)).Status.Should().Be(EntityStatus.Modified);
    }

    [Fact]
    public async Task UpdateEntityDefinitionAsync_ShouldInvalidateFieldMetadataCache()
    {
        await using var db = CreateContext();

        var entity = SeedDefinition(Guid.NewGuid(), EntityStatus.Draft);
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var cache = new Mock<IFieldMetadataCache>(MockBehavior.Strict);
        cache.Setup(x => x.Invalidate("BobCrm.Test.Order"));

        var svc = CreateService(db, cache);
        await svc.UpdateEntityDefinitionAsync(entity.Id, "u1", "zh", new UpdateEntityDefinitionDto
        {
            DisplayName = Ml("订单", "Order", "注文")
        });

        cache.Verify(x => x.Invalidate("BobCrm.Test.Order"), Times.Once);
    }

    [Fact]
    public async Task UpdateEntityDefinitionAsync_RemovingProtectedFields_ShouldThrowFieldProtectedBySource()
    {
        await using var db = CreateContext();
        var svc = CreateService(db);

        var entity = SeedDefinition(Guid.NewGuid(), EntityStatus.Draft);
        var systemField = new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Id",
            DataType = FieldDataType.Guid,
            SortOrder = 1,
            Source = FieldSource.System
        };
        var customField = new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Name",
            DataType = FieldDataType.String,
            SortOrder = 2,
            Source = FieldSource.Custom
        };
        entity.Fields.Add(systemField);
        entity.Fields.Add(customField);
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var act = async () => await svc.UpdateEntityDefinitionAsync(entity.Id, "u1", "zh", new UpdateEntityDefinitionDto
        {
            Fields = [new UpdateFieldMetadataDto { Id = customField.Id }]
        });

        var ex = await act.Should().ThrowAsync<ServiceException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.FieldProtectedBySource);
    }

    [Fact]
    public async Task UpdateEntityDefinitionAsync_WhenLockedAndDeletingField_ShouldThrowEntityLocked()
    {
        await using var db = CreateContext();
        var svc = CreateService(db);

        var entity = SeedDefinition(Guid.NewGuid(), EntityStatus.Draft, isLocked: true);
        var field = new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Code",
            DataType = FieldDataType.String,
            SortOrder = 1,
            Source = FieldSource.Custom
        };
        entity.Fields.Add(field);
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var act = async () => await svc.UpdateEntityDefinitionAsync(entity.Id, "u1", "zh", new UpdateEntityDefinitionDto
        {
            Fields = []
        });

        var ex = await act.Should().ThrowAsync<ServiceException>();
        ex.Which.ErrorCode.Should().Be("ENTITY_LOCKED");
    }

    [Fact]
    public async Task UpdateEntityDefinitionAsync_WhenLockedAndRemovingLockedInterface_ShouldThrowEntityLocked()
    {
        await using var db = CreateContext();
        var svc = CreateService(db);

        var entity = SeedDefinition(Guid.NewGuid(), EntityStatus.Draft, isLocked: true);
        entity.Interfaces.Add(new EntityInterface
        {
            EntityDefinitionId = entity.Id,
            InterfaceType = "Audit",
            IsEnabled = true,
            IsLocked = true
        });
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var act = async () => await svc.UpdateEntityDefinitionAsync(entity.Id, "u1", "zh", new UpdateEntityDefinitionDto
        {
            Interfaces = []
        });

        var ex = await act.Should().ThrowAsync<ServiceException>();
        ex.Which.ErrorCode.Should().Be("ENTITY_LOCKED");
    }

    [Fact]
    public async Task UpdateEntityDefinitionAsync_EnumToNonEnum_ShouldClearEnumConfig()
    {
        await using var db = CreateContext();
        var svc = CreateService(db);

        var entity = SeedDefinition(Guid.NewGuid(), EntityStatus.Draft);
        var field = new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Status",
            DataType = FieldDataType.Enum,
            SortOrder = 1,
            EnumDefinitionId = Guid.NewGuid(),
            IsMultiSelect = true,
            Source = FieldSource.Custom
        };
        entity.Fields.Add(field);
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        await svc.UpdateEntityDefinitionAsync(entity.Id, "u1", "zh", new UpdateEntityDefinitionDto
        {
            Fields =
            [
                new UpdateFieldMetadataDto
                {
                    Id = field.Id,
                    DataType = FieldDataType.String,
                    Length = 20
                }
            ]
        });

        var stored = await db.FieldMetadatas.AsNoTracking().SingleAsync(f => f.Id == field.Id);
        stored.DataType.Should().Be(FieldDataType.String);
        stored.EnumDefinitionId.Should().BeNull();
        stored.IsMultiSelect.Should().BeFalse();
    }
}
