namespace BobCrm.Api.Contracts.Requests.Access;

/// <summary>
/// 菜单导入节点
/// </summary>
public record MenuImportNode
{
    public string Code { get; init; } = string.Empty;
    public string? Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public string? Route { get; init; }
    public string? Icon { get; init; }
    public bool IsMenu { get; init; } = true;
    public int SortOrder { get; init; } = 100;
    public List<MenuImportNode>? Children { get; init; }
}
