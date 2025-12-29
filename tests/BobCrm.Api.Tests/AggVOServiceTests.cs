using BobCrm.Api.Base.Aggregates;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using BobCrm.Api.Services.Aggregates;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// AggVOService 聚合值对象服务测试
/// </summary>
public class AggVOServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Mock<ILogger<AggVOService>> _mockLogger;
    private readonly Mock<ILogger<ReflectionPersistenceService>> _mockPersistenceLogger;

    public AggVOServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _mockLogger = new Mock<ILogger<AggVOService>>();
        _mockPersistenceLogger = new Mock<ILogger<ReflectionPersistenceService>>();
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

    #region Test Classes

    /// <summary>
    /// 测试用主实体VO
    /// </summary>
    private class TestOrderVO
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// 测试用子实体VO
    /// </summary>
    private class TestOrderLineVO
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// 测试用聚合VO
    /// </summary>
    private class TestOrderAggVO : AggBaseVO
    {
        public TestOrderVO? Order { get; set; }
        public List<TestOrderLineVO> Lines { get; set; } = new();

        public override Type GetHeadEntityType() => typeof(TestOrderVO);
        public override List<Type> GetSubEntityTypes() => new() { typeof(TestOrderLineVO) };
        public override object GetHeadVO() => Order!;
        public override void SetHeadVO(object headVO) => Order = (TestOrderVO)headVO;
        public override Task<int> SaveAsync() => Task.FromResult(Order?.Id ?? 0);
        public override Task LoadAsync(int id) => Task.CompletedTask;
        public override Task DeleteAsync() => Task.CompletedTask;
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void AggVO_Validate_WithNullHead_ShouldReturnError()
    {
        // Arrange
        var aggVO = new TestOrderAggVO { Order = null };

        // Act
        var errors = aggVO.Validate();

        // Assert
        errors.Should().Contain("Head entity cannot be null");
    }

    [Fact]
    public void AggVO_Validate_WithValidHead_ShouldReturnNoErrors()
    {
        // Arrange
        var aggVO = new TestOrderAggVO
        {
            Order = new TestOrderVO { Id = 1, Code = "ORD001", Amount = 100 }
        };

        // Act
        var errors = aggVO.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region GetHeadId Tests

    [Fact]
    public void AggVO_GetHeadId_ShouldReturnCorrectId()
    {
        // Arrange
        var aggVO = new TestOrderAggVO
        {
            Order = new TestOrderVO { Id = 42, Code = "ORD001" }
        };

        // Act
        var id = aggVO.GetHeadId();

        // Assert
        id.Should().Be(42);
    }

    [Fact]
    public void AggVO_GetHeadId_WithNullHead_ShouldReturnZero()
    {
        // Arrange
        var aggVO = new TestOrderAggVO { Order = null };

        // Act
        var id = aggVO.GetHeadId();

        // Assert
        id.Should().Be(0);
    }

    #endregion

    #region SubEntity Tests

    [Fact]
    public void AggVO_GetSubEntities_ShouldReturnSubEntityList()
    {
        // Arrange
        var aggVO = new TestOrderAggVO
        {
            Order = new TestOrderVO { Id = 1 },
            Lines = new List<TestOrderLineVO>
            {
                new TestOrderLineVO { Id = 1, OrderId = 1, ProductName = "Product A", Quantity = 2 },
                new TestOrderLineVO { Id = 2, OrderId = 1, ProductName = "Product B", Quantity = 3 }
            }
        };

        // Act
        var lines = aggVO.GetSubEntities(typeof(TestOrderLineVO));

        // Assert
        lines.Should().NotBeNull();
        lines.Should().HaveCount(2);
    }

    [Fact]
    public void AggVO_SetSubEntities_ShouldUpdateList()
    {
        // Arrange
        var aggVO = new TestOrderAggVO
        {
            Order = new TestOrderVO { Id = 1 }
        };
        var newLines = new List<object>
        {
            new TestOrderLineVO { Id = 10, OrderId = 1, ProductName = "New Product" }
        };

        // Act
        aggVO.SetSubEntities(typeof(TestOrderLineVO), newLines);

        // Assert
        aggVO.Lines.Should().HaveCount(1);
        aggVO.Lines[0].Id.Should().Be(10);
    }

    [Fact]
    public void AggVO_GetTotalSubEntityCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var aggVO = new TestOrderAggVO
        {
            Order = new TestOrderVO { Id = 1 },
            Lines = new List<TestOrderLineVO>
            {
                new TestOrderLineVO { Id = 1 },
                new TestOrderLineVO { Id = 2 },
                new TestOrderLineVO { Id = 3 }
            }
        };

        // Act
        var count = aggVO.GetTotalSubEntityCount();

        // Assert
        count.Should().Be(3);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void AggVO_Clone_ShouldCreateDeepCopy()
    {
        // Arrange
        var original = new TestOrderAggVO
        {
            Order = new TestOrderVO { Id = 1, Code = "ORD001", Amount = 100 },
            Lines = new List<TestOrderLineVO>
            {
                new TestOrderLineVO { Id = 1, OrderId = 1, ProductName = "Product A" }
            }
        };

        // Act
        var cloned = (TestOrderAggVO)original.Clone();

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.Order.Should().NotBeSameAs(original.Order);
        cloned.Order!.Code.Should().Be("ORD001");
        cloned.Lines.Should().HaveCount(1);
    }

    [Fact]
    public void AggVO_Clone_ModifyingClone_ShouldNotAffectOriginal()
    {
        // Arrange
        var original = new TestOrderAggVO
        {
            Order = new TestOrderVO { Id = 1, Code = "ORD001" }
        };

        // Act
        var cloned = (TestOrderAggVO)original.Clone();
        cloned.Order!.Code = "MODIFIED";

        // Assert
        original.Order!.Code.Should().Be("ORD001");
    }

    #endregion

    #region Entity Types Tests

    [Fact]
    public void AggVO_GetHeadEntityType_ShouldReturnCorrectType()
    {
        // Arrange
        var aggVO = new TestOrderAggVO();

        // Act
        var type = aggVO.GetHeadEntityType();

        // Assert
        type.Should().Be(typeof(TestOrderVO));
    }

    [Fact]
    public void AggVO_GetSubEntityTypes_ShouldReturnCorrectTypes()
    {
        // Arrange
        var aggVO = new TestOrderAggVO();

        // Act
        var types = aggVO.GetSubEntityTypes();

        // Assert
        types.Should().HaveCount(1);
        types.Should().Contain(typeof(TestOrderLineVO));
    }

    #endregion
}
