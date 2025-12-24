using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using AntDesign;
using BobCrm.App.Services.Widgets;

namespace BobCrm.App.Models.Widgets;

[WidgetMetadata("radio", "LBL_RADIO", "Outline.DotChart", WidgetCategory.Basic)]
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

    public override Type? PreviewComponentType => typeof(BobCrm.App.Components.Designer.WidgetPreviews.RadioPreview);

    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        var properties = base.GetPropertyMetadata();

        properties.AddRange(new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
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
            var flexDirection = Direction == "vertical" ? "column" : "row";
            var radioGroupName = $"radio_{Id}";

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", "display:flex; flex-direction:column; gap:6px;");
            RenderFieldLabel(builder, context.Label);

            builder.OpenElement(4, "div");
            builder.AddAttribute(5, "style", $"display:flex; flex-direction:{flexDirection}; gap:8px; flex-wrap:wrap;");

            foreach (var item in Items)
            {
                builder.OpenElement(6, "label");
                builder.AddAttribute(7, "style", "display:flex; align-items:center; gap:4px; cursor:pointer;");
                builder.OpenElement(8, "input");
                builder.AddAttribute(9, "type", "radio");
                builder.AddAttribute(10, "name", radioGroupName);
                builder.AddAttribute(11, "value", item.Value);
                builder.AddAttribute(12, "checked", item.Value == value);
                if (context.ValueSetter != null)
                {
                    builder.AddAttribute(13, "onchange",
                        callbackFactory.Create<ChangeEventArgs>(context.EventTarget,
                            e => context.ValueSetter!(e.Value?.ToString() ?? "")));
                }
                builder.CloseElement(); // input
                builder.OpenElement(14, "span");
                builder.AddContent(15, item.Label ?? item.Value);
                builder.CloseElement(); // span
                builder.CloseElement(); // label
            }

            builder.CloseElement(); // radio container
            builder.CloseElement(); // outer container
        }
        else
        {
            var displayValue = Items.FirstOrDefault(i => i.Value == value)?.Label ?? value;
            RenderReadOnlyValue(context, displayValue);
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

        var flexDirection = Direction == "vertical" ? "column" : "row";
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", $"display:flex; flex-direction:{flexDirection}; gap:8px;");

        // Show preview radio buttons
        for (int i = 0; i < Math.Min(Items.Count, 3); i++)
        {
            builder.OpenElement(7, "div");
            builder.AddAttribute(8, "style", "display:flex; align-items:center; gap:4px;");
            builder.OpenElement(9, "div");
            builder.AddAttribute(10, "style", "width:14px; height:14px; border:1px solid #d9d9d9; border-radius:50%; background:#fff;");
            builder.CloseElement();
            builder.OpenElement(11, "span");
            builder.AddAttribute(12, "style", "font-size:12px; color:#666;");
            builder.AddContent(13, Items[i].Label ?? Items[i].Value);
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement(); // radio container
        builder.CloseElement(); // outer container
    }

    public override string GetDefaultCodePrefix()
    {
        return "radio";
    }
}

