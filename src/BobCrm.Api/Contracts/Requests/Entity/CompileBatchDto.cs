namespace BobCrm.Api.Contracts.Requests.Entity;

public record CompileBatchDto
{
    public List<Guid> EntityIds { get; init; } = new();
}
