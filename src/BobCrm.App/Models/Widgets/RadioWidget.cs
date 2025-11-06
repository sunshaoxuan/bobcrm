namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 单选按钮组控件
/// </summary>
public class RadioWidget : TextWidget
{
    public RadioWidget()
    {
        Type = "radio";
        Label = "LBL_RADIO";
    }

    /// <summary>选项集合</summary>
    public List<ListItem> Items { get; set; } = new();

    /// <summary>默认选中的值</summary>
    public string? DefaultValue { get; set; }

    /// <summary>是否显示为按钮样式</summary>
    public bool ButtonStyle { get; set; } = false;

    /// <summary>布局方向（horizontal: 横向, vertical: 纵向）</summary>
    public string Direction { get; set; } = "horizontal";

    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        return new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "DefaultValue", Label = "LBL_DEFAULT_VALUE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "ButtonStyle", Label = "PROP_BUTTON_STYLE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Direction", Label = "PROP_FLEX_DIRECTION", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Select,
                Options = new List<BobCrm.App.Models.Designer.PropertyOption>
                {
                    new() { Value = "horizontal", Label = "PROP_DIRECTION_ROW" },
                    new() { Value = "vertical", Label = "PROP_DIRECTION_COLUMN" }
                }
            },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = GetMaxWidth() }
        };
    }
}

