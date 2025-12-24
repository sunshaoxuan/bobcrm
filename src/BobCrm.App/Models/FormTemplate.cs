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
    public TemplateUsageType UsageType { get; set; } = TemplateUsageType.Detail;
    public string? LayoutJson { get; set; }
    public string? Description { get; set; }
    public string? RequiredFunctionCode { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsInUse { get; set; }
}
