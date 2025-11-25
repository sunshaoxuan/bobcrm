using BobCrm.App.Models;

namespace BobCrm.App.Services.Designer;

/// <summary>
/// 设计器拖拽状态（用于跨组件共享当前拖拽的实体字段）
/// </summary>
public class DesignerDragState
{
    /// <summary>当前拖拽的实体字段</summary>
    public EntityFieldNode? CurrentEntityField { get; set; }
}
