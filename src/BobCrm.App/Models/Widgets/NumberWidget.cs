namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 数字输入控件
/// </summary>
public class NumberWidget : TextWidget
{
    public NumberWidget()
    {
        Type = "number";
        Label = "LBL_NUMBER";
    }

    /// <summary>默认值</summary>
    public double? DefaultValue { get; set; }

    /// <summary>最小值</summary>
    public double? MinValue { get; set; }

    /// <summary>最大值</summary>
    public double? MaxValue { get; set; }

    /// <summary>步长</summary>
    public double Step { get; set; } = 1;

    /// <summary>是否允许小数</summary>
    public bool AllowDecimal { get; set; } = true;

    /// <summary>是否显示千分位</summary>
    public bool ShowThousandsSeparator { get; set; } = false;

    public override Type? PreviewComponentType => typeof(BobCrm.App.Components.Designer.WidgetPreviews.NumberPreview);

    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        return new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "DefaultValue", Label = "LBL_DEFAULT_VALUE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number },
            new() { PropertyPath = "MinValue", Label = "PROP_MIN_VALUE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number },
            new() { PropertyPath = "MaxValue", Label = "PROP_MAX_VALUE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number },
            new() { PropertyPath = "Step", Label = "PROP_STEP", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 0, Max = 100 },
            new() { PropertyPath = "AllowDecimal", Label = "PROP_ALLOW_DECIMAL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = GetMaxWidth() }
        };
    }
}
