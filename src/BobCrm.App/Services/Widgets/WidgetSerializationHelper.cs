using System.Text.Json;
using BobCrm.App.Models.Widgets;

namespace BobCrm.App.Services.Widgets;

/// <summary>
/// Widget序列化助手类
/// 负责Widget的JSON序列化和反序列化逻辑
/// </summary>
public static class WidgetSerializationHelper
{
    /// <summary>
    /// 将Widget序列化为字典（用于保存布局）
    /// </summary>
    public static Dictionary<string, object> SerializeWidget(DraggableWidget widget, int order)
    {
        var data = new Dictionary<string, object>
        {
            ["id"] = widget.Id,
            ["type"] = widget.Type,
            ["label"] = widget.Label ?? "",
            ["order"] = order,
            ["visible"] = widget.Visible,
            ["Width"] = widget.Width,
            ["WidthUnit"] = widget.WidthUnit,
            ["Height"] = widget.Height,
            ["HeightUnit"] = widget.HeightUnit
        };

        if (widget.NewLine)
            data["newLine"] = widget.NewLine;

        if (!string.IsNullOrWhiteSpace(widget.DataField))
            data["dataField"] = widget.DataField;

        // 特定控件类型的属性
        switch (widget)
        {
            case TextboxWidget textbox:
                if (!string.IsNullOrWhiteSpace(textbox.DefaultValue))
                    data["defaultValue"] = textbox.DefaultValue;
                if (!string.IsNullOrWhiteSpace(textbox.Placeholder))
                    data["placeholder"] = textbox.Placeholder;
                if (textbox.Required)
                    data["required"] = textbox.Required;
                if (textbox.Readonly)
                    data["readonly"] = textbox.Readonly;
                if (textbox.MinLength.HasValue)
                    data["minLength"] = textbox.MinLength.Value;
                if (textbox.MaxLength.HasValue)
                    data["maxLength"] = textbox.MaxLength.Value;
                if (!string.IsNullOrWhiteSpace(textbox.ValidationPattern))
                    data["validationPattern"] = textbox.ValidationPattern;
                break;

            case NumberWidget number:
                if (number.DefaultValue.HasValue)
                    data["defaultValue"] = number.DefaultValue.Value;
                if (number.MinValue.HasValue)
                    data["minValue"] = number.MinValue.Value;
                if (number.MaxValue.HasValue)
                    data["maxValue"] = number.MaxValue.Value;
                if (number.Step != 1)
                    data["step"] = number.Step;
                if (!number.AllowDecimal)
                    data["allowDecimal"] = number.AllowDecimal;
                if (number.ShowThousandsSeparator)
                    data["showThousandsSeparator"] = number.ShowThousandsSeparator;
                break;

            case SelectWidget select:
                data["items"] = select.Items;
                if (!string.IsNullOrWhiteSpace(select.DefaultValue))
                    data["defaultValue"] = select.DefaultValue;
                if (!string.IsNullOrWhiteSpace(select.Placeholder))
                    data["placeholder"] = select.Placeholder;
                if (select.AllowSearch)
                    data["allowSearch"] = select.AllowSearch;
                break;

            case ListboxWidget listbox:
                data["items"] = listbox.Items;
                if (listbox.MultiSelect)
                    data["multiSelect"] = listbox.MultiSelect;
                break;

            case CalendarWidget calendar:
                data["format"] = calendar.Format ?? "yyyy-MM-dd";
                if (calendar.ShowTime)
                    data["showTime"] = calendar.ShowTime;
                break;

            case TextareaWidget textarea:
                if (!string.IsNullOrWhiteSpace(textarea.DefaultValue))
                    data["defaultValue"] = textarea.DefaultValue;
                if (!string.IsNullOrWhiteSpace(textarea.Placeholder))
                    data["placeholder"] = textarea.Placeholder;
                if (textarea.MinLength.HasValue)
                    data["minLength"] = textarea.MinLength.Value;
                if (textarea.MaxLength.HasValue)
                    data["maxLength"] = textarea.MaxLength.Value;
                if (textarea.Rows != 4)
                    data["rows"] = textarea.Rows;
                if (!textarea.AutoSize)
                    data["autoSize"] = textarea.AutoSize;
                break;

            case ButtonWidget button:
                data["variant"] = button.Variant;
                data["size"] = button.Size;
                if (!string.IsNullOrWhiteSpace(button.Action))
                    data["action"] = button.Action;
                if (!string.IsNullOrWhiteSpace(button.ActionPayload))
                    data["actionPayload"] = button.ActionPayload;
                if (!string.IsNullOrWhiteSpace(button.Icon))
                    data["icon"] = button.Icon;
                if (button.Block)
                    data["block"] = button.Block;
                break;

            case LabelWidget label:
                if (!string.IsNullOrWhiteSpace(label.Text))
                    data["text"] = label.Text;
                if (label.Bold)
                    data["bold"] = label.Bold;
                break;

            case FrameWidget frame:
                SerializeContainerProperties(data, frame);
                data["borderStyle"] = frame.BorderStyle;
                data["borderColor"] = frame.BorderColor;
                data["borderWidth"] = frame.BorderWidth;
                data["backgroundColor"] = frame.BackgroundColor;
                data["padding"] = frame.Padding;
                break;

            case SectionWidget section:
                SerializeContainerProperties(data, section);
                SerializeContainerLayout(data, section.ContainerLayout);
                if (!string.IsNullOrWhiteSpace(section.Title))
                    data["title"] = section.Title;
                if (section.ShowTitle)
                    data["showTitle"] = section.ShowTitle;
                if (section.Collapsible)
                    data["collapsible"] = section.Collapsible;
                if (section.Collapsed)
                    data["collapsed"] = section.Collapsed;
                break;

            case PanelWidget panel:
                SerializeContainerProperties(data, panel);
                SerializeContainerLayout(data, panel.ContainerLayout);
                break;

            case GridWidget grid:
                SerializeContainerProperties(data, grid);
                data["columns"] = grid.Columns;
                data["gap"] = grid.Gap;
                break;

            case TabContainerWidget tabContainer:
                SerializeContainerProperties(data, tabContainer);
                // Tabs序列化
                if (tabContainer.Children != null)
                {
                    var tabs = tabContainer.Children.OfType<TabWidget>().ToList();
                    data["tabs"] = tabs.Select((t, i) => SerializeTabWidget(t, i)).ToList();
                }
                break;

            case TabWidget tab:
                data["tabId"] = tab.TabId;
                if (tab.IsDefault)
                    data["isDefault"] = tab.IsDefault;
                if (!string.IsNullOrWhiteSpace(tab.Icon))
                    data["icon"] = tab.Icon;
                break;
        }

        // 序列化文本样式属性（TextWidget子类）
        if (widget is TextWidget textWidget)
        {
            if (textWidget.FontSize != 14)
                data["fontSize"] = textWidget.FontSize;
            if (textWidget.FontColor != "#333333")
                data["fontColor"] = textWidget.FontColor;
            if (textWidget.FontFamily != "inherit")
                data["fontFamily"] = textWidget.FontFamily;
            if (textWidget.FontWeight != "normal")
                data["fontWeight"] = textWidget.FontWeight;
            if (textWidget.TextAlign != "left")
                data["textAlign"] = textWidget.TextAlign;
        }

        // 序列化ExtendedProperties
        if (widget.ExtendedProperties != null && widget.ExtendedProperties.Count > 0)
        {
            data["extendedProperties"] = widget.ExtendedProperties;
        }

        return data;
    }

