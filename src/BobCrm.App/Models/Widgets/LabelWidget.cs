namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 标签控件
/// 纯文本显示，不能编辑
/// </summary>
public class LabelWidget : TextWidget
{
    public LabelWidget()
    {
        Type = "label";
        Label = "LBL_LABEL";
    }

    public string? Text { get; set; }
    public bool Bold { get; set; } = false;

    public override bool CanEditProperty(string propertyName)
    {
        // 标签控件不能编辑DataField属性
        if (propertyName == "DataField") return false;
        return base.CanEditProperty(propertyName);
    }
}
