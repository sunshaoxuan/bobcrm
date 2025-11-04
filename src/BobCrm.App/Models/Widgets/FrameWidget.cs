namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 框架控件（容器）
/// 可以包含其他子控件
/// </summary>
public class FrameWidget : ContainerWidget
{
    public FrameWidget()
    {
        Type = "frame";
        Label = "LBL_FRAME";
        Width = 100;
        WidthUnit = "%";
        Height = 200;
    }

    public string BorderStyle { get; set; } = "solid";
    public string BorderColor { get; set; } = "#d9d9d9";
    public int BorderWidth { get; set; } = 2;
    public string BackgroundColor { get; set; } = "#fafafa";
    public int Padding { get; set; } = 8;

    public override bool CanEditProperty(string propertyName)
    {
        // 框架控件不能编辑DataField属性
        if (propertyName == "DataField") return false;
        return base.CanEditProperty(propertyName);
    }
}
