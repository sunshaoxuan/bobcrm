using BobCrm.App.Models.Widgets;
using BobCrm.App.Services.Widgets.Rendering;

namespace BobCrm.App.Services.Widgets;

/// <summary>
/// Widget样式生成助手类
/// 集中管理控件样式的生成逻辑，避免在页面中重复代码
/// </summary>
public static class WidgetStyleHelper
{
    /// <summary>
    /// 计算FlexBasis（宽度基础值）
    /// </summary>
    public static string CalculateFlexBasis(DraggableWidget widget, int gap = 6)
    {
        return widget.WidthUnit == "%"
            ? $"calc({widget.Width}% - {gap}px)"
            : $"{widget.Width}px";
    }

    /// <summary>
    /// 生成基本设计态控件样式
    /// </summary>
    public static string GetDesignWidgetStyle(DraggableWidget widget, bool isSelected, int gap = 6)
    {
        var basis = CalculateFlexBasis(widget, gap);
        var border = isSelected ? "border:2px solid #1890ff !important" : "border:1px solid #e0e0e0";
        
        return $"width:{basis}; {border}";
    }

    /// <summary>
    /// 生成Flow布局控件样式（设计态）
    /// </summary>
    public static string GetFlowWidgetStyle(DraggableWidget widget, bool isSelected, int gap = 4)
    {
        var basis = CalculateFlexBasis(widget, gap);
        
        // 设计态使用组件自己定义的最小高度，而不是运行态的 Height 属性
        var minHeight = widget.GetDesignMinHeight();
        var heightStyle = $"min-height:{minHeight}px;";
        
        var border = isSelected ? "2px solid #1890ff" : "1px solid #e0e0e0";
        var zIndex = isSelected ? "z-index:100" : "z-index:1";
        
        return $"flex:0 0 {basis}; max-width:{basis}; {heightStyle} background:#fff; border-radius:4px; " +
               $"border:{border}; box-shadow:0 1px 3px rgba(0,0,0,0.1); user-select:none; position:relative; " +
               $"margin:4px; {zIndex} box-sizing:border-box; display:flex; flex-direction:column;";
    }

    /// <summary>
    /// 生成运行态控件样式
    /// </summary>
    public static string GetRuntimeWidgetStyle(DraggableWidget widget, RuntimeWidgetRenderMode mode)
    {
        var basis = CalculateFlexBasis(widget, gap: 8);
        var minHeight = widget.HeightUnit == "px" ? Math.Max(widget.Height, 32) : 64;
        var background = mode == RuntimeWidgetRenderMode.Edit ? "#ffffff" : "#fdfdfd";
        
        return $"flex:0 0 {basis}; max-width:{basis}; min-height:{minHeight}px; background:{background}; " +
               $"border-radius:6px; border:1px solid #e0e0e0; box-shadow:0 1px 2px rgba(0,0,0,0.06); " +
               $"padding:12px; box-sizing:border-box; display:flex; flex-direction:column; gap:8px;";
    }

    /// <summary>
    /// 生成嵌套子项样式（设计态）
    /// </summary>
    public static string GetNestedChildStyle(DraggableWidget widget, bool isSelected)
    {
        var basis = CalculateFlexBasis(widget, gap: 4);
        var border = isSelected ? "2px solid #ff4d4f" : "1px solid #d0d0d0";
        var zIndex = isSelected ? "z-index:100" : "z-index:1";
        
        return $"flex:0 0 {basis}; max-width:{basis}; min-height:32px; background:#fff; border-radius:2px; " +
               $"border:{border}; box-shadow:0 1px 2px rgba(0,0,0,0.05); user-select:none; position:relative; " +
               $"{zIndex} box-sizing:border-box; cursor:move; display:flex; flex-direction:column;";
    }

    /// <summary>
    /// 判断是否为容器类型
    /// </summary>
    public static bool IsContainerType(string? type)
    {
        return type switch
        {
            "frame" => true,
            "section" => true,
            "panel" => true,
            "grid" => true,
            "tabbox" => true,
            "tab" => true,
            _ => false
        };
    }

    /// <summary>
    /// 生成文本样式（从控件属性）
    /// </summary>
    public static string GetTextStyle(DraggableWidget widget)
    {
        if (widget is not TextWidget textWidget)
            return string.Empty;

        return $"font-size:{textWidget.FontSize}px; color:{textWidget.FontColor}; " +
               $"font-family:{textWidget.FontFamily}; font-weight:{textWidget.FontWeight}; " +
               $"text-align:{textWidget.TextAlign};";
    }

    /// <summary>
    /// 从ExtendedProperties获取背景颜色
    /// </summary>
    public static string GetBackgroundColor(DraggableWidget widget)
    {
        if (widget?.ExtendedProperties == null)
            return "#fafafa";

        if (widget.ExtendedProperties.TryGetValue("backgroundColor", out var bg) && bg != null)
            return bg.ToString() ?? "#fafafa";

        return "#fafafa";
    }
}

