using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using AntDesign;
using BobCrm.App.Services.Widgets;

namespace BobCrm.App.Models.Widgets;

[WidgetMetadata("calendar", "LBL_CALENDAR", "Outline.Calendar", WidgetCategory.Basic)]
/// <summary>
/// 日历控件
/// 用于日期选择
/// </summary>
public class CalendarWidget : TextWidget
{
    public CalendarWidget()
    {
        Type = "calendar";
        Label = "LBL_CALENDAR";
    }

    public string Format { get; set; } = "yyyy-MM-dd";
    public bool ShowTime { get; set; } = false;
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public DateTime? DefaultValue { get; set; }

    public override Type? PreviewComponentType => typeof(BobCrm.App.Components.Designer.WidgetPreviews.CalendarPreview);

    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        var properties = base.GetPropertyMetadata();

        properties.AddRange(new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "Format", Label = "PROP_DATE_FORMAT", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text, Placeholder = "yyyy-MM-dd" },
            new() { PropertyPath = "ShowTime", Label = "PROP_SHOW_TIME", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = GetMaxWidth() }
        });

        return properties;
    }

    public override void RenderRuntime(RuntimeRenderContext context)
    {
        var value = context.ValueGetter?.Invoke() ?? string.Empty;
        if (DateTime.TryParse(value, out var dt))
        {
            value = dt.ToString("yyyy-MM-dd");
        }

        if (context.Mode == RuntimeWidgetRenderMode.Edit)
        {
            var builder = context.Builder;
            var callbackFactory = new EventCallbackFactory();

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", "display:flex; flex-direction:column; gap:6px;");
            RenderFieldLabel(builder, context.Label);
            builder.OpenElement(4, "input");
            builder.AddAttribute(5, "class", "runtime-field-input");
            builder.AddAttribute(6, "type", "date");
            builder.AddAttribute(7, "value", value);
            if (context.ValueSetter != null)
            {
                builder.AddAttribute(8, "onchange",
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
        builder.AddAttribute(6, "style", "height:32px; background:#fff; border:1px solid #e0e0e0; border-radius:2px; display:flex; align-items:center; padding:0 4px;");
        builder.OpenElement(7, "span");
        builder.AddAttribute(8, "style", "font-size:10px; color:#999;");
        builder.AddContent(9, "📅");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }

    public override string GetDefaultCodePrefix()
    {
        return "calendar";
    }
}
