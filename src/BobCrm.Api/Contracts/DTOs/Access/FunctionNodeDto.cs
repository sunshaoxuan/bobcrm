using System.Text.Json.Serialization;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Contracts.DTOs.Access;

/// <summary>
/// 功能节点 DTO
/// </summary>
public record FunctionNodeDto
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    /// <summary>
    /// 单语显示名（单语模式返回）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; init; }
    /// <summary>
    /// 多语显示名（向后兼容返回）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DisplayNameTranslations { get; init; }
    public string? Route { get; init; }
    public string? Icon { get; init; }
    public bool IsMenu { get; init; }
    public int SortOrder { get; init; }
    public int? TemplateId { get; init; }
    public string? TemplateName { get; init; }
    public List<FunctionNodeDto> Children { get; init; } = new();
    public List<FunctionTemplateOptionDto> TemplateOptions { get; init; } = new();
    public List<FunctionNodeTemplateBindingDto> TemplateBindings { get; init; } = new();
}
