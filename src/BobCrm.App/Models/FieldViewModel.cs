namespace BobCrm.App.Models;

/// <summary>
/// 字段视图模型（用于子实体字段的表格行内编辑）
/// </summary>
public class FieldViewModel
{
    /// <summary>
    /// 临时ID（用于前端标识）
    /// </summary>
    public Guid TempId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 实际ID（保存后的数据库ID）
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 字段编码（属性名）
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称（多语言）
    /// </summary>
    public MultilingualTextDto DisplayName { get; set; } = new();

    /// <summary>
    /// 数据类型
    /// </summary>
    public string DataType { get; set; } = "String";

    /// <summary>
    /// 长度（可选）
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// 精度（Decimal类型时）
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// 小数位数（Decimal类型时）
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 验证规则（JSON格式）
    /// </summary>
    public string? ValidationRules { get; set; }

    /// <summary>
    /// 是否处于编辑状态
    /// </summary>
    public bool IsEditing { get; set; }

    /// <summary>
    /// 是否为新增字段（未保存到数据库）
    /// </summary>
    public bool IsNew => Id == Guid.Empty;

    /// <summary>
    /// 验证错误消息
    /// </summary>
    public string? ValidationError { get; set; }

    /// <summary>
    /// 克隆字段（用于取消编辑时恢复）
    /// </summary>
    public FieldViewModel Clone()
    {
        return new FieldViewModel
        {
            TempId = this.TempId,
            Id = this.Id,
            PropertyName = this.PropertyName,
            DisplayName = new MultilingualTextDto(this.DisplayName),
            DataType = this.DataType,
            Length = this.Length,
            Precision = this.Precision,
            Scale = this.Scale,
            IsRequired = this.IsRequired,
            DefaultValue = this.DefaultValue,
            Description = this.Description,
            SortOrder = this.SortOrder,
            ValidationRules = this.ValidationRules,
            IsEditing = this.IsEditing,
            ValidationError = this.ValidationError
        };
    }

    /// <summary>
    /// 验证字段
    /// </summary>
    /// <returns>是否验证通过</returns>
    public bool Validate()
    {
        ValidationError = null;

        if (string.IsNullOrWhiteSpace(PropertyName))
        {
            ValidationError = "字段编码不能为空";
            return false;
        }

        if (!char.IsLetter(PropertyName[0]))
        {
            ValidationError = "字段编码必须以字母开头";
            return false;
        }

        if (!PropertyName.All(c => char.IsLetterOrDigit(c) || c == '_'))
        {
            ValidationError = "字段编码只能包含字母、数字和下划线";
            return false;
        }

        if (DisplayName == null || !DisplayName.Any())
        {
            ValidationError = "显示名称不能为空";
            return false;
        }

        if (string.IsNullOrWhiteSpace(DataType))
        {
            ValidationError = "数据类型不能为空";
            return false;
        }

        if (DataType == "String" && Length.HasValue && Length.Value <= 0)
        {
            ValidationError = "字符串长度必须大于0";
            return false;
        }

        if (DataType == "Decimal")
        {
            if (Precision.HasValue && Precision.Value <= 0)
            {
                ValidationError = "精度必须大于0";
                return false;
            }

            if (Scale.HasValue && Scale.Value < 0)
            {
                ValidationError = "小数位数不能小于0";
                return false;
            }

            if (Precision.HasValue && Scale.HasValue && Scale.Value > Precision.Value)
            {
                ValidationError = "小数位数不能大于精度";
                return false;
            }
        }

        return true;
    }
}
