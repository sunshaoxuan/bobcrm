namespace BobCrm.App.Services.Widgets;

/// <summary>
/// 字段Payload（用于API提交）
/// </summary>
public class FieldPayload
{
    public string key { get; set; } = string.Empty;
    public object? value { get; set; }
}
