namespace BobCrm.App.Models;

public class FunctionTemplateBindingSummary
{
    public int BindingId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public TemplateUsageType UsageType { get; set; }
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public List<FunctionTemplateOption> TemplateOptions { get; set; } = new();
}
