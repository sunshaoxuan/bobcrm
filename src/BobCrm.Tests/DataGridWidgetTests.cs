using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using BobCrm.App.Models.Designer;
using BobCrm.App.Models.Widgets;
using Xunit;

namespace BobCrm.Tests;

public class DataGridWidgetTests
{
    [Fact]
    public void PropertyMetadata_UsesColumnDefinitionEditor()
    {
        var widget = new DataGridWidget();
        var props = widget.GetPropertyMetadata();
        var columnsMeta = props.FirstOrDefault(p => p.PropertyPath == "ColumnsJson");

        Assert.NotNull(columnsMeta);
        Assert.Equal(PropertyEditorType.ColumnDefinition, columnsMeta!.EditorType);
    }

    [Fact]
    public void DataGridColumn_SerializesWithCamelCase()
    {
        var cols = new List<DataGridColumn>
        {
            new()
            {
                Field = "code",
                Label = "Code",
                Width = 120,
                Visible = false,
                Sortable = true,
                Align = "center",
                Format = "N2"
            }
        };

        var json = JsonSerializer.Serialize(cols, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Contains("\"field\":\"code\"", json);
        Assert.Contains("\"label\":\"Code\"", json);
        Assert.Contains("\"width\":120", json);
        Assert.Contains("\"visible\":false", json);
        Assert.Contains("\"align\":\"center\"", json);
        Assert.Contains("\"format\":\"N2\"", json);
    }
}
