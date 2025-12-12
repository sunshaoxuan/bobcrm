namespace BobCrm.Api.Contracts.Requests.Organization;

/// <summary>
/// 创建组织请求
/// </summary>
public record CreateOrganizationRequest
{
    public Guid? ParentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
