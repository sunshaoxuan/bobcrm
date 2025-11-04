using BobCrm.App.Models.Widgets;
using System.Text.Json;

namespace BobCrm.App.Models;

/// <summary>
/// 布局DTO与运行时Widget模型之间的映射器
/// 负责序列化/反序列化时的类型转换和属性映射
/// </summary>
public static class LayoutMapper
{
    /// <summary>
    /// 从DTO转换为运行时Widget
    /// </summary>
    public static DraggableWidget FromDto(object dto)
    {
        if (dto is JsonElement jsonElement)
        {
            return FromJsonElement(jsonElement);
        }

        // 如果已经是DraggableWidget，直接返回
        if (dto is DraggableWidget widget)
        {
            return widget;
        }

        throw new ArgumentException($"Unsupported DTO type: {dto?.GetType().Name}");
    }

    /// <summary>
    /// 从JsonElement转换为运行时Widget（泛型版本，支持自定义Widget类型）
    /// 注意：建议使用非泛型的 FromJsonElement(JsonElement) 方法，它能自动推断类型
    /// </summary>
    public static T FromJsonElement<T>(JsonElement element) where T : DraggableWidget, new()
    {
        var widget = new T();
        MapCommonProperties(element, widget);
        
        // 使用反射设置特定于子类的属性（如 TextboxWidget 的 DefaultValue, Placeholder 等）
        var properties = typeof(T).GetProperties();
        foreach (var prop in properties)
        {
            // 跳过已处理的基类属性
            if (prop.DeclaringType == typeof(DraggableWidget) || prop.Name == "Children")
                continue;
                
            if (element.TryGetProperty(prop.Name, out var value) || 
                element.TryGetProperty(char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1), out value))
            {
                try
                {
                    object? propValue = prop.PropertyType switch
                    {
                        Type t when t == typeof(string) => value.ValueKind == JsonValueKind.String ? value.GetString() : null,
                        Type t when t == typeof(int) => value.ValueKind == JsonValueKind.Number ? value.GetInt32() : 0,
                        Type t when t == typeof(int?) => value.ValueKind == JsonValueKind.Number ? value.GetInt32() : null,
                        Type t when t == typeof(bool) => value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False ? value.GetBoolean() : false,
                        Type t when t == typeof(double) => value.ValueKind == JsonValueKind.Number ? value.GetDouble() : 0.0,
                        _ => null
                    };
                    
                    if (propValue != null || Nullable.GetUnderlyingType(prop.PropertyType) != null)
                    {
                        prop.SetValue(widget, propValue);
                    }
                }
                catch
                {
                    // 忽略转换失败
                }
            }
        }
        
