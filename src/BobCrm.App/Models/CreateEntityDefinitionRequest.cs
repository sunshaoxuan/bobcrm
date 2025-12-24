namespace BobCrm.App.Models;

/// <summary>
/// 创建实体定义请求
/// </summary>
public class CreateEntityDefinitionRequest
{
    public string Namespace { get; set; } = "BobCrm.Base.Custom";
    public string EntityName { get; set; } = string.Empty;

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
    public string StructureType { get; set; } = "Single";
    public List<string> Interfaces { get; set; } = new();
    public List<FieldMetadataDto> Fields { get; set; } = new();
}
