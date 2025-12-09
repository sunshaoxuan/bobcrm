using BobCrm.App.Services.Widgets;
using BobCrm.App.Services.Widgets.Rendering;

namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 容器控件抽象基类
/// 可以包含子控件的容器
/// Children 属性继承自 DraggableWidget 基类
/// </summary>
public abstract class ContainerWidget : DraggableWidget
{
    // Children 属性已在基类 DraggableWidget 中定义

    public override string GetDefaultCodePrefix()
    {
        return "container";
    }

    /// <summary>
    /// 容器的设计态最小高度
    /// 默认 100px，子类可重写以提供自己的值
    /// </summary>
    public override int GetDesignMinHeight()
    {
        // 容器需要足够的空间显示拖放区域
        // 100px = 标题栏(~40px) + 内容区域(~60px)
        return 100;
    }

    /// <summary>
    /// 运行态渲染：默认使用 RuntimeContainerRenderer 渲染通用容器结构
    /// </summary>
    public override void RenderRuntime(RuntimeRenderContext context)
    {
        RuntimeContainerRenderer.RenderContainer(
            context.Builder,
            this,
            context.Mode,
            (child, mode) => context.RenderChild(child),
            w => w.Label ?? w.Type,
            (w, mode) => WidgetStyleHelper.GetRuntimeWidgetStyle(w, mode)
        );
    }
}
