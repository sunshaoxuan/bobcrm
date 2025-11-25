using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;

namespace BobCrm.Application.Templates;

public interface IDefaultTemplateGenerator
{
    Task<FormTemplate> GenerateAsync(
        EntityDefinition entityDefinition,
        FormTemplateUsageType usageType = FormTemplateUsageType.Detail,
        CancellationToken cancellationToken = default);

    Task<DefaultTemplateGenerationResult> EnsureTemplatesAsync(
        EntityDefinition entityDefinition,
        bool force = false,
        CancellationToken cancellationToken = default);
}

public class DefaultTemplateGenerationResult
{
    public Dictionary<FormTemplateUsageType, FormTemplate> Templates { get; } = new();
    public List<FormTemplate> Created { get; } = new();
    public List<FormTemplate> Updated { get; } = new();
}
