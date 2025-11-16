using System;
using System.Collections.Generic;

namespace BobCrm.App.Models;

public class FunctionMenuNode
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public MultilingualTextDto? DisplayNameTranslations { get; set; }
    public string? Route { get; set; }
    public string? Icon { get; set; }
    public bool IsMenu { get; set; }
    public int SortOrder { get; set; }
    public int? TemplateId { get; set; }
    public string? TemplateName { get; set; }
    public List<FunctionMenuNode> Children { get; set; } = new();
    public List<FunctionTemplateOption> TemplateOptions { get; set; } = new();
    public List<FunctionTemplateBindingSummary> TemplateBindings { get; set; } = new();
}

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
