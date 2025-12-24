using BobCrm.Api.Base;

namespace BobCrm.Api.Contracts.Responses.Template;

/// <summary>
/// 模板状态绑定摘要（用于菜单与模板交集接口）。
/// </summary>
public class TemplateStateBindingSummaryDto
{
    /// <summary>
    /// 绑定 Id。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 实体类型（路由名）。
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 视图状态（如 List/DetailEdit/Create 等）。
    /// </summary>
    public string ViewState { get; set; } = string.Empty;

    /// <summary>
    /// 用途类型（由 ViewState 推导）。
    /// </summary>
    public FormTemplateUsageType UsageType { get; set; }

    /// <summary>
    /// 当前绑定模板 Id（可选）。
    /// </summary>
    public int? TemplateId { get; set; }

    /// <summary>
    /// 是否默认绑定。
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 必要权限（可选）。
    /// </summary>
    public string? RequiredPermission { get; set; }
}
