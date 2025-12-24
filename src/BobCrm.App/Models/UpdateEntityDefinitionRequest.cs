namespace BobCrm.App.Models;

/// <summary>
/// 更新实体定义请求
/// </summary>
public class UpdateEntityDefinitionRequest
{
    /// <summary>
    /// 显示名（多语言）
    /// </summary>
    public MultilingualTextDto DisplayName { get; set; } = new();

    /// <summary>
    /// 描述（多语言）
    /// </summary>
    public MultilingualTextDto? Description { get; set; }

    public string? Icon { get; set; }
    public string? Category { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Order { get; set; }
    public List<FieldMetadataDto> Fields { get; set; } = new();
    public List<string> Interfaces { get; set; } = new();
}
