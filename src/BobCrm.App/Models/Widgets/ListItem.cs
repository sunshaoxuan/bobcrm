namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 列表项
/// 用于下拉框、列表框等控件的选项
/// </summary>
public class ListItem
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
    public bool Selected { get; set; } = false;
}
