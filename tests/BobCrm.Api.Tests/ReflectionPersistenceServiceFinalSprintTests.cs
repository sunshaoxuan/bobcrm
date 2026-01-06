using System.Text.Json;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BobCrm.Api.Tests;

public class ReflectionPersistenceServiceFinalSprintTests
{
    private const string SoftDeleteTypeName = "BobCrm.Api.Tests.SoftDeleteThing";

    [Fact]
    public async Task SoftDeleteEntity_CRUD_ShouldApplySoftDeleteFilter()
    {
        await using var db = await CreateSqliteContextAsync();
        var dynamicEntityService = new StubDynamicEntityService(db, typeof(SoftDeleteThing));
        var service = new ReflectionPersistenceService(db, dynamicEntityService, NullLogger<ReflectionPersistenceService>.Instance);

        var created = await service.CreateAsync(SoftDeleteTypeName, new Dictionary<string, object>
        {
            ["Name"] = JsonDocument.Parse("\"hello\"").RootElement
        });
        created.Should().BeOfType<SoftDeleteThing>();

        var listBefore = await service.QueryAsync(SoftDeleteTypeName);
        listBefore.Should().HaveCount(1);

        var id = ((SoftDeleteThing)created).Id;
        var deleted = await service.DeleteAsync(SoftDeleteTypeName, id, deletedBy: "tester");
        deleted.Should().BeTrue();

        var listAfter = await service.QueryAsync(SoftDeleteTypeName);
        listAfter.Should().BeEmpty();

        var raw = await db.Set<SoftDeleteThing>().IgnoreQueryFilters().FirstAsync(x => x.Id == id);
        raw.IsDeleted.Should().BeTrue();
        raw.DeletedAt.Should().NotBeNull();
        raw.DeletedBy.Should().Be("tester");
    }

    [Fact]
    public async Task QueryAsync_ShouldSupportFiltersOrderAndPaging()
    {
        await using var db = await CreateSqliteContextAsync();
        db.Set<SoftDeleteThing>().AddRange(
            new SoftDeleteThing { Name = "a", IsDeleted = false },
            new SoftDeleteThing { Name = "b", IsDeleted = false },
            new SoftDeleteThing { Name = "bb", IsDeleted = false });
        await db.SaveChangesAsync();

        var dynamicEntityService = new StubDynamicEntityService(db, typeof(SoftDeleteThing));
        var service = new ReflectionPersistenceService(db, dynamicEntityService, NullLogger<ReflectionPersistenceService>.Instance);

        var results = await service.QueryAsync(SoftDeleteTypeName, new QueryOptions
        {
            Filters = new List<FilterCondition>
            {
                new() { Field = "Name", Operator = "contains", Value = "b" }
            },
            OrderBy = "Name",
            OrderByDescending = true,
            Skip = 0,
            Take = 1
        });

        results.Should().HaveCount(1);
        ((SoftDeleteThing)results[0]).Name.Should().Be("bb");
    }

    [Fact]
    public async Task QueryAsync_WhenOrderByMissing_ShouldNotThrow()
    {
        await using var db = await CreateSqliteContextAsync();
        db.Set<SoftDeleteThing>().Add(new SoftDeleteThing { Name = "x", IsDeleted = false });
        await db.SaveChangesAsync();

        var dynamicEntityService = new StubDynamicEntityService(db, typeof(SoftDeleteThing));
        var service = new ReflectionPersistenceService(db, dynamicEntityService, NullLogger<ReflectionPersistenceService>.Instance);

        var results = await service.QueryAsync(SoftDeleteTypeName, new QueryOptions { OrderBy = "NotAField" });
        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task QueryAsync_WhenUnsupportedOperator_ShouldThrow()
    {
        await using var db = await CreateSqliteContextAsync();
        var dynamicEntityService = new StubDynamicEntityService(db, typeof(SoftDeleteThing));
        var service = new ReflectionPersistenceService(db, dynamicEntityService, NullLogger<ReflectionPersistenceService>.Instance);

        await Assert.ThrowsAsync<NotSupportedException>(() => service.QueryAsync(SoftDeleteTypeName, new QueryOptions
        {
            Filters = new List<FilterCondition>
            {
                new() { Field = "Name", Operator = "startsWith", Value = "a" }
            }
        }));
    }

    [Fact]
    public async Task QueryRawAsync_ShouldBuildSqlWithFilterOrderAndPaging()
    {
        await using var db = await CreateSqliteContextAsync();
        db.Set<SoftDeleteThing>().AddRange(
            new SoftDeleteThing { Name = "x", IsDeleted = false },
            new SoftDeleteThing { Name = "y", IsDeleted = false });
        await db.SaveChangesAsync();

        var dynamicEntityService = new StubDynamicEntityService(db, typeof(SoftDeleteThing));
        var service = new ReflectionPersistenceService(db, dynamicEntityService, NullLogger<ReflectionPersistenceService>.Instance);

        var rows = await service.QueryRawAsync("SoftDeleteThings", new QueryOptions
        {
            Filters = new List<FilterCondition> { new() { Field = "Name", Operator = "equals", Value = "y" } },
            OrderBy = "Name",
            Take = 10,
            Skip = 0
        });

        rows.Should().HaveCount(1);
        rows[0]["Name"]!.ToString().Should().Be("y");
    }

    private static async Task<TestAppDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new TestAppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private sealed class StubDynamicEntityService : DynamicEntityService
    {
        private readonly Type _entityType;

        public StubDynamicEntityService(AppDbContext db, Type entityType)
            : base(db, new CSharpCodeGenerator(), new RoslynCompiler(NullLogger<RoslynCompiler>.Instance), NullLogger<DynamicEntityService>.Instance)
        {
            _entityType = entityType;
        }

        public override Type? GetEntityType(string fullTypeName)
        {
            return string.Equals(fullTypeName, SoftDeleteTypeName, StringComparison.Ordinal)
                ? _entityType
                : null;
        }
    }

    private sealed class TestAppDbContext : AppDbContext
    {
        public TestAppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Entity<SoftDeleteThing>().ToTable("SoftDeleteThings");
        }
    }

    private sealed class SoftDeleteThing
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }
}

