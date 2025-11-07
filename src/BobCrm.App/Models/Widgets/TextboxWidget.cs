using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

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

    public override void RenderRuntime(RuntimeRenderContext context)
    {
        var value = context.ValueGetter?.Invoke() ?? string.Empty;

        if (context.Mode == RuntimeWidgetRenderMode.Edit)
        {
            var builder = context.Builder;
            var callbackFactory = new EventCallbackFactory();

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", "display:flex; flex-direction:column; gap:6px;");
            RenderFieldLabel(builder, context.Label);
            builder.OpenElement(4, "input");
            builder.AddAttribute(5, "class", "runtime-field-input");
            builder.AddAttribute(6, "value", value);
            builder.AddAttribute(7, "type", Readonly ? "text" : "text");
            if (!string.IsNullOrWhiteSpace(Placeholder))
            {
                builder.AddAttribute(8, "placeholder", Placeholder);
            }
            if (MaxLength.HasValue)
            {
                builder.AddAttribute(9, "maxlength", MaxLength.Value);
            }
            if (Readonly)
            {
                builder.AddAttribute(10, "readonly", true);
            }
            if (context.ValueSetter != null)
            {
                builder.AddAttribute(11, "oninput",
                    callbackFactory.Create<ChangeEventArgs>(context.EventTarget,
                        e => context.ValueSetter!(e.Value?.ToString())));
            }
            builder.CloseElement(); // input
            builder.CloseElement(); // container
        }
        else
        {
            RenderReadOnlyValue(context, value);
        }
    }

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
        builder.AddAttribute(6, "style", "height:32px; background:#fff; border:1px solid #e0e0e0; border-radius:2px; display:flex; align-items:center; justify-content:space-between; padding:0 8px;");
        builder.OpenElement(7, "span");
        builder.AddAttribute(8, "style", "font-size:12px; color:#999;");
        builder.AddContent(9, context.Localize("LBL_PLACEHOLDER_TEXTBOX"));
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }

    public override string GetDefaultCodePrefix()
    {
        return "textbox";
    }
}
