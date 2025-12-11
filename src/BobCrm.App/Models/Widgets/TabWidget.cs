using AntDesign;
using BobCrm.App.Services.Widgets;

namespace BobCrm.App.Models.Widgets;

[WidgetMetadata("tab", "LBL_TAB", "Outline.Tag", WidgetRegistry.WidgetCategory.Layout)]
/// <summary>
/// Tabs 容器中的单个 Tab 面板
/// </summary>
public class TabWidget : ContainerWidget
{
    public TabWidget()
    {
        Type = "tab";
        Label = "Tab";
        TabId = Guid.NewGuid().ToString("N");
        Width = 100;
        WidthUnit = "%";
        HeightUnit = "auto";
        Children = new List<DraggableWidget>();
    }

    /// <summary>标签唯一标识</summary>
    public string TabId { get; set; }

    /// <summary>是否默认激活</summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>标签图标（Ant Design Icon 名称，可选）</summary>
    public string? Icon { get; set; }

    public override string GetDefaultCodePrefix()
    {
        return "tab";
    }
}
