using BobCrm.App.Services.Widgets;

namespace BobCrm.App.Models.Widgets;

[WidgetMetadata("select", "LBL_SELECT", "Outline.Select", WidgetCategory.Basic)]
/// <summary>
/// 单选下拉控件
/// </summary>
public class SelectWidget : TextWidget
{
    public SelectWidget()
    {
        Type = "select";
        Label = "LBL_SELECT";
    }

    /// <summary>选项集合</summary>
    public List<ListItem> Items { get; set; } = new();

    /// <summary>默认值</summary>
    public string? DefaultValue { get; set; }

    /// <summary>占位提示</summary>
    public string? Placeholder { get; set; }

    /// <summary>是否允许搜索</summary>
    public bool AllowSearch { get; set; } = false;

    public override Type? PreviewComponentType => typeof(BobCrm.App.Components.Designer.WidgetPreviews.SelectPreview);
    public override Type? RuntimeComponentType => typeof(BobCrm.App.Components.Widgets.Runtime.SelectWidgetComponent);

    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        var properties = base.GetPropertyMetadata();

        properties.AddRange(new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "Placeholder", Label = "LBL_PLACEHOLDER", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "DefaultValue", Label = "LBL_DEFAULT_VALUE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "AllowSearch", Label = "PROP_ALLOW_SEARCH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = GetMaxWidth() }
        });

        return properties;
    }

    public override void RenderRuntime(RuntimeRenderContext context) { }

    public override void RenderDesign(DesignRenderContext context)
    {
        var builder = context.Builder;
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:6px; background:{context.BackgroundResolver(this)}; pointer-events:none;");
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", $"{context.TextStyleResolver(this)} font-size:11px; margin-bottom:2px;");
        builder.AddContent(4, Label);
        builder.CloseElement();
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "height:32px; background:#fff; border:1px solid #e0e0e0; border-radius:2px; display:flex; align-items:center; justify-content:space-between; padding:0 6px;");
        builder.OpenElement(7, "span");
        builder.AddAttribute(8, "style", "font-size:12px; color:#999;");
        builder.AddContent(9, "Option");
        builder.CloseElement();
        builder.OpenElement(10, "span");
        builder.AddAttribute(11, "style", "font-size:10px; color:#999;");
        builder.AddContent(12, "▼");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }

    public override string GetDefaultCodePrefix()
    {
        return "select";
    }
}
