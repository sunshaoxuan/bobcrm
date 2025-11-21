using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Infrastructure.Ef;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BobCrm.Tests.Services;

public class FieldPermissionServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly IRepository<FieldPermission> _repo;
    private readonly IUnitOfWork _uow;
    private readonly FieldPermissionService _service;

    public FieldPermissionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _repo = new EfRepository<FieldPermission>(_dbContext);
        _uow = new EfUnitOfWork(_dbContext);
        _service = new FieldPermissionService(_repo, _dbContext, _uow, _cache, NullLogger<FieldPermissionService>.Instance);
    }

    [Fact]
    public async Task GetUserFieldPermissionAsync_ShouldMergePermissionsFromRoles()
    {
        var userId = "user-1";
        var role1Id = Guid.NewGuid();
        var role2Id = Guid.NewGuid();

        _dbContext.RoleAssignments.AddRange(
            new RoleAssignment { UserId = userId, RoleId = role1Id },
            new RoleAssignment { UserId = userId, RoleId = role2Id });

        _dbContext.FieldPermissions.AddRange(
            new FieldPermission
            {
                RoleId = role1Id,
                EntityType = "customer",
                FieldName = "salary",
                CanRead = true,
                CanWrite = false
            },
            new FieldPermission
            {
                RoleId = role2Id,
                EntityType = "customer",
                FieldName = "salary",
                CanRead = false,
                CanWrite = true
            });

        await _dbContext.SaveChangesAsync();

        var result = await _service.GetUserFieldPermissionAsync(userId, "customer", "salary");

        result.Should().NotBeNull();
        result!.CanRead.Should().BeTrue();
        result.CanWrite.Should().BeTrue();
    }

    [Fact]
    public async Task UpsertPermissionAsync_ShouldRefreshCachedPermissions()
    {
        var userId = "user-2";
        var roleId = Guid.NewGuid();
        _dbContext.RoleAssignments.Add(new RoleAssignment { UserId = userId, RoleId = roleId });
        await _dbContext.SaveChangesAsync();

        // Prime the cache (returns default policy)
        var initial = await _service.GetUserFieldPermissionAsync(userId, "customer", "salary");
        initial.Should().NotBeNull();
        initial!.CanWrite.Should().BeFalse();

        await _service.UpsertPermissionAsync(roleId, "customer", "salary", canRead: true, canWrite: true, userId: userId);

        // Fetch again after cache invalidation
        var result = await _service.GetUserFieldPermissionAsync(userId, "customer", "salary");

        result.Should().NotBeNull();
        result!.CanWrite.Should().BeTrue();
    }
}
