using BobCrm.Api.Base.Aggregates;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// AggregateMetadataPublisher 聚合元数据发布服务测试
/// </summary>
public class AggregateMetadataPublisherTests
{
    private readonly Mock<ILogger<AggregateMetadataPublisher>> _mockLogger;

    public AggregateMetadataPublisherTests()
    {
        _mockLogger = new Mock<ILogger<AggregateMetadataPublisher>>();
    }

    private AggregateMetadataPublisher CreatePublisher()
    {
        return new AggregateMetadataPublisher(_mockLogger.Object);
    }

    private static EntityDefinitionAggregate CreateTestAggregate()
    {
        var rootEntity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "Order",
            Namespace = "Sales",
            FullTypeName = "Sales.Order",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "订单" },
            Description = new Dictionary<string, string?> { ["zh"] = "销售订单" },
            Status = EntityStatus.Published,
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    PropertyName = "code",
                    DataType = FieldDataType.String,
                    DisplayName = new Dictionary<string, string?> { ["zh"] = "订单编号" },
                    IsRequired = true,
                    SortOrder = 1
                },
                new FieldMetadata
                {
                    PropertyName = "amount",
                    DataType = FieldDataType.Decimal,
                    DisplayName = new Dictionary<string, string?> { ["zh"] = "金额" },
                    IsRequired = true,
                    SortOrder = 2
                }
            }
        };

        var subEntities = new List<SubEntityDefinition>
        {
            new SubEntityDefinition
            {
                Code = "OrderLine",
                DisplayName = new Dictionary<string, string?> { ["zh"] = "订单行" },
                Description = new Dictionary<string, string?> { ["zh"] = "订单明细行" },
                SortOrder = 1,
                ForeignKeyField = "OrderId",
                CollectionPropertyName = "Lines",
                Fields = new List<FieldMetadata>
                {
                    new FieldMetadata
                    {
                        PropertyName = "productName",
                        DataType = FieldDataType.String,
                        DisplayName = new Dictionary<string, string?> { ["zh"] = "产品名称" },
                        IsRequired = true,
                        SortOrder = 1
                    },
                    new FieldMetadata
                    {
                        PropertyName = "quantity",
                        DataType = FieldDataType.Int32,
                        DisplayName = new Dictionary<string, string?> { ["zh"] = "数量" },
                        IsRequired = true,
                        SortOrder = 2
                    }
                }
            }
        };

        return new EntityDefinitionAggregate(rootEntity, subEntities);
    }

    #region PublishAsync Tests

    [Fact]
    public async Task PublishAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var publisher = CreatePublisher();
        var aggregate = CreateTestAggregate();

        // Act & Assert - should not throw
        await publisher.PublishAsync(aggregate);
    }

    [Fact]
    public async Task PublishAsync_ShouldLogInformation()
    {
        // Arrange
        var publisher = CreatePublisher();
        var aggregate = CreateTestAggregate();

        // Act
        await publisher.PublishAsync(aggregate);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing metadata")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region GenerateMetadataJson Tests

    [Fact]
    public void GenerateMetadataJson_ShouldReturnValidJson()
    {
        // Arrange
        var publisher = CreatePublisher();
        var aggregate = CreateTestAggregate();

        // Act
        var json = publisher.GenerateMetadataJson(aggregate);

        // Assert
        json.Should().NotBeNullOrEmpty();
        var action = () => JsonDocument.Parse(json);
        action.Should().NotThrow();
    }

    [Fact]
    public void GenerateMetadataJson_ShouldIncludeEntityName()
    {
        // Arrange
        var publisher = CreatePublisher();
        var aggregate = CreateTestAggregate();

        // Act
        var json = publisher.GenerateMetadataJson(aggregate);

        // Assert
        json.Should().Contain("Order");
        json.Should().Contain("entityName");
    }

    [Fact]
    public void GenerateMetadataJson_ShouldIncludeNamespace()
    {
        // Arrange
        var publisher = CreatePublisher();
        var aggregate = CreateTestAggregate();

        // Act
        var json = publisher.GenerateMetadataJson(aggregate);

        // Assert
        json.Should().Contain("Sales");
        json.Should().Contain("namespace");
    }

    [Fact]
    public void GenerateMetadataJson_ShouldIncludeMasterFields()
    {
        // Arrange
        var publisher = CreatePublisher();
        var aggregate = CreateTestAggregate();

        // Act
        var json = publisher.GenerateMetadataJson(aggregate);

        // Assert
        json.Should().Contain("master");
        json.Should().Contain("fields");
        json.Should().Contain("code");
        json.Should().Contain("amount");
    }

    [Fact]
    public void GenerateMetadataJson_ShouldIncludeSubEntities()
    {
        // Arrange
        var publisher = CreatePublisher();
        var aggregate = CreateTestAggregate();

        // Act
        var json = publisher.GenerateMetadataJson(aggregate);

        // Assert
        json.Should().Contain("subEntities");
        json.Should().Contain("OrderLine");
        json.Should().Contain("productName");
        json.Should().Contain("quantity");
    }

    [Fact]
    public void GenerateMetadataJson_ShouldIncludeForeignKeyField()
    {
        // Arrange
        var publisher = CreatePublisher();
        var aggregate = CreateTestAggregate();

        // Act
        var json = publisher.GenerateMetadataJson(aggregate);

        // Assert
        json.Should().Contain("foreignKeyField");
        json.Should().Contain("OrderId");
    }

    [Fact]
    public void GenerateMetadataJson_ShouldIncludeCollectionPropertyName()
    {
        // Arrange
        var publisher = CreatePublisher();
        var aggregate = CreateTestAggregate();

        // Act
        var json = publisher.GenerateMetadataJson(aggregate);

        // Assert
        json.Should().Contain("collectionPropertyName");
        json.Should().Contain("Lines");
    }

    [Fact]
    public void GenerateMetadataJson_ShouldIncludePublishedAt()
    {
        // Arrange
        var publisher = CreatePublisher();
        var aggregate = CreateTestAggregate();

        // Act
        var json = publisher.GenerateMetadataJson(aggregate);

        // Assert
        json.Should().Contain("publishedAt");
    }

    [Fact]
    public void GenerateMetadataJson_ShouldBeCamelCase()
    {
        // Arrange
        var publisher = CreatePublisher();
        var aggregate = CreateTestAggregate();

        // Act
        var json = publisher.GenerateMetadataJson(aggregate);

        // Assert
        // Verify camelCase naming
        json.Should().Contain("entityName");
        json.Should().Contain("propertyName");
        json.Should().Contain("dataType");
    }

    [Fact]
    public void GenerateMetadataJson_WithNoSubEntities_ShouldReturnEmptyArray()
    {
        // Arrange
        var publisher = CreatePublisher();
        var rootEntity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "Simple",
            Namespace = "Test",
            FullTypeName = "Test.Simple",
            Fields = new List<FieldMetadata>()
        };
        var aggregate = new EntityDefinitionAggregate(rootEntity);

        // Act
        var json = publisher.GenerateMetadataJson(aggregate);

        // Assert
        json.Should().Contain("\"subEntities\": []");
    }

    #endregion

    #region Field Ordering Tests

    [Fact]
    public void GenerateMetadataJson_ShouldOrderFieldsBySortOrder()
    {
        // Arrange
        var publisher = CreatePublisher();
        var rootEntity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "Test",
            Namespace = "Test",
            FullTypeName = "Test.Test",
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata { PropertyName = "third", SortOrder = 3, DataType = FieldDataType.String },
                new FieldMetadata { PropertyName = "first", SortOrder = 1, DataType = FieldDataType.String },
                new FieldMetadata { PropertyName = "second", SortOrder = 2, DataType = FieldDataType.String }
            }
        };
        var aggregate = new EntityDefinitionAggregate(rootEntity);

        // Act
        var json = publisher.GenerateMetadataJson(aggregate);

        // Assert - first should appear before second, second before third
        var firstIndex = json.IndexOf("first");
        var secondIndex = json.IndexOf("second");
        var thirdIndex = json.IndexOf("third");

        firstIndex.Should().BeLessThan(secondIndex);
        secondIndex.Should().BeLessThan(thirdIndex);
    }

    #endregion
}
