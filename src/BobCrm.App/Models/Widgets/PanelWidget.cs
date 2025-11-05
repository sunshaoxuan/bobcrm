namespace BobCrm.App.Models.Widgets;

/// <summary>
/// Panel 容器控件
/// </summary>
public class PanelWidget : ContainerWidget
{
    public PanelWidget()
    {
        Type = "panel";
        Label = "LBL_PANEL";
        Width = 100;
        WidthUnit = "%";
        HeightUnit = "auto";
        ContainerLayout = new ContainerLayoutOptions
        {
            Mode = ContainerLayoutMode.Flow,
            FlexDirection = "row",
            FlexWrap = true,
            JustifyContent = "flex-start",
            AlignItems = "flex-start",
            Gap = 12,
            Padding = 16,
            BackgroundColor = "#ffffff",
            BorderRadius = 6,
            BorderStyle = "solid",
            BorderColor = "#e5e5e5",
            BorderWidth = 1
        };
    }

    /// <summary>Panel 标题</summary>
    public string? Title { get; set; }

    /// <summary>是否显示标题栏</summary>
    public bool ShowHeader { get; set; } = true;

    /// <summary>容器布局选项</summary>
    public ContainerLayoutOptions ContainerLayout { get; set; } = new();
}
