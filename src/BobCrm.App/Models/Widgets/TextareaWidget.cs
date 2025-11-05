namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 多行文本输入控件
/// </summary>
public class TextareaWidget : TextWidget
{
    public TextareaWidget()
    {
        Type = "textarea";
        Label = "LBL_TEXTAREA";
    }

    /// <summary>默认值</summary>
    public string? DefaultValue { get; set; }

    /// <summary>占位提示</summary>
    public string? Placeholder { get; set; }

    /// <summary>最大长度</summary>
    public int? MaxLength { get; set; }

    /// <summary>行数</summary>
    public int Rows { get; set; } = 4;

    /// <summary>是否根据内容自动增长</summary>
    public bool AutoSize { get; set; } = true;
}
