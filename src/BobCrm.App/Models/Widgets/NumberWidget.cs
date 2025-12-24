using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using AntDesign;
using BobCrm.App.Services.Widgets;

namespace BobCrm.App.Models.Widgets;

[WidgetMetadata("number", "LBL_NUMBER", "Outline.FieldNumber", WidgetCategory.Basic)]
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
        var properties = base.GetPropertyMetadata();

        properties.AddRange(new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "DefaultValue", Label = "LBL_DEFAULT_VALUE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number },
            new() { PropertyPath = "MinValue", Label = "PROP_MIN_VALUE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number },
            new() { PropertyPath = "MaxValue", Label = "PROP_MAX_VALUE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number },
            new() { PropertyPath = "Step", Label = "PROP_STEP", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 0, Max = 100 },
            new() { PropertyPath = "AllowDecimal", Label = "PROP_ALLOW_DECIMAL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
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
            builder.OpenElement(4, "input");
            builder.AddAttribute(5, "class", "runtime-field-input");
            builder.AddAttribute(6, "type", AllowDecimal ? "number" : "number");
            builder.AddAttribute(7, "step", AllowDecimal ? Step.ToString() : "1");
            if (MinValue.HasValue)
            {
                builder.AddAttribute(8, "min", MinValue.Value);
            }
            if (MaxValue.HasValue)
            {
                builder.AddAttribute(9, "max", MaxValue.Value);
            }
            builder.AddAttribute(10, "value", value);
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
        builder.AddAttribute(6, "style", "height:32px; background:#fff; border:1px solid #e0e0e0; border-radius:2px; display:flex; align-items:center; justify-content:space-between; padding:0 6px; font-size:12px; color:#666;");
        builder.OpenElement(7, "span");
        builder.AddContent(8, "-");
        builder.CloseElement();
        builder.OpenElement(9, "span");
        builder.AddContent(10, "123");
        builder.CloseElement();
        builder.OpenElement(11, "span");
        builder.AddContent(12, "+");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }

    public override string GetDefaultCodePrefix()
    {
        return "number";
    }
}
