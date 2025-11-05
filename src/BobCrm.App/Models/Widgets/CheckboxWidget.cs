namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 复选框控件（支持单个复选框或复选框组）
/// </summary>
public class CheckboxWidget : TextWidget
{
    public CheckboxWidget()
    {
        Type = "checkbox";
        Label = "LBL_CHECKBOX";
    }

    /// <summary>选项集合（多个选项时为复选框组，空时为单个复选框）</summary>
    public List<ListItem> Items { get; set; } = new();

    /// <summary>默认选中的值（多选时为逗号分隔）</summary>
    public string? DefaultValue { get; set; }

    /// <summary>是否显示为按钮样式</summary>
    public bool ButtonStyle { get; set; } = false;

    /// <summary>布局方向（horizontal: 横向, vertical: 纵向）</summary>
    public string Direction { get; set; } = "horizontal";
}

