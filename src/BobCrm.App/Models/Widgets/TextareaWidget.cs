namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 多行文本输入控件
/// </summary>
public class TextareaWidget : TextWidget
{
    public TextareaWidget()
    {
        Type = "textarea";
        Label = "LBL_TEXTAREA";
    }

    /// <summary>默认值</summary>
    public string? DefaultValue { get; set; }

    /// <summary>占位提示</summary>
    public string? Placeholder { get; set; }

    /// <summary>最大长度</summary>
    public int? MaxLength { get; set; }

    /// <summary>行数</summary>
    public int Rows { get; set; } = 4;

    /// <summary>是否根据内容自动增长</summary>
    public bool AutoSize { get; set; } = true;

    public override Type? PreviewComponentType => typeof(BobCrm.App.Components.Designer.WidgetPreviews.TextareaPreview);

    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        return new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "Placeholder", Label = "LBL_PLACEHOLDER", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "DefaultValue", Label = "LBL_DEFAULT_VALUE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "MaxLength", Label = "LBL_MAX_LENGTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = 10000 },
            new() { PropertyPath = "Rows", Label = "PROP_ROWS", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 2, Max = 20 },
            new() { PropertyPath = "AutoSize", Label = "PROP_AUTO_SIZE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = GetMaxWidth() }
        };
    }
}
