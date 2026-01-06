using System.Text.Json;
using BobCrm.App.Services.Runtime;

namespace BobCrm.App.Tests;

public class LegacyLayoutParserTests
{
    [Fact]
    public void ParseLayoutFromJson_ParsesItemsObject()
    {
        var json = """
                   {
                     "items": {
                       "a": { "type": "textbox", "label": "A", "dataField": "fieldA", "w": 12, "visible": true, "newLine": true },
                       "b": { "type": "text", "label": "B", "dataField": "fieldB", "Width": 24, "visible": false }
                     }
                   }
                   """;

        using var doc = JsonDocument.Parse(json);
        var parser = new LegacyLayoutParser();
        var widgets = parser.ParseLayoutFromJson(doc.RootElement);

        Assert.Equal(2, widgets.Count);
        Assert.Contains(widgets, w => w.Type == "textbox" && w.DataField == "fieldA" && w.Width == 12 && w.Visible && w.NewLine);
        Assert.Contains(widgets, w => w.Type == "text" && w.DataField == "fieldB" && w.Width == 24 && !w.Visible);
    }

    [Fact]
    public void ParseLayoutFromJson_ParsesItemPrefixProperties()
    {
        var json = """
                   {
                     "item_1": { "type": "textbox", "label": "A", "dataField": "fieldA", "w": 12, "visible": true },
                     "item_2": { "type": "textbox", "label": "B", "dataField": "fieldB", "w": 24, "visible": true }
                   }
                   """;

        using var doc = JsonDocument.Parse(json);
        var parser = new LegacyLayoutParser();
        var widgets = parser.ParseLayoutFromJson(doc.RootElement);

        Assert.Equal(2, widgets.Count);
        Assert.Contains(widgets, w => w.DataField == "fieldA" && w.Width == 12);
        Assert.Contains(widgets, w => w.DataField == "fieldB" && w.Width == 24);
    }
}

