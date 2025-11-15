using System.Collections.Generic;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Contracts.DTOs;

public static class TemplateDtoExtensions
{
    public static TemplateBindingDto ToDto(this TemplateBinding binding) =>
        new(
            binding.Id,
            binding.EntityType,
            binding.UsageType,
            binding.TemplateId,
            binding.IsSystem,
            binding.RequiredFunctionCode,
            binding.UpdatedBy,
            binding.UpdatedAt);

    public static TemplateDescriptorDto ToDescriptor(this FormTemplate template) =>
        new(
            template.Id,
            template.Name,
            template.EntityType,
            template.UsageType,
            template.LayoutJson,
            template.Tags ?? new List<string>(),
            template.Description);
}
