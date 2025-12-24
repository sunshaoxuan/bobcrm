using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Contracts.Responses.Template;

/// <summary>
/// 菜单节点摘要（用于菜单与模板交集接口）。
/// </summary>
public class MenuNodeSummaryDto
{
    /// <summary>
    /// 菜单节点 Id。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 菜单编码（已规范化）。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 菜单名称（已按语言解析）。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名 key（可选）。
    /// </summary>
    public string? DisplayNameKey { get; set; }

    /// <summary>
    /// 单语言显示名（可选）。
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 多语言显示名（可选）。
    /// </summary>
    public MultilingualText? DisplayNameTranslations { get; set; }

    /// <summary>
    /// 路由（可选）。
    /// </summary>
    public string? Route { get; set; }

    /// <summary>
    /// 图标（可选）。
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 排序。
    /// </summary>
    public int SortOrder { get; set; }
}

