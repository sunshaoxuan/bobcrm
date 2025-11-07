namespace BobCrm.App.Models;

/// <summary>
/// 表单模板DTO（前端）
/// </summary>
public class FormTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsUserDefault { get; set; }
    public bool IsSystemDefault { get; set; }
    public string? LayoutJson { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsInUse { get; set; }
}

/// <summary>
/// 模板分组（按实体类型）
/// </summary>
public class TemplateGroupByEntity
{
    public string EntityType { get; set; } = string.Empty;
    public List<FormTemplate> Templates { get; set; } = new();
}

/// <summary>
/// 模板分组（按用户）
/// </summary>
public class TemplateGroupByUser
{
    public string UserId { get; set; } = string.Empty;
    public List<FormTemplate> Templates { get; set; } = new();
}
