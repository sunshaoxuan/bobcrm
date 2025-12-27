using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Infrastructure.Ef;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Tests;

public class FieldPermissionServiceTests
{
    [Fact]
    public async Task GetUserFieldPermissionAsync_AggregatesAcrossRoles()
    {
        await using var db = CreateContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(db, cache);

        var userId = "u1";
        var roleA = Guid.NewGuid();
        var roleB = Guid.NewGuid();

        db.RoleAssignments.AddRange(
            new RoleAssignment { UserId = userId, RoleId = roleA, OrganizationId = null },
            new RoleAssignment { UserId = userId, RoleId = roleB, OrganizationId = null });

        db.FieldPermissions.AddRange(
            new FieldPermission { RoleId = roleA, EntityType = "customer", FieldName = "Name", CanRead = true, CanWrite = false },
            new FieldPermission { RoleId = roleB, EntityType = "customer", FieldName = "Name", CanRead = false, CanWrite = true });

        await db.SaveChangesAsync();

        var permission = await service.GetUserFieldPermissionAsync(userId, "customer", "Name");
        permission.Should().NotBeNull();
        permission!.CanRead.Should().BeTrue();
        permission.CanWrite.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserFieldPermissionAsync_WhenNoExplicitRule_ReturnsReadOnlyDefault()
    {
        await using var db = CreateContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(db, cache);

        db.RoleAssignments.Add(new RoleAssignment { UserId = "u1", RoleId = Guid.NewGuid(), OrganizationId = null });
        await db.SaveChangesAsync();

        var permission = await service.GetUserFieldPermissionAsync("u1", "customer", "UnknownField");
        permission.Should().NotBeNull();
        permission!.CanRead.Should().BeTrue();
        permission.CanWrite.Should().BeFalse();
    }

    [Fact]
    public async Task UpsertPermissionAsync_UpdatesCachedPermission()
    {
        await using var db = CreateContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(db, cache);

        var userId = "u1";
        var roleId = Guid.NewGuid();

        db.RoleAssignments.Add(new RoleAssignment { UserId = userId, RoleId = roleId, OrganizationId = null });
        db.FieldPermissions.Add(new FieldPermission
        {
            RoleId = roleId,
            EntityType = "customer",
            FieldName = "Name",
            CanRead = true,
            CanWrite = false
        });
        await db.SaveChangesAsync();

        (await service.CanUserWriteFieldAsync(userId, "customer", "Name")).Should().BeFalse();

        await service.UpsertPermissionAsync(
            roleId,
            "customer",
            "Name",
            canRead: true,
            canWrite: true,
            remarks: "test",
            userId: "admin");

        (await service.CanUserWriteFieldAsync(userId, "customer", "Name")).Should().BeTrue();
    }

    [Fact]
    public async Task BulkUpsertPermissionsAsync_CreatesAndUpdatesRecords()
    {
        await using var db = CreateContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(db, cache);

        var userId = "u1";
        var roleId = Guid.NewGuid();

        db.RoleAssignments.Add(new RoleAssignment { UserId = userId, RoleId = roleId, OrganizationId = null });
        db.FieldPermissions.Add(new FieldPermission
        {
            RoleId = roleId,
            EntityType = "customer",
            FieldName = "Name",
            CanRead = true,
            CanWrite = false
        });
        await db.SaveChangesAsync();

        await service.BulkUpsertPermissionsAsync(roleId, "customer", new List<FieldPermissionDto>
        {
            new("Name", CanRead: true, CanWrite: true, Remarks: null),
            new("Email", CanRead: true, CanWrite: false, Remarks: "read-only")
        }, userId: "admin");

        var updated = await db.FieldPermissions.AsNoTracking().ToListAsync();
        updated.Should().HaveCount(2);
        updated.Should().ContainSingle(p => p.FieldName == "Name" && p.CanWrite);
        updated.Should().ContainSingle(p => p.FieldName == "Email" && p.CanRead && !p.CanWrite);

        (await service.CanUserWriteFieldAsync(userId, "customer", "Name")).Should().BeTrue();
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static FieldPermissionService CreateService(AppDbContext db, IMemoryCache cache)
    {
        var repo = new EfRepository<FieldPermission>(db);
        var uow = new EfUnitOfWork(db);
        var logger = NullLogger<FieldPermissionService>.Instance;
        return new FieldPermissionService(repo, db, uow, cache, logger);
    }
}
