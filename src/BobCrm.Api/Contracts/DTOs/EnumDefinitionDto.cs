using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 枚举定义 DTO
/// </summary>
public class EnumDefinitionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    /// <summary>
    /// 单语显示名（单语模式返回）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
    /// <summary>
    /// 单语描述（单语模式返回）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
    /// <summary>
    /// 多语显示名（向后兼容）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DisplayNameTranslations { get; set; }
    /// <summary>
    /// 多语描述（向后兼容）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DescriptionTranslations { get; set; }
    public bool IsSystem { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<EnumOptionDto> Options { get; set; } = new();
}

/// <summary>
/// 枚举选项 DTO
/// </summary>
public class EnumOptionDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    /// <summary>
    /// 单语显示名（单语模式返回）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
    /// <summary>
    /// 单语描述（单语模式返回）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
    /// <summary>
    /// 多语显示名（向后兼容）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DisplayNameTranslations { get; set; }
    /// <summary>
    /// 多语描述（向后兼容）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DescriptionTranslations { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
    public string? ColorTag { get; set; }
    public string? Icon { get; set; }
}

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

/// <summary>
/// 更新枚举定义请求
/// </summary>
public class UpdateEnumDefinitionRequest
{
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    public Dictionary<string, string?> Description { get; set; } = new();
    public bool IsEnabled { get; set; }
    public List<EnumOptionDto> Options { get; set; } = new();
}

/// <summary>
/// 批量更新枚举选项请求
/// </summary>
public class UpdateEnumOptionsRequest
{
    public List<UpdateEnumOptionRequest> Options { get; set; } = new();
}

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
    public string? ColorTag { get; set; }
    public string? Icon { get; set; }
}
