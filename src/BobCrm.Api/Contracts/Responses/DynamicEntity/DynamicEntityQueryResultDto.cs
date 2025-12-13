using System.Text.Json.Serialization;

namespace BobCrm.Api.Contracts.Responses.DynamicEntity;

public class DynamicEntityQueryResultDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DynamicEntityMetaDto? Meta { get; set; }

    public List<object> Data { get; set; } = new();

    public int Total { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
