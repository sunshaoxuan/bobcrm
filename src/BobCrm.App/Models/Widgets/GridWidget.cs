using AntDesign;
using BobCrm.App.Services.Widgets;

namespace BobCrm.App.Models.Widgets;

[WidgetMetadata("grid", "LBL_GRID", "Outline.BorderOuter", WidgetRegistry.WidgetCategory.Layout)]
/// <summary>
/// Grid 容器控件（结构化布局）
/// </summary>
public class GridWidget : ContainerWidget
{
    public GridWidget()
    {
        Type = "grid";
        Label = "LBL_GRID";
        Width = 100;
        WidthUnit = "%";
        Columns = 3;
        Gap = 12;
        Padding = 12;
        BackgroundColor = "#fafafa";
    }

    /// <summary>列数</summary>
    public int Columns { get; set; } = 3;

    /// <summary>行列间距</summary>
    public int Gap { get; set; } = 12;

    /// <summary>内边距</summary>
    public int Padding { get; set; } = 12;

    /// <summary>背景色</summary>
    public string BackgroundColor { get; set; } = "#fafafa";

    /// <summary>是否显示边框</summary>
    public bool Bordered { get; set; } = false;

    /// <summary>
    /// 获取 Grid 控件的属性元数据
    /// </summary>
    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        var properties = base.GetPropertyMetadata();

        properties.AddRange(new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Columns", Label = "PROP_COLUMNS", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = 12 },
            new() { PropertyPath = "Gap", Label = "PROP_GAP", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 0, Max = 48 },
            new() { PropertyPath = "Padding", Label = "PROP_PADDING", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 0, Max = 48 },
            new() { PropertyPath = "BackgroundColor", Label = "PROP_BACKGROUND_COLOR", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Color, Placeholder = "#fafafa" },
            new() { PropertyPath = "Bordered", Label = "PROP_SHOW_BORDER", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean }
        });

        return properties;
    }

    public override string GetDefaultCodePrefix()
    {
        return "grid";
    }
}
