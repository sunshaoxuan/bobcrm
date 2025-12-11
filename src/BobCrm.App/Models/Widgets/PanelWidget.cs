using AntDesign;
using BobCrm.App.Services.Widgets;

namespace BobCrm.App.Models.Widgets;

[WidgetMetadata("panel", "LBL_PANEL", "Outline.Appstore", WidgetRegistry.WidgetCategory.Layout)]
/// <summary>
/// Panel 容器控件
/// </summary>
public class PanelWidget : ContainerWidget
{
    public PanelWidget()
    {
        Type = "panel";
        Label = "LBL_PANEL";
        Width = 100;
        WidthUnit = "%";
        HeightUnit = "auto";
        ContainerLayout = new ContainerLayoutOptions
        {
            Mode = ContainerLayoutMode.Flow,
            FlexDirection = "row",
            FlexWrap = true,
            JustifyContent = "flex-start",
            AlignItems = "flex-start",
            Gap = 12,
            Padding = 16,
            BackgroundColor = "#ffffff",
            BorderRadius = 6,
            BorderStyle = "solid",
            BorderColor = "#e5e5e5",
            BorderWidth = 1
        };
    }

    /// <summary>Panel 标题</summary>
    public string? Title { get; set; }

    /// <summary>是否显示标题栏</summary>
    public bool ShowHeader { get; set; } = true;

    /// <summary>容器布局选项</summary>
    public ContainerLayoutOptions ContainerLayout { get; set; } = new();

    /// <summary>
    /// 获取 Panel 控件的属性元数据
    /// </summary>
    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        var properties = base.GetPropertyMetadata();

        properties.AddRange(new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Title", Label = "PROP_TITLE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text, Placeholder = "PROP_PANEL_TITLE_PLACEHOLDER" },
            new() { PropertyPath = "ShowHeader", Label = "PROP_SHOW_HEADER", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "ContainerLayout.Gap", Label = "PROP_GAP", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 0, Max = 48, Group = "PROP_GROUP_LAYOUT" },
            new() { PropertyPath = "ContainerLayout.Padding", Label = "PROP_PADDING", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 0, Max = 48, Group = "PROP_GROUP_LAYOUT" },
            new() { PropertyPath = "ContainerLayout.FlexDirection", Label = "PROP_FLEX_DIRECTION", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Select, Group = "PROP_GROUP_LAYOUT",
                Options = new List<BobCrm.App.Models.Designer.PropertyOption>
                {
                    new() { Value = "row", Label = "PROP_DIRECTION_ROW" },
                    new() { Value = "column", Label = "PROP_DIRECTION_COLUMN" }
                }
            },
            new() { PropertyPath = "ContainerLayout.FlexWrap", Label = "PROP_FLEX_WRAP", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean, Group = "PROP_GROUP_LAYOUT" }
        });

        return properties;
    }

    public override string GetDefaultCodePrefix()
    {
        return "panel";
    }
}
