namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 日历控件
/// 用于日期选择
/// </summary>
public class CalendarWidget : TextWidget
{
    public CalendarWidget()
    {
        Type = "calendar";
        Label = "LBL_CALENDAR";
    }

    public string Format { get; set; } = "yyyy-MM-dd";
    public bool ShowTime { get; set; } = false;
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public DateTime? DefaultValue { get; set; }

    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        return new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "Format", Label = "PROP_DATE_FORMAT", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text, Placeholder = "yyyy-MM-dd" },
            new() { PropertyPath = "ShowTime", Label = "PROP_SHOW_TIME", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = 100 }
        };
    }
}
