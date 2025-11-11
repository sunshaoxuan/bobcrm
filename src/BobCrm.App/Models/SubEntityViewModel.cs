namespace BobCrm.App.Models;

/// <summary>
/// 子实体视图模型
/// </summary>
public class SubEntityViewModel
{
    /// <summary>
    /// 临时ID（用于前端标识，保存后替换为实际ID）
    /// </summary>
    public Guid TempId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 实际ID（保存后的数据库ID）
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 所属主实体ID
    /// </summary>
    public Guid EntityDefinitionId { get; set; }

    /// <summary>
    /// 子实体编码（如 "Lines", "Items"）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称（多语言）
    /// </summary>
    public MultilingualTextDto DisplayName { get; set; } = new();

    /// <summary>
    /// 描述（多语言）
    /// </summary>
    public MultilingualTextDto? Description { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 默认排序字段
    /// </summary>
    public string? DefaultSortField { get; set; }

    /// <summary>
    /// 是否降序排序
    /// </summary>
    public bool IsDescending { get; set; }

    /// <summary>
    /// 外键字段名
    /// </summary>
    public string? ForeignKeyField { get; set; }

    /// <summary>
    /// 集合属性名
    /// </summary>
    public string? CollectionPropertyName { get; set; }

    /// <summary>
    /// 级联删除行为
    /// </summary>
    public string CascadeDeleteBehavior { get; set; } = "Cascade";

    /// <summary>
    /// 字段集合
    /// </summary>
    public List<FieldViewModel> Fields { get; set; } = new();

    /// <summary>
    /// 是否有验证错误
    /// </summary>
    public bool HasValidationErrors { get; set; }

    /// <summary>
    /// 验证错误数量
    /// </summary>
    public int ValidationErrorCount => ValidationErrors.Count;

    /// <summary>
    /// 验证错误消息
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// 克隆子实体（用于取消编辑时恢复）
    /// </summary>
    public SubEntityViewModel Clone()
    {
        return new SubEntityViewModel
        {
            TempId = this.TempId,
            Id = this.Id,
            EntityDefinitionId = this.EntityDefinitionId,
            Code = this.Code,
            DisplayName = new MultilingualTextDto(this.DisplayName),
            Description = this.Description != null ? new MultilingualTextDto(this.Description) : null,
            SortOrder = this.SortOrder,
            DefaultSortField = this.DefaultSortField,
            IsDescending = this.IsDescending,
            ForeignKeyField = this.ForeignKeyField,
            CollectionPropertyName = this.CollectionPropertyName,
            CascadeDeleteBehavior = this.CascadeDeleteBehavior,
            Fields = this.Fields.Select(f => f.Clone()).ToList(),
            HasValidationErrors = this.HasValidationErrors,
            ValidationErrors = new List<string>(this.ValidationErrors)
        };
    }
}
