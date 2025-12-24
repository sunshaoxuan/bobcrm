namespace BobCrm.App.Models;

/// <summary>
/// DDL历史记录DTO
/// </summary>
public class DDLHistoryDto
{
    public Guid Id { get; set; }
    public string ScriptType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? ErrorMessage { get; set; }
    public string ScriptPreview { get; set; } = string.Empty;
}
