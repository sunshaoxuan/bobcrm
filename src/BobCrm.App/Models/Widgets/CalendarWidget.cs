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
}
