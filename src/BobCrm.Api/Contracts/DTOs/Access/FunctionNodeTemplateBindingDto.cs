using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Contracts.DTOs.Access;

/// <summary>
/// 功能节点与模板绑定 DTO
/// </summary>
public record FunctionNodeTemplateBindingDto(
    int BindingId,
    string EntityType,
    FormTemplateUsageType UsageType,
    int TemplateId,
    string TemplateName,
    bool IsSystem);
