namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 列表框控件
/// 下拉选择框
/// </summary>
public class ListboxWidget : TextWidget
{
    public ListboxWidget()
    {
        Type = "listbox";
        Label = "LBL_LISTBOX";
    }

    public List<ListItem> Items { get; set; } = new();
    public bool MultiSelect { get; set; } = false;
    public string? Placeholder { get; set; }
    public bool AllowSearch { get; set; } = false;
    public string? DefaultValue { get; set; }
}
