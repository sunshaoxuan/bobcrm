using System;
using System.Collections.Generic;
using System.Linq;
using AntDesign;
using BobCrm.App.Services.Widgets;
using Microsoft.AspNetCore.Components;

namespace BobCrm.App.Models.Widgets;

[WidgetMetadata("grid", "LBL_GRID", "Outline.BorderOuter", WidgetCategory.Layout)]
/// <summary>
/// Grid 容器控件（结构化布局）
/// </summary>
public class GridWidget : ContainerWidget
{
    public GridWidget()
    {
        Type = "grid";
        Label = "LBL_GRID";
        Width = 100;
        WidthUnit = "%";
        Columns = 3;
        Gap = 12;
        Padding = 12;
        BackgroundColor = "#fafafa";
    }

    /// <summary>列数</summary>
    public int Columns { get; set; } = 3;

    /// <summary>行列间距</summary>
    public int Gap { get; set; } = 12;

    /// <summary>内边距</summary>
    public int Padding { get; set; } = 12;

    /// <summary>背景色</summary>
    public string BackgroundColor { get; set; } = "#fafafa";

    /// <summary>是否显示边框</summary>
    public bool Bordered { get; set; } = false;

    /// <summary>
    /// 获取 Grid 控件的属性元数据
    /// </summary>
    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        var properties = base.GetPropertyMetadata();

        properties.AddRange(new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Columns", Label = "PROP_COLUMNS", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = 12 },
            new() { PropertyPath = "Gap", Label = "PROP_GAP", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 0, Max = 48 },
            new() { PropertyPath = "Padding", Label = "PROP_PADDING", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 0, Max = 48 },
            new() { PropertyPath = "BackgroundColor", Label = "PROP_BACKGROUND_COLOR", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Color, Placeholder = "#fafafa" },
            new() { PropertyPath = "Bordered", Label = "PROP_SHOW_BORDER", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean }
        });

        return properties;
    }

    public override string GetDefaultCodePrefix()
    {
        return "grid";
    }

    public override void RenderRuntime(RuntimeRenderContext context)
    {
        var children = Children?.Where(c => c.Visible).ToList() ?? new List<DraggableWidget>();

        context.Builder.OpenComponent<Row>(0);
        context.Builder.AddAttribute(1, "Gutter", new int[] { Gap, Gap });

        var style = $"width:100%; padding:{Padding}px; background:{BackgroundColor};";
        if (Bordered)
        {
            style += " border:1px solid #d9d9d9; border-radius:6px;";
        }
        context.Builder.AddAttribute(2, "Style", style);

        context.Builder.AddAttribute(3, "ChildContent", (RenderFragment)(builder =>
        {
            if (children.Count == 0)
            {
                builder.OpenComponent<Col>(0);
                builder.AddAttribute(1, "Span", 24);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
                {
                    b.OpenComponent<Empty>(0);
                    b.AddAttribute(1, "Description", "拖入组件到网格");
                    b.CloseComponent();
                }));
                builder.CloseComponent();
                return;
            }

            foreach (var child in children)
            {
                var span = ResolveSpan(child);
                builder.OpenComponent<Col>(0);
                builder.AddAttribute(1, "Span", span);
                builder.AddAttribute(2, "Style", BobCrm.App.Services.Widgets.WidgetStyleHelper.GetRuntimeWidgetStyle(child, context.Mode));
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, context.RenderChild(child))));
                builder.CloseComponent();
            }
        }));

        context.Builder.CloseComponent();
    }

    private static int ResolveSpan(DraggableWidget widget)
    {
        if (string.Equals(widget.WidthUnit, "%", StringComparison.OrdinalIgnoreCase))
        {
            return ClampSpan((int)Math.Round(widget.Width / 100.0 * 24.0));
        }

        if (string.Equals(widget.WidthUnit, "px", StringComparison.OrdinalIgnoreCase))
        {
            return ClampSpan((int)Math.Round(widget.Width / 50.0));
        }

        return 24;
    }

    private static int ClampSpan(int span) => Math.Max(1, Math.Min(24, span));
}
