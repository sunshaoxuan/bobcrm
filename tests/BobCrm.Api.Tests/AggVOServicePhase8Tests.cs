using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

public class AggVOServicePhase8Tests : IDisposable
{
    private readonly SqliteConnection _connection;

    public AggVOServicePhase8Tests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private TestAggDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        var ctx = new TestAggDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task SaveAggVOAsync_WhenValidationFails_ShouldThrowBeforeDbWork()
    {
        using var ctx = CreateContext();
        var service = CreateAggVoService(ctx, seedDefinitions: true);

        var agg = new InvalidAggVO { Order = new TestOrderEntity { Code = "X" } };

        var act = async () => await service.SaveAggVOAsync(agg);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Validation failed:*");

        (await ctx.TestOrders.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SaveAggVOAsync_WhenMasterDefinitionMissing_ShouldThrow()
    {
        using var ctx = CreateContext();
        var service = CreateAggVoService(ctx, seedDefinitions: false);

        var agg = new TestOrderAggVO { Order = new TestOrderEntity { Code = "ORD" } };

        var act = async () => await service.SaveAggVOAsync(agg);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Entity definition not found*");
    }

    [Fact]
    public async Task SaveAggVOAsync_WhenChildTypeCannotResolve_ShouldStillSaveMaster()
    {
        using var ctx = CreateContext();

        var masterDef = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderEntity",
            FullTypeName = typeof(TestOrderEntity).FullName!,
            EntityRoute = "testorder",
            ApiEndpoint = "/api/testorder",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true
        };

        var childDef = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "MissingChild",
            FullTypeName = "Missing.Type, Missing.Assembly",
            EntityRoute = "missingchild",
            ApiEndpoint = "/api/missingchild",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true,
            ParentEntityId = masterDef.Id,
            ParentForeignKeyField = nameof(TestOrderLineEntity.MasterId),
            AutoCascadeSave = true,
            CascadeDeleteBehavior = CascadeDeleteBehavior.Cascade,
            Order = 1
        };

        ctx.EntityDefinitions.AddRange(masterDef, childDef);
        ctx.SaveChanges();

        var service = CreateAggVoService(ctx, seedDefinitions: false, masterTypeOverride: typeof(TestOrderEntity).FullName!);
        var agg = new TestOrderAggVO
        {
            Order = new TestOrderEntity { Code = "ORD001", Amount = 1 },
            Lines = [new TestOrderLineEntity { ProductName = "X", Quantity = 1 }]
        };

        var id = await service.SaveAggVOAsync(agg);
        id.Should().BeGreaterThan(0);
        (await ctx.TestOrders.CountAsync()).Should().Be(1);
        (await ctx.TestOrderLines.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task LoadAggVOAsync_WhenMasterRecordMissing_ShouldThrow()
    {
        using var ctx = CreateContext();
        var service = CreateAggVoService(ctx, seedDefinitions: true);

        var agg = new TestOrderAggVO();

        var act = async () => await service.LoadAggVOAsync(agg, masterId: 999);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*was not found*");
    }

    [Fact]
    public async Task DeleteAggVOAsync_WhenNoHeadId_ShouldThrow()
    {
        using var ctx = CreateContext();
        var service = CreateAggVoService(ctx, seedDefinitions: true);

        var agg = new TestOrderAggVO { Order = new TestOrderEntity { Id = 0 } };

        var act = async () => await service.DeleteAggVOAsync(agg);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*valid ID*");
    }

    [Fact]
    public async Task SaveAggVOAsync_WithAutoCascadeChildren_ShouldCreateMasterAndChildren_AndSetIds()
    {
        using var ctx = CreateContext();

        var masterDef = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderEntity",
            FullTypeName = typeof(TestOrderEntity).FullName!,
            EntityRoute = "testorder",
            ApiEndpoint = "/api/testorder",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true
        };

        var childDef = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderLineEntity",
            FullTypeName = typeof(TestOrderLineEntity).AssemblyQualifiedName!,
            EntityRoute = "testorderline",
            ApiEndpoint = "/api/testorderline",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true,
            ParentEntityId = masterDef.Id,
            ParentForeignKeyField = nameof(TestOrderLineEntity.MasterId),
            AutoCascadeSave = true,
            CascadeDeleteBehavior = CascadeDeleteBehavior.Cascade,
            Order = 1
        };

        ctx.EntityDefinitions.AddRange(masterDef, childDef);
        await ctx.SaveChangesAsync();

        var service = CreateAggVoService(ctx, seedDefinitions: false);
        var agg = new TestOrderAggVO
        {
            Order = new TestOrderEntity { Code = "ORD001", Amount = 1 },
            Lines = [new TestOrderLineEntity { ProductName = "X", Quantity = 1 }]
        };

        var id = await service.SaveAggVOAsync(agg);
        id.Should().BeGreaterThan(0);
        agg.Order!.Id.Should().Be(id);
        agg.Lines.Single().Id.Should().BeGreaterThan(0);

        (await ctx.TestOrders.CountAsync()).Should().Be(1);
        (await ctx.TestOrderLines.CountAsync()).Should().Be(1);
        (await ctx.TestOrderLines.SingleAsync()).MasterId.Should().Be(id);
    }

