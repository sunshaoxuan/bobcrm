using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 功能树节点
/// </summary>
public class FunctionNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }
    public FunctionNode? Parent { get; set; }
    public List<FunctionNode> Children { get; set; } = new();

    [Required, MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 多语言显示名（jsonb）
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string?>? DisplayName { get; set; }

    /// <summary>
    /// 多语资源键
    /// </summary>
    [MaxLength(200)]
    public string? DisplayNameKey { get; set; }

    [MaxLength(256)]
    public string? Route { get; set; }

    [MaxLength(64)]
    public string? Icon { get; set; }

    public bool IsMenu { get; set; } = true;
    public int SortOrder { get; set; } = 100;

    public List<RoleFunctionPermission> Roles { get; set; } = new();

    public int? TemplateBindingId { get; set; }
    public TemplateBinding? TemplateBinding { get; set; }
}
