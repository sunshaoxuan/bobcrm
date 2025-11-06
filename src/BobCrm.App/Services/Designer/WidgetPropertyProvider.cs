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
            new() { PropertyPath = "Columns", Label = "列数", EditorType = PropertyEditorType.Number, Min = 1, Max = 12 },
            new() { PropertyPath = "Gap", Label = "间距 (px)", EditorType = PropertyEditorType.Number, Min = 0, Max = 48 },
            new() { PropertyPath = "Padding", Label = "内边距 (px)", EditorType = PropertyEditorType.Number, Min = 0, Max = 48 },
            new() { PropertyPath = "BackgroundColor", Label = "背景色", EditorType = PropertyEditorType.Color, Placeholder = "#fafafa" },
            new() { PropertyPath = "Bordered", Label = "显示边框", EditorType = PropertyEditorType.Boolean }
        };
    }

    private static List<WidgetPropertyMetadata> GetPanelProperties()
    {
        return new List<WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Title", Label = "标题", EditorType = PropertyEditorType.Text, Placeholder = "面板标题" },
            new() { PropertyPath = "ShowHeader", Label = "显示标题", EditorType = PropertyEditorType.Boolean },
            new() { PropertyPath = "ContainerLayout.Gap", Label = "间距 (px)", EditorType = PropertyEditorType.Number, Min = 0, Max = 48, Group = "布局设置" },
            new() { PropertyPath = "ContainerLayout.Padding", Label = "内边距 (px)", EditorType = PropertyEditorType.Number, Min = 0, Max = 48, Group = "布局设置" },
            new() { PropertyPath = "ContainerLayout.FlexDirection", Label = "方向", EditorType = PropertyEditorType.Select, Group = "布局设置", 
                Options = new List<PropertyOption> 
                { 
                    new() { Value = "row", Label = "横向" },
                    new() { Value = "column", Label = "纵向" }
                }
            },
            new() { PropertyPath = "ContainerLayout.FlexWrap", Label = "自动换行", EditorType = PropertyEditorType.Boolean, Group = "布局设置" }
        };
    }

    private static List<WidgetPropertyMetadata> GetSectionProperties()
    {
        return new List<WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Title", Label = "标题", EditorType = PropertyEditorType.Text, Placeholder = "分组标题" },
            new() { PropertyPath = "ShowTitle", Label = "显示标题", EditorType = PropertyEditorType.Boolean },
            new() { PropertyPath = "Collapsible", Label = "可折叠", EditorType = PropertyEditorType.Boolean },
            new() { PropertyPath = "Collapsed", Label = "默认折叠", EditorType = PropertyEditorType.Boolean, VisibleWhen = "Collapsible", VisibleWhenValue = true },
            new() { PropertyPath = "ContainerLayout.Gap", Label = "间距 (px)", EditorType = PropertyEditorType.Number, Min = 0, Max = 48, Group = "布局设置" },
            new() { PropertyPath = "ContainerLayout.Padding", Label = "内边距 (px)", EditorType = PropertyEditorType.Number, Min = 0, Max = 48, Group = "布局设置" },
            new() { PropertyPath = "ContainerLayout.BackgroundColor", Label = "背景色", EditorType = PropertyEditorType.Color, Placeholder = "#f5f5f5", Group = "布局设置" },
            new() { PropertyPath = "ContainerLayout.BorderRadius", Label = "圆角 (px)", EditorType = PropertyEditorType.Number, Min = 0, Max = 24, Group = "布局设置" }
        };
    }

    private static List<WidgetPropertyMetadata> GetFrameProperties()
    {
        return new List<WidgetPropertyMetadata>
        {
            new() { PropertyPath = "BorderStyle", Label = "边框样式", EditorType = PropertyEditorType.Select,
                Options = new List<PropertyOption>
                {
                    new() { Value = "solid", Label = "实线" },
                    new() { Value = "dashed", Label = "虚线" },
                    new() { Value = "dotted", Label = "点线" },
                    new() { Value = "none", Label = "无边框" }
                }
            },
            new() { PropertyPath = "BorderColor", Label = "边框颜色", EditorType = PropertyEditorType.Color, Placeholder = "#d9d9d9" },
            new() { PropertyPath = "BorderWidth", Label = "边框宽度 (px)", EditorType = PropertyEditorType.Number, Min = 0, Max = 10 },
            new() { PropertyPath = "BackgroundColor", Label = "背景色", EditorType = PropertyEditorType.Color, Placeholder = "#fff" },
            new() { PropertyPath = "Padding", Label = "内边距 (px)", EditorType = PropertyEditorType.Number, Min = 0, Max = 48 }
        };
    }

    private static List<WidgetPropertyMetadata> GetTabContainerProperties()
    {
        return new List<WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Animated", Label = "动画效果", EditorType = PropertyEditorType.Boolean },
            new() { PropertyPath = "Centered", Label = "居中显示", EditorType = PropertyEditorType.Boolean },
            new() { PropertyPath = "Size", Label = "尺寸", EditorType = PropertyEditorType.Select,
                Options = new List<PropertyOption>
                {
                    new() { Value = "small", Label = "小" },
                    new() { Value = "default", Label = "默认" },
                    new() { Value = "large", Label = "大" }
                }
            },
            new() { PropertyPath = "Type", Label = "类型", EditorType = PropertyEditorType.Select,
                Options = new List<PropertyOption>
                {
                    new() { Value = "line", Label = "线条" },
                    new() { Value = "card", Label = "卡片" }
                }
            },
            new() { PropertyPath = "TabPosition", Label = "标签位置", EditorType = PropertyEditorType.Select,
                Options = new List<PropertyOption>
                {
                    new() { Value = "top", Label = "顶部" },
                    new() { Value = "bottom", Label = "底部" },
                    new() { Value = "left", Label = "左侧" },
                    new() { Value = "right", Label = "右侧" }
                }
            }
        };
    }

    private static List<WidgetPropertyMetadata> GetCommonProperties()
    {
        return new List<WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "标签", EditorType = PropertyEditorType.Text },
            new() { PropertyPath = "Width", Label = "宽度", EditorType = PropertyEditorType.Number, Min = 1, Max = 100 },
            new() { PropertyPath = "Visible", Label = "可见", EditorType = PropertyEditorType.Boolean }
        };
    }
}