    private static void SerializeContainerProperties(Dictionary<string, object> data, ContainerWidget container)
    {
        if (container.Children != null && container.Children.Count > 0)
        {
            data["children"] = container.Children.Select((c, i) => SerializeWidget(c, i)).ToList();
        }
    }

    private static void SerializeContainerLayout(Dictionary<string, object> data, ContainerLayoutOptions layout)
    {
        data["containerLayout"] = new Dictionary<string, object>
        {
            ["mode"] = layout.Mode.ToString(),
            ["flexDirection"] = layout.FlexDirection,
            ["flexWrap"] = layout.FlexWrap,
            ["justifyContent"] = layout.JustifyContent,
            ["alignItems"] = layout.AlignItems,
            ["gap"] = layout.Gap,
            ["padding"] = layout.Padding,
            ["backgroundColor"] = layout.BackgroundColor,
            ["borderRadius"] = layout.BorderRadius,
            ["borderStyle"] = layout.BorderStyle,
            ["borderColor"] = layout.BorderColor,
            ["borderWidth"] = layout.BorderWidth
        };
    }

    private static Dictionary<string, object> SerializeTabWidget(TabWidget tab, int order)
    {
        var tabData = SerializeWidget(tab, order);
        // Tab的children已经在SerializeWidget中处理
        return tabData;
    }

    /// <summary>
    /// 解析选项字符串为ListItem列表（格式：value:label,value:label）
    /// </summary>
    public static List<ListItem> ParseOptions(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<ListItem>();

        try
        {
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p =>
                {
                    var parts = p.Split(':', 2);
                    return new ListItem
                    {
                        Value = parts[0].Trim(),
                        Label = parts.Length > 1 ? parts[1].Trim() : parts[0].Trim()
                    };
                })
                .ToList();
        }
        catch
        {
            return new List<ListItem>();
        }
    }
}

