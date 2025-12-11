namespace BobCrm.Api.Contracts.Requests.Enum;

/// <summary>
/// 更新枚举定义请求
/// </summary>
public class UpdateEnumDefinitionRequest
{
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    public Dictionary<string, string?> Description { get; set; } = new();
    public bool IsEnabled { get; set; }
    public List<BobCrm.Api.Contracts.DTOs.Enum.EnumOptionDto> Options { get; set; } = new();
}
