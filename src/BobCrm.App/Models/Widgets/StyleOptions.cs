namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 统一的样式选项
/// 避免样式散落在各控件，提供组合式外观配置
/// </summary>
public class StyleOptions
{
    /// <summary>背景色（支持 CSS 变量）</summary>
    public string BackgroundColor { get; set; } = "transparent";

    /// <summary>边框颜色</summary>
    public string BorderColor { get; set; } = "var(--border-color, #d9d9d9)";

    /// <summary>边框宽度（像素）</summary>
    public int BorderWidth { get; set; } = 0;

    /// <summary>边框样式</summary>
    public string BorderStyle { get; set; } = "solid";

    /// <summary>圆角（像素）</summary>
    public int BorderRadius { get; set; } = 0;

    /// <summary>阴影级别</summary>
    public ShadowLevel Shadow { get; set; } = ShadowLevel.None;

    /// <summary>内边距（像素）</summary>
    public int Padding { get; set; } = 0;

    /// <summary>外边距（像素）</summary>
    public int Margin { get; set; } = 0;

    /// <summary>文字颜色</summary>
    public string? TextColor { get; set; }

    /// <summary>字体大小（像素）</summary>
    public int? FontSize { get; set; }

    /// <summary>字体粗细</summary>
    public string? FontWeight { get; set; }

    /// <summary>
    /// 生成 CSS 样式字符串
    /// 与 LayoutOptions.GetCssStyle() 并列使用
    /// </summary>
    public string GetCssStyle()
    {
        var styles = new List<string>();

        // 背景
        if (!string.IsNullOrEmpty(BackgroundColor) && BackgroundColor != "transparent")
        {
            styles.Add($"background-color: {BackgroundColor}");
        }

        // 边框
        if (BorderWidth > 0)
        {
            styles.Add($"border: {BorderWidth}px {BorderStyle} {BorderColor}");
        }

        // 圆角
        if (BorderRadius > 0)
        {
            styles.Add($"border-radius: {BorderRadius}px");
        }

        // 阴影
        if (Shadow != ShadowLevel.None)
        {
            styles.Add($"box-shadow: {GetShadowValue(Shadow)}");
        }

        // 内边距
        if (Padding > 0)
        {
            styles.Add($"padding: {Padding}px");
        }

        // 外边距
        if (Margin > 0)
        {
            styles.Add($"margin: {Margin}px");
        }

        // 文字样式
        if (!string.IsNullOrEmpty(TextColor))
        {
            styles.Add($"color: {TextColor}");
        }

        if (FontSize.HasValue && FontSize.Value > 0)
        {
            styles.Add($"font-size: {FontSize.Value}px");
        }

        if (!string.IsNullOrEmpty(FontWeight))
        {
            styles.Add($"font-weight: {FontWeight}");
        }

        return styles.Count > 0 ? string.Join("; ", styles) : "";
    }

    /// <summary>
    /// 获取阴影的 CSS 值
    /// </summary>
    private static string GetShadowValue(ShadowLevel level)
    {
        return level switch
        {
            ShadowLevel.XS => "0 1px 2px rgba(0,0,0,0.05)",
            ShadowLevel.Small => "0 1px 3px rgba(0,0,0,0.1), 0 1px 2px rgba(0,0,0,0.06)",
            ShadowLevel.Medium => "0 4px 6px rgba(0,0,0,0.1), 0 2px 4px rgba(0,0,0,0.06)",
            ShadowLevel.Large => "0 10px 15px rgba(0,0,0,0.1), 0 4px 6px rgba(0,0,0,0.05)",
            ShadowLevel.XL => "0 20px 25px rgba(0,0,0,0.1), 0 10px 10px rgba(0,0,0,0.04)",
            _ => ""
        };
    }

    /// <summary>
    /// 创建卡片预设样式
    /// </summary>
    public static StyleOptions CardPreset()
    {
        return new StyleOptions
        {
            BackgroundColor = "var(--card-bg, #ffffff)",
            BorderColor = "var(--border-color, #e8e8e8)",
            BorderWidth = 1,
            BorderRadius = 8,
            Shadow = ShadowLevel.Small,
            Padding = 16
        };
    }

    /// <summary>
    /// 创建面板预设样式
    /// </summary>
    public static StyleOptions PanelPreset()
    {
        return new StyleOptions
        {
            BackgroundColor = "var(--panel-bg, #fafafa)",
            BorderColor = "var(--border-color, #d9d9d9)",
            BorderWidth = 1,
            BorderRadius = 4,
            Padding = 12
        };
    }

    /// <summary>
    /// 创建强对比色块预设样式
    /// </summary>
    public static StyleOptions ContrastBlockPreset()
    {
        return new StyleOptions
        {
            BackgroundColor = "var(--primary-color, #1890ff)",
            BorderRadius = 8,
            TextColor = "#ffffff",
            Padding = 16,
            Shadow = ShadowLevel.Medium
        };
    }
}

/// <summary>
/// 阴影级别枚举
/// </summary>
public enum ShadowLevel
{
    /// <summary>无阴影</summary>
    None,

    /// <summary>极小阴影</summary>
    XS,

    /// <summary>小阴影</summary>
    Small,

    /// <summary>中等阴影</summary>
    Medium,

    /// <summary>大阴影</summary>
    Large,

    /// <summary>超大阴影</summary>
    XL
}
