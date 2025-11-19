namespace BobCrm.App.Models;

/// <summary>
/// 模态框大小
/// </summary>
public enum ModalSize
{
    /// <summary>
    /// 小型模态框 - 适合简单表单
    /// </summary>
    Small = 1,
    
    /// <summary>
    /// 中型模态框 - 默认大小
    /// </summary>
    Medium = 2,
    
    /// <summary>
    /// 大型模态框 - 适合复杂表单
    /// </summary>
    Large = 3,
    
    /// <summary>
    /// 超大型模态框 - 接近全屏
    /// </summary>
    ExtraLarge = 4
}
