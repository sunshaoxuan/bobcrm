namespace BobCrm.App.Models.Widgets;

/// <summary>
/// Grid 容器控件（结构化布局）
/// </summary>
public class GridWidget : ContainerWidget
{
    public GridWidget()
    {
        Type = "grid";
        Label = "LBL_GRID";
        Width = 100;
        WidthUnit = "%";
        Columns = 3;
        Gap = 12;
        Padding = 12;
        BackgroundColor = "#fafafa";
    }

    /// <summary>列数</summary>
    public int Columns { get; set; } = 3;

    /// <summary>行列间距</summary>
    public int Gap { get; set; } = 12;

    /// <summary>内边距</summary>
    public int Padding { get; set; } = 12;

    /// <summary>背景色</summary>
    public string BackgroundColor { get; set; } = "#fafafa";

    /// <summary>是否显示边框</summary>
    public bool Bordered { get; set; } = false;
}
