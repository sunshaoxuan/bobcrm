using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BobCrm.Api.Tests;

public class DynamicEntityServicePhase7Tests : IDisposable
{
    public sealed class DummyDynamicEntity
    {
        public string? Name { get; set; }
    }

    private readonly AppDbContext _db;
    private readonly Mock<CSharpCodeGenerator> _codeGen;
    private readonly Mock<RoslynCompiler> _compiler;
    private readonly DynamicEntityService _svc;

    public DynamicEntityServicePhase7Tests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _codeGen = new Mock<CSharpCodeGenerator>();
        _codeGen.Setup(x => x.GenerateEntityClass(It.IsAny<EntityDefinition>()))
            .Returns("public class Dummy {}");
        _codeGen.Setup(x => x.GenerateInterfaces())
            .Returns("public interface IEntity {}");

        var compilerLogger = new Mock<ILogger<RoslynCompiler>>();
        _compiler = new Mock<RoslynCompiler>(compilerLogger.Object);

        var logger = new Mock<ILogger<DynamicEntityService>>();
        _svc = new DynamicEntityService(_db, _codeGen.Object, _compiler.Object, logger.Object);
    }

    public void Dispose()
    {
        _svc.ClearAllLoadedEntities();
        _db.Dispose();
    }

    [Fact]
    public async Task GetEntityTypeInfo_WhenTypeLoaded_ShouldReturnProperties()
    {
        var entityId = Guid.NewGuid();
        var fullTypeName = typeof(DummyDynamicEntity).FullName!;

        _db.EntityDefinitions.Add(new EntityDefinition
        {
            Id = entityId,
            Namespace = "BobCrm.Api.Tests",
            EntityName = "DummyDynamicEntity",
            FullTypeName = fullTypeName,
            EntityRoute = "dummy",
            ApiEndpoint = "/api/dummy",
            Status = EntityStatus.Published,
            StructureType = EntityStructureType.Single,
            Source = EntitySource.Custom
        });
        await _db.SaveChangesAsync();

        _compiler.Setup(x => x.CompileMultiple(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()))
            .Returns(new CompilationResult
            {
                Success = true,
                Assembly = typeof(DummyDynamicEntity).Assembly,
                LoadContext = new System.Runtime.Loader.AssemblyLoadContext(null, true)
            });

        (await _svc.CompileEntityAsync(entityId)).Success.Should().BeTrue();

        _svc.GetEntityType(fullTypeName).Should().Be(typeof(DummyDynamicEntity));
        _svc.CreateEntityInstance(fullTypeName).Should().BeOfType<DummyDynamicEntity>();

        var props = _svc.GetEntityProperties(fullTypeName);
        props.Should().Contain(p => p.Name == nameof(DummyDynamicEntity.Name));

        var info = _svc.GetEntityTypeInfo(fullTypeName);
        info.Should().NotBeNull();
        info!.IsLoaded.Should().BeTrue();
        info.FullName.Should().Be(fullTypeName);
        info.Properties.Should().Contain(p => p.Name == nameof(DummyDynamicEntity.Name));
    }

    [Fact]
    public async Task CompileMultipleEntitiesAsync_WhenNoEntitiesFound_ShouldThrow()
    {
        var act = async () => await _svc.CompileMultipleEntitiesAsync([Guid.NewGuid()]);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*No entities found*");
    }

    [Fact]
    public async Task CompileMultipleEntitiesAsync_WhenAllUnpublished_ShouldThrow()
    {
        var entityId = Guid.NewGuid();
        _db.EntityDefinitions.Add(new EntityDefinition
        {
            Id = entityId,
            Namespace = "BobCrm.Api.Tests",
            EntityName = "DraftEntity",
            FullTypeName = "BobCrm.Api.Tests.DraftEntity",
            EntityRoute = "draft",
            ApiEndpoint = "/api/draft",
            Status = EntityStatus.Draft,
            StructureType = EntityStructureType.Single,
            Source = EntitySource.Custom
        });
        await _db.SaveChangesAsync();

        var act = async () => await _svc.CompileMultipleEntitiesAsync([entityId]);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No valid entities to compile*");
    }
}

