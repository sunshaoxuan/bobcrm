namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 单选按钮组控件
/// </summary>
public class RadioWidget : TextWidget
{
    public RadioWidget()
    {
        Type = "radio";
        Label = "LBL_RADIO";
    }

    /// <summary>选项集合</summary>
    public List<ListItem> Items { get; set; } = new();

    /// <summary>默认选中的值</summary>
    public string? DefaultValue { get; set; }

    /// <summary>是否显示为按钮样式</summary>
    public bool ButtonStyle { get; set; } = false;

    /// <summary>布局方向（horizontal: 横向, vertical: 纵向）</summary>
    public string Direction { get; set; } = "horizontal";
}

