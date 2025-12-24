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
