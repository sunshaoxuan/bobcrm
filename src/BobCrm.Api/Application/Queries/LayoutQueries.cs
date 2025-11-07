using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Domain;

namespace BobCrm.Api.Application.Queries;

public interface ILayoutQueries
{
    // 旧版API（向后兼容）
    object GetUserLayout(string userId, int customerId);
    object GetLayout(string userId, int customerId, string scope);
    object GetEffectiveLayout(string userId, int customerId);

    // 新版API（支持EntityType）
    object GetLayoutByEntityType(string userId, string entityType, string scope);
    object GetEffectiveLayoutByEntityType(string userId, string entityType);
}

public class LayoutQueries : ILayoutQueries
{
    private readonly IRepository<UserLayout> _repo;
    public LayoutQueries(IRepository<UserLayout> repo) => _repo = repo;
    private const string DefaultUserId = "__default__";

    // ===== 旧版方法（向后兼容，基于CustomerId） =====

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

    // ===== 新版方法（基于EntityType） =====

    /// <summary>
    /// 根据实体类型获取布局（支持user/default/effective三种scope）
    /// </summary>
    public object GetLayoutByEntityType(string userId, string entityType, string scope)
    {
        scope = (scope ?? "effective").ToLowerInvariant();
        return scope switch
        {
            "user" => GetUserLayoutByEntityType(userId, entityType),
            "default" => GetDefaultLayoutByEntityType(entityType),
            _ => GetEffectiveLayoutByEntityType(userId, entityType)
        };
    }

    /// <summary>
    /// 获取用户级布局（根据EntityType）
    /// </summary>
    private object GetUserLayoutByEntityType(string userId, string entityType)
    {
        var layout = _repo.Query(x => x.UserId == userId && x.EntityType == entityType).FirstOrDefault();
        return ReadJson(layout?.LayoutJson);
    }

    /// <summary>
    /// 获取默认布局（根据EntityType）
    /// </summary>
    private object GetDefaultLayoutByEntityType(string entityType)
    {
        var layout = _repo.Query(x => x.UserId == DefaultUserId && x.EntityType == entityType).FirstOrDefault();
        return ReadJson(layout?.LayoutJson);
    }

    /// <summary>
    /// 获取有效布局（用户级 > 默认级，根据EntityType）
    /// </summary>
    public object GetEffectiveLayoutByEntityType(string userId, string entityType)
    {
        // 1. 优先查找用户级布局
        var userLayout = _repo.Query(x => x.UserId == userId && x.EntityType == entityType).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(userLayout?.LayoutJson))
        {
            return ReadJson(userLayout.LayoutJson);
        }

        // 2. 查找默认布局
        var defaultLayout = _repo.Query(x => x.UserId == DefaultUserId && x.EntityType == entityType).FirstOrDefault();
        return ReadJson(defaultLayout?.LayoutJson);
    }

    // ===== 辅助方法 =====

    private static object ReadJson(string? layout)
    {
        var json = string.IsNullOrWhiteSpace(layout) ? new { } : System.Text.Json.JsonSerializer.Deserialize<object>(layout!);
        return json ?? new { };
    }
}
