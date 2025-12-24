namespace BobCrm.App.Models;

/// <summary>
/// 模板分组（按用户）
/// </summary>
public class TemplateGroupByUser
{
    public string UserId { get; set; } = string.Empty;
    public List<FormTemplate> Templates { get; set; } = new();
}
