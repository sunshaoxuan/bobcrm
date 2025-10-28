using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Domain;

namespace BobCrm.Api.Application.Queries;

public interface ILayoutQueries
{
    object GetUserLayout(string userId, int customerId);
}

public class LayoutQueries : ILayoutQueries
{
    private readonly IRepository<UserLayout> _repo;
    public LayoutQueries(IRepository<UserLayout> repo) => _repo = repo;
    public object GetUserLayout(string userId, int customerId)
    {
        var entity = _repo.Query(x => x.UserId == userId && x.CustomerId == customerId).FirstOrDefault();
        var layout = entity?.LayoutJson;
        var json = string.IsNullOrWhiteSpace(layout) ? new { } : System.Text.Json.JsonSerializer.Deserialize<object>(layout!);
        return json ?? new { };
    }
}
