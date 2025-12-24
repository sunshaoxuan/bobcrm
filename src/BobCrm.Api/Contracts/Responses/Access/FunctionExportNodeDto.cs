namespace BobCrm.Api.Contracts.Responses.Access;

/// <summary>
/// 菜单/功能树导出节点。
/// </summary>
public class FunctionExportNodeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string?>? DisplayName { get; set; }
    public string? Route { get; set; }
    public string? Icon { get; set; }
    public bool IsMenu { get; set; }
    public int SortOrder { get; set; }
    public List<FunctionExportNodeDto>? Children { get; set; }
}

