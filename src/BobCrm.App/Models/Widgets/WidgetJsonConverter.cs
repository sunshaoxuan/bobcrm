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
        if (reader.TokenType == JsonTokenType.Null) return null;

        // 避免在已经消耗 reader 的情况下再次解析
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // 读取 Type 属性
        if (!root.TryGetProperty("Type", out var typeProperty) &&
            !root.TryGetProperty("type", out typeProperty))
        {
            throw new JsonException("Widget JSON 必须包含 'Type' 属性。源 JSON: " + root.GetRawText());
        }

        var widgetType = typeProperty.GetString();
        if (string.IsNullOrWhiteSpace(widgetType))
        {
            throw new JsonException("Widget Type 不能为空");
        }

        // 获取具体子类类型
        Type subType;
        try 
        {
            var def = WidgetRegistry.GetDefinition(widgetType);
            var tempInstance = def.Factory();
            subType = tempInstance.GetType();
        }
        catch (Exception ex)
        {
            throw new JsonException($"解析组件时出错。Type: '{widgetType}', JSON: {root.GetRawText()}", ex);
        }

        // 反序列化为具体类型
        // 由于 CanConvert 仅对 DraggableWidget 基类返回 true，
        // 反序列化具体子类型（如 TabContainerWidget）时不会再次触发此 Converter 的 Read 方法，
        // 从而避免了无限递归。同时，这允许子类中的 List<DraggableWidget> 属性正确使用此 Converter。
        var json = root.GetRawText();
        var result = JsonSerializer.Deserialize(json, subType, options) as DraggableWidget;
        
        if (result != null)
        {
            // 确保 Type 属性与 Registry 一致
            result.Type = widgetType;
        }

        return result;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        // 关键：仅对基类 DraggableWidget 生效。
        // 对于具体子类（如 TabContainerWidget），使用默认反序列化或其自身的转换器。
        return typeToConvert == typeof(DraggableWidget);
    }

    public override void Write(Utf8JsonWriter writer, DraggableWidget value, JsonSerializerOptions options)
    {
        // 序列化时使用实际类型
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
