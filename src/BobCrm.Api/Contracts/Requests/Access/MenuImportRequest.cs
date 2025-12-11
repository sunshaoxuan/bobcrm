namespace BobCrm.Api.Contracts.Requests.Access;

/// <summary>
/// 菜单导入请求
/// </summary>
public record MenuImportRequest
{
    public List<MenuImportNode> Functions { get; init; } = new();
    public string? MergeStrategy { get; init; }  // "replace" or "skip"
}
