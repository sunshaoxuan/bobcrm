namespace BobCrm.App.Models.Designer;

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
    Textarea,

    /// <summary>JSON编辑器</summary>
    Json,

    /// <summary>数据集选择器</summary>
    DataSetPicker,

    /// <summary>字段选择器</summary>
    FieldPicker,

    /// <summary>DataGrid 列定义编辑器</summary>
    ColumnDefinition
}