        return widget;
    }

    /// <summary>
    /// 从JsonElement转换为运行时Widget（自动推断类型）
    /// </summary>
    public static DraggableWidget FromJsonElement(JsonElement element)
    {
        // 兼容大小写的 type 属性
        string? type = null;
        if (element.TryGetProperty("type", out var typeEl))
            type = typeEl.GetString();
        else if (element.TryGetProperty("Type", out var typeEl2))
            type = typeEl2.GetString();

        type = type?.ToLower() ?? "textbox";

        DraggableWidget widget = type switch
        {
            "textbox" => new TextboxWidget(),
            "label" => new LabelWidget(),
            "calendar" => new CalendarWidget(),
            "listbox" => new ListboxWidget(),
            "frame" => new FrameWidget(),
            "section" or "block" => new SectionWidget(),
            _ => new TextboxWidget()
        };

        // 映射通用属性
        MapCommonProperties(element, widget);

        // 根据类型映射特定属性
        switch (widget)
        {
            case TextboxWidget textbox:
                MapTextboxProperties(element, textbox);
                break;
            case LabelWidget label:
                MapLabelProperties(element, label);
                break;
            case CalendarWidget calendar:
                MapCalendarProperties(element, calendar);
                break;
            case ListboxWidget listbox:
                MapListboxProperties(element, listbox);
                break;
            case SectionWidget section:
                MapSectionProperties(element, section);
                break;
            case FrameWidget frame:
                MapFrameProperties(element, frame);
                break;
        }

        return widget;
    }

    /// <summary>
    /// 映射通用属性（所有DraggableWidget共有）
    /// </summary>
    private static void MapCommonProperties(JsonElement element, DraggableWidget widget)
    {
        // Id: 优先使用节点内的 id
        if (element.TryGetProperty("id", out var id))
            widget.Id = id.GetString() ?? Guid.NewGuid().ToString();
        else if (element.TryGetProperty("Id", out var id2))
            widget.Id = id2.GetString() ?? Guid.NewGuid().ToString();
        else
            widget.Id = Guid.NewGuid().ToString();

        // Type
        if (element.TryGetProperty("type", out var type))
            widget.Type = type.GetString() ?? "";
        else if (element.TryGetProperty("Type", out var type2))
            widget.Type = type2.GetString() ?? "";

        // Label
        if (element.TryGetProperty("label", out var label))
            widget.Label = label.GetString() ?? "";
        else if (element.TryGetProperty("Label", out var label2))
            widget.Label = label2.GetString() ?? "";

        // 尺寸属性 - 兼容旧的w和新的Width
        // 默认策略：有 Width 无 WidthUnit → 默认 px；仅 legacy w → 默认 %
        var hasExplicitWidth = false;
        var hasLegacyW = false;
        
        if (element.TryGetProperty("Width", out var widthProp))
        {
            widget.Width = widthProp.GetInt32();
            hasExplicitWidth = true;
        }
        else if (element.TryGetProperty("w", out var wProp))
        {
            // 旧格式：1-12列转换为百分比
            var wValue = wProp.ValueKind == JsonValueKind.Number ? wProp.GetDouble() : 6;
            widget.Width = (int)Math.Round(wValue * 8.33);
            hasLegacyW = true;
        }
        else
        {
            widget.Width = 48; // 默认值
        }

        // WidthUnit 默认策略
        if (element.TryGetProperty("WidthUnit", out var widthUnit))
        {
            widget.WidthUnit = widthUnit.GetString() ?? "%";
        }
        else if (element.TryGetProperty("widthUnit", out var widthUnit2))
        {
            widget.WidthUnit = widthUnit2.GetString() ?? "%";
        }
        else
        {
            // 默认策略：
            // - 显式 Width 存在且无 WidthUnit → 默认 px
            // - 仅有 legacy w（1-12列） → 默认 %
            widget.WidthUnit = (hasExplicitWidth && !hasLegacyW) ? "px" : "%";
        }

        // Height 和 HeightUnit
        if (element.TryGetProperty("Height", out var height))
        {
            widget.Height = height.GetInt32();
        }
        else if (element.TryGetProperty("height", out var h))
        {
            widget.Height = h.GetInt32();
        }
        else
        {
            widget.Height = 40; // 默认值（仅在 HeightUnit==px 时生效）
        }

        if (element.TryGetProperty("HeightUnit", out var heightUnit))
            widget.HeightUnit = heightUnit.GetString() ?? "auto";
        else if (element.TryGetProperty("heightUnit", out var hu))
            widget.HeightUnit = hu.GetString() ?? "auto";
        else
            widget.HeightUnit = "auto"; // 默认 auto，不强制高度

        // 位置属性
        if (element.TryGetProperty("x", out var x))
            widget.X = x.GetInt32();
        else if (element.TryGetProperty("X", out var x2))
            widget.X = x2.GetInt32();

        if (element.TryGetProperty("y", out var y))
            widget.Y = y.GetInt32();
        else if (element.TryGetProperty("Y", out var y2))
            widget.Y = y2.GetInt32();

        // 可见性和布局
        if (element.TryGetProperty("visible", out var visible))
            widget.Visible = visible.GetBoolean();
        else if (element.TryGetProperty("Visible", out var vis))
            widget.Visible = vis.GetBoolean();

        if (element.TryGetProperty("newLine", out var newLine))
            widget.NewLine = newLine.GetBoolean();
        else if (element.TryGetProperty("NewLine", out var nl))
            widget.NewLine = nl.GetBoolean();

        // 数据绑定键：优先用节点内 dataField；没有时回退顶层 key（由调用方在外部设置）
        if (element.TryGetProperty("dataField", out var dataField))
            widget.DataField = dataField.GetString();
        else if (element.TryGetProperty("DataField", out var dataField2))
            widget.DataField = dataField2.GetString();

        // 递归加载子控件（容器支持）
        if (element.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
        {
            widget.Children = children.EnumerateArray()
                .Select(child => FromJsonElement(child))
                .ToList();
        }
        else if (element.TryGetProperty("Children", out var childrenUpper) && childrenUpper.ValueKind == JsonValueKind.Array)
        {
            widget.Children = childrenUpper.EnumerateArray()
                .Select(child => FromJsonElement(child))
                .ToList();
        }

        // 扩展属性（将表单控件特定属性存入字典）
        var extProps = new Dictionary<string, object>();

        // 已知的基础属性（不应放入 ExtendedProperties）
        var knownProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id", "type", "label", "dataField", "width", "w", "widthUnit", 
            "height", "h", "heightUnit", "visible", "newLine", "x", "y", 
            "zIndex", "children", "order", "defaultValue", "placeholder", 
            "required", "readonly", "maxLength", "text", "bold", "format", 
            "showTime", "minDate", "maxDate", "multiSelect", "items", "allowSearch",
            "borderStyle", "borderColor", "borderWidth", "backgroundColor", "padding",
            "title", "showTitle", "collapsible", "collapsed", "containerLayout",
            "fontSize", "fontColor", "fontFamily", "fontWeight", "textAlign"
        };

        // 自动加载所有未被基础属性覆盖的字段到 ExtendedProperties
        foreach (var prop in element.EnumerateObject())
        {
            if (knownProps.Contains(prop.Name))
                continue;

            // 根据 JSON 类型转换为合适的 C# 类型
            var value = prop.Value.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => prop.Value.GetString(),
                System.Text.Json.JsonValueKind.Number => prop.Value.TryGetInt32(out var i) ? (object)i : prop.Value.GetDouble(),
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                _ => prop.Value.GetRawText()
            };

            if (value != null)
                extProps[prop.Name] = value;
        }

        if (extProps.Count > 0)
            widget.ExtendedProperties = extProps;
    }

    /// <summary>
    /// 映射Textbox特定属性
    /// </summary>
    private static void MapTextboxProperties(JsonElement element, TextboxWidget textbox)
    {
        if (element.TryGetProperty("Placeholder", out var placeholder))
            textbox.Placeholder = placeholder.GetString();

        if (element.TryGetProperty("Required", out var required))
            textbox.Required = required.GetBoolean();

        if (element.TryGetProperty("Readonly", out var readonly_))
            textbox.Readonly = readonly_.GetBoolean();

        if (element.TryGetProperty("MaxLength", out var maxLength))
            textbox.MaxLength = maxLength.GetInt32();
    }

    /// <summary>
    /// 映射Label特定属性
    /// </summary>
    private static void MapLabelProperties(JsonElement element, LabelWidget label)
    {
        if (element.TryGetProperty("Text", out var text))
            label.Text = text.GetString();

        if (element.TryGetProperty("Bold", out var bold))
            label.Bold = bold.GetBoolean();
    }

    /// <summary>
    /// 映射Calendar特定属性
    /// </summary>
    private static void MapCalendarProperties(JsonElement element, CalendarWidget calendar)
    {
        if (element.TryGetProperty("Format", out var format))
            calendar.Format = format.GetString() ?? "yyyy-MM-dd";

        if (element.TryGetProperty("ShowTime", out var showTime))
            calendar.ShowTime = showTime.GetBoolean();
    }

    /// <summary>
    /// 映射Listbox特定属性
    /// </summary>
    private static void MapListboxProperties(JsonElement element, ListboxWidget listbox)
    {
        if (element.TryGetProperty("MultiSelect", out var multiSelect))
            listbox.MultiSelect = multiSelect.GetBoolean();

        if (element.TryGetProperty("Items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            listbox.Items = items.EnumerateArray()
                .Select(item => new ListItem
                {
                    Value = item.GetProperty("Value").GetString() ?? "",
                    Label = item.GetProperty("Label").GetString() ?? ""
                })
                .ToList();
        }
    }

    /// <summary>
    /// 映射Frame特定属性（包括子控件）
    /// </summary>
    private static void MapFrameProperties(JsonElement element, FrameWidget frame)
    {
        if (element.TryGetProperty("BorderStyle", out var borderStyle))
            frame.BorderStyle = borderStyle.GetString() ?? "solid";

        if (element.TryGetProperty("BorderColor", out var borderColor))
            frame.BorderColor = borderColor.GetString() ?? "#d9d9d9";

        if (element.TryGetProperty("BorderWidth", out var borderWidth))
            frame.BorderWidth = borderWidth.GetInt32();

        // 递归映射子控件（兼容大小写）
        if (element.TryGetProperty("Children", out var children) && children.ValueKind == JsonValueKind.Array)
        {
            frame.Children = children.EnumerateArray()
                .Select(child => FromJsonElement(child))
                .ToList();
        }
        else if (element.TryGetProperty("children", out var childrenLower) && childrenLower.ValueKind == JsonValueKind.Array)
        {
            frame.Children = childrenLower.EnumerateArray()
                .Select(child => FromJsonElement(child))
                .ToList();
        }
    }

    /// <summary>
    /// 映射Section特定属性（包括容器布局和子控件）
    /// </summary>
    private static void MapSectionProperties(JsonElement element, SectionWidget section)
    {
        // Section特有属性
        if (element.TryGetProperty("Title", out var title))
            section.Title = title.GetString();

        if (element.TryGetProperty("ShowTitle", out var showTitle))
            section.ShowTitle = showTitle.GetBoolean();

        if (element.TryGetProperty("Collapsible", out var collapsible))
            section.Collapsible = collapsible.GetBoolean();

        if (element.TryGetProperty("Collapsed", out var collapsed))
            section.Collapsed = collapsed.GetBoolean();

        // 容器内布局配置
        if (element.TryGetProperty("ContainerLayout", out var containerLayout) ||
            element.TryGetProperty("containerLayout", out containerLayout))
        {
            if (containerLayout.TryGetProperty("FlexDirection", out var flexDirection))
                section.ContainerLayout.FlexDirection = flexDirection.GetString() ?? "row";

            if (containerLayout.TryGetProperty("FlexWrap", out var flexWrap))
                section.ContainerLayout.FlexWrap = flexWrap.GetBoolean();

            if (containerLayout.TryGetProperty("JustifyContent", out var justifyContent))
                section.ContainerLayout.JustifyContent = justifyContent.GetString() ?? "flex-start";

            if (containerLayout.TryGetProperty("AlignItems", out var alignItems))
                section.ContainerLayout.AlignItems = alignItems.GetString() ?? "flex-start";

            if (containerLayout.TryGetProperty("Gap", out var gap))
                section.ContainerLayout.Gap = gap.GetInt32();

            if (containerLayout.TryGetProperty("Padding", out var padding))
                section.ContainerLayout.Padding = padding.GetInt32();

            if (containerLayout.TryGetProperty("BackgroundColor", out var bgColor))
                section.ContainerLayout.BackgroundColor = bgColor.GetString() ?? "#ffffff";

            if (containerLayout.TryGetProperty("BorderRadius", out var borderRadius))
                section.ContainerLayout.BorderRadius = borderRadius.GetInt32();

            if (containerLayout.TryGetProperty("BorderStyle", out var borderStyle))
                section.ContainerLayout.BorderStyle = borderStyle.GetString() ?? "solid";

            if (containerLayout.TryGetProperty("BorderColor", out var borderColor))
                section.ContainerLayout.BorderColor = borderColor.GetString() ?? "#d9d9d9";

            if (containerLayout.TryGetProperty("BorderWidth", out var borderWidth))
                section.ContainerLayout.BorderWidth = borderWidth.GetInt32();
        }

        // 递归映射子控件
        if (element.TryGetProperty("Children", out var children) && children.ValueKind == JsonValueKind.Array)
        {
            section.Children = children.EnumerateArray()
                .Select(child => FromJsonElement(child))
                .ToList();
        }
        else if (element.TryGetProperty("children", out var childrenLower) && childrenLower.ValueKind == JsonValueKind.Array)
        {
            section.Children = childrenLower.EnumerateArray()
                .Select(child => FromJsonElement(child))
                .ToList();
        }
    }

    /// <summary>
    /// 将运行时Widget转换为可序列化的DTO
    /// </summary>
    public static object ToDto(DraggableWidget widget)
    {
        // 直接返回widget本身，因为它已经是可序列化的
        // JsonSerializer会自动处理继承关系
        return widget;
    }

    /// <summary>
    /// 批量转换Widget列表
    /// </summary>
    public static List<DraggableWidget> FromDtoList(IEnumerable<object> dtoList)
    {
        return dtoList.Select(FromDto).ToList();
    }

    /// <summary>
    /// 批量转换为DTO列表
    /// </summary>
    public static List<object> ToDtoList(IEnumerable<DraggableWidget> widgetList)
    {
        return widgetList.Select(ToDto).ToList();
    }
}
