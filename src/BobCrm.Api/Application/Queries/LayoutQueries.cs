using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Domain;

namespace BobCrm.Api.Application.Queries;

public interface ILayoutQueries
{
    object GetUserLayout(string userId, int customerId);
    object GetLayout(string userId, int customerId, string scope);
    object GetEffectiveLayout(string userId, int customerId);
}

public class LayoutQueries : ILayoutQueries
{
    private readonly IRepository<UserLayout> _repo;
    public LayoutQueries(IRepository<UserLayout> repo) => _repo = repo;
    private const string DefaultUserId = "__default__";

    public object GetUserLayout(string userId, int customerId)
        => ReadJson(_repo.Query(x => x.UserId == userId && x.CustomerId == customerId).FirstOrDefault()?.LayoutJson);

    public object GetLayout(string userId, int customerId, string scope)
    {
        scope = (scope ?? "effective").ToLowerInvariant();
        return scope switch
        {
            "user" => GetUserLayout(userId, customerId),
            "default" => ReadJson(_repo.Query(x => x.UserId == DefaultUserId && x.CustomerId == customerId).FirstOrDefault()?.LayoutJson),
            _ => GetEffectiveLayout(userId, customerId)
        };
    }

    public object GetEffectiveLayout(string userId, int customerId)
    {
        var user = _repo.Query(x => x.UserId == userId && x.CustomerId == customerId).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(user?.LayoutJson)) return ReadJson(user!.LayoutJson);
        var def = _repo.Query(x => x.UserId == DefaultUserId && x.CustomerId == customerId).FirstOrDefault();
        return ReadJson(def?.LayoutJson);
    }

    private static object ReadJson(string? layout)
    {
        var json = string.IsNullOrWhiteSpace(layout) ? new { } : System.Text.Json.JsonSerializer.Deserialize<object>(layout!);
        return json ?? new { };
    }
}
