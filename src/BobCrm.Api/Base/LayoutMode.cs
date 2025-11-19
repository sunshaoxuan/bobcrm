namespace BobCrm.Api.Base;

/// <summary>
/// 布局模式 - 定义列表和详情的显示方式
/// </summary>
public enum LayoutMode
{
    /// <summary>
    /// 左列表右明细 - 左侧显示简化列表（仅关键字段），右侧显示完整详情
    /// </summary>
    LeftRightSplit = 1,
    
    /// <summary>
    /// 上列表下明细 - 上方显示完整列表表格，下方显示详情表单
    /// </summary>
    TopBottomSplit = 2,
    
    /// <summary>
    /// 仅列表模式 - 全页显示列表，详情通过模态框或独立页面展示
    /// </summary>
    ListOnly = 3
}
