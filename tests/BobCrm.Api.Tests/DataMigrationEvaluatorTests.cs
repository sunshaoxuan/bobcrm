using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services.DataMigration;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BobCrm.Api.Tests;

public sealed class DataMigrationEvaluatorTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public DataMigrationEvaluatorTests()
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

    [Fact]
    public async Task EvaluateImpactAsync_WhenEntityMissing_ShouldThrow()
    {
        await using var db = CreateContext();
        var evaluator = new DataMigrationEvaluator(db, NullLogger<DataMigrationEvaluator>.Instance);

        var act = async () => await evaluator.EvaluateImpactAsync(Guid.NewGuid(), new List<FieldMetadata>());

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task EvaluateImpactAsync_WhenDraftEntity_ShouldReturnLowRiskWithWarning()
    {
        await using var db = CreateContext();
        var evaluator = new DataMigrationEvaluator(db, NullLogger<DataMigrationEvaluator>.Instance);

        var entity = new EntityDefinition
        {
            EntityName = "DraftEntity",
            EntityRoute = "draft-entity",
            FullTypeName = "X",
            Namespace = "X",
            ApiEndpoint = "/api/x",
            Status = EntityStatus.Draft,
            IsEnabled = true
        };
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var impact = await evaluator.EvaluateImpactAsync(entity.Id, new List<FieldMetadata>());

        impact.RiskLevel.Should().Be(RiskLevel.Low);
        impact.Warnings.Should().Contain(w => w.Contains("draft", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateImpactAsync_WhenPublished_DroppingFieldWithRows_ShouldBeCritical()
    {
        await using var db = CreateContext();
        var evaluator = new DataMigrationEvaluator(db, NullLogger<DataMigrationEvaluator>.Instance);

        var entity = new EntityDefinition
        {
            EntityName = "ImpactEntity",
            EntityRoute = "impact-entity",
            FullTypeName = "X",
            Namespace = "X",
            ApiEndpoint = "/api/x",
            Status = EntityStatus.Published,
            IsEnabled = true
        };
        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Old",
            DataType = FieldDataType.String,
            IsRequired = false
        });
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var table = entity.DefaultTableName.Replace("\"", "\"\"");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE \"" + table + "\" (\"Id\" INTEGER NOT NULL);");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO \"" + table + "\" (\"Id\") VALUES (1);");

        var impact = await evaluator.EvaluateImpactAsync(entity.Id, new List<FieldMetadata>());

        impact.AffectedRows.Should().Be(1);
        impact.Errors.Should().NotBeEmpty();
        impact.Operations.Should().Contain(op => op.OperationType == MigrationOperationType.DropColumn && op.FieldName == "Old");
        impact.RiskLevel.Should().Be(RiskLevel.Critical);
    }

    [Fact]
    public async Task EvaluateImpactAsync_WhenPublished_TableMissing_ShouldReturnRowsZero()
    {
        await using var db = CreateContext();
        var evaluator = new DataMigrationEvaluator(db, NullLogger<DataMigrationEvaluator>.Instance);

        var entity = new EntityDefinition
        {
            EntityName = "MissingTableEntity",
            EntityRoute = "missing-table",
            FullTypeName = "X",
            Namespace = "X",
            ApiEndpoint = "/api/x",
            Status = EntityStatus.Published,
            IsEnabled = true
        };
        entity.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "Name",
            DataType = FieldDataType.String,
            IsRequired = false
        });
        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var impact = await evaluator.EvaluateImpactAsync(entity.Id, new List<FieldMetadata>());

        impact.AffectedRows.Should().Be(0);
        impact.Operations.Should().Contain(op => op.OperationType == MigrationOperationType.DropColumn && op.FieldName == "Name");
    }

    [Fact]
    public async Task EvaluateImpactAsync_WhenPublished_WithMixedChanges_ShouldDetectOperationsAndSql()
    {
        await using var db = CreateContext();
        var evaluator = new DataMigrationEvaluator(db, NullLogger<DataMigrationEvaluator>.Instance);

        var entity = new EntityDefinition
        {
            EntityName = "MixedImpact",
            EntityRoute = "mixed-impact",
            FullTypeName = "X",
            Namespace = "X",
            ApiEndpoint = "/api/x",
            Status = EntityStatus.Published,
            IsEnabled = true
        };

        entity.Fields.AddRange(new[]
        {
            new FieldMetadata { EntityDefinitionId = entity.Id, PropertyName = "Old", DataType = FieldDataType.String, IsRequired = false },
            new FieldMetadata { EntityDefinitionId = entity.Id, PropertyName = "Name", DataType = FieldDataType.String, Length = 100, IsRequired = false },
            new FieldMetadata { EntityDefinitionId = entity.Id, PropertyName = "Amount", DataType = FieldDataType.Decimal, Precision = 18, Scale = 2, IsRequired = false },
            new FieldMetadata { EntityDefinitionId = entity.Id, PropertyName = "Code", DataType = FieldDataType.String, Length = 50, IsRequired = false }
        });

        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();

        var table = entity.DefaultTableName.Replace("\"", "\"\"");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE \"" + table + "\" (\"Id\" INTEGER NOT NULL);");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO \"" + table + "\" (\"Id\") VALUES (1);");

        var newFields = new List<FieldMetadata>
        {
            new() { PropertyName = "Name", DataType = FieldDataType.String, Length = 50, IsRequired = false },
            new() { PropertyName = "Amount", DataType = FieldDataType.String, IsRequired = false },
            new() { PropertyName = "Code", DataType = FieldDataType.Int32, IsRequired = false },
            new() { PropertyName = "CreatedAt", DataType = FieldDataType.DateTime, IsRequired = true, DefaultValue = "NOW" }
        };

        var impact = await evaluator.EvaluateImpactAsync(entity.Id, newFields);

        impact.AffectedRows.Should().Be(1);
        impact.Operations.Should().Contain(op => op.OperationType == MigrationOperationType.DropColumn && op.FieldName == "Old");
        impact.Operations.Should().Contain(op => op.OperationType == MigrationOperationType.AddColumn && op.FieldName == "CreatedAt" && op.SqlPreview!.Contains("CURRENT_TIMESTAMP"));
        impact.Operations.Should().Contain(op => op.OperationType == MigrationOperationType.AlterColumn && op.FieldName == "Name" && op.MayLoseData);
        impact.Operations.Should().Contain(op => op.OperationType == MigrationOperationType.AlterColumn && op.FieldName == "Amount" && op.RequiresConversion);
        impact.Operations.Should().Contain(op => op.OperationType == MigrationOperationType.AlterColumn && op.FieldName == "Code" && op.MayLoseData);
        impact.RiskLevel.Should().Be(RiskLevel.Critical);
    }
}
