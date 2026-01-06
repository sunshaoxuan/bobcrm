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

public class AggVOServiceIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public AggVOServiceIntegrationTests()
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
    public async Task SaveLoadDeleteAggVOAsync_ShouldWork_EndToEnd()
    {
        using var ctx = CreateContext();
        var (aggService, aggVo) = CreateAggVoServiceAndSeedDefinitions(ctx, cascadeDeleteBehavior: CascadeDeleteBehavior.Cascade);

        aggVo.Order = new TestOrderEntity { Code = "ORD001", Amount = 123 };
        aggVo.Lines.Add(new TestOrderLineEntity { ProductName = "A", Quantity = 2 });
        aggVo.Lines.Add(new TestOrderLineEntity { ProductName = "B", Quantity = 3 });

        var masterId = await aggService.SaveAggVOAsync(aggVo);
        masterId.Should().BeGreaterThan(0);
        aggVo.Order!.Id.Should().Be(masterId);
        var persistedLines = await ctx.TestOrderLines.Where(l => l.MasterId == masterId).ToListAsync();
        persistedLines.Should().HaveCount(2);

        var loaded = new TestOrderAggVO();
        await aggService.LoadAggVOAsync(loaded, masterId);
        loaded.Order.Should().NotBeNull();
        loaded.Order!.Id.Should().Be(masterId);
        loaded.Lines.Should().HaveCount(2);

        await aggService.DeleteAggVOAsync(loaded);

        var master = await ctx.TestOrders.SingleAsync(e => e.Id == masterId);
        master.IsDeleted.Should().BeTrue();
        (await ctx.TestOrderLines.Where(l => l.MasterId == masterId).CountAsync()).Should().Be(2);
        (await ctx.TestOrderLines.Where(l => l.MasterId == masterId && !l.IsDeleted).CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteAggVOAsync_WithRestrictChildren_ShouldThrow()
    {
        using var ctx = CreateContext();
        var (aggService, aggVo) = CreateAggVoServiceAndSeedDefinitions(ctx, cascadeDeleteBehavior: CascadeDeleteBehavior.Restrict);

        aggVo.Order = new TestOrderEntity { Code = "ORD002", Amount = 1 };
        aggVo.Lines.Add(new TestOrderLineEntity { ProductName = "A", Quantity = 1 });

        var masterId = await aggService.SaveAggVOAsync(aggVo);
        masterId.Should().BeGreaterThan(0);

        var loaded = new TestOrderAggVO();
        await aggService.LoadAggVOAsync(loaded, masterId);

        var act = async () => await aggService.DeleteAggVOAsync(loaded);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cascade delete behavior 'Restrict'*");
    }

    private static (AggVOService service, TestOrderAggVO aggVo) CreateAggVoServiceAndSeedDefinitions(
        TestAggDbContext ctx,
        string cascadeDeleteBehavior)
    {
        var masterTypeName = typeof(TestOrderEntity).FullName!;
        var childTypeName = typeof(TestOrderLineEntity).AssemblyQualifiedName!;

        var masterEntityDefinition = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderEntity",
            FullTypeName = masterTypeName,
            EntityRoute = "testorder",
            ApiEndpoint = "/api/testorder",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "订单" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var childEntityDefinition = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Test",
            EntityName = "TestOrderLineEntity",
            FullTypeName = childTypeName,
            EntityRoute = "testorderline",
            ApiEndpoint = "/api/testorderline",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            IsEnabled = true,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "明细" },
            ParentEntityId = masterEntityDefinition.Id,
            ParentForeignKeyField = nameof(TestOrderLineEntity.MasterId),
            AutoCascadeSave = true,
            CascadeDeleteBehavior = cascadeDeleteBehavior,
            Order = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        ctx.EntityDefinitions.AddRange(masterEntityDefinition, childEntityDefinition);
        ctx.SaveChanges();

        var dynamicLogger = new Mock<ILogger<DynamicEntityService>>();
        var roslynLogger = new Mock<ILogger<RoslynCompiler>>();
        var persistenceLogger = new Mock<ILogger<ReflectionPersistenceService>>();
        var aggLogger = new Mock<ILogger<AggVOService>>();

        var dynamicEntityService = new StaticDynamicEntityService(ctx, dynamicLogger.Object, roslynLogger.Object);
        dynamicEntityService.Register(masterTypeName, typeof(TestOrderEntity));
        dynamicEntityService.Register(childTypeName, typeof(TestOrderLineEntity));

        var persistence = new ReflectionPersistenceService(ctx, dynamicEntityService, persistenceLogger.Object);
        var service = new AggVOService(ctx, persistence, aggLogger.Object);
        return (service, new TestOrderAggVO());
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

    private sealed class TestOrderAggVO : AggBaseVO
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
                return Lines.Cast<object>().ToList();
            }
            return new List<object>();
        }

        public override void SetSubEntities(Type subEntityType, List<object> entities)
        {
            if (subEntityType == typeof(TestOrderLineEntity))
            {
                Lines = entities.Cast<TestOrderLineEntity>().ToList();
            }
        }
    }
}
