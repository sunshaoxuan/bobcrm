using Microsoft.AspNetCore.Components;
using AntDesign;
using BobCrm.App.Services.Widgets;

namespace BobCrm.App.Models.Widgets;

[WidgetMetadata("label", "LBL_LABEL", "Outline.FileText", WidgetCategory.Basic)]
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

    public override Type? PreviewComponentType => typeof(BobCrm.App.Components.Designer.WidgetPreviews.LabelPreview);

    public override bool CanEditProperty(string propertyName)
    {
        // 标签控件不能编辑DataField属性
        if (propertyName == "DataField") return false;
        return base.CanEditProperty(propertyName);
    }

    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        var properties = base.GetPropertyMetadata();

        properties.AddRange(new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "Text", Label = "PROP_TEXT", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "Bold", Label = "PROP_BOLD", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = GetMaxWidth() }
        });

        return properties;
    }

    public override void RenderRuntime(RuntimeRenderContext context)
    {
        var builder = context.Builder;
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"{context.ResolveTextStyle()} font-weight:600;");
        builder.AddContent(2, context.Label);
        builder.CloseElement();
    }

    public override void RenderDesign(DesignRenderContext context)
    {
        var builder = context.Builder;
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:6px; background:{context.BackgroundResolver(this)}; pointer-events:none;");
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", $"{context.TextStyleResolver(this)} font-size:11px; font-weight:500;");
        builder.AddContent(4, Label);
        builder.CloseElement();
        builder.CloseElement();
    }

    public override string GetDefaultCodePrefix()
    {
        return "label";
    }
}
