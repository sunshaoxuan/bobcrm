using System;

namespace BobCrm.Api.Domain.Models;

/// <summary>
/// 描述实体与模板之间的绑定关系。
/// </summary>
public class TemplateBinding
{
    public int Id { get; set; }

    /// <summary>实体类型（如 customer、organization 等）</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>模板用途</summary>
    public FormTemplateUsageType UsageType { get; set; } = FormTemplateUsageType.Detail;

    /// <summary>绑定的模板</summary>
    public int TemplateId { get; set; }
    public FormTemplate? Template { get; set; }

    /// <summary>是否系统级绑定（而非用户自定义）</summary>
    public bool IsSystem { get; set; } = true;

    /// <summary>可覆盖模板自身的功能编码</summary>
    public string? RequiredFunctionCode { get; set; }

    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
