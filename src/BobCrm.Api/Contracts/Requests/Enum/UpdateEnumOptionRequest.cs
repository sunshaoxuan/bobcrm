using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Contracts.Requests.Enum;

/// <summary>
/// 更新单个枚举选项请求
/// </summary>
public class UpdateEnumOptionRequest
{
    public Guid Id { get; set; }
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    public Dictionary<string, string?> Description { get; set; } = new();
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
    [MaxLength(16)]
    public string? ColorTag { get; set; }
    [MaxLength(64)]
    public string? Icon { get; set; }
}
