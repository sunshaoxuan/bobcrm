namespace BobCrm.App.Models;

/// <summary>
/// 保存聚合请求
/// </summary>
public class SaveEntityDefinitionAggregateRequest
{
    public Guid Id { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    public Dictionary<string, string?>? Description { get; set; }
    public List<SubEntityDto> SubEntities { get; set; } = new();
}
