using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 运行时渲染模式
/// </summary>
public enum RuntimeWidgetRenderMode
{
    Browse,
    Edit
}

/// <summary>
/// 可拖放控件基类 - 所有控件的基础
/// 提供所有控件通用的拖放、调整大小、布局等功能
/// 使用组合模式管理不同的布局选项
/// </summary>
public abstract class DraggableWidget : IResizable, IFlowSized, IAbsolutePositioned
{
    // ===== 基本标识 =====

    /// <summary>控件唯一标识（GUID）</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>控件类型</summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// 控件代码/名称（人类可读，用于引用）
    /// 例如：textbox1, button2, myCustomInput
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>控件标签/标题</summary>
    public string Label { get; set; } = "";

    // ===== 布局管理 - 使用组合模式 =====

    /// <summary>布局选项 - 统一管理不同布局模式的属性</summary>
    public LayoutOptions Layout { get; set; } = new LayoutOptions();

    // ===== IFlowSized 接口实现（委托给 Layout） =====

    /// <summary>宽度数值</summary>
    public int Width
    {
        get => Layout.Width;
        set => Layout.Width = value;
    }

    /// <summary>宽度单位（%或px）</summary>
    public string WidthUnit
    {
        get => Layout.WidthUnit;
        set => Layout.WidthUnit = value;
    }

    /// <summary>高度数值</summary>
    public int Height
    {
        get => Layout.Height;
        set => Layout.Height = value;
    }

    /// <summary>高度单位（px或auto）</summary>
    public string HeightUnit
    {
        get => Layout.HeightUnit;
        set => Layout.HeightUnit = value;
    }

    /// <summary>是否从新行开始</summary>
    public bool NewLine
    {
        get => Layout.NewLine;
        set => Layout.NewLine = value;
    }

    // ===== IAbsolutePositioned 接口实现（委托给 Layout） =====

    /// <summary>X坐标</summary>
    public int X
    {
        get => Layout.X;
        set => Layout.X = value;
    }

    /// <summary>Y坐标</summary>
    public int Y
    {
        get => Layout.Y;
        set => Layout.Y = value;
    }

    /// <summary>绝对定位宽度（像素）</summary>
    public int W
    {
        get => Layout.W;
        set => Layout.W = value;
    }

    /// <summary>绝对定位高度（像素）</summary>
    public int H
    {
        get => Layout.H;
        set => Layout.H = value;
    }

    /// <summary>Z-Index层级</summary>
    public int ZIndex
    {
        get => Layout.ZIndex;
        set => Layout.ZIndex = value;
    }

    // ===== 其他通用属性 =====

    /// <summary>是否可见</summary>
    public bool Visible { get; set; } = true;

    // ===== 数据绑定 =====

    /// <summary>绑定的数据字段</summary>
    public string? DataField { get; set; }

    // ===== 容器支持 =====

    /// <summary>子控件列表（容器类型才使用）</summary>
    public List<DraggableWidget>? Children { get; set; }

    // ===== 扩展属性 =====

    /// <summary>扩展属性字典，用于存储特殊的自定义属性</summary>
    public Dictionary<string, object>? ExtendedProperties { get; set; }

    // ===== 方法 =====

    /// <summary>
    /// 获取控件代码的默认前缀
    /// 子类必须实现此方法以提供自己的前缀（如 "textbox", "button"）
    /// 用于生成默认的 Code，例如：textbox1, textbox2, button1, button2
    /// </summary>
    /// <returns>小写的前缀字符串</returns>
    public abstract string GetDefaultCodePrefix();

    /// <summary>
    /// 判断某个属性是否可编辑（根据控件类型）
    /// 子类可以重写此方法来限制可编辑的属性
    /// </summary>
    public virtual bool CanEditProperty(string propertyName) => true;

    /// <summary>
    /// 获取宽度的最大值（子类可以重写）
    /// </summary>
    public virtual int? GetMaxWidth()
    {
        // % 模式：限制为 100
        // px 模式：不限制（返回 null 表示无限制，由画布自然约束）
        return WidthUnit == "%" ? 100 : null;
    }

