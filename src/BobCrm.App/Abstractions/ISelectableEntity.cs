namespace BobCrm.App.Abstractions;

/// <summary>
/// 可选择实体接口 - 所有可用于 EntitySelector 组件的实体都应实现此接口
/// 确保实体具有必需的显示属性
/// </summary>
public interface ISelectableEntity
{
    /// <summary>
    /// 实体的唯一标识值（用于绑定）
    /// </summary>
    string Value { get; }
    
    /// <summary>
    /// 实体的显示名称（已翻译）
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// 实体的描述（已翻译，可选）
    /// </summary>
    string? Description { get; }
    
    /// <summary>
    /// 实体的图标（可选）
    /// </summary>
    string? Icon { get; }
}

