namespace BobCrm.App.Models.Designer;

/// <summary>
/// 下拉选项
/// </summary>
public class PropertyOption
{
    public required string Value { get; init; }
    /// <summary>选项标签（多语言键，如 "PROP_DIRECTION_ROW"）</summary>
    public required string Label { get; init; }
}
