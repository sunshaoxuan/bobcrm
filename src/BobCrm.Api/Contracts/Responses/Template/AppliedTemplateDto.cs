namespace BobCrm.Api.Contracts.Responses.Template;

/// <summary>
/// 已应用模板信息。
/// </summary>
public class AppliedTemplateDto
{
    /// <summary>
    /// 模板 Id。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 模板名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 实体类型（可选）。
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// 用途类型（当前端点可能无法准确解析时可返回 Unknown）。
    /// </summary>
    public string UsageType { get; set; } = "Unknown";

    /// <summary>
    /// 是否为用户默认模板。
    /// </summary>
    public bool IsUserDefault { get; set; }

    /// <summary>
    /// 是否为系统默认模板。
    /// </summary>
    public bool IsSystemDefault { get; set; }
}

