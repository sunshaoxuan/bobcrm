using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Domain;

namespace BobCrm.Api.Application.Queries;

public interface IFieldQueries
{
    List<object> GetDefinitions();
}

public class FieldQueries : IFieldQueries
{
    private readonly IRepository<FieldDefinition> _repo;
    public FieldQueries(IRepository<FieldDefinition> repo) => _repo = repo;
    public List<object> GetDefinitions()
    {
        var defs = _repo.Query().ToList();
        return defs.Select(f => new
        {
            key = f.Key,
            label = f.DisplayName,
            type = f.DataType,
            tags = string.IsNullOrWhiteSpace(f.Tags) ? new string[0] : System.Text.Json.JsonSerializer.Deserialize<string[]>(f.Tags!)!,
            actions = string.IsNullOrWhiteSpace(f.Actions) ? Array.Empty<object>() : System.Text.Json.JsonSerializer.Deserialize<object[]>(f.Actions!)!
        }).Cast<object>().ToList();
    }
}
