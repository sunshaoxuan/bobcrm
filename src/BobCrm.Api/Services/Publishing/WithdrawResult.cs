namespace BobCrm.Api.Services;

/// <summary>
/// 撤回结果
/// </summary>
public class WithdrawResult
{
    public Guid EntityDefinitionId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DDLScript { get; set; }
    public Guid? ScriptId { get; set; }
    public string Mode { get; set; } = string.Empty;
}

