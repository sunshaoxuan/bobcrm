namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 标签控件
/// 纯文本显示，不能编辑
/// </summary>
public class LabelWidget : TextWidget
{
    public LabelWidget()
    {
        Type = "label";
        Label = "LBL_LABEL";
    }

    public string? Text { get; set; }
    public bool Bold { get; set; } = false;

    public override bool CanEditProperty(string propertyName)
    {
        // 标签控件不能编辑DataField属性
        if (propertyName == "DataField") return false;
        return base.CanEditProperty(propertyName);
    }

    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        return new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "Text", Label = "PROP_TEXT", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "Bold", Label = "PROP_BOLD", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = GetMaxWidth() }
        };
    }
}
