using BobCrm.App.Models.Designer;
using BobCrm.App.Models.Widgets;

namespace BobCrm.App.Services.Designer;

/// <summary>
/// 控件属性元数据提供者
/// 根据控件类型返回对应的属性清单
/// </summary>
public interface IWidgetPropertyProvider
{
    /// <summary>
    /// 获取指定控件的属性元数据列表
    /// </summary>
    List<WidgetPropertyMetadata> GetProperties(DraggableWidget widget);
}

public class WidgetPropertyProvider : IWidgetPropertyProvider
{
    public List<WidgetPropertyMetadata> GetProperties(DraggableWidget widget)
    {
        return widget switch
        {
            GridWidget => GetGridProperties(),
            PanelWidget => GetPanelProperties(),
            SectionWidget => GetSectionProperties(),
            FrameWidget => GetFrameProperties(),
            TabContainerWidget => GetTabContainerProperties(),
            _ => GetCommonProperties()
        };
    }

    private static List<WidgetPropertyMetadata> GetGridProperties()
    {
        return new List<WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Columns", Label = "PROP_COLUMNS", EditorType = PropertyEditorType.Number, Min = 1, Max = 12 },
            new() { PropertyPath = "Gap", Label = "PROP_GAP", EditorType = PropertyEditorType.Number, Min = 0, Max = 48 },
            new() { PropertyPath = "Padding", Label = "PROP_PADDING", EditorType = PropertyEditorType.Number, Min = 0, Max = 48 },
            new() { PropertyPath = "BackgroundColor", Label = "PROP_BACKGROUND_COLOR", EditorType = PropertyEditorType.Color, Placeholder = "#fafafa" },
            new() { PropertyPath = "Bordered", Label = "PROP_SHOW_BORDER", EditorType = PropertyEditorType.Boolean }
        };
    }

    private static List<WidgetPropertyMetadata> GetPanelProperties()
    {
        return new List<WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Title", Label = "PROP_TITLE", EditorType = PropertyEditorType.Text, Placeholder = "PROP_PANEL_TITLE_PLACEHOLDER" },
            new() { PropertyPath = "ShowHeader", Label = "PROP_SHOW_HEADER", EditorType = PropertyEditorType.Boolean },
            new() { PropertyPath = "ContainerLayout.Gap", Label = "PROP_GAP", EditorType = PropertyEditorType.Number, Min = 0, Max = 48, Group = "PROP_GROUP_LAYOUT" },
            new() { PropertyPath = "ContainerLayout.Padding", Label = "PROP_PADDING", EditorType = PropertyEditorType.Number, Min = 0, Max = 48, Group = "PROP_GROUP_LAYOUT" },
            new() { PropertyPath = "ContainerLayout.FlexDirection", Label = "PROP_FLEX_DIRECTION", EditorType = PropertyEditorType.Select, Group = "PROP_GROUP_LAYOUT", 
                Options = new List<PropertyOption> 
                { 
                    new() { Value = "row", Label = "PROP_DIRECTION_ROW" },
                    new() { Value = "column", Label = "PROP_DIRECTION_COLUMN" }
                }
            },
            new() { PropertyPath = "ContainerLayout.FlexWrap", Label = "PROP_FLEX_WRAP", EditorType = PropertyEditorType.Boolean, Group = "PROP_GROUP_LAYOUT" }
        };
    }

    private static List<WidgetPropertyMetadata> GetSectionProperties()
    {
        return new List<WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Title", Label = "PROP_TITLE", EditorType = PropertyEditorType.Text, Placeholder = "PROP_SECTION_TITLE_PLACEHOLDER" },
            new() { PropertyPath = "ShowTitle", Label = "PROP_SHOW_TITLE", EditorType = PropertyEditorType.Boolean },
            new() { PropertyPath = "Collapsible", Label = "PROP_COLLAPSIBLE", EditorType = PropertyEditorType.Boolean },
            new() { PropertyPath = "Collapsed", Label = "PROP_COLLAPSED_DEFAULT", EditorType = PropertyEditorType.Boolean, VisibleWhen = "Collapsible", VisibleWhenValue = true },
            new() { PropertyPath = "ContainerLayout.Gap", Label = "PROP_GAP", EditorType = PropertyEditorType.Number, Min = 0, Max = 48, Group = "PROP_GROUP_LAYOUT" },
            new() { PropertyPath = "ContainerLayout.Padding", Label = "PROP_PADDING", EditorType = PropertyEditorType.Number, Min = 0, Max = 48, Group = "PROP_GROUP_LAYOUT" },
            new() { PropertyPath = "ContainerLayout.BackgroundColor", Label = "PROP_BACKGROUND_COLOR", EditorType = PropertyEditorType.Color, Placeholder = "#f5f5f5", Group = "PROP_GROUP_LAYOUT" },
            new() { PropertyPath = "ContainerLayout.BorderRadius", Label = "PROP_BORDER_RADIUS", EditorType = PropertyEditorType.Number, Min = 0, Max = 24, Group = "PROP_GROUP_LAYOUT" }
        };
    }

    private static List<WidgetPropertyMetadata> GetFrameProperties()
    {
        return new List<WidgetPropertyMetadata>
        {
            new() { PropertyPath = "BorderStyle", Label = "PROP_BORDER_STYLE", EditorType = PropertyEditorType.Select,
                Options = new List<PropertyOption>
                {
                    new() { Value = "solid", Label = "PROP_BORDER_SOLID" },
                    new() { Value = "dashed", Label = "PROP_BORDER_DASHED" },
                    new() { Value = "dotted", Label = "PROP_BORDER_DOTTED" },
                    new() { Value = "none", Label = "PROP_BORDER_NONE" }
                }
            },
            new() { PropertyPath = "BorderColor", Label = "PROP_BORDER_COLOR", EditorType = PropertyEditorType.Color, Placeholder = "#d9d9d9" },
            new() { PropertyPath = "BorderWidth", Label = "PROP_BORDER_WIDTH", EditorType = PropertyEditorType.Number, Min = 0, Max = 10 },
            new() { PropertyPath = "BackgroundColor", Label = "PROP_BACKGROUND_COLOR", EditorType = PropertyEditorType.Color, Placeholder = "#fff" },
            new() { PropertyPath = "Padding", Label = "PROP_PADDING", EditorType = PropertyEditorType.Number, Min = 0, Max = 48 }
        };
    }

    private static List<WidgetPropertyMetadata> GetTabContainerProperties()
    {
        return new List<WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Animated", Label = "PROP_ANIMATED", EditorType = PropertyEditorType.Boolean },
            new() { PropertyPath = "Centered", Label = "PROP_CENTERED", EditorType = PropertyEditorType.Boolean },
            new() { PropertyPath = "Size", Label = "PROP_SIZE", EditorType = PropertyEditorType.Select,
                Options = new List<PropertyOption>
                {
                    new() { Value = "small", Label = "PROP_SIZE_SMALL" },
                    new() { Value = "default", Label = "PROP_SIZE_DEFAULT" },
                    new() { Value = "large", Label = "PROP_SIZE_LARGE" }
                }
            },
            new() { PropertyPath = "Type", Label = "PROP_TYPE", EditorType = PropertyEditorType.Select,
                Options = new List<PropertyOption>
                {
                    new() { Value = "line", Label = "PROP_TAB_TYPE_LINE" },
                    new() { Value = "card", Label = "PROP_TAB_TYPE_CARD" }
                }
            },
            new() { PropertyPath = "TabPosition", Label = "PROP_TAB_POSITION", EditorType = PropertyEditorType.Select,
                Options = new List<PropertyOption>
                {
                    new() { Value = "top", Label = "PROP_POSITION_TOP" },
                    new() { Value = "bottom", Label = "PROP_POSITION_BOTTOM" },
                    new() { Value = "left", Label = "PROP_POSITION_LEFT" },
                    new() { Value = "right", Label = "PROP_POSITION_RIGHT" }
                }
            }
        };
    }

    private static List<WidgetPropertyMetadata> GetCommonProperties()
    {
        return new List<WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = PropertyEditorType.Text },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = PropertyEditorType.Number, Min = 1, Max = 100 },
            new() { PropertyPath = "Visible", Label = "PROP_VISIBLE", EditorType = PropertyEditorType.Boolean }
        };
    }
}

