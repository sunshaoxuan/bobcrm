namespace BobCrm.Api.Contracts.Responses.Entity;

public class LoadedEntitiesDto
{
    public int Count { get; set; }
    public List<string> Entities { get; set; } = new();
}
