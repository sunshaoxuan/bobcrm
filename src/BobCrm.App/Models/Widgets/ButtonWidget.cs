namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 按钮控件
/// </summary>
public class ButtonWidget : TextWidget
{
    public ButtonWidget()
    {
        Type = "button";
        Label = "LBL_BUTTON";
    }

    /// <summary>按钮样式：primary/default/dashed/link/text</summary>
    public string Variant { get; set; } = "primary";

    /// <summary>按钮尺寸：large/default/small</summary>
    public string Size { get; set; } = "default";

    /// <summary>关联动作标识（如 openUrl/downloadRdp 等）</summary>
    public string? Action { get; set; }

    /// <summary>动作参数（URL、文件Key 等）</summary>
    public string? ActionPayload { get; set; }

    /// <summary>图标（Ant Design Icon 名称）</summary>
    public string? Icon { get; set; }

    /// <summary>是否块级按钮</summary>
    public bool Block { get; set; } = false;
}
