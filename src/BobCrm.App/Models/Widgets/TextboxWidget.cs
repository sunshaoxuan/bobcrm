namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 文本框控件
/// </summary>
public class TextboxWidget : TextWidget
{
    public TextboxWidget()
    {
        Type = "textbox";
        Label = "LBL_TEXTBOX";
    }

    public string? Placeholder { get; set; }
    public bool Required { get; set; } = false;
    public bool Readonly { get; set; } = false;
    public int? MaxLength { get; set; }
    public string? ValidationPattern { get; set; }
    public string? DefaultValue { get; set; }

    public override Type? PreviewComponentType => typeof(BobCrm.App.Components.Designer.WidgetPreviews.TextboxPreview);

    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        return new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "Placeholder", Label = "LBL_PLACEHOLDER", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "DefaultValue", Label = "LBL_DEFAULT_VALUE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "MaxLength", Label = "LBL_MAX_LENGTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = 1000 },
            new() { PropertyPath = "Required", Label = "LBL_REQUIRED", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Readonly", Label = "LBL_READONLY", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = GetMaxWidth() }
        };
    }
}
