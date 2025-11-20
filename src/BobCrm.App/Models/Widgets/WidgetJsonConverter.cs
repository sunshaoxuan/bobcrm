using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BobCrm.App.Models.Widgets;

/// <summary>
/// Widget JSON 转换器 - 根据 Type 属性反序列化为正确的 Widget 子类
/// </summary>
public class WidgetJsonConverter : JsonConverter<DraggableWidget>
{
    public override DraggableWidget? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // 读取 Type 属性来确定 widget 类型
        if (!root.TryGetProperty("Type", out var typeProperty) &&
            !root.TryGetProperty("type", out typeProperty))
        {
            throw new JsonException("Widget JSON must have a 'Type' property");
        }

        var widgetType = typeProperty.GetString();
        if (string.IsNullOrWhiteSpace(widgetType))
        {
            throw new JsonException("Widget Type cannot be empty");
        }

        // 根据 type 创建对应的 widget 实例
        var widget = CreateWidgetByType(widgetType);
        if (widget == null)
        {
            throw new JsonException($"Unknown widget type: {widgetType}");
        }

        // 反序列化到具体类型
        var json = root.GetRawText();
        var deserializedWidget = JsonSerializer.Deserialize(json, widget.GetType(), options) as DraggableWidget;

        return deserializedWidget;
    }

    public override void Write(Utf8JsonWriter writer, DraggableWidget value, JsonSerializerOptions options)
    {
        // 序列化时使用实际类型
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }

    private static DraggableWidget? CreateWidgetByType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            // Basic widgets
            "textbox" => new TextboxWidget(),
            "textarea" => new TextareaWidget(),
            "number" => new NumberWidget(),
            "checkbox" => new CheckboxWidget(),
            "select" => new SelectWidget(),
            "radio" => new RadioWidget(),
            "listbox" => new ListboxWidget(),
            "calendar" => new CalendarWidget(),
            "button" => new ButtonWidget(),
            "label" => new LabelWidget(),

            // Layout widgets
            "panel" => new PanelWidget(),
            "section" => new SectionWidget(),
            "grid" => new GridWidget(),
            "frame" => new FrameWidget(),
            "tab" => new TabWidget(),
            "tabcontainer" => new TabContainerWidget(),

            // Data widgets
            "datagrid" => new DataGridWidget(),
            "orgtree" => new OrganizationTreeWidget(),
            "permtree" => new RolePermissionTreeWidget(),
            "userrole" => new UserRoleAssignmentWidget(),

            _ => null
        };
    }
}
