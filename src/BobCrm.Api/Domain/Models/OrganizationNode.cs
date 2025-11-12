using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Domain.Models;

/// <summary>
/// 组织节点（树形结构）
/// </summary>
public class OrganizationNode
{
    public Guid Id { get; set; }

    /// <summary>
    /// 节点编码，在同一层级内唯一
    /// </summary>
    [Required, MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 节点名称
    /// </summary>
    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 父节点
    /// </summary>
    public Guid? ParentId { get; set; }

    public OrganizationNode? Parent { get; set; }

    /// <summary>
    /// 子节点集合
    /// </summary>
    public List<OrganizationNode> Children { get; set; } = new();

    /// <summary>
    /// 树形层级（根为0）
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 递归展开编码，每层追加 2 位（例如 01.02.03）
    /// </summary>
    [Required, MaxLength(256)]
    public string PathCode { get; set; } = string.Empty;

    /// <summary>
    /// 同级排序
    /// </summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
