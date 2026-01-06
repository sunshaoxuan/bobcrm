using System.Text.Json;
using BobCrm.App.Models.Widgets;
using BobCrm.App.Services.Widgets;

namespace BobCrm.App.Services.Runtime;

public sealed class LegacyLayoutParser
{
    public List<DraggableWidget> ParseLayoutFromJson(JsonElement root)
    {
        var widgets = new List<DraggableWidget>();

        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var item in itemsElement.EnumerateObject())
                {
                    if (item.Value.ValueKind == JsonValueKind.Object)
                    {
                        var widget = ParseWidgetFromJson(item.Value);
                        if (widget != null)
                        {
                            widgets.Add(widget);
                        }
                    }
                }
            }

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name.StartsWith("item_") && prop.Value.ValueKind == JsonValueKind.Object)
                {
                    var widget = ParseWidgetFromJson(prop.Value);
                    if (widget != null)
                    {
                        widgets.Add(widget);
                    }
                }
            }
        }

        return widgets
            .OrderBy(w =>
            {
                if (w.ExtendedProperties?.TryGetValue("order", out var orderObj) == true)
                {
                    return Convert.ToInt32(orderObj);
                }

                return 0;
            })
            .ToList();
    }

    private static DraggableWidget? ParseWidgetFromJson(JsonElement element)
    {
        if (!element.TryGetProperty("type", out var typeElement))
        {
            return null;
        }

        var type = typeElement.GetString() ?? string.Empty;
        var label = element.TryGetProperty("label", out var labelElement) ? labelElement.GetString() : string.Empty;

        var widget = WidgetRegistry.Create(type, label ?? string.Empty);

        if (element.TryGetProperty("dataField", out var dataFieldElement))
        {
            widget.DataField = dataFieldElement.GetString();
        }

        if (element.TryGetProperty("Width", out var widthElement))
        {
            widget.Width = widthElement.GetInt32();
        }
        else if (element.TryGetProperty("w", out var wElement) && wElement.ValueKind == JsonValueKind.Number)
        {
            widget.Width = wElement.GetInt32();
        }

        if (element.TryGetProperty("Height", out var heightElement))
        {
            widget.Height = heightElement.GetInt32();
        }

        if (element.TryGetProperty("visible", out var visibleElement))
        {
            widget.Visible = visibleElement.GetBoolean();
        }

        if (element.TryGetProperty("newLine", out var newLineElement))
        {
            widget.NewLine = newLineElement.GetBoolean();
        }

        return widget;
    }
}

