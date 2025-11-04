namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 文本框控件
/// </summary>
public class TextboxWidget : TextWidget
{
    public TextboxWidget()
    {
        Type = "textbox";
        Label = "LBL_TEXTBOX";
    }

    public string? Placeholder { get; set; }
    public bool Required { get; set; } = false;
    public bool Readonly { get; set; } = false;
    public int? MaxLength { get; set; }
    public string? ValidationPattern { get; set; }
    public string? DefaultValue { get; set; }
}
