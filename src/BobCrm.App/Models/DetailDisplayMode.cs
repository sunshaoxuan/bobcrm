namespace BobCrm.App.Models;

/// <summary>
/// 详情显示模式 - 定义详情如何展示
/// </summary>
public enum DetailDisplayMode
{
    /// <summary>
    /// 内嵌显示 - 用于分栏模式，详情直接显示在页面中
    /// </summary>
    Inline = 1,
    
    /// <summary>
    /// 模态框显示 - 详情在模态对话框中展示
    /// </summary>
    Modal = 2,
    
    /// <summary>
    /// 独立页面显示 - 详情在单独的路由页面中展示
    /// </summary>
    Page = 3
}
