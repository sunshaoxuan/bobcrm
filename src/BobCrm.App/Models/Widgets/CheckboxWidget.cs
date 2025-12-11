using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using AntDesign;
using BobCrm.App.Services.Widgets;

namespace BobCrm.App.Models.Widgets;

[WidgetMetadata("checkbox", "LBL_CHECKBOX", "Outline.CheckSquare", WidgetRegistry.WidgetCategory.Basic)]
/// <summary>
/// 复选框控件（支持单个复选框或复选框组）
/// </summary>
public class CheckboxWidget : TextWidget
{
    public CheckboxWidget()
    {
        Type = "checkbox";
        Label = "LBL_CHECKBOX";
    }

    /// <summary>选项集合（多个选项时为复选框组，空时为单个复选框）</summary>
    public List<ListItem> Items { get; set; } = new();

    /// <summary>默认选中的值（多选时为逗号分隔）</summary>
    public string? DefaultValue { get; set; }

    /// <summary>是否显示为按钮样式</summary>
    public bool ButtonStyle { get; set; } = false;

    /// <summary>布局方向（horizontal: 横向, vertical: 纵向）</summary>
    public string Direction { get; set; } = "horizontal";

    public override Type? PreviewComponentType => typeof(BobCrm.App.Components.Designer.WidgetPreviews.CheckboxPreview);

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
        var selectedValues = value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim()).ToHashSet();

        if (context.Mode == RuntimeWidgetRenderMode.Edit)
        {
            var builder = context.Builder;
            var callbackFactory = new EventCallbackFactory();
            var flexDirection = Direction == "vertical" ? "column" : "row";

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", "display:flex; flex-direction:column; gap:6px;");
            RenderFieldLabel(builder, context.Label);

            builder.OpenElement(4, "div");
            builder.AddAttribute(5, "style", $"display:flex; flex-direction:{flexDirection}; gap:8px; flex-wrap:wrap;");

            if (Items.Count == 0)
            {
                // Single checkbox
                builder.OpenElement(6, "label");
                builder.AddAttribute(7, "style", "display:flex; align-items:center; gap:4px; cursor:pointer;");
                builder.OpenElement(8, "input");
                builder.AddAttribute(9, "type", "checkbox");
                builder.AddAttribute(10, "checked", !string.IsNullOrWhiteSpace(value));
                if (context.ValueSetter != null)
                {
                    builder.AddAttribute(11, "onchange",
                        callbackFactory.Create<ChangeEventArgs>(context.EventTarget,
                            e => context.ValueSetter!(e.Value?.ToString() == "true" ? "true" : "")));
                }
                builder.CloseElement(); // input
                builder.OpenElement(12, "span");
                builder.AddContent(13, Label);
                builder.CloseElement(); // span
                builder.CloseElement(); // label
            }
            else
            {
                // Checkbox group
                foreach (var item in Items)
                {
                    builder.OpenElement(14, "label");
                    builder.AddAttribute(15, "style", "display:flex; align-items:center; gap:4px; cursor:pointer;");
                    builder.OpenElement(16, "input");
                    builder.AddAttribute(17, "type", "checkbox");
                    builder.AddAttribute(18, "value", item.Value);
                    builder.AddAttribute(19, "checked", selectedValues.Contains(item.Value));
                    if (context.ValueSetter != null)
                    {
                        builder.AddAttribute(20, "onchange",
                            callbackFactory.Create<ChangeEventArgs>(context.EventTarget,
                                e =>
                                {
                                    var isChecked = e.Value?.ToString() == "true";
                                    if (isChecked)
                                    {
                                        selectedValues.Add(item.Value);
                                    }
                                    else
                                    {
                                        selectedValues.Remove(item.Value);
                                    }
                                    return context.ValueSetter!(string.Join(",", selectedValues));
                                }));
                    }
                    builder.CloseElement(); // input
                    builder.OpenElement(21, "span");
                    builder.AddContent(22, item.Label ?? item.Value);
                    builder.CloseElement(); // span
                    builder.CloseElement(); // label
                }
            }

            builder.CloseElement(); // checkbox container
            builder.CloseElement(); // outer container
        }
        else
        {
            var displayValue = Items.Count > 0
                ? string.Join(", ", selectedValues.Select(v => Items.FirstOrDefault(i => i.Value == v)?.Label ?? v))
                : (string.IsNullOrWhiteSpace(value) ? "No" : "Yes");
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

        // Show preview checkboxes
        for (int i = 0; i < Math.Min(Items.Count == 0 ? 1 : Items.Count, 3); i++)
        {
            builder.OpenElement(7, "div");
            builder.AddAttribute(8, "style", "display:flex; align-items:center; gap:4px;");
            builder.OpenElement(9, "div");
            builder.AddAttribute(10, "style", "width:14px; height:14px; border:1px solid #d9d9d9; border-radius:2px; background:#fff;");
            builder.CloseElement();
            builder.OpenElement(11, "span");
            builder.AddAttribute(12, "style", "font-size:12px; color:#666;");
            builder.AddContent(13, Items.Count == 0 ? Label : (Items[i].Label ?? Items[i].Value));
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement(); // checkbox container
        builder.CloseElement(); // outer container
    }

    public override string GetDefaultCodePrefix()
    {
        return "checkbox";
    }
}

