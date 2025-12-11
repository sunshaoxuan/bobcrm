namespace BobCrm.Api.Contracts.Requests.Access;

/// <summary>
/// 功能权限选择项（含模板绑定）
/// </summary>
public record FunctionPermissionSelectionDto
{
    public Guid FunctionId { get; init; }
    public int? TemplateBindingId { get; init; }
}
