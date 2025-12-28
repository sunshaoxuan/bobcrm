using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Base.Models.Metadata;
using BobCrm.Api.Contracts.Requests.DataSet;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Tests;

public class DataSetServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateDataSet_WhenTypeAndConfigValid()
    {
        await using var db = CreateContext();
        db.DataSourceTypes.Add(new DataSourceTypeEntry { Code = "entity", HandlerType = "tests" });
        await db.SaveChangesAsync();

        var handler = new StubDataSourceHandler("entity")
        {
            Validate = _ => Task.FromResult(DataSourceValidationResult.Success())
        };
        var service = new DataSetService(db, NullLogger<DataSetService>.Instance, new[] { handler });

        var result = await service.CreateAsync(new CreateDataSetRequest
        {
            Code = "DS_CUSTOMERS",
            Name = "Customers",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "客户", ["ja"] = "顧客", ["en"] = "Customers" },
            Description = new Dictionary<string, string?> { ["zh"] = "desc" },
            DataSourceTypeCode = "entity",
            ConfigJson = """{"entityType":"customer"}""",
            FieldsJson = "[]",
            DefaultPageSize = 50,
            IsEnabled = true,
            CreatedBy = "tester"
        });

        result.Id.Should().BeGreaterThan(0);
        result.Code.Should().Be("DS_CUSTOMERS");
        result.DataSourceTypeCode.Should().Be("entity");
        result.DefaultPageSize.Should().Be(50);

        var stored = await db.DataSets.AsNoTracking().SingleAsync(x => x.Id == result.Id);
        stored.Code.Should().Be("DS_CUSTOMERS");
        stored.CreatedBy.Should().Be("tester");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenDuplicateCode()
    {
        await using var db = CreateContext();
        db.DataSourceTypes.Add(new DataSourceTypeEntry { Code = "entity", HandlerType = "tests" });
        db.DataSets.Add(new DataSet { Code = "DS1", Name = "n1", DataSourceTypeCode = "entity" });
        await db.SaveChangesAsync();

        var service = new DataSetService(db, NullLogger<DataSetService>.Instance, new[] { new StubDataSourceHandler("entity") });

        var act = async () => await service.CreateAsync(new CreateDataSetRequest
        {
            Code = "DS1",
            Name = "n2",
            DataSourceTypeCode = "entity"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DataSet with code 'DS1' already exists");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenDataSourceTypeMissing()
    {
        await using var db = CreateContext();
        var service = new DataSetService(db, NullLogger<DataSetService>.Instance, new[] { new StubDataSourceHandler("entity") });

        var act = async () => await service.CreateAsync(new CreateDataSetRequest
        {
            Code = "DS1",
            Name = "n1",
            DataSourceTypeCode = "entity"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DataSourceType 'entity' not found");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenConfigInvalid()
    {
        await using var db = CreateContext();
        db.DataSourceTypes.Add(new DataSourceTypeEntry { Code = "entity", HandlerType = "tests" });
        await db.SaveChangesAsync();

        var handler = new StubDataSourceHandler("entity")
        {
            Validate = _ => Task.FromResult(DataSourceValidationResult.Failure("bad config"))
        };
        var service = new DataSetService(db, NullLogger<DataSetService>.Instance, new[] { handler });

        var act = async () => await service.CreateAsync(new CreateDataSetRequest
        {
            Code = "DS1",
            Name = "n1",
            DataSourceTypeCode = "entity",
            ConfigJson = "{}"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Configuration validation failed: bad config");
    }

    [Fact]
    public async Task UpdateAsync_ShouldValidateConfigAndPersistChanges()
    {
        await using var db = CreateContext();
        db.DataSets.Add(new DataSet
        {
            Code = "DS1",
            Name = "n1",
            DataSourceTypeCode = "entity",
            ConfigJson = "{}",
            DefaultPageSize = 20,
            IsEnabled = true
        });
        await db.SaveChangesAsync();

        var handler = new StubDataSourceHandler("entity")
        {
            Validate = cfg => Task.FromResult(cfg.Contains("bad", StringComparison.OrdinalIgnoreCase)
                ? DataSourceValidationResult.Failure("bad")
                : DataSourceValidationResult.Success())
        };
        var service = new DataSetService(db, NullLogger<DataSetService>.Instance, new[] { handler });

        var updated = await service.UpdateAsync(1, new UpdateDataSetRequest
        {
            Name = "n2",
            ConfigJson = """{"ok":true}""",
            FieldsJson = "[1]",
            DefaultPageSize = 99,
            UpdatedBy = "tester",
            IsEnabled = false
        });

        updated.Name.Should().Be("n2");
        updated.IsEnabled.Should().BeFalse();
        updated.DefaultPageSize.Should().Be(99);

        var stored = await db.DataSets.AsNoTracking().SingleAsync(x => x.Id == 1);
        stored.UpdatedBy.Should().Be("tester");
        stored.FieldsJson.Should().Be("[1]");

        var act = async () => await service.UpdateAsync(1, new UpdateDataSetRequest
        {
            Name = "n3",
            ConfigJson = "bad",
            UpdatedBy = "tester2"
        });
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Configuration validation failed: bad");
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenSystemDataSet()
    {
        await using var db = CreateContext();
        db.DataSets.Add(new DataSet
        {
            Code = "SYS",
            Name = "sys",
            DataSourceTypeCode = "entity",
            IsSystem = true
        });
        await db.SaveChangesAsync();

        var service = new DataSetService(db, NullLogger<DataSetService>.Instance, new[] { new StubDataSourceHandler("entity") });
        var act = async () => await service.DeleteAsync(1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete system DataSet");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseDefaultPagingAndSortAndPassRuntimeParameters()
    {
        await using var db = CreateContext();
        db.DataSets.Add(new DataSet
        {
            Code = "DS1",
            Name = "n1",
            DataSourceTypeCode = "entity",
            DefaultPageSize = 50,
            DefaultSortField = "Name",
            DefaultSortDirection = "desc",
            IsEnabled = true
        });
        await db.SaveChangesAsync();

        DataSourceExecutionRequest? captured = null;
        var handler = new StubDataSourceHandler("entity")
        {
            Execute = (req, _) =>
            {
                captured = req;
                return Task.FromResult(new DataSourceExecutionResult
                {
                    DataJson = """[{"id":1}]""",
                    TotalCount = 101,
                    Page = req.Page,
                    PageSize = req.PageSize,
                    TotalPages = 3,
                    AppliedScopes = ["org:1"]
                });
            }
        };
        var service = new DataSetService(db, NullLogger<DataSetService>.Instance, new[] { handler });

        var response = await service.ExecuteAsync(1, new DataSetExecutionRequest
        {
            DataSetId = 999,
            Page = 2,
            PageSize = null,
            SortField = null,
            SortDirection = null,
            RuntimeParametersJson = """{"q":"abc"}"""
        });

        captured.Should().NotBeNull();
        captured!.Page.Should().Be(2);
        captured.PageSize.Should().Be(50);
        captured.SortField.Should().Be("Name");
        captured.SortDirection.Should().Be("desc");
        captured.RuntimeParametersJson.Should().Be("""{"q":"abc"}""");

        response.DataSetId.Should().Be(1);
        response.DataSetCode.Should().Be("DS1");
        response.TotalCount.Should().Be(101);
        response.Page.Should().Be(2);
        response.PageSize.Should().Be(50);
        response.TotalPages.Should().Be(3);
        response.AppliedScopes.Should().BeEquivalentTo(["org:1"]);
        response.DataJson.Should().Be("""[{"id":1}]""");
        response.ExecutionTimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRespectExplicitPagingAndSortOverrides()
    {
        await using var db = CreateContext();
        db.DataSets.Add(new DataSet
        {
            Code = "DS1",
            Name = "n1",
            DataSourceTypeCode = "entity",
            DefaultPageSize = 50,
            DefaultSortField = "Name",
            DefaultSortDirection = "desc",
            IsEnabled = true
        });
        await db.SaveChangesAsync();

        DataSourceExecutionRequest? captured = null;
        var handler = new StubDataSourceHandler("entity")
        {
            Execute = (req, _) =>
            {
                captured = req;
                return Task.FromResult(new DataSourceExecutionResult
                {
                    DataJson = "[]",
                    TotalCount = 0,
                    Page = req.Page,
                    PageSize = req.PageSize,
                    TotalPages = 0
                });
            }
        };
        var service = new DataSetService(db, NullLogger<DataSetService>.Instance, new[] { handler });

        await service.ExecuteAsync(1, new DataSetExecutionRequest
        {
            DataSetId = 1,
            Page = 1,
            PageSize = 10,
            SortField = "CreatedAt",
            SortDirection = "asc"
        });

        captured.Should().NotBeNull();
        captured!.PageSize.Should().Be(10);
        captured.SortField.Should().Be("CreatedAt");
        captured.SortDirection.Should().Be("asc");
    }

    [Fact]
    public async Task GetFieldsAsync_ShouldDelegateToHandler()
    {
        await using var db = CreateContext();
        db.DataSets.Add(new DataSet
        {
            Code = "DS1",
            Name = "n1",
            DataSourceTypeCode = "entity",
            ConfigJson = """{"x":1}"""
        });
        await db.SaveChangesAsync();

        var handler = new StubDataSourceHandler("entity")
        {
            GetFields = (cfg, _) =>
            {
                cfg.Should().Be("""{"x":1}""");
                return Task.FromResult(new List<DataSourceFieldMetadata>
                {
                    new() { Name = "id", DataType = "int", Sortable = true },
                    new() { Name = "name", DataType = "string", Filterable = true }
                });
            }
        };
        var service = new DataSetService(db, NullLogger<DataSetService>.Instance, new[] { handler });

        var fields = await service.GetFieldsAsync(1);
        fields.Select(x => x.Name).Should().ContainInOrder("id", "name");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyEnabledOrderedByName()
    {
        await using var db = CreateContext();
        db.DataSets.AddRange(
            new DataSet { Code = "A", Name = "Z", DataSourceTypeCode = "entity", IsEnabled = true },
            new DataSet { Code = "B", Name = "A", DataSourceTypeCode = "entity", IsEnabled = true },
            new DataSet { Code = "C", Name = "M", DataSourceTypeCode = "entity", IsEnabled = false });
        await db.SaveChangesAsync();

        var service = new DataSetService(db, NullLogger<DataSetService>.Instance, new[] { new StubDataSourceHandler("entity") });
        var all = await service.GetAllAsync();

        all.Should().HaveCount(2);
        all.Select(x => x.Name).Should().ContainInOrder("A", "Z");
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class StubDataSourceHandler : IDataSourceHandler
    {
        public string TypeCode { get; }

        public Func<DataSourceExecutionRequest, CancellationToken, Task<DataSourceExecutionResult>>? Execute { get; init; }
        public Func<string, Task<DataSourceValidationResult>>? Validate { get; init; }
        public Func<string, CancellationToken, Task<List<DataSourceFieldMetadata>>>? GetFields { get; init; }

        public StubDataSourceHandler(string typeCode)
        {
            TypeCode = typeCode;
        }

        public Task<DataSourceExecutionResult> ExecuteAsync(DataSourceExecutionRequest request, CancellationToken cancellationToken = default)
        {
            if (Execute != null)
            {
                return Execute(request, cancellationToken);
            }

            return Task.FromResult(new DataSourceExecutionResult
            {
                DataJson = "[]",
                TotalCount = 0,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = 0
            });
        }

        public Task<DataSourceValidationResult> ValidateConfigAsync(string configJson)
        {
            if (Validate != null)
            {
                return Validate(configJson);
            }

            return Task.FromResult(DataSourceValidationResult.Success());
        }

        public Task<List<DataSourceFieldMetadata>> GetFieldsAsync(string configJson, CancellationToken cancellationToken = default)
        {
            if (GetFields != null)
            {
                return GetFields(configJson, cancellationToken);
            }

            return Task.FromResult(new List<DataSourceFieldMetadata>());
        }
    }
}

