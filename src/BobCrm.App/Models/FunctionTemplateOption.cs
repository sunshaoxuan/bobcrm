namespace BobCrm.App.Models;

public class FunctionTemplateOption
{
    public int BindingId { get; set; }
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public TemplateUsageType UsageType { get; set; } = TemplateUsageType.Detail;
    public bool IsSystem { get; set; }
    public bool IsDefault { get; set; }
}
