using System.Text.Json.Serialization;
using BobCrm.Api.Contracts.Responses.Entity;

namespace BobCrm.Api.Contracts.Responses.DynamicEntity;

public class DynamicEntityMetaDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FieldMetadataDto>? Fields { get; set; }
}
