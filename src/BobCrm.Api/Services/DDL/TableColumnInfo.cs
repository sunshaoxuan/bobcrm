namespace BobCrm.Api.Services;

/// <summary>
/// 表列信息
/// </summary>
public class TableColumnInfo
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public string IsNullable { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
}

