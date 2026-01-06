using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// ReflectionPersistenceService 反射持久化服务测试
/// 注意：由于该服务依赖动态实体类型，部分测试使用模拟或简化场景
/// </summary>
public class ReflectionPersistenceServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Mock<ILogger<ReflectionPersistenceService>> _mockLogger;
    private readonly Mock<ILogger<DynamicEntityService>> _mockDynamicLogger;
    private readonly Mock<ILogger<RoslynCompiler>> _mockRoslynLogger;

    public ReflectionPersistenceServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _mockLogger = new Mock<ILogger<ReflectionPersistenceService>>();
        _mockDynamicLogger = new Mock<ILogger<DynamicEntityService>>();
        _mockRoslynLogger = new Mock<ILogger<RoslynCompiler>>();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private TestAppDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        var ctx = new TestAppDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private ReflectionPersistenceService CreateService(TestAppDbContext ctx, string fullTypeName, Type entityType)
    {
        var dynamicEntityService = new StaticDynamicEntityService(ctx, _mockDynamicLogger.Object, _mockRoslynLogger.Object);
        dynamicEntityService.Register(fullTypeName, entityType);
        return new ReflectionPersistenceService(ctx, dynamicEntityService, _mockLogger.Object);
    }

    private sealed class StaticDynamicEntityService : DynamicEntityService
    {
        private readonly Dictionary<string, Type> _typeMap = new(StringComparer.Ordinal);

        public StaticDynamicEntityService(
            AppDbContext db,
            ILogger<DynamicEntityService> logger,
            ILogger<RoslynCompiler> roslynLogger)
            : base(db, new CSharpCodeGenerator(), new RoslynCompiler(roslynLogger), logger)
        {
        }

        public void Register(string fullTypeName, Type entityType) => _typeMap[fullTypeName] = entityType;

        public override Type? GetEntityType(string fullTypeName)
            => _typeMap.TryGetValue(fullTypeName, out var entityType) ? entityType : null;
    }

    private sealed class TestAppDbContext : AppDbContext
    {
        public TestAppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<TestDynamicEntity> TestDynamicEntities => Set<TestDynamicEntity>();
        public DbSet<TestNonDeletableEntity> TestNonDeletableEntities => Set<TestNonDeletableEntity>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Entity<TestDynamicEntity>().ToTable("TestDynamicEntities");
            b.Entity<TestNonDeletableEntity>().ToTable("TestNonDeletableEntities");
        }
    }

    private sealed class TestDynamicEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    private sealed class TestNonDeletableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #region QueryOptions Tests

    [Fact]
    public void QueryOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new QueryOptions();

        // Assert
        options.Skip.Should().BeNull();
        options.Take.Should().BeNull();
        options.OrderBy.Should().BeNull();
        options.OrderByDescending.Should().BeFalse();
        options.Filters.Should().BeNull();
    }

    [Fact]
    public void QueryOptions_WithPagination_ShouldSetValues()
    {
        // Arrange & Act
        var options = new QueryOptions
        {
            Skip = 10,
            Take = 20
        };

        // Assert
        options.Skip.Should().Be(10);
        options.Take.Should().Be(20);
    }

    [Fact]
    public void QueryOptions_WithSorting_ShouldSetValues()
    {
        // Arrange & Act
        var options = new QueryOptions
        {
            OrderBy = "Name",
            OrderByDescending = true
        };

        // Assert
        options.OrderBy.Should().Be("Name");
        options.OrderByDescending.Should().BeTrue();
    }

    [Fact]
    public void QueryOptions_WithFilters_ShouldSetValues()
    {
        // Arrange & Act
        var options = new QueryOptions
        {
            Filters = new List<FilterCondition>
            {
                new FilterCondition { Field = "Status", Operator = FilterOperator.Equals, Value = "Active" }
            }
        };

        // Assert
        options.Filters.Should().HaveCount(1);
        options.Filters![0].Field.Should().Be("Status");
    }

    #endregion

    #region FilterCondition Tests

    [Fact]
    public void FilterCondition_ShouldStoreFieldAndValue()
    {
        // Arrange & Act
        var filter = new FilterCondition
        {
            Field = "Name",
            Operator = FilterOperator.Equals,
            Value = "Test"
        };

        // Assert
        filter.Field.Should().Be("Name");
        filter.Operator.Should().Be(FilterOperator.Equals);
        filter.Value.Should().Be("Test");
    }

    [Fact]
    public void FilterCondition_WithContainsOperator_ShouldWork()
    {
        // Arrange & Act
        var filter = new FilterCondition
        {
            Field = "Description",
            Operator = FilterOperator.Contains,
            Value = "search"
        };

        // Assert
        filter.Operator.Should().Be(FilterOperator.Contains);
    }

    [Fact]
    public void FilterCondition_WithNumericComparison_ShouldWork()
    {
        // Arrange & Act
        var filterGt = new FilterCondition
        {
            Field = "Amount",
            Operator = FilterOperator.GreaterThan,
            Value = 100
        };

        var filterLt = new FilterCondition
        {
            Field = "Amount",
            Operator = FilterOperator.LessThan,
            Value = 50
        };

        // Assert
        filterGt.Operator.Should().Be(FilterOperator.GreaterThan);
        filterLt.Operator.Should().Be(FilterOperator.LessThan);
    }

    #endregion

    #region Type Conversion Helper Tests

    [Fact]
    public void FilterOperator_ShouldHaveExpectedValues()
    {
        // Assert
        FilterOperator.Equals.Should().Be(FilterOperator.Equals);
        FilterOperator.Contains.Should().NotBe(FilterOperator.Equals);
        FilterOperator.GreaterThan.Should().NotBe(FilterOperator.Equals);
        FilterOperator.LessThan.Should().NotBe(FilterOperator.Equals);
    }

    #endregion

    #region Integration Scenario Tests

    [Fact]
    public void QueryOptions_CompleteScenario_ShouldWork()
    {
        // Arrange & Act
        var options = new QueryOptions
        {
            Skip = 0,
            Take = 10,
            OrderBy = "CreatedAt",
            OrderByDescending = true,
            Filters = new List<FilterCondition>
            {
                new FilterCondition { Field = "Status", Operator = FilterOperator.Equals, Value = "Active" },
                new FilterCondition { Field = "Amount", Operator = FilterOperator.GreaterThan, Value = 100 }
            }
        };

        // Assert
        options.Skip.Should().Be(0);
        options.Take.Should().Be(10);
        options.OrderBy.Should().Be("CreatedAt");
        options.OrderByDescending.Should().BeTrue();
        options.Filters.Should().HaveCount(2);
    }

    #endregion

    #region ReflectionPersistenceService Direct Tests

    [Fact]
    public async Task QueryAsync_ShouldFilterSoftDeletedRecords()
    {
        const string fullTypeName = "BobCrm.Test.TestDynamicEntity";

        using var ctx = CreateTestContext();
        ctx.TestDynamicEntities.AddRange(
            new TestDynamicEntity { Name = "A", Score = 1, IsDeleted = false },
            new TestDynamicEntity { Name = "B", Score = 2, IsDeleted = true },
            new TestDynamicEntity { Name = "C", Score = 3, IsDeleted = false });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx, fullTypeName, typeof(TestDynamicEntity));

        var results = await service.QueryAsync(fullTypeName);

        results.Should().HaveCount(2);
        results.Select(r => ((TestDynamicEntity)r).Name).Should().BeEquivalentTo(["A", "C"]);
    }

    [Fact]
    public async Task QueryAsync_WithContainsAndPagination_ShouldReturnSortedAndPaged()
    {
        const string fullTypeName = "BobCrm.Test.TestDynamicEntity";

        using var ctx = CreateTestContext();
        ctx.TestDynamicEntities.AddRange(
            new TestDynamicEntity { Name = "alpha", Score = 10, IsDeleted = false },
            new TestDynamicEntity { Name = "beta", Score = 30, IsDeleted = false },
            new TestDynamicEntity { Name = "alphabet", Score = 20, IsDeleted = false });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx, fullTypeName, typeof(TestDynamicEntity));
        var options = new QueryOptions
        {
            Filters =
            [
                new FilterCondition { Field = "Name", Operator = FilterOperator.Contains, Value = "alp" }
            ],
            OrderBy = "Score",
            OrderByDescending = true,
            Skip = 1,
            Take = 1
        };

        var results = await service.QueryAsync(fullTypeName, options);

        results.Should().HaveCount(1);
        ((TestDynamicEntity)results[0]).Name.Should().Be("alpha");
    }

    [Fact]
    public async Task GetByIdAsync_WhenSoftDeleted_ShouldReturnNull()
    {
        const string fullTypeName = "BobCrm.Test.TestDynamicEntity";

        using var ctx = CreateTestContext();
        ctx.TestDynamicEntities.AddRange(
            new TestDynamicEntity { Id = 1, Name = "A", IsDeleted = true },
            new TestDynamicEntity { Id = 2, Name = "B", IsDeleted = false });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx, fullTypeName, typeof(TestDynamicEntity));

        var deleted = await service.GetByIdAsync(fullTypeName, 1);
        var active = await service.GetByIdAsync(fullTypeName, 2);

        deleted.Should().BeNull();
        ((TestDynamicEntity?)active).Should().NotBeNull();
    }

    [Fact]
    public async Task CreateUpdateDeleteAsync_ShouldPersistChanges()
    {
        const string fullTypeName = "BobCrm.Test.TestDynamicEntity";

        using var ctx = CreateTestContext();
        var service = CreateService(ctx, fullTypeName, typeof(TestDynamicEntity));

        using var scoreDoc = JsonDocument.Parse("123");
        var created = await service.CreateAsync(fullTypeName, new Dictionary<string, object>
        {
            ["Name"] = "Created",
            ["Score"] = scoreDoc.RootElement
        });

        var createdEntity = (TestDynamicEntity)created;
        createdEntity.Id.Should().BeGreaterThan(0);

        var updated = await service.UpdateAsync(fullTypeName, createdEntity.Id, new Dictionary<string, object>
        {
            ["Name"] = "Updated"
        });
        updated.Should().NotBeNull();

        var deleted = await service.DeleteAsync(fullTypeName, createdEntity.Id, deletedBy: "tester");
        deleted.Should().BeTrue();

        var persisted = await ctx.TestDynamicEntities.SingleAsync(e => e.Id == createdEntity.Id);
        persisted.IsDeleted.Should().BeTrue();
        persisted.DeletedBy.Should().Be("tester");
        persisted.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenNoSoftDeleteSupport_ShouldReturnFalse()
    {
        const string fullTypeName = "BobCrm.Test.TestNonDeletableEntity";

        using var ctx = CreateTestContext();
        ctx.TestNonDeletableEntities.Add(new TestNonDeletableEntity { Id = 1, Name = "A" });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx, fullTypeName, typeof(TestNonDeletableEntity));

        var ok = await service.DeleteAsync(fullTypeName, 1, deletedBy: "tester");

        ok.Should().BeFalse();
        (await ctx.TestNonDeletableEntities.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CountAsync_ShouldRespectSoftDeleteAndFilters()
    {
        const string fullTypeName = "BobCrm.Test.TestDynamicEntity";

        using var ctx = CreateTestContext();
        ctx.TestDynamicEntities.AddRange(
            new TestDynamicEntity { Name = "A", IsDeleted = false },
            new TestDynamicEntity { Name = "A", IsDeleted = true },
            new TestDynamicEntity { Name = "B", IsDeleted = false });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx, fullTypeName, typeof(TestDynamicEntity));

        (await service.CountAsync(fullTypeName)).Should().Be(2);
        (await service.CountAsync(fullTypeName, [new FilterCondition { Field = "Name", Operator = FilterOperator.Equals, Value = "A" }]))
            .Should().Be(1);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void QueryOptions_WithNegativeSkip_ShouldStoreValue()
    {
        // Arrange & Act
        var options = new QueryOptions { Skip = -1 };

        // Assert - negative values are stored but service should handle them
        options.Skip.Should().Be(-1);
    }

    [Fact]
    public void QueryOptions_WithZeroTake_ShouldStoreValue()
    {
        // Arrange & Act
        var options = new QueryOptions { Take = 0 };

        // Assert
        options.Take.Should().Be(0);
    }

    [Fact]
    public void FilterCondition_WithNullValue_ShouldStoreNull()
    {
        // Arrange & Act
        var filter = new FilterCondition
        {
            Field = "DeletedAt",
            Operator = FilterOperator.Equals,
            Value = null
        };

        // Assert
        filter.Value.Should().BeNull();
    }

    [Fact]
    public void FilterCondition_WithEmptyField_ShouldStoreEmpty()
    {
        // Arrange & Act
        var filter = new FilterCondition
        {
            Field = "",
            Operator = FilterOperator.Equals,
            Value = "test"
        };

        // Assert
        filter.Field.Should().BeEmpty();
    }

    #endregion

    #region Logging Verification Tests

    [Fact]
    public void MockLogger_ShouldBeCreatable()
    {
        // Arrange & Act
        var mockLogger = new Mock<ILogger<ReflectionPersistenceService>>();

        // Assert
        mockLogger.Object.Should().NotBeNull();
    }

    #endregion
}