    [Fact]
    public async Task SaveAggVOAsync_WhenUpdatingExistingMasterAndChild_ShouldUpdateRecords()
    {
        using var ctx = CreateContext();

        var masterDef = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderEntity",
            FullTypeName = typeof(TestOrderEntity).FullName!,
            EntityRoute = "testorder",
            ApiEndpoint = "/api/testorder",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true
        };
        var childDef = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderLineEntity",
            FullTypeName = typeof(TestOrderLineEntity).AssemblyQualifiedName!,
            EntityRoute = "testorderline",
            ApiEndpoint = "/api/testorderline",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true,
            ParentEntityId = masterDef.Id,
            ParentForeignKeyField = nameof(TestOrderLineEntity.MasterId),
            AutoCascadeSave = true,
            CascadeDeleteBehavior = CascadeDeleteBehavior.Cascade,
            Order = 1
        };
        ctx.EntityDefinitions.AddRange(masterDef, childDef);

        var existingOrder = new TestOrderEntity { Id = 10, Code = "OLD", Amount = 1 };
        var existingLine = new TestOrderLineEntity { Id = 20, MasterId = 10, ProductName = "Old", Quantity = 1 };
        ctx.TestOrders.Add(existingOrder);
        ctx.TestOrderLines.Add(existingLine);
        await ctx.SaveChangesAsync();

        var service = CreateAggVoService(ctx, seedDefinitions: false);
        var agg = new TestOrderAggVO
        {
            Order = new TestOrderEntity { Id = 10, Code = "NEW", Amount = 2 },
            Lines = [new TestOrderLineEntity { Id = 20, MasterId = 10, ProductName = "New", Quantity = 9 }]
        };

        var id = await service.SaveAggVOAsync(agg);
        id.Should().Be(10);

        (await ctx.TestOrders.SingleAsync(o => o.Id == 10)).Code.Should().Be("NEW");
        (await ctx.TestOrders.SingleAsync(o => o.Id == 10)).Amount.Should().Be(2);
        (await ctx.TestOrderLines.SingleAsync(l => l.Id == 20)).ProductName.Should().Be("New");
        (await ctx.TestOrderLines.SingleAsync(l => l.Id == 20)).Quantity.Should().Be(9);
    }

    [Fact]
    public async Task DeleteAggVOAsync_WhenRestrictAndChildrenExist_ShouldThrow()
    {
        using var ctx = CreateContext();

        var masterDef = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderEntity",
            FullTypeName = typeof(TestOrderEntity).FullName!,
            EntityRoute = "testorder",
            ApiEndpoint = "/api/testorder",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true
        };
        var childDef = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderLineEntity",
            FullTypeName = typeof(TestOrderLineEntity).AssemblyQualifiedName!,
            EntityRoute = "testorderline",
            ApiEndpoint = "/api/testorderline",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true,
            ParentEntityId = masterDef.Id,
            ParentForeignKeyField = nameof(TestOrderLineEntity.MasterId),
            AutoCascadeSave = true,
            CascadeDeleteBehavior = CascadeDeleteBehavior.Restrict,
            Order = 1
        };
        ctx.EntityDefinitions.AddRange(masterDef, childDef);
        ctx.TestOrders.Add(new TestOrderEntity { Id = 1, Code = "ORD", Amount = 1 });
        ctx.TestOrderLines.Add(new TestOrderLineEntity { Id = 2, MasterId = 1, ProductName = "X", Quantity = 1 });
        await ctx.SaveChangesAsync();

        var service = CreateAggVoService(ctx, seedDefinitions: false);
        var agg = new TestOrderAggVO { Order = new TestOrderEntity { Id = 1 } };

        var act = async () => await service.DeleteAggVOAsync(agg);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Restrict*");
    }

    [Fact]
    public async Task DeleteAggVOAsync_WhenNoAction_ShouldDeleteMasterAndKeepChildren()
    {
        using var ctx = CreateContext();

        var masterDef = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderEntity",
            FullTypeName = typeof(TestOrderEntity).FullName!,
            EntityRoute = "testorder",
            ApiEndpoint = "/api/testorder",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true
        };
        var childDef = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderLineEntity",
            FullTypeName = typeof(TestOrderLineEntity).AssemblyQualifiedName!,
            EntityRoute = "testorderline",
            ApiEndpoint = "/api/testorderline",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true,
            ParentEntityId = masterDef.Id,
            ParentForeignKeyField = nameof(TestOrderLineEntity.MasterId),
            AutoCascadeSave = true,
            CascadeDeleteBehavior = CascadeDeleteBehavior.NoAction,
            Order = 1
        };
        ctx.EntityDefinitions.AddRange(masterDef, childDef);
        ctx.TestOrders.Add(new TestOrderEntity { Id = 1, Code = "ORD", Amount = 1 });
        ctx.TestOrderLines.Add(new TestOrderLineEntity { Id = 2, MasterId = 1, ProductName = "X", Quantity = 1 });
        await ctx.SaveChangesAsync();

        var service = CreateAggVoService(ctx, seedDefinitions: false);
        var agg = new TestOrderAggVO { Order = new TestOrderEntity { Id = 1 } };

        await service.DeleteAggVOAsync(agg);

        (await ctx.TestOrderLines.CountAsync()).Should().Be(1);
        (await ctx.TestOrders.SingleAsync(o => o.Id == 1)).IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAggVOAsync_WithChildren_ShouldPopulateAggVo()
    {
        using var ctx = CreateContext();

        var masterDef = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderEntity",
            FullTypeName = typeof(TestOrderEntity).FullName!,
            EntityRoute = "testorder",
            ApiEndpoint = "/api/testorder",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true
        };
        var childDef = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderLineEntity",
            FullTypeName = typeof(TestOrderLineEntity).AssemblyQualifiedName!,
            EntityRoute = "testorderline",
            ApiEndpoint = "/api/testorderline",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true,
            ParentEntityId = masterDef.Id,
            ParentForeignKeyField = nameof(TestOrderLineEntity.MasterId),
            CascadeDeleteBehavior = CascadeDeleteBehavior.Cascade,
            Order = 1
        };
        ctx.EntityDefinitions.AddRange(masterDef, childDef);

        ctx.TestOrders.Add(new TestOrderEntity { Id = 1, Code = "ORD001", Amount = 1 });
        ctx.TestOrderLines.AddRange(
            new TestOrderLineEntity { Id = 10, MasterId = 1, ProductName = "A", Quantity = 1 },
            new TestOrderLineEntity { Id = 11, MasterId = 1, ProductName = "B", Quantity = 2 });
        await ctx.SaveChangesAsync();

        var service = CreateAggVoService(ctx, seedDefinitions: false);
        var agg = new TestOrderAggVO();

        await service.LoadAggVOAsync(agg, masterId: 1);

        agg.Order.Should().NotBeNull();
        agg.Order!.Id.Should().Be(1);
        agg.Order.Code.Should().Be("ORD001");
        agg.Lines.Should().HaveCount(2);
        agg.Lines.Should().Contain(l => l.Id == 10 && l.ProductName == "A");
        agg.Lines.Should().Contain(l => l.Id == 11 && l.ProductName == "B");
    }

    [Fact]
    public async Task DeleteAggVOAsync_WhenCascade_ShouldSoftDeleteChildrenAndMaster()
    {
        using var ctx = CreateContext();

        var masterDef = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderEntity",
            FullTypeName = typeof(TestOrderEntity).FullName!,
            EntityRoute = "testorder",
            ApiEndpoint = "/api/testorder",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true
        };
        var childDef = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderLineEntity",
            FullTypeName = typeof(TestOrderLineEntity).AssemblyQualifiedName!,
            EntityRoute = "testorderline",
            ApiEndpoint = "/api/testorderline",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true,
            ParentEntityId = masterDef.Id,
            ParentForeignKeyField = nameof(TestOrderLineEntity.MasterId),
            CascadeDeleteBehavior = CascadeDeleteBehavior.Cascade,
            Order = 1
        };
        ctx.EntityDefinitions.AddRange(masterDef, childDef);
        ctx.TestOrders.Add(new TestOrderEntity { Id = 1, Code = "ORD001", Amount = 1 });
        ctx.TestOrderLines.Add(new TestOrderLineEntity { Id = 10, MasterId = 1, ProductName = "A", Quantity = 1 });
        await ctx.SaveChangesAsync();

        var service = CreateAggVoService(ctx, seedDefinitions: false);
        var agg = new TestOrderAggVO { Order = new TestOrderEntity { Id = 1 } };

        await service.DeleteAggVOAsync(agg);

        (await ctx.TestOrders.SingleAsync(o => o.Id == 1)).IsDeleted.Should().BeTrue();
        (await ctx.TestOrderLines.SingleAsync(l => l.Id == 10)).IsDeleted.Should().BeTrue();
    }

    private static AggVOService CreateAggVoService(
        TestAggDbContext ctx,
        bool seedDefinitions,
        string? masterTypeOverride = null)
    {
        if (seedDefinitions)
        {
            var masterTypeName = masterTypeOverride ?? typeof(TestOrderEntity).FullName!;
            var masterDef = new EntityDefinition
            {
                Id = Guid.NewGuid(),
                Namespace = "BobCrm.Test",
                EntityName = "TestOrderEntity",
                FullTypeName = masterTypeName,
                EntityRoute = "testorder",
                ApiEndpoint = "/api/testorder",
                StructureType = EntityStructureType.Single,
                Status = EntityStatus.Published,
                IsEnabled = true
            };
            ctx.EntityDefinitions.Add(masterDef);
            ctx.SaveChanges();
        }

        var dynamicLogger = new Mock<ILogger<DynamicEntityService>>();
        var roslynLogger = new Mock<ILogger<RoslynCompiler>>();
        var persistenceLogger = new Mock<ILogger<ReflectionPersistenceService>>();
        var aggLogger = new Mock<ILogger<AggVOService>>();

        var dynamicEntityService = new StaticDynamicEntityService(ctx, dynamicLogger.Object, roslynLogger.Object);
        dynamicEntityService.Register(typeof(TestOrderEntity).FullName!, typeof(TestOrderEntity));
        dynamicEntityService.Register(typeof(TestOrderLineEntity).AssemblyQualifiedName!, typeof(TestOrderLineEntity));

        var persistence = new ReflectionPersistenceService(ctx, dynamicEntityService, persistenceLogger.Object);
        return new AggVOService(ctx, persistence, aggLogger.Object);
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

    private sealed class TestAggDbContext : AppDbContext
    {
        public TestAggDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<TestOrderEntity> TestOrders => Set<TestOrderEntity>();
        public DbSet<TestOrderLineEntity> TestOrderLines => Set<TestOrderLineEntity>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Entity<TestOrderEntity>().ToTable("TestOrders");
            b.Entity<TestOrderLineEntity>().ToTable("TestOrderLines");
        }
    }

    private sealed class TestOrderEntity
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public decimal Amount { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    private sealed class TestOrderLineEntity
    {
        public int Id { get; set; }
        public int MasterId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    private class TestOrderAggVO : AggBaseVO
    {
        public TestOrderEntity? Order { get; set; }
        public List<TestOrderLineEntity> Lines { get; set; } = new();

        public override Type GetHeadEntityType() => typeof(TestOrderEntity);
        public override List<Type> GetSubEntityTypes() => new() { typeof(TestOrderLineEntity) };
        public override object GetHeadVO() => Order!;
        public override void SetHeadVO(object headVO) => Order = (TestOrderEntity)headVO;
        public override Task<int> SaveAsync() => Task.FromResult(Order?.Id ?? 0);
        public override Task LoadAsync(int id) => Task.CompletedTask;
        public override Task DeleteAsync() => Task.CompletedTask;

        public override List<object> GetSubEntities(Type subEntityType)
        {
            if (subEntityType == typeof(TestOrderLineEntity))
            {
                return Lines.ConvertAll(x => (object)x);
            }
            return new List<object>();
        }
    }

    private sealed class InvalidAggVO : TestOrderAggVO
    {
        public override List<string> Validate() => new() { "bad" };
    }
}
