namespace BobCrm.App.Models.Designer;

/// <summary>
/// 控件属性元数据定义
/// </summary>
public class WidgetPropertyMetadata
{
    /// <summary>属性路径（支持嵌套，如 "Columns" 或 "ContainerLayout.Gap"）</summary>
    public required string PropertyPath { get; init; }
    
    /// <summary>显示标签（多语言键，如 "PROP_COLUMNS"）</summary>
    public required string Label { get; init; }
    
    /// <summary>属性类型</summary>
    public required PropertyEditorType EditorType { get; init; }
    
    /// <summary>分组名称（多语言键，如 "PROP_GROUP_LAYOUT"）</summary>
    public string? Group { get; init; }
    
    /// <summary>最小值（数字类型）</summary>
    public int? Min { get; init; }
    
    /// <summary>最大值（数字类型）</summary>
    public int? Max { get; init; }
    
    /// <summary>枚举选项（下拉类型）</summary>
    public List<PropertyOption>? Options { get; init; }
    
    /// <summary>占位符文本</summary>
    public string? Placeholder { get; init; }
    
    /// <summary>是否条件显示（依赖其他属性的值）</summary>
    public string? VisibleWhen { get; init; }
    
    /// <summary>条件值（VisibleWhen 对应的属性值为 true 时显示）</summary>
    public object? VisibleWhenValue { get; init; }
    
    /// <summary>帮助文本（多语言键）</summary>
    public string? HelpText { get; init; }
}

/// <summary>
/// 属性编辑器类型
/// </summary>
public enum PropertyEditorType
{
    /// <summary>文本输入框</summary>
    Text,
    
    /// <summary>数字输入框</summary>
    Number,
    
    /// <summary>开关按钮</summary>
    Boolean,
    
    /// <summary>颜色选择器</summary>
    Color,
    
    /// <summary>下拉选择框</summary>
    Select,
    
    /// <summary>文本区域</summary>
    Textarea
}

/// <summary>
/// 下拉选项
/// </summary>
public class PropertyOption
{
    public required string Value { get; init; }
    /// <summary>选项标签（多语言键，如 "PROP_DIRECTION_ROW"）</summary>
    public required string Label { get; init; }
}

