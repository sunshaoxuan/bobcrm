using BobCrm.Api.Base;

namespace BobCrm.Api.Services;

/// <summary>
/// 已发布的菜单信息
/// </summary>
public record PublishedMenuInfo(
    string Code,
    Guid NodeId,
    Guid? ParentId,
    string? Route,
    string ViewState,
    FormTemplateUsageType UsageType);

