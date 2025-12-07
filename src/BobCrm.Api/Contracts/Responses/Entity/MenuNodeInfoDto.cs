namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 菜单节点信息 DTO - 用于发布结果
/// </summary>
public class MenuNodeInfoDto
{
    public string Code { get; set; } = string.Empty;
    public Guid NodeId { get; set; }
    public Guid? ParentId { get; set; }
    public string? Route { get; set; }
    public string ViewState { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string UsageType { get; set; } = string.Empty;
}
