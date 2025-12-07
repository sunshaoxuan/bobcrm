namespace BobCrm.Api.Contracts.Responses.Entity;

public class PreviewDdlResultDto
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DdlScript { get; set; } = string.Empty;
}
