namespace BobCrm.App.Models;

/// <summary>
/// 实体字段节点 - 用于树形展示实体结构
/// </summary>
public class EntityFieldNode
{
    /// <summary>
    /// 节点键（用于Tree组件）
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// 字段属性名
    /// </summary>
    public string PropertyName { get; set; } = "";

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// 数据类型
    /// </summary>
    public string DataType { get; set; } = "";

    /// <summary>
    /// 字段长度（String类型）
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// 精度（Decimal类型）
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// 小数位数（Decimal类型）
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
    /// 字段来源（Custom/Base/Archive/Audit/Tree等）
    /// </summary>
    public string Source { get; set; } = "Custom";

    /// <summary>
    /// 是否可以拖拽
    /// </summary>
    public bool IsDraggable { get; set; } = true;

    /// <summary>
    /// 建议的组件类型
    /// </summary>
    public string SuggestedWidgetType { get; set; } = "Input";

    /// <summary>
    /// 图标
    /// </summary>
    public string Icon { get; set; } = "field";

    /// <summary>
    /// 子节点（用于分组显示）
    /// </summary>
    public List<EntityFieldNode> Children { get; set; } = new();
}
