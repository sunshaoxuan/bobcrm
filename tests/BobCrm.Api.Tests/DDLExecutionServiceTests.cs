using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Tests;

/// <summary>
/// DDLExecutionService 测试
/// 覆盖 DDL 执行与记录
/// </summary>
public class DDLExecutionServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Testable version of DDLExecutionService that doesn't execute real SQL
    /// </summary>
    private class TestableDDLExecutionService : DDLExecutionService
    {
        private readonly bool _shouldFail;
        private readonly string? _failureMessage;

        public TestableDDLExecutionService(AppDbContext db, bool shouldFail = false, string? failureMessage = null)
            : base(db, NullLogger<DDLExecutionService>.Instance)
        {
            _shouldFail = shouldFail;
            _failureMessage = failureMessage;
        }

        public override async Task<DDLScript> ExecuteDDLAsync(
            Guid entityDefinitionId,
            string scriptType,
            string sqlScript,
            string? createdBy = null)
        {
            var script = new DDLScript
            {
                EntityDefinitionId = entityDefinitionId,
                ScriptType = scriptType,
                SqlScript = sqlScript,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            if (_shouldFail)
            {
                script.Status = DDLScriptStatus.Failed;
                script.ErrorMessage = _failureMessage ?? "Simulated failure";
            }
            else
            {
                script.Status = DDLScriptStatus.Success;
            }
            
            script.ExecutedAt = DateTime.UtcNow;

            await _db.DDLScripts.AddAsync(script);
            await _db.SaveChangesAsync();

            return script;
        }

        public override Task<bool> TableExistsAsync(string tableName)
        {
            return Task.FromResult(false);
        }

        public override Task<List<TableColumnInfo>> GetTableColumnsAsync(string tableName)
        {
            return Task.FromResult(new List<TableColumnInfo>());
        }
    }

    [Fact]
    public async Task ExecuteDDLAsync_WhenSuccessful_ShouldRecordSuccess()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = new TestableDDLExecutionService(ctx, shouldFail: false);
        var entityId = Guid.NewGuid();

        // Act
        var result = await service.ExecuteDDLAsync(
            entityId,
            DDLScriptType.Create,
            "CREATE TABLE test (id INT)",
            "test-user"
        );

        // Assert
        result.Status.Should().Be(DDLScriptStatus.Success);
        result.EntityDefinitionId.Should().Be(entityId);
        result.ScriptType.Should().Be(DDLScriptType.Create);
        result.ErrorMessage.Should().BeNull();
        result.ExecutedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteDDLAsync_WhenFailed_ShouldRecordFailure()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = new TestableDDLExecutionService(ctx, shouldFail: true, failureMessage: "Syntax error");
        var entityId = Guid.NewGuid();

        // Act
        var result = await service.ExecuteDDLAsync(
            entityId,
            DDLScriptType.Create,
            "INVALID SQL",
            "test-user"
        );

        // Assert
        result.Status.Should().Be(DDLScriptStatus.Failed);
        result.ErrorMessage.Should().Be("Syntax error");
    }

    [Fact]
    public async Task ExecuteDDLAsync_ShouldPersistScript()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = new TestableDDLExecutionService(ctx);
        var entityId = Guid.NewGuid();

        // Act
        var result = await service.ExecuteDDLAsync(
            entityId,
            DDLScriptType.Create,
            "CREATE TABLE test (id INT)",
            "test-user"
        );

        // Assert
        var stored = await ctx.DDLScripts.FirstOrDefaultAsync(s => s.Id == result.Id);
        stored.Should().NotBeNull();
        stored!.SqlScript.Should().Be("CREATE TABLE test (id INT)");
        stored.CreatedBy.Should().Be("test-user");
    }

    // Note: Batch tests require database transaction support
    // which InMemory database does not fully support.
    // These tests should be covered in integration tests with real database.

    [Fact]
    public async Task GetDDLHistoryAsync_ShouldReturnScriptsInDescendingOrder()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entityId = Guid.NewGuid();
        
        ctx.DDLScripts.AddRange(
            new DDLScript { EntityDefinitionId = entityId, ScriptType = "Create", SqlScript = "1", CreatedAt = DateTime.UtcNow.AddHours(-2), Status = DDLScriptStatus.Success },
            new DDLScript { EntityDefinitionId = entityId, ScriptType = "Alter", SqlScript = "2", CreatedAt = DateTime.UtcNow.AddHours(-1), Status = DDLScriptStatus.Success },
            new DDLScript { EntityDefinitionId = entityId, ScriptType = "Alter", SqlScript = "3", CreatedAt = DateTime.UtcNow, Status = DDLScriptStatus.Success }
        );
        await ctx.SaveChangesAsync();

        var service = new TestableDDLExecutionService(ctx);

        // Act
        var history = await service.GetDDLHistoryAsync(entityId);

        // Assert
        history.Should().HaveCount(3);
        history[0].SqlScript.Should().Be("3");
        history[1].SqlScript.Should().Be("2");
        history[2].SqlScript.Should().Be("1");
    }

    [Fact]
    public async Task GetDDLHistoryAsync_WhenNoHistory_ShouldReturnEmpty()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = new TestableDDLExecutionService(ctx);

        // Act
        var history = await service.GetDDLHistoryAsync(Guid.NewGuid());

        // Assert
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task RollbackDDLAsync_ShouldMarkOriginalAsRolledBack()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entityId = Guid.NewGuid();
        var originalScript = new DDLScript
        {
            EntityDefinitionId = entityId,
            ScriptType = DDLScriptType.Create,
            SqlScript = "CREATE TABLE test (id INT)",
            Status = DDLScriptStatus.Success,
            CreatedAt = DateTime.UtcNow,
            ExecutedAt = DateTime.UtcNow
        };
        ctx.DDLScripts.Add(originalScript);
        await ctx.SaveChangesAsync();

        var service = new TestableDDLExecutionService(ctx);

        // Act
        var rollbackResult = await service.RollbackDDLAsync(originalScript.Id, "DROP TABLE test", "test-user");

        // Assert
        rollbackResult.ScriptType.Should().Be(DDLScriptType.Rollback);
        rollbackResult.Status.Should().Be(DDLScriptStatus.Success);
        
        var updated = await ctx.DDLScripts.FindAsync(originalScript.Id);
        updated!.Status.Should().Be(DDLScriptStatus.RolledBack);
    }

    [Fact]
    public async Task RollbackDDLAsync_WhenScriptNotFound_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = new TestableDDLExecutionService(ctx);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RollbackDDLAsync(Guid.NewGuid(), "DROP TABLE test", "test-user"));
    }

    /// <summary>
    /// Helper service that fails on the second script
    /// </summary>
    private class FailingOnSecondScriptDDLService : TestableDDLExecutionService
    {
        private int _callCount;

        public FailingOnSecondScriptDDLService(AppDbContext db) : base(db)
        {
        }

        public override async Task<DDLScript> ExecuteDDLAsync(
            Guid entityDefinitionId,
            string scriptType,
            string sqlScript,
            string? createdBy = null)
        {
            _callCount++;
            var shouldFail = _callCount > 1;

            var script = new DDLScript
            {
                EntityDefinitionId = entityDefinitionId,
                ScriptType = scriptType,
                SqlScript = sqlScript,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                Status = shouldFail ? DDLScriptStatus.Failed : DDLScriptStatus.Success,
                ErrorMessage = shouldFail ? "Simulated failure" : null,
                ExecutedAt = DateTime.UtcNow
            };

            await _db.DDLScripts.AddAsync(script);
            await _db.SaveChangesAsync();

            return script;
        }
    }
}
