using System.Data.Common;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Tests;

public class DDLExecutionServiceSqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public DDLExecutionServiceSqliteTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
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

    private static async Task<bool> TableExistsAsync(DbConnection connection, string tableName)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name;";
        var p = cmd.CreateParameter();
        p.ParameterName = "$name";
        p.Value = tableName;
        cmd.Parameters.Add(p);
        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    private static async Task EnsureEntityDefinitionAsync(AppDbContext db, Guid entityId)
    {
        if (await db.EntityDefinitions.AnyAsync(e => e.Id == entityId))
        {
            return;
        }

        db.EntityDefinitions.Add(new EntityDefinition
        {
            Id = entityId,
            Namespace = "BobCrm.Tests",
            EntityName = $"DdlEntity_{entityId:N}",
            FullTypeName = $"BobCrm.Tests.DdlEntity_{entityId:N}",
            EntityRoute = $"ddl_{entityId:N}",
            ApiEndpoint = $"/api/ddl/{entityId:N}",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Draft,
            Source = EntitySource.Custom,
            IsEnabled = true,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "DDL测试实体", ["en"] = "DDL Test Entity", ["ja"] = "DDLテスト" }
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteDDLAsync_WhenSqlValid_ShouldCreateTableAndRecordSuccess()
    {
        await using var ctx = CreateContext();
        var service = new DDLExecutionService(ctx, NullLogger<DDLExecutionService>.Instance);
        var entityId = Guid.NewGuid();
        await EnsureEntityDefinitionAsync(ctx, entityId);

        var result = await service.ExecuteDDLAsync(entityId, DDLScriptType.Create, "CREATE TABLE ddl_test_a (id INTEGER PRIMARY KEY);", "tester");

        result.Status.Should().Be(DDLScriptStatus.Success);
        result.ExecutedAt.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();

        (await TableExistsAsync(_connection, "ddl_test_a")).Should().BeTrue();

        ctx.ChangeTracker.Clear();
        var stored = await ctx.DDLScripts.AsNoTracking().SingleAsync(s => s.Id == result.Id);
        stored.Status.Should().Be(DDLScriptStatus.Success);
        stored.CreatedBy.Should().Be("tester");
    }

    [Fact]
    public async Task ExecuteDDLAsync_WhenSqlInvalid_ShouldRecordFailure()
    {
        await using var ctx = CreateContext();
        var service = new DDLExecutionService(ctx, NullLogger<DDLExecutionService>.Instance);
        var entityId = Guid.NewGuid();
        await EnsureEntityDefinitionAsync(ctx, entityId);

        var result = await service.ExecuteDDLAsync(entityId, DDLScriptType.Create, "CREAT TABLE ddl_test_b (id INT);", "tester");

        result.Status.Should().Be(DDLScriptStatus.Failed);
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        result.ExecutedAt.Should().NotBeNull();

        (await TableExistsAsync(_connection, "ddl_test_b")).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteDDLBatchAsync_WhenAllSuccess_ShouldCommitAndPersistScripts()
    {
        await using var ctx = CreateContext();
        var service = new DDLExecutionService(ctx, NullLogger<DDLExecutionService>.Instance);
        var entityId = Guid.NewGuid();
        await EnsureEntityDefinitionAsync(ctx, entityId);

        var scripts = new List<(string ScriptType, string SqlScript)>
        {
            (DDLScriptType.Create, "CREATE TABLE ddl_batch_1 (id INTEGER PRIMARY KEY);"),
            (DDLScriptType.Create, "CREATE TABLE ddl_batch_2 (id INTEGER PRIMARY KEY);")
        };

        var results = await service.ExecuteDDLBatchAsync(entityId, scripts, "tester");

        results.Should().HaveCount(2);
        results.All(r => r.Status == DDLScriptStatus.Success).Should().BeTrue();

        (await TableExistsAsync(_connection, "ddl_batch_1")).Should().BeTrue();
        (await TableExistsAsync(_connection, "ddl_batch_2")).Should().BeTrue();

        ctx.ChangeTracker.Clear();
        (await ctx.DDLScripts.AsNoTracking().CountAsync(s => s.EntityDefinitionId == entityId)).Should().Be(2);
    }

    [Fact]
    public async Task ExecuteDDLBatchAsync_WhenSecondFails_ShouldRollbackAndNotPersistScripts()
    {
        await using var ctx = CreateContext();
        var service = new DDLExecutionService(ctx, NullLogger<DDLExecutionService>.Instance);
        var entityId = Guid.NewGuid();
        await EnsureEntityDefinitionAsync(ctx, entityId);

        var scripts = new List<(string ScriptType, string SqlScript)>
        {
            (DDLScriptType.Create, "CREATE TABLE ddl_batch_fail_1 (id INTEGER PRIMARY KEY);"),
            (DDLScriptType.Create, "CREAT TABLE ddl_batch_fail_2 (id INT);")
        };

        var results = await service.ExecuteDDLBatchAsync(entityId, scripts, "tester");

        results.Should().HaveCount(2);
        results[0].Status.Should().Be(DDLScriptStatus.Success);
        results[1].Status.Should().Be(DDLScriptStatus.Failed);

        (await TableExistsAsync(_connection, "ddl_batch_fail_1")).Should().BeFalse();
        (await TableExistsAsync(_connection, "ddl_batch_fail_2")).Should().BeFalse();

        ctx.ChangeTracker.Clear();
        (await ctx.DDLScripts.AsNoTracking().CountAsync(s => s.EntityDefinitionId == entityId)).Should().Be(0);
    }

    [Fact]
    public async Task ValidateDDLAsync_WithValidQuery_ShouldReturnTrue()
    {
        await using var ctx = CreateContext();
        var service = new DDLExecutionService(ctx, NullLogger<DDLExecutionService>.Instance);

        var (isValid, error) = await service.ValidateDDLAsync("SELECT 1;");

        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public async Task ValidateDDLAsync_WithInvalidQuery_ShouldReturnFalse()
    {
        await using var ctx = CreateContext();
        var service = new DDLExecutionService(ctx, NullLogger<DDLExecutionService>.Instance);

        var (isValid, error) = await service.ValidateDDLAsync("SELECT FROM;");

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RollbackDDLAsync_WhenRollbackSuccess_ShouldMarkOriginalRolledBack()
    {
        await using var ctx = CreateContext();
        var service = new DDLExecutionService(ctx, NullLogger<DDLExecutionService>.Instance);
        var entityId = Guid.NewGuid();
        await EnsureEntityDefinitionAsync(ctx, entityId);

        var original = await service.ExecuteDDLAsync(entityId, DDLScriptType.Create, "CREATE TABLE ddl_rb (id INTEGER PRIMARY KEY);", "tester");
        original.Status.Should().Be(DDLScriptStatus.Success);
        (await TableExistsAsync(_connection, "ddl_rb")).Should().BeTrue();

        var rollback = await service.RollbackDDLAsync(original.Id, "DROP TABLE ddl_rb;", "tester");

        rollback.Status.Should().Be(DDLScriptStatus.Success);
        rollback.ScriptType.Should().Be(DDLScriptType.Rollback);
        (await TableExistsAsync(_connection, "ddl_rb")).Should().BeFalse();

        ctx.ChangeTracker.Clear();
        var updatedOriginal = await ctx.DDLScripts.AsNoTracking().SingleAsync(s => s.Id == original.Id);
        updatedOriginal.Status.Should().Be(DDLScriptStatus.RolledBack);
    }
}
