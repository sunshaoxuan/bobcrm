using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BobCrm.Api.Base;

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
    /// 节点名称的多语言文本（jsonb 存储）
    /// </summary>
    public Dictionary<string, string?>? DisplayName { get; set; }

    [MaxLength(200)]
    public string? DisplayNameKey { get; set; }

    [MaxLength(256)]
    public string? Route { get; set; }

    [MaxLength(64)]
    public string? Icon { get; set; }

    public bool IsMenu { get; set; } = true;
    public int SortOrder { get; set; } = 100;

    /// <summary>
    /// 关联的模板（可选，已废弃）
    /// </summary>
    [System.Obsolete("Use TemplateStateBinding instead")]
    public int? TemplateId { get; set; }
    public FormTemplate? Template { get; set; }

    public List<RoleFunctionPermission> Roles { get; set; } = new();

    /// <summary>
    /// 旧的模板绑定（已废弃）
    /// </summary>
    [System.Obsolete("Use TemplateStateBindingId instead")]
    public int? TemplateBindingId { get; set; }
    public TemplateBinding? TemplateBinding { get; set; }

    /// <summary>
    /// 新的模板状态绑定
    /// 链接到支持多视图状态的 TemplateStateBinding
    /// </summary>
    public int? TemplateStateBindingId { get; set; }
    public TemplateStateBinding? TemplateStateBinding { get; set; }
}
