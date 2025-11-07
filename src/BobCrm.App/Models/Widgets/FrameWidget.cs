namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 框架控件（容器）
/// 可以包含其他子控件
/// </summary>
public class FrameWidget : ContainerWidget
{
    public FrameWidget()
    {
        Type = "frame";
        Label = "LBL_FRAME";
        Width = 100;
        WidthUnit = "%";
        Height = 200;
    }

    public string BorderStyle { get; set; } = "solid";
    public string BorderColor { get; set; } = "#d9d9d9";
    public int BorderWidth { get; set; } = 2;
    public string BackgroundColor { get; set; } = "#fafafa";
    public int Padding { get; set; } = 8;

    public override bool CanEditProperty(string propertyName)
    {
        // 框架控件不能编辑DataField属性
        if (propertyName == "DataField") return false;
        return base.CanEditProperty(propertyName);
    }

    /// <summary>
    /// 获取 Frame 控件的属性元数据
    /// </summary>
    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        return new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "BorderStyle", Label = "PROP_BORDER_STYLE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Select,
                Options = new List<BobCrm.App.Models.Designer.PropertyOption>
                {
                    new() { Value = "solid", Label = "PROP_BORDER_SOLID" },
                    new() { Value = "dashed", Label = "PROP_BORDER_DASHED" },
                    new() { Value = "dotted", Label = "PROP_BORDER_DOTTED" },
                    new() { Value = "none", Label = "PROP_BORDER_NONE" }
                }
            },
            new() { PropertyPath = "BorderColor", Label = "PROP_BORDER_COLOR", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Color, Placeholder = "#d9d9d9" },
            new() { PropertyPath = "BorderWidth", Label = "PROP_BORDER_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 0, Max = 10 },
            new() { PropertyPath = "BackgroundColor", Label = "PROP_BACKGROUND_COLOR", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Color, Placeholder = "#fff" },
            new() { PropertyPath = "Padding", Label = "PROP_PADDING", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 0, Max = 48 }
        };
    }

    public override string GetDefaultCodePrefix()
    {
        return "frame";
    }
}
