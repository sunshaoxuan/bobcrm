using BobCrm.Api.Base.Aggregates;
using FluentAssertions;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// AggBaseVO 聚合根基类测试
/// </summary>
public class AggBaseVOTests
{
    #region Test Classes

    /// <summary>
    /// 测试用主实体VO
    /// </summary>
    private class TestHeadVO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    /// <summary>
    /// 测试用子实体VO
    /// </summary>
    private class TestChildVO
    {
        public int Id { get; set; }
        public int HeadId { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// 测试用聚合VO
    /// </summary>
    private class TestAggVO : AggBaseVO
    {
        public TestHeadVO? Head { get; set; }
        public List<TestChildVO> Children { get; set; } = new();

        public override Type GetHeadEntityType() => typeof(TestHeadVO);

        public override List<Type> GetSubEntityTypes() => new() { typeof(TestChildVO) };

        public override object GetHeadVO() => Head!;

        public override void SetHeadVO(object headVO)
        {
            Head = (TestHeadVO)headVO;
        }

        public override Task<int> SaveAsync() => Task.FromResult(Head?.Id ?? 0);

        public override Task LoadAsync(int id) => Task.CompletedTask;

        public override Task DeleteAsync() => Task.CompletedTask;
    }

    /// <summary>
    /// 测试用空主实体聚合VO
    /// </summary>
    private class TestNullHeadAggVO : AggBaseVO
    {
        public TestHeadVO? Head { get; set; }

        public override Type GetHeadEntityType() => typeof(TestHeadVO);

        public override List<Type> GetSubEntityTypes() => new();

        public override object GetHeadVO() => Head!;

        public override void SetHeadVO(object headVO)
        {
            Head = (TestHeadVO)headVO;
        }

        public override Task<int> SaveAsync() => Task.FromResult(0);

        public override Task LoadAsync(int id) => Task.CompletedTask;

        public override Task DeleteAsync() => Task.CompletedTask;
    }

    #endregion

    #region GetHeadId Tests

    [Fact]
    public void GetHeadId_WithValidId_ShouldReturnId()
    {
        // Arrange
        var aggVO = new TestAggVO
        {
            Head = new TestHeadVO { Id = 42, Name = "Test" }
        };

        // Act
        var headId = aggVO.GetHeadId();

        // Assert
        headId.Should().Be(42);
    }

    [Fact]
    public void GetHeadId_WithZeroId_ShouldReturnZero()
    {
        // Arrange
        var aggVO = new TestAggVO
        {
            Head = new TestHeadVO { Id = 0, Name = "Test" }
        };

        // Act
        var headId = aggVO.GetHeadId();

        // Assert
        headId.Should().Be(0);
    }

    [Fact]
    public void GetHeadId_WithNullHead_ShouldReturnZero()
    {
        // Arrange
        var aggVO = new TestNullHeadAggVO { Head = null };

        // Act
        var headId = aggVO.GetHeadId();

        // Assert
        headId.Should().Be(0);
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_WithValidHead_ShouldReturnNoErrors()
    {
        // Arrange
        var aggVO = new TestAggVO
        {
            Head = new TestHeadVO { Id = 1, Name = "Valid" }
        };

        // Act
        var errors = aggVO.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithNullHead_ShouldReturnError()
    {
        // Arrange
        var aggVO = new TestNullHeadAggVO { Head = null };

        // Act
        var errors = aggVO.Validate();

        // Assert
        errors.Should().Contain("Head entity cannot be null");
    }

    #endregion

    #region GetSubEntities Tests

    [Fact]
    public void GetSubEntities_WithExistingType_ShouldReturnList()
    {
        // Arrange
        var aggVO = new TestAggVO
        {
            Head = new TestHeadVO { Id = 1 },
            Children = new List<TestChildVO>
            {
                new TestChildVO { Id = 1, HeadId = 1, Description = "Child 1" },
                new TestChildVO { Id = 2, HeadId = 1, Description = "Child 2" }
            }
        };

        // Act
        var children = aggVO.GetSubEntities(typeof(TestChildVO));

        // Assert
        children.Should().NotBeNull();
        children.Should().HaveCount(2);
    }

    [Fact]
    public void GetSubEntities_WithEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var aggVO = new TestAggVO
        {
            Head = new TestHeadVO { Id = 1 },
            Children = new List<TestChildVO>()
        };

        // Act
        var children = aggVO.GetSubEntities(typeof(TestChildVO));

        // Assert
        children.Should().NotBeNull();
        children.Should().BeEmpty();
    }

    [Fact]
    public void GetSubEntities_WithUnknownType_ShouldReturnNull()
    {
        // Arrange
        var aggVO = new TestAggVO
        {
            Head = new TestHeadVO { Id = 1 }
        };

        // Act
        var result = aggVO.GetSubEntities(typeof(string));

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SetSubEntities Tests

    [Fact]
    public void SetSubEntities_ShouldSetEntities()
    {
        // Arrange
        var aggVO = new TestAggVO
        {
            Head = new TestHeadVO { Id = 1 }
        };

        var newChildren = new List<object>
        {
            new TestChildVO { Id = 10, HeadId = 1, Description = "New Child" }
        };

        // Act
        aggVO.SetSubEntities(typeof(TestChildVO), newChildren);

        // Assert
        aggVO.Children.Should().HaveCount(1);
        aggVO.Children[0].Id.Should().Be(10);
    }

    [Fact]
    public void SetSubEntities_WithEmptyList_ShouldClearEntities()
    {
        // Arrange
        var aggVO = new TestAggVO
        {
            Head = new TestHeadVO { Id = 1 },
            Children = new List<TestChildVO>
            {
                new TestChildVO { Id = 1, HeadId = 1 }
            }
        };

        // Act
        aggVO.SetSubEntities(typeof(TestChildVO), new List<object>());

        // Assert
        aggVO.Children.Should().BeEmpty();
    }

    #endregion

    #region GetTotalSubEntityCount Tests

    [Fact]
    public void GetTotalSubEntityCount_WithChildren_ShouldReturnCount()
    {
        // Arrange
        var aggVO = new TestAggVO
        {
            Head = new TestHeadVO { Id = 1 },
            Children = new List<TestChildVO>
            {
                new TestChildVO { Id = 1 },
                new TestChildVO { Id = 2 },
                new TestChildVO { Id = 3 }
            }
        };

        // Act
        var count = aggVO.GetTotalSubEntityCount();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public void GetTotalSubEntityCount_WithNoChildren_ShouldReturnZero()
    {
        // Arrange
        var aggVO = new TestAggVO
        {
            Head = new TestHeadVO { Id = 1 },
            Children = new List<TestChildVO>()
        };

        // Act
        var count = aggVO.GetTotalSubEntityCount();

        // Assert
        count.Should().Be(0);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_ShouldCreateDeepCopy()
    {
        // Arrange
        var original = new TestAggVO
        {
            Head = new TestHeadVO { Id = 1, Name = "Original" },
            Children = new List<TestChildVO>
            {
                new TestChildVO { Id = 1, HeadId = 1, Description = "Child" }
            }
        };

        // Act
        var cloned = (TestAggVO)original.Clone();

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.Head.Should().NotBeSameAs(original.Head);
        cloned.Head!.Id.Should().Be(original.Head!.Id);
        cloned.Head.Name.Should().Be(original.Head.Name);
        cloned.Children.Should().HaveCount(1);
        cloned.Children[0].Should().NotBeSameAs(original.Children[0]);
    }

    [Fact]
    public void Clone_ModifyingClone_ShouldNotAffectOriginal()
    {
        // Arrange
        var original = new TestAggVO
        {
            Head = new TestHeadVO { Id = 1, Name = "Original" }
        };

        // Act
        var cloned = (TestAggVO)original.Clone();
        cloned.Head!.Name = "Modified";

        // Assert
        original.Head!.Name.Should().Be("Original");
        cloned.Head.Name.Should().Be("Modified");
    }

    #endregion

    #region GetHeadEntityType and GetSubEntityTypes Tests

    [Fact]
    public void GetHeadEntityType_ShouldReturnCorrectType()
    {
        // Arrange
        var aggVO = new TestAggVO();

        // Act
        var headType = aggVO.GetHeadEntityType();

        // Assert
        headType.Should().Be(typeof(TestHeadVO));
    }

    [Fact]
    public void GetSubEntityTypes_ShouldReturnCorrectTypes()
    {
        // Arrange
        var aggVO = new TestAggVO();

        // Act
        var subTypes = aggVO.GetSubEntityTypes();

        // Assert
        subTypes.Should().HaveCount(1);
        subTypes.Should().Contain(typeof(TestChildVO));
    }

    #endregion
}
