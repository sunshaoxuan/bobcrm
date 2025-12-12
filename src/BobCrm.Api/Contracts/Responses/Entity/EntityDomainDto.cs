using System.Text.Json.Serialization;
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Contracts.Responses.Entity;

public class EntityDomainDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? NameTranslations { get; set; }

    public int SortOrder { get; set; }
    public bool IsSystem { get; set; }
}

