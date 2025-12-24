using System.Text.Json.Serialization;

namespace BobCrm.Api.Contracts.Responses.DynamicEntity;

/// <summary>
/// 动态实体单条查询结果 DTO。
/// </summary>
public class DynamicEntityGetResultDto
{
    /// <summary>
    /// 可选的字段元数据（当请求 includeMeta=true 时返回）。
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DynamicEntityMetaDto? Meta { get; set; }

    /// <summary>
    /// 单条实体数据（运行时动态类型）。
    /// </summary>
    public object Data { get; set; } = new();
}

