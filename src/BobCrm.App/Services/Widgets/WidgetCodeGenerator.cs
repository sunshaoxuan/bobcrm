using BobCrm.App.Models.Widgets;

namespace BobCrm.App.Services.Widgets;

/// <summary>
/// Widget Code 生成器和校验器
/// 负责为新组件生成唯一的 Code，并验证 Code 的唯一性
/// </summary>
public static class WidgetCodeGenerator
{
    /// <summary>
    /// 为新创建的 Widget 生成唯一的 Code
    /// </summary>
    /// <param name="widget">要生成 Code 的组件</param>
    /// <param name="allWidgets">所有现有的组件（用于检查唯一性）</param>
    /// <returns>生成的唯一 Code</returns>
    public static string GenerateUniqueCode(DraggableWidget widget, IEnumerable<DraggableWidget> allWidgets)
    {
        var prefix = widget.GetDefaultCodePrefix();
        var existingCodes = GetAllCodes(allWidgets).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 如果已经有 Code 且唯一，直接返回
        if (!string.IsNullOrWhiteSpace(widget.Code) && !existingCodes.Contains(widget.Code))
        {
            return widget.Code;
        }

        // 生成新的 Code：prefix + 递增数字
        int counter = 1;
        string candidateCode;

        do
        {
            candidateCode = $"{prefix}{counter}";
            counter++;
        } while (existingCodes.Contains(candidateCode));

        return candidateCode;
    }

    /// <summary>
    /// 验证 Code 是否唯一
    /// </summary>
    /// <param name="code">要验证的 Code</param>
    /// <param name="widgetId">当前组件的 Id（用于排除自己）</param>
    /// <param name="allWidgets">所有现有的组件</param>
    /// <returns>如果唯一返回 true，否则返回 false</returns>
    public static bool IsCodeUnique(string code, string widgetId, IEnumerable<DraggableWidget> allWidgets)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return !allWidgets.Any(w =>
            w.Id != widgetId &&
            string.Equals(w.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取所有组件的 Code（包括嵌套的子组件）
    /// </summary>
    private static IEnumerable<string> GetAllCodes(IEnumerable<DraggableWidget> widgets)
    {
        foreach (var widget in widgets)
        {
            if (!string.IsNullOrWhiteSpace(widget.Code))
            {
                yield return widget.Code;
            }

            // 递归处理子组件
            if (widget.Children != null && widget.Children.Count > 0)
            {
                foreach (var childCode in GetAllCodes(widget.Children))
                {
                    yield return childCode;
                }
            }
        }
    }

    /// <summary>
    /// 验证并修复 Code（如果不唯一则生成新的）
    /// </summary>
    /// <param name="widget">要验证的组件</param>
    /// <param name="allWidgets">所有现有的组件</param>
    /// <returns>验证结果和可能的新 Code</returns>
    public static (bool IsValid, string SuggestedCode) ValidateAndSuggestCode(
        DraggableWidget widget,
        IEnumerable<DraggableWidget> allWidgets)
    {
        if (string.IsNullOrWhiteSpace(widget.Code))
        {
            var newCode = GenerateUniqueCode(widget, allWidgets);
            return (false, newCode);
        }

        var isUnique = IsCodeUnique(widget.Code, widget.Id, allWidgets);

        if (!isUnique)
        {
            var newCode = GenerateUniqueCode(widget, allWidgets);
            return (false, newCode);
        }

        return (true, widget.Code);
    }
}
