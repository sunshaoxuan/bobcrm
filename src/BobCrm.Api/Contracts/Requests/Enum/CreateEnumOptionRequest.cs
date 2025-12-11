using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Contracts.Requests.Enum;

/// <summary>
/// 创建枚举选项请求
/// </summary>
public class CreateEnumOptionRequest
{
    [Required, MaxLength(64)]
    public string Value { get; set; } = string.Empty;
    
    [Required]
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    
    public Dictionary<string, string?> Description { get; set; } = new();
    
    public int SortOrder { get; set; }
    
    [MaxLength(16)]
    public string? ColorTag { get; set; }
    
    [MaxLength(64)]
    public string? Icon { get; set; }
}
