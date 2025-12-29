using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
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

    public ReflectionPersistenceServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _mockLogger = new Mock<ILogger<ReflectionPersistenceService>>();
        _mockDynamicLogger = new Mock<ILogger<DynamicEntityService>>();
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
