namespace BobCrm.Api.Contracts.Requests.Organization;

/// <summary>
/// 更新组织请求
/// </summary>
public record UpdateOrganizationRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
