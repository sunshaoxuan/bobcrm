namespace BobCrm.Api.Contracts.Requests.Enum;

/// <summary>
/// 批量更新枚举选项请求
/// </summary>
public class UpdateEnumOptionsRequest
{
    public List<UpdateEnumOptionRequest> Options { get; set; } = new();
}
