using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Domain.Models;

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

    [MaxLength(256)]
    public string? Route { get; set; }

    [MaxLength(64)]
    public string? Icon { get; set; }

    public bool IsMenu { get; set; } = true;
    public int SortOrder { get; set; } = 100;

    public List<RoleFunctionPermission> Roles { get; set; } = new();
}
