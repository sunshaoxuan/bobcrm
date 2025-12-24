using System.Text.Json;

namespace BobCrm.Api.Application.Queries;

public interface ILayoutQueries
{
    // 旧版API（向后兼容）
    JsonElement GetUserLayout(string userId, int customerId);
    JsonElement GetLayout(string userId, int customerId, string scope);
    JsonElement GetEffectiveLayout(string userId, int customerId);

    // 新版API（支持EntityType）
    JsonElement GetLayoutByEntityType(string userId, string entityType, string scope);
    JsonElement GetEffectiveLayoutByEntityType(string userId, string entityType);
}
