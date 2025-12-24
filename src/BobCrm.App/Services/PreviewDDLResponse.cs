namespace BobCrm.App.Services;

/// <summary>
/// 预览DDL响应
/// </summary>
public class PreviewDDLResponse
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DDLScript { get; set; } = string.Empty;
}
