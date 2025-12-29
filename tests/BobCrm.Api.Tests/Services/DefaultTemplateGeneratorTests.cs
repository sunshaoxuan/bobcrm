using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Base.Models.Metadata;
using BobCrm.Api.Services;
using BobCrm.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace BobCrm.Api.Tests.Services;

public class DefaultTemplateGeneratorTests : IDisposable
{
    private readonly BobCrm.Api.Infrastructure.AppDbContext _db;
    private readonly Mock<ILogger<DefaultTemplateGenerator>> _loggerMock;
    private readonly DefaultTemplateGenerator _generator;

    public DefaultTemplateGeneratorTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext(nameof(DefaultTemplateGeneratorTests));
        _loggerMock = new Mock<ILogger<DefaultTemplateGenerator>>();
        _generator = new DefaultTemplateGenerator(_db, _loggerMock.Object);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task GenerateAsync_SimpleEntity_ShouldReturnTemplate()
    {
        var entity = new EntityDefinition
        {
            EntityName = "TestEntity",
            EntityRoute = "tests",
            Fields = new List<FieldMetadata>
            {
                new() { PropertyName = "Name", DataType = FieldDataType.String.ToString() }
            }
        };

        var result = await _generator.GenerateAsync(entity, FormTemplateUsageType.Detail);

        Assert.NotNull(result);
        Assert.Equal("TEMPLATE_NAME_DETAILVIEW_TESTS", result.Name);
        Assert.Equal("tests", result.EntityType);
        Assert.Contains("Name", result.LayoutJson);
    }

    [Fact]
    public async Task EnsureTemplatesAsync_ShouldCreateAllViewStates()
    {
        var entity = new EntityDefinition
        {
            EntityName = "ComplexEntity",
            EntityRoute = "complex",
            Fields = new List<FieldMetadata>
            {
                new() { PropertyName = "Code", DataType = FieldDataType.String.ToString() },
                new() { PropertyName = "Amount", DataType = FieldDataType.Decimal.ToString() }
            }
        };

        var result = await _generator.EnsureTemplatesAsync(entity);

        Assert.Equal(3, result.Created.Count); // List, DetailView, DetailEdit
        Assert.True(result.Templates.ContainsKey("List"));
        Assert.True(result.Templates.ContainsKey("DetailView"));
        Assert.True(result.Templates.ContainsKey("DetailEdit"));
        
        using var scope = _db.Database.BeginTransaction(); // Check DB
        var savedTemplates = await _db.FormTemplates.Where(t => t.EntityType == "complex").ToListAsync();
        Assert.Equal(3, savedTemplates.Count);
    }

    [Fact]
    public async Task EnsureTemplatesAsync_WhenFieldsChanged_ShouldUpdateLayout()
    {
        var entity = new EntityDefinition
        {
            EntityName = "Updatable",
            EntityRoute = "updatable",
            Fields = new List<FieldMetadata>
            {
                new() { PropertyName = "F1", DataType = FieldDataType.String.ToString() }
            }
        };

        // First run
        await _generator.EnsureTemplatesAsync(entity);

        // Add field
        entity.Fields.Add(new FieldMetadata { PropertyName = "F2", DataType = FieldDataType.Int32.ToString(), IsRequired = true });

        // Second run
        var result = await _generator.EnsureTemplatesAsync(entity);

        Assert.True(result.Updated.Count > 0);
        Assert.Contains("F2", result.Templates["DetailView"]!.LayoutJson);
    }

    [Fact]
    public async Task EnsureTemplatesAsync_Customer360_ShouldInjectRelatedTabs()
    {
        var customer = new EntityDefinition { EntityName = "Customer", EntityRoute = "customer", Fields = new List<FieldMetadata> { new() { PropertyName = "Name" } } };
        var contact = new EntityDefinition { EntityName = "Contact", EntityRoute = "contacts", Fields = new List<FieldMetadata> { new() { PropertyName = "Phone" } } };
        
        _db.EntityDefinitions.AddRange(customer, contact);
        await _db.SaveChangesAsync();

        var result = await _generator.EnsureTemplatesAsync(customer);
        var detailTemplate = result.Templates["DetailView"];

        Assert.Contains("tabbox", detailTemplate!.LayoutJson);
        Assert.Contains("Contact", detailTemplate.LayoutJson);
    }

    [Fact]
    public async Task GenerateAsync_ListUsage_ShouldGenerateDataGrid()
    {
        var entity = new EntityDefinition
        {
            EntityName = "ListTest",
            EntityRoute = "list-test",
            Fields = new List<FieldMetadata>
            {
                new() { PropertyName = "Col1", DataType = "String" },
                new() { PropertyName = "Col2", DataType = "Int32", SortOrder = 1 }
            }
        };

        var result = await _generator.GenerateAsync(entity, FormTemplateUsageType.List);
        
        // Use JsonDocument to verify structure
        using var doc = JsonDocument.Parse(result.LayoutJson!);
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        var grid = root[0];
        Assert.Equal("datagrid", grid.GetProperty("type").GetString());
        
        var columnsJson = grid.GetProperty("columnsJson").GetString();
        Assert.Contains("col1", columnsJson); // camelCase property name logic in generator?
        // Actually generator uses ToLowerInvariant for list columns: field: f.PropertyName?.ToLowerInvariant()
    }
    
    [Theory]
    [InlineData(FieldDataType.String, "text")]
    [InlineData(FieldDataType.Boolean, "checkbox")]
    [InlineData(FieldDataType.Int32, "number")]
    [InlineData(FieldDataType.Date, "date")]
    [InlineData(FieldDataType.DateTime, "date")]
    [InlineData(FieldDataType.Enum, "enumselector")]
    public async Task WidgetTypeResolution_ShouldBeCorrect(string type, string expectedWidget)
    {
        var entity = new EntityDefinition
        {
            EntityName = "WidgetTest",
            Fields = new List<FieldMetadata>
            {
                new() { PropertyName = "Field1", DataType = type }
            }
        };

        var result = await _generator.GenerateAsync(entity, FormTemplateUsageType.Detail);
        
        Assert.Contains($"\"type\":\"{expectedWidget}\"", result.LayoutJson);
    }
}
