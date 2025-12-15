using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 实体接口 - 通过勾选快速添加常用字段
/// </summary>
public class EntityInterface
{
    /// <summary>
    /// 实体接口ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 所属实体定义ID
    /// </summary>
    [Required]
    public Guid EntityDefinitionId { get; set; }

    /// <summary>
    /// 接口类型（Base、Archive、Audit、Version、TimeVersion、Organization）
    /// </summary>
    [Required, MaxLength(50)]
    public string InterfaceType { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 是否已被引用锁定（锁定后不可删除）
    /// </summary>
    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 所属实体定义（导航属性）
    /// </summary>
    public EntityDefinition? EntityDefinition { get; set; }
}
