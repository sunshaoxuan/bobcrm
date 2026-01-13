using System.Text.Json;
using System;

namespace BobCrm.App.Models;

public record TemplateRuntimeRequest
{
    public TemplateUsageType UsageType { get; init; } = TemplateUsageType.Detail;
    public int? TemplateId { get; init; }
    public string? ViewState { get; init; }
    public Guid? MenuNodeId { get; init; }
    public string? FunctionCodeOverride { get; init; }
    public int? EntityId { get; init; }
    public JsonElement? EntityData { get; init; }
}
