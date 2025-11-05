namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 单选下拉控件
/// </summary>
public class SelectWidget : TextWidget
{
    public SelectWidget()
    {
        Type = "select";
        Label = "LBL_SELECT";
    }

    /// <summary>选项集合</summary>
    public List<ListItem> Items { get; set; } = new();

    /// <summary>默认值</summary>
    public string? DefaultValue { get; set; }

    /// <summary>占位提示</summary>
    public string? Placeholder { get; set; }

    /// <summary>是否允许搜索</summary>
    public bool AllowSearch { get; set; } = false;
}
