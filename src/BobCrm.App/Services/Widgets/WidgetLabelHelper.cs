using BobCrm.App.Models.Widgets;

namespace BobCrm.App.Services.Widgets;

/// <summary>
/// Widget标签助手
/// 负责处理Widget标签的获取、翻译、显示等逻辑
/// </summary>
public static class WidgetLabelHelper
{
    /// <summary>
    /// 获取Widget在设计模式下的显示标签
    /// 处理国际化键和旧数据兼容
    /// </summary>
    public static string GetDesignLabel(DraggableWidget widget, Func<string, string> translator)
    {
        // 处理旧数据中可能存在的中文"容器"标签
        if (widget.Type == "frame" && (widget.Label == "容器" || widget.Label == "LBL_FRAME"))
        {
            return translator("LBL_FRAME");
        }
        
        // 如果Label是翻译键，尝试翻译
        if (widget.Label?.StartsWith("LBL_") == true)
        {
            return translator(widget.Label);
        }
        
        return widget.Label ?? "";
    }

    /// <summary>
    /// 获取Widget在运行模式下的标签
    /// 优先使用字段定义的label，其次是控件的Label属性
    /// </summary>
    public static string GetRuntimeLabel(
        DraggableWidget widget, 
        Func<string, string?> fieldLabelGetter,
        Func<string, string> translator)
    {
        // 优先使用字段定义的标签
        if (!string.IsNullOrWhiteSpace(widget.DataField))
        {
            var fieldLabel = fieldLabelGetter(widget.DataField);
            if (!string.IsNullOrWhiteSpace(fieldLabel))
            {
                return fieldLabel;
            }
        }

        // 其次使用控件的Label属性
        if (!string.IsNullOrWhiteSpace(widget.Label))
        {
            return widget.Label.StartsWith("LBL_", StringComparison.Ordinal)
                ? translator(widget.Label)
                : widget.Label;
        }

        // 最后使用控件类型
        return widget.Type;
    }
}

