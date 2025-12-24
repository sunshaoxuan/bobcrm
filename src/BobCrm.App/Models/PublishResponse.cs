namespace BobCrm.App.Models;

/// <summary>
/// 发布响应
/// </summary>
public class PublishResponse
{
    public bool Success { get; set; }
    public Guid ScriptId { get; set; }
    public string DDLScript { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public ChangeAnalysisDto? ChangeAnalysis { get; set; }
    public MenuRegistrationDto? MenuRegistration { get; set; }
}
