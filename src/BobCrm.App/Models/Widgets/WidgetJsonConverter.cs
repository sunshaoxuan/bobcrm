using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using BobCrm.App.Services.Widgets;

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
            throw new JsonException("Widget JSON 必须包含 'Type' 属性");
        }

        var widgetType = typeProperty.GetString();
        if (string.IsNullOrWhiteSpace(widgetType))
        {
            throw new JsonException("Widget Type 不能为空");
        }

        // 使用 Registry 创建实例
        DraggableWidget widget;
        try 
        {
            var def = WidgetRegistry.GetDefinition(widgetType);
            widget = def.Factory();
        }
        catch (InvalidOperationException ex)
        {
            throw new JsonException($"未知的 Widget 类型: '{widgetType}'。请确保该类型已在 WidgetRegistry 中注册。", ex);
        }

        // 反序列化到具体类型
        var json = root.GetRawText();
        // 注意：这里需要避免无限递归。
        // 因为 WidgetRegistry.Create 出来的对象是具体类型（如 TextboxWidget），
        // 且这些具体类型本身没有标记 [JsonConverter]，所以调用 Deserialize 不会再次触发这个 Converter（除非 options 里强制指定了）。
        // 如果 options 里包含此 Converter，我们需要小心。
        // 但通常 converter 是针对基类的。Deserialize<T> T 是具体子类时，通常不应用针对基类的 Converter（除非是多态反序列化场景）。
        // 
        // 为了安全起见，我们可以创建一个新的 Options 副本，去除 Converter，或者确信 System.Text.Json 的行为。
        // 实际测试表明，如果 T 是具体子类，且没有直接在类上标记 Converter，JsonSerializer 不会使用基类的 Converter。
        var deserializedWidget = JsonSerializer.Deserialize(json, widget.GetType(), options) as DraggableWidget;

        return deserializedWidget;
    }

    public override void Write(Utf8JsonWriter writer, DraggableWidget value, JsonSerializerOptions options)
    {
        // 序列化时使用实际类型
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
