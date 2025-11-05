namespace BobCrm.App.Models.Widgets;

/// <summary>
/// Tabs 容器控件
/// </summary>
public class TabContainerWidget : ContainerWidget
{
    public TabContainerWidget()
    {
        Type = "tabbox";
        Label = "LBL_TABBOX";
        Width = 100;
        WidthUnit = "%";
        HeightUnit = "auto";
        Children = new List<DraggableWidget>();
    }

    /// <summary>默认激活的 TabId</summary>
    public string? ActiveTabId { get; set; }

    /// <summary>是否带动画切换</summary>
    public bool Animated { get; set; } = true;

    /// <summary>标签是否可居中显示</summary>
    public bool Centered { get; set; } = false;
}
