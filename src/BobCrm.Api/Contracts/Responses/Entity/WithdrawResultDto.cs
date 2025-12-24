namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 实体撤回结果 DTO
/// </summary>
public class WithdrawResultDto
{
    public bool Success { get; set; }
    public Guid? ScriptId { get; set; }
    public string? DdlScript { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