    /// <summary>
    /// 获取控件的属性元数据列表
    /// 子类应重写此方法以提供自己的属性定义
    /// </summary>
    public virtual List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        // 默认返回通用属性
        return new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Code", Label = "PROP_CODE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text, Group = "PROP_GROUP_BASIC" },
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = GetMaxWidth() },
            new() { PropertyPath = "Visible", Label = "PROP_VISIBLE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean }
        };
    }

    /// <summary>
    /// 获取设计态预览组件类型
    /// 子类应重写此属性以指定自己的预览组件
    /// 使用动态组件渲染，无需在 FormDesigner 中使用 switch
    /// </summary>
    public virtual Type? PreviewComponentType => null;

    /// <summary>
    /// 获取运行时组件类型
    /// 子类可重写此属性以指定自己的运行时组件
    /// </summary>
    public virtual Type? RuntimeComponentType => null;

    /// <summary>
    /// 获取设计态最小高度（px）
    /// 子类可重写以提供自己的最小高度
    /// </summary>
    public virtual int GetDesignMinHeight()
    {
        // 普通组件：默认最小高度 32px
        return 32;
    }

    // ===== 渲染方法（OOP多态） =====

    /// <summary>
    /// 运行时渲染上下文
    /// </summary>
    public sealed record RuntimeRenderContext
    {
        public required RenderTreeBuilder Builder { get; init; }
        public required RuntimeWidgetRenderMode Mode { get; init; }
        public required ComponentBase EventTarget { get; init; }
        public required DraggableWidget Widget { get; init; }
        public required string Label { get; init; }

        /// <summary>
        /// 递归渲染子控件的委托
        /// </summary>
        public required Func<DraggableWidget, RenderFragment> RenderChild { get; init; }

        public Func<string>? ValueGetter { get; init; }
        public Func<string?, Task>? ValueSetter { get; init; }
        public Func<DraggableWidget, string>? GetWidgetTextStyle { get; init; }
        public Func<DraggableWidget, string>? GetWidgetBackground { get; init; }
        public IReadOnlyList<ListItem>? Items { get; init; }

        public string ResolveTextStyle()
            => GetWidgetTextStyle?.Invoke(Widget) ?? "font-size:12px; color:#333;";

        public string ResolveBackground()
            => GetWidgetBackground?.Invoke(Widget) ?? "#fff";
    }

    /// <summary>
    /// 设计态渲染上下文
    /// </summary>
    public sealed record DesignRenderContext
    {
        public required RenderTreeBuilder Builder { get; init; }
        public required Func<DraggableWidget, string> TextStyleResolver { get; init; }
        public required Func<DraggableWidget, string> BackgroundResolver { get; init; }
        public required Func<string, string> Localize { get; init; }
    }

    /// <summary>
    /// 渲染运行时视图（Browse或Edit模式）
    /// 子类应重写此方法以实现自己的运行时渲染逻辑
    /// </summary>
    public virtual void RenderRuntime(RuntimeRenderContext context)
    {
        // 默认实现：简单的只读文本显示
        var value = context.ValueGetter?.Invoke() ?? string.Empty;
        RenderReadOnlyValue(context, value);
    }

    /// <summary>
    /// 渲染设计态预览
    /// 子类应重写此方法以实现自己的设计态渲染逻辑
    /// </summary>
    public virtual void RenderDesign(DesignRenderContext context)
    {
        // 默认实现：带边框的标签占位符
        var builder = context.Builder;
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:6px; background:{context.BackgroundResolver(this)}; pointer-events:none;");
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", $"{context.TextStyleResolver(this)} font-size:11px; margin-bottom:2px;");
        builder.AddContent(4, Label);
        builder.CloseElement();
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "height:32px; background:#fff; border:1px solid #e0e0e0; border-radius:2px;");
        builder.CloseElement();
        builder.CloseElement();
    }

    // ===== 辅助渲染方法 =====

    protected static void RenderFieldLabel(RenderTreeBuilder builder, string label)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "runtime-field-label");
        builder.AddContent(2, label);
        builder.CloseElement();
    }

    protected static void RenderReadOnlyValue(RuntimeRenderContext context, string value)
    {
        var builder = context.Builder;
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"display:flex; flex-direction:column; gap:4px; background:{context.ResolveBackground()}; padding:4px 0;");
        RenderFieldLabel(builder, context.Label);
        builder.OpenElement(4, "div");
        builder.AddAttribute(5, "class", "runtime-field-value");
        builder.AddAttribute(6, "style", context.ResolveTextStyle());
        builder.AddContent(7, string.IsNullOrWhiteSpace(value) ? "--" : value);
        builder.CloseElement();
        builder.CloseElement();
    }

    // ===== IResizable 接口实现 =====

    // 调整大小时的初始状态（用于基于总增量计算）
    private int? _resizeStartWidth;
    private int? _resizeStartHeight;
    private int? _resizeStartX;
    private int? _resizeStartY;
    private int? _resizeStartW;
    private int? _resizeStartH;

    /// <summary>
    /// 开始调整大小（保存初始状态）
    /// </summary>
    public virtual void OnResizeStart()
    {
        _resizeStartWidth = Width;
        _resizeStartHeight = Height;
        _resizeStartX = X;
        _resizeStartY = Y;
        _resizeStartW = W;
        _resizeStartH = H;
    }

    /// <summary>
    /// 响应调整大小事件
    /// 子类可以重写此方法来实现自定义的调整逻辑
    /// </summary>
    /// <param name="direction">方向</param>
    /// <param name="deltaX">相对于上次的X增量（通常不用）</param>
    /// <param name="deltaY">相对于上次的Y增量（通常不用）</param>
    /// <param name="totalDeltaX">相对于起始点的总X增量（用这个！）</param>
    /// <param name="totalDeltaY">相对于起始点的总Y增量（用这个！）</param>
    /// <param name="containerWidth">容器宽度</param>
    public virtual void OnResize(string direction, int deltaX, int deltaY, int totalDeltaX, int totalDeltaY, double containerWidth)
    {
        if (!_resizeStartWidth.HasValue)
        {
            // 如果没有调用OnResizeStart，使用当前值作为起始值
            OnResizeStart();
        }

        var dir = (direction ?? string.Empty).Trim().ToLowerInvariant();
        dir = dir switch
        {
            "horizontal" => "e",
            "vertical" => "s",
            "both" => "se",
            _ => dir
        };

        var affectsWidth = dir.Contains('e') || dir.Contains('w');
        var affectsHeight = dir.Contains('n') || dir.Contains('s');
        var fromWest = dir.Contains('w');
        var fromNorth = dir.Contains('n');

        var widthDelta = fromWest ? -totalDeltaX : totalDeltaX;
        var heightDelta = fromNorth ? -totalDeltaY : totalDeltaY;

        const int minWidthPx = 50;
        const int minHeightPx = 30;

        if (Layout.Mode == LayoutMode.Absolute && _resizeStartW.HasValue && _resizeStartH.HasValue && _resizeStartX.HasValue && _resizeStartY.HasValue)
        {
            var startX = _resizeStartX.Value;
            var startY = _resizeStartY.Value;
            var startW = _resizeStartW.Value;
            var startH = _resizeStartH.Value;

            var rightEdge = startX + startW;
            var bottomEdge = startY + startH;

            var newW = affectsWidth ? startW + widthDelta : startW;
            var newH = affectsHeight ? startH + heightDelta : startH;

            newW = Math.Max(minWidthPx, newW);
            newH = Math.Max(minHeightPx, newH);

            if (affectsWidth)
            {
                W = newW;
                if (fromWest)
                {
                    X = rightEdge - newW;
                }
            }

            if (affectsHeight)
            {
                H = newH;
                if (fromNorth)
                {
                    Y = bottomEdge - newH;
                }
            }

            return;
        }

        if (affectsWidth)
        {
            if (WidthUnit == "%")
            {
                var widthBasis = containerWidth > 0 ? containerWidth : 1200.0;
                var minPercent = (minWidthPx / widthBasis) * 100.0;
                var deltaPercent = (widthDelta / widthBasis) * 100.0;
                var newWidth = (int)Math.Round(_resizeStartWidth!.Value + deltaPercent);
                var clamped = (int)Math.Round(Math.Max(minPercent, Math.Min(100.0, newWidth)));
                Width = Math.Max(1, clamped);
            }
            else
            {
                Width = Math.Max(minWidthPx, _resizeStartWidth!.Value + widthDelta);
            }
        }

        if (affectsHeight && HeightUnit == "px" && _resizeStartHeight.HasValue)
        {
            Height = Math.Max(minHeightPx, _resizeStartHeight.Value + heightDelta);
        }
    }

    /// <summary>
    /// 调整大小结束
    /// 子类可以重写此方法来处理完成后的清理工作
    /// </summary>
    public virtual void OnResizeEnd()
    {
        // 清除初始状态
        _resizeStartWidth = null;
        _resizeStartHeight = null;
        _resizeStartX = null;
        _resizeStartY = null;
        _resizeStartW = null;
        _resizeStartH = null;
    }
}
