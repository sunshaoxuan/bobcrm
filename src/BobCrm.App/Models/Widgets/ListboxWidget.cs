namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 列表框控件
/// 下拉选择框
/// </summary>
public class ListboxWidget : TextWidget
{
    public ListboxWidget()
    {
        Type = "listbox";
        Label = "LBL_LISTBOX";
    }

    public List<ListItem> Items { get; set; } = new();
    public bool MultiSelect { get; set; } = false;
    public string? Placeholder { get; set; }
    public bool AllowSearch { get; set; } = false;
    public string? DefaultValue { get; set; }

    public override Type? PreviewComponentType => typeof(BobCrm.App.Components.Designer.WidgetPreviews.SelectPreview);

    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        return new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "Placeholder", Label = "LBL_PLACEHOLDER", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "DefaultValue", Label = "LBL_DEFAULT_VALUE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "MultiSelect", Label = "PROP_MULTI_SELECT", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "AllowSearch", Label = "PROP_ALLOW_SEARCH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = GetMaxWidth() }
        };
    }
}
