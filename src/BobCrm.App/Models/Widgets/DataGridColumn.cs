using System.Text.Json.Serialization;

namespace BobCrm.App.Models.Widgets;

/// <summary>
/// DataGrid 列定义模型（设计态与运行态共享）
/// </summary>
public class DataGridColumn
{
    /// <summary>绑定字段名</summary>
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    /// <summary>列标题（多语言键或直接文本）</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>列宽（像素，可空表示自适应）</summary>
    [JsonPropertyName("width")]
    public int? Width { get; set; }

    /// <summary>是否显示</summary>
    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    /// <summary>是否可排序</summary>
    [JsonPropertyName("sortable")]
    public bool Sortable { get; set; } = true;

    /// <summary>格式化字符串（可选）</summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    /// <summary>对齐方式（left/center/right）</summary>
    [JsonPropertyName("align")]
    public string Align { get; set; } = "left";
}
