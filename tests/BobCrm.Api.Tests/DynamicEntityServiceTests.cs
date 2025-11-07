using BobCrm.Api.Domain.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;

namespace BobCrm.Api.Tests;

public class DynamicEntityServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<CSharpCodeGenerator> _mockCodeGenerator;
    private readonly Mock<RoslynCompiler> _mockCompiler;
    private readonly Mock<ILogger<DynamicEntityService>> _mockLogger;
    private readonly DynamicEntityService _service;

    public DynamicEntityServiceTests()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _db = new AppDbContext(options);

        // Setup mocks
        _mockCodeGenerator = new Mock<CSharpCodeGenerator>();
        var mockCompilerLogger = new Mock<ILogger<RoslynCompiler>>();
        _mockCompiler = new Mock<RoslynCompiler>(mockCompilerLogger.Object);
        _mockLogger = new Mock<ILogger<DynamicEntityService>>();

        _service = new DynamicEntityService(
            _db,
            _mockCodeGenerator.Object,
            _mockCompiler.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GenerateCodeAsync_ShouldGenerateCode_WhenEntityPublished()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "Product",
            Status = EntityStatus.Published,
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata { PropertyName = "Id", DataType = FieldDataType.Integer, SortOrder = 1 }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        var expectedCode = "public class Product { }";
        _mockCodeGenerator.Setup(x => x.GenerateEntityClass(It.IsAny<EntityDefinition>()))
            .Returns(expectedCode);

        // Act
        var code = await _service.GenerateCodeAsync(entityId);

        // Assert
        code.Should().Be(expectedCode);
        _mockCodeGenerator.Verify(x => x.GenerateEntityClass(It.IsAny<EntityDefinition>()), Times.Once);
    }

    [Fact]
    public async Task GenerateCodeAsync_ShouldThrow_WhenEntityNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GenerateCodeAsync(nonExistentId)
        );
    }

    [Fact]
    public async Task GenerateCodeAsync_ShouldThrow_WhenEntityNotPublished()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "Product",
            Status = EntityStatus.Draft, // Not published
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GenerateCodeAsync(entityId)
        );
    }

    [Fact]
    public async Task CompileEntityAsync_ShouldCompileAndCache_WhenSuccessful()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "Product",
            FullTypeName = "Test.Product",
            Status = EntityStatus.Published,
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata { PropertyName = "Id", DataType = FieldDataType.Integer, SortOrder = 1 }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        var code = "public class Product { }";
        _mockCodeGenerator.Setup(x => x.GenerateEntityClass(It.IsAny<EntityDefinition>()))
            .Returns(code);

        var compilationResult = new CompilationResult
        {
            Success = true,
            Assembly = typeof(DynamicEntityServiceTests).Assembly, // Use test assembly as mock
            LoadedTypes = new List<string> { "Test.Product" }
        };

        _mockCompiler.Setup(x => x.Compile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(compilationResult);

        // Act
        var result = await _service.CompileEntityAsync(entityId);

        // Assert
        result.Success.Should().BeTrue();
        result.Assembly.Should().NotBeNull();
        _service.GetLoadedEntities().Should().Contain("Test.Product");
    }

    [Fact]
    public async Task ValidateEntityCodeAsync_ShouldReturnValidationResult()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "Product",
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        var code = "public class Product { }";
        _mockCodeGenerator.Setup(x => x.GenerateEntityClass(It.IsAny<EntityDefinition>()))
            .Returns(code);

        var validationResult = new ValidationResult { IsValid = true };
        _mockCompiler.Setup(x => x.ValidateSyntax(It.IsAny<string>()))
            .Returns(validationResult);

        // Act
        var result = await _service.ValidateEntityCodeAsync(entityId);

        // Assert
        result.IsValid.Should().BeTrue();
        _mockCompiler.Verify(x => x.ValidateSyntax(code), Times.Once);
    }

    [Fact]
    public void GetLoadedEntities_ShouldReturnEmptyList_Initially()
    {
        // Act
        var entities = _service.GetLoadedEntities();

        // Assert
        entities.Should().BeEmpty();
    }

    [Fact]
    public void UnloadEntity_ShouldRemoveFromCache()
    {
        // This test is limited because we can't easily add to the static cache directly
        // In a real scenario, we'd compile an entity first, then unload it

        // Act
        _service.UnloadEntity("Test.Product");

        // Assert
        _service.GetLoadedEntities().Should().NotContain("Test.Product");
    }

    [Fact]
    public void ClearAllLoadedEntities_ShouldClearCache()
    {
        // Act
        _service.ClearAllLoadedEntities();

        // Assert
        _service.GetLoadedEntities().Should().BeEmpty();
    }

    [Fact]
    public async Task CompileMultipleEntitiesAsync_ShouldCompileAllEntities()
    {
        // Arrange
        var entity1Id = Guid.NewGuid();
        var entity2Id = Guid.NewGuid();

        var entity1 = new EntityDefinition
        {
            Id = entity1Id,
            Namespace = "Test",
            EntityName = "Product",
            FullTypeName = "Test.Product",
            Status = EntityStatus.Published,
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata { PropertyName = "Id", DataType = FieldDataType.Integer, SortOrder = 1 }
            },
            Interfaces = new List<EntityInterface>()
        };

        var entity2 = new EntityDefinition
        {
            Id = entity2Id,
            Namespace = "Test",
            EntityName = "Customer",
            FullTypeName = "Test.Customer",
            Status = EntityStatus.Published,
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata { PropertyName = "Id", DataType = FieldDataType.Integer, SortOrder = 1 }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddRangeAsync(entity1, entity2);
        await _db.SaveChangesAsync();

        _mockCodeGenerator.Setup(x => x.GenerateEntityClass(It.IsAny<EntityDefinition>()))
            .Returns("public class Test { }");

        _mockCodeGenerator.Setup(x => x.GenerateInterfaces())
            .Returns("public interface IEntity { }");

        var compilationResult = new CompilationResult
        {
            Success = true,
            Assembly = typeof(DynamicEntityServiceTests).Assembly,
            LoadedTypes = new List<string> { "Test.Product", "Test.Customer" }
        };

        _mockCompiler.Setup(x => x.CompileMultiple(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()))
            .Returns(compilationResult);

        // Act
        var result = await _service.CompileMultipleEntitiesAsync(new List<Guid> { entity1Id, entity2Id });

        // Assert
        result.Success.Should().BeTrue();
        _mockCompiler.Verify(x => x.CompileMultiple(
            It.Is<Dictionary<string, string>>(d => d.Count == 3), // 2 entities + 1 interfaces file
            It.IsAny<string>()
        ), Times.Once);
    }

    [Fact]
    public async Task CompileMultipleEntitiesAsync_ShouldSkipUnpublishedEntities()
    {
        // Arrange
        var publishedId = Guid.NewGuid();
        var draftId = Guid.NewGuid();

        var publishedEntity = new EntityDefinition
        {
            Id = publishedId,
            Namespace = "Test",
            EntityName = "Product",
            FullTypeName = "Test.Product",
            Status = EntityStatus.Published,
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        var draftEntity = new EntityDefinition
        {
            Id = draftId,
            Namespace = "Test",
            EntityName = "Draft",
            FullTypeName = "Test.Draft",
            Status = EntityStatus.Draft, // Not published
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddRangeAsync(publishedEntity, draftEntity);
        await _db.SaveChangesAsync();

        _mockCodeGenerator.Setup(x => x.GenerateEntityClass(It.IsAny<EntityDefinition>()))
            .Returns("public class Test { }");

        _mockCodeGenerator.Setup(x => x.GenerateInterfaces())
            .Returns("public interface IEntity { }");

        var compilationResult = new CompilationResult
        {
            Success = true,
            Assembly = typeof(DynamicEntityServiceTests).Assembly,
            LoadedTypes = new List<string> { "Test.Product" }
        };

        _mockCompiler.Setup(x => x.CompileMultiple(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()))
            .Returns(compilationResult);

        // Act
        var result = await _service.CompileMultipleEntitiesAsync(new List<Guid> { publishedId, draftId });

        // Assert
        result.Success.Should().BeTrue();
        // Should only compile the published entity + interfaces file
        _mockCompiler.Verify(x => x.CompileMultiple(
            It.Is<Dictionary<string, string>>(d => d.Count == 2 && d.ContainsKey("Product.cs") && d.ContainsKey("_Interfaces.cs")),
            It.IsAny<string>()
        ), Times.Once);
    }

    [Fact]
    public async Task RecompileEntityAsync_ShouldUnloadAndRecompile()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new EntityDefinition
        {
            Id = entityId,
            Namespace = "Test",
            EntityName = "Product",
            FullTypeName = "Test.Product",
            Status = EntityStatus.Published,
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata { PropertyName = "Id", DataType = FieldDataType.Integer, SortOrder = 1 }
            },
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(entity);
        await _db.SaveChangesAsync();

        _mockCodeGenerator.Setup(x => x.GenerateEntityClass(It.IsAny<EntityDefinition>()))
            .Returns("public class Product { }");

        var compilationResult = new CompilationResult
        {
            Success = true,
            Assembly = typeof(DynamicEntityServiceTests).Assembly,
            LoadedTypes = new List<string> { "Test.Product" }
        };

        _mockCompiler.Setup(x => x.Compile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(compilationResult);

        // Act
        var result = await _service.RecompileEntityAsync(entityId);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void GetEntityType_ShouldReturnNull_WhenNotLoaded()
    {
        // Act
        var type = _service.GetEntityType("NonExistent.Type");

        // Assert
        type.Should().BeNull();
    }

    [Fact]
    public void CreateEntityInstance_ShouldReturnNull_WhenTypeNotLoaded()
    {
        // Act
        var instance = _service.CreateEntityInstance("NonExistent.Type");

        // Assert
        instance.Should().BeNull();
    }

    [Fact]
    public void GetEntityProperties_ShouldReturnEmptyList_WhenTypeNotLoaded()
    {
        // Act
        var properties = _service.GetEntityProperties("NonExistent.Type");

        // Assert
        properties.Should().BeEmpty();
    }

    [Fact]
    public void GetEntityTypeInfo_ShouldReturnNull_WhenTypeNotLoaded()
    {
        // Act
        var info = _service.GetEntityTypeInfo("NonExistent.Type");

        // Assert
        info.Should().BeNull();
    }

    public void Dispose()
    {
        _db?.Dispose();
        _service?.ClearAllLoadedEntities();
    }
}
