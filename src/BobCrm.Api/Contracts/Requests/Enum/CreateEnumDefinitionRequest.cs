using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Contracts.Requests.Enum;

/// <summary>
/// 创建枚举定义请求
/// </summary>
public class CreateEnumDefinitionRequest
{
    [Required, MaxLength(128)]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    
    public Dictionary<string, string?> Description { get; set; } = new();
    
    public bool IsEnabled { get; set; } = true;
    
    public List<CreateEnumOptionRequest> Options { get; set; } = new();
}
