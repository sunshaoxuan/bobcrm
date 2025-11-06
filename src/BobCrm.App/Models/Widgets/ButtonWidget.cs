namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 按钮控件
/// </summary>
public class ButtonWidget : TextWidget
{
    public ButtonWidget()
    {
        Type = "button";
        Label = "LBL_BUTTON";
    }

    /// <summary>按钮样式：primary/default/dashed/link/text</summary>
    public string Variant { get; set; } = "primary";

    /// <summary>按钮尺寸：large/default/small</summary>
    public string Size { get; set; } = "default";

    /// <summary>关联动作标识（如 openUrl/downloadRdp 等）</summary>
    public string? Action { get; set; }

    /// <summary>动作参数（URL、文件Key 等）</summary>
    public string? ActionPayload { get; set; }

    /// <summary>图标（Ant Design Icon 名称）</summary>
    public string? Icon { get; set; }

    /// <summary>是否块级按钮</summary>
    public bool Block { get; set; } = false;

    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        return new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "Variant", Label = "PROP_BUTTON_VARIANT", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Select,
                Options = new List<BobCrm.App.Models.Designer.PropertyOption>
                {
                    new() { Value = "primary", Label = "PROP_BUTTON_PRIMARY" },
                    new() { Value = "default", Label = "PROP_BUTTON_DEFAULT" },
                    new() { Value = "dashed", Label = "PROP_BUTTON_DASHED" },
                    new() { Value = "link", Label = "PROP_BUTTON_LINK" },
                    new() { Value = "text", Label = "PROP_BUTTON_TEXT" }
                }
            },
            new() { PropertyPath = "Size", Label = "PROP_SIZE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Select,
                Options = new List<BobCrm.App.Models.Designer.PropertyOption>
                {
                    new() { Value = "small", Label = "PROP_SIZE_SMALL" },
                    new() { Value = "default", Label = "PROP_SIZE_DEFAULT" },
                    new() { Value = "large", Label = "PROP_SIZE_LARGE" }
                }
            },
            new() { PropertyPath = "Block", Label = "PROP_BUTTON_BLOCK", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = GetMaxWidth() }
        };
    }
}
