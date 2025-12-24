namespace BobCrm.App.Models;

/// <summary>
/// 模板分组（按实体类型）
/// </summary>
public class TemplateGroupByEntity
{
    public string EntityType { get; set; } = string.Empty;
    public List<FormTemplate> Templates { get; set; } = new();
}
