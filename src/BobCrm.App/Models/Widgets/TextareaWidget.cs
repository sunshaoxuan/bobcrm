using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

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
        var properties = base.GetPropertyMetadata();

        properties.AddRange(new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "Placeholder", Label = "LBL_PLACEHOLDER", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "DefaultValue", Label = "LBL_DEFAULT_VALUE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "MaxLength", Label = "LBL_MAX_LENGTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = 10000 },
            new() { PropertyPath = "Rows", Label = "PROP_ROWS", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 2, Max = 20 },
            new() { PropertyPath = "AutoSize", Label = "PROP_AUTO_SIZE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = GetMaxWidth() }
        });

        return properties;
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
            builder.OpenElement(4, "textarea");
            builder.AddAttribute(5, "class", "runtime-field-input");
            builder.AddAttribute(6, "style", "min-height:80px; resize:vertical;");
            builder.AddContent(7, value);
            if (context.ValueSetter != null)
            {
                builder.AddAttribute(8, "oninput",
                    callbackFactory.Create<ChangeEventArgs>(context.EventTarget,
                        e => context.ValueSetter!(e.Value?.ToString())));
            }
            builder.CloseElement(); // textarea
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
        builder.AddAttribute(6, "style", "min-height:56px; background:#fff; border:1px solid #e0e0e0; border-radius:2px;");
        builder.CloseElement();
        builder.CloseElement();
    }

    public override string GetDefaultCodePrefix()
    {
        return "textarea";
    }
}
