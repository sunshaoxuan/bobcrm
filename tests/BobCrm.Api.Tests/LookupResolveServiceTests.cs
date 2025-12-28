using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Tests;

public class LookupResolveServiceTests
{
    [Fact]
    public async Task ResolveAsync_ShouldResolveSingleId_ByEntityRoute()
    {
        await using var db = CreateContext();

        db.EntityDefinitions.Add(new EntityDefinition
        {
            EntityRoute = "customer",
            EntityName = "Customer",
            FullTypeName = typeof(Customer).FullName!,
            Status = EntityStatus.Published,
            Source = EntitySource.System,
            StructureType = EntityStructureType.Single,
            IsEnabled = true
        });
        db.Customers.Add(new Customer { Id = 1, Code = "C1", Name = "Alice" });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ResolveAsync("customer", new[] { "1" }, displayField: null, CancellationToken.None);

        result.Should().ContainKey("1");
        result["1"].Should().Be("Alice");
    }

    [Fact]
    public async Task ResolveAsync_ShouldResolveBatchIds_AndIgnoreMissing()
    {
        await using var db = CreateContext();

        db.EntityDefinitions.Add(new EntityDefinition
        {
            EntityRoute = "customer",
            EntityName = "Customer",
            FullTypeName = typeof(Customer).FullName!,
            Status = EntityStatus.Published,
            Source = EntitySource.System,
            StructureType = EntityStructureType.Single,
            IsEnabled = true
        });
        db.Customers.AddRange(
            new Customer { Id = 1, Code = "C1", Name = "Alice" },
            new Customer { Id = 2, Code = "C2", Name = "Bob" });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ResolveAsync("customer", new[] { "1", "2", "999" }, displayField: "Name", CancellationToken.None);

        result.Should().HaveCount(2);
        result["1"].Should().Be("Alice");
        result["2"].Should().Be("Bob");
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnEmpty_WhenTargetUnknownOrIdsInvalid()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        (await service.ResolveAsync(" ", Array.Empty<string>(), null, CancellationToken.None))
            .Should().BeEmpty();

        (await service.ResolveAsync("unknown", new[] { "1" }, null, CancellationToken.None))
            .Should().BeEmpty();

        db.EntityDefinitions.Add(new EntityDefinition
        {
            EntityRoute = "customer",
            EntityName = "Customer",
            FullTypeName = typeof(Customer).FullName!,
            Status = EntityStatus.Published,
            Source = EntitySource.System,
            StructureType = EntityStructureType.Single,
            IsEnabled = true
        });
        await db.SaveChangesAsync();

        (await service.ResolveAsync("customer", new[] { "not-an-int" }, null, CancellationToken.None))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_ShouldSupportMultipleEntityTypes()
    {
        await using var db = CreateContext();

        db.EntityDefinitions.AddRange(
            new EntityDefinition
            {
                EntityRoute = "customer",
                EntityName = "Customer",
                FullTypeName = typeof(Customer).FullName!,
                Status = EntityStatus.Published,
                Source = EntitySource.System,
                StructureType = EntityStructureType.Single,
                IsEnabled = true
            },
            new EntityDefinition
            {
                EntityRoute = "role",
                EntityName = "RoleProfile",
                FullTypeName = typeof(RoleProfile).FullName!,
                Status = EntityStatus.Published,
                Source = EntitySource.System,
                StructureType = EntityStructureType.Single,
                IsEnabled = true
            });

        db.Customers.Add(new Customer { Id = 1, Code = "C1", Name = "Alice" });
        var roleId = Guid.NewGuid();
        db.RoleProfiles.Add(new RoleProfile { Id = roleId, Code = "R1", Name = "Admin", IsEnabled = true, IsSystem = false });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var customer = await service.ResolveAsync("customer", new[] { "1" }, null, CancellationToken.None);
        var role = await service.ResolveAsync("role", new[] { roleId.ToString() }, null, CancellationToken.None);

        customer["1"].Should().Be("Alice");
        role[roleId.ToString()].Should().Be("Admin");
    }

    private static LookupResolveService CreateService(AppDbContext db)
    {
        var dynamicEntityService = new DynamicEntityService(
            db,
            new CSharpCodeGenerator(),
            new RoslynCompiler(NullLogger<RoslynCompiler>.Instance),
            NullLogger<DynamicEntityService>.Instance);

        return new LookupResolveService(db, dynamicEntityService);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
