using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Tests;

public class AuditLogServiceTests
{
    [Fact]
    public async Task SearchAsync_ShouldFilterAndPageAndOrderByOccurredAtDesc()
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;

        db.AuditLogs.AddRange(
            new AuditLog { Module = "ENTITY", OperationType = "C", ActorId = "u1", ActorName = "Alice", OccurredAt = now.AddMinutes(-10) },
            new AuditLog { Module = "ENTITY", OperationType = "U", ActorId = "u2", ActorName = "Bob", OccurredAt = now.AddMinutes(-5) },
            new AuditLog { Module = "ROLE", OperationType = "U", ActorId = "u2", ActorName = "Bobby", OccurredAt = now.AddMinutes(-1) },
            new AuditLog { Module = "ROLE", OperationType = "U", ActorId = "x", ActorName = "Other", OccurredAt = now.AddMinutes(-20) });
        await db.SaveChangesAsync();

        var service = new AuditLogService(db);

        var result = await service.SearchAsync(
            page: 1,
            pageSize: 1,
            module: "ROLE",
            operationType: "U",
            actorQuery: "Bob",
            fromUtc: now.AddMinutes(-3),
            toUtc: now,
            ct: CancellationToken.None);

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.TotalCount.Should().Be(1);

        var item = result.Data.Should().ContainSingle().Subject;
        item.Module.Should().Be("ROLE");
        item.OperationType.Should().Be("U");
        item.ActorName.Should().Be("Bobby");
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnSecondPage()
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;

        db.AuditLogs.AddRange(
            new AuditLog { Module = "ENTITY", OperationType = "U", OccurredAt = now.AddMinutes(-1) },
            new AuditLog { Module = "ENTITY", OperationType = "U", OccurredAt = now.AddMinutes(-2) },
            new AuditLog { Module = "ENTITY", OperationType = "U", OccurredAt = now.AddMinutes(-3) });
        await db.SaveChangesAsync();

        var service = new AuditLogService(db);
        var page2 = await service.SearchAsync(2, 1, "ENTITY", "U", null, null, null, CancellationToken.None);

        page2.TotalCount.Should().Be(3);
        page2.Data.Should().NotBeNull();
        page2.Data!.Should().ContainSingle();
        page2.Data!.Single().OccurredAt.Should().BeCloseTo(now.AddMinutes(-2), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetModulesAsync_ShouldReturnDistinctSortedAndClampLimit()
    {
        await using var db = CreateContext();
        db.AuditLogs.AddRange(
            new AuditLog { Module = "B", OperationType = "C" },
            new AuditLog { Module = "A", OperationType = "C" },
            new AuditLog { Module = "B", OperationType = "U" },
            new AuditLog { Module = "", OperationType = "U" });
        await db.SaveChangesAsync();

        var service = new AuditLogService(db);

        var one = await service.GetModulesAsync(limit: 0, ct: CancellationToken.None);
        one.Should().HaveCount(1);
        one.Single().Should().Be("A");

        var all = await service.GetModulesAsync(limit: 2000, ct: CancellationToken.None);
        all.Should().Equal("A", "B");
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
