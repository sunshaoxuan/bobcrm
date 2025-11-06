namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 可拖放控件基类 - 所有控件的基础
/// 提供所有控件通用的拖放、调整大小、布局等功能
/// 使用组合模式管理不同的布局选项
/// </summary>
public abstract class DraggableWidget : IResizable, IFlowSized, IAbsolutePositioned
{
    // ===== 基本标识 =====

    /// <summary>控件唯一标识</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>控件类型</summary>
    public string Type { get; set; } = "";

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
            new() { PropertyPath = "Label", Label = "PROP_LABEL", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text },
            new() { PropertyPath = "Width", Label = "PROP_WIDTH", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 1, Max = GetMaxWidth() },
            new() { PropertyPath = "Visible", Label = "PROP_VISIBLE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean }
        };
    }

    // ===== IResizable 接口实现 =====

    // 调整大小时的初始状态（用于基于总增量计算）
    private int? _resizeStartWidth;
    private int? _resizeStartHeight;

    /// <summary>
    /// 开始调整大小（保存初始状态）
    /// </summary>
    public virtual void OnResizeStart()
    {
        _resizeStartWidth = Width;
        _resizeStartHeight = Height;
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
            _resizeStartWidth = Width;
            _resizeStartHeight = Height;
        }

        if (direction == "horizontal" || direction == "both")
        {
            if (WidthUnit == "%")
            {
                // 百分比模式：将像素总增量转换为百分比增量
                if (containerWidth > 0)
                {
                    double deltaPercent = (totalDeltaX / containerWidth) * 100;
                    int newWidth = (int)Math.Round(_resizeStartWidth.Value + deltaPercent);
                    Width = Math.Max(8, Math.Min(100, newWidth));
                }
                else
                {
                    // 容器宽度为0时的保底处理：假设1200px作为基准
                    double deltaPercent = (totalDeltaX / 1200.0) * 100;
                    int newWidth = (int)Math.Round(_resizeStartWidth.Value + deltaPercent);
                    Width = Math.Max(8, Math.Min(100, newWidth));
                    System.Diagnostics.Debug.WriteLine($"[DraggableWidget] Container width is 0, using fallback 1200px. Width: {_resizeStartWidth.Value}% -> {Width}%");
                }
            }
            else
            {
                // 像素模式：使用总增量
                Width = Math.Max(100, _resizeStartWidth.Value + totalDeltaX);
            }
        }

        if (direction == "vertical" || direction == "both")
        {
            if (HeightUnit == "px" && _resizeStartHeight.HasValue)
            {
                Height = Math.Max(40, _resizeStartHeight.Value + totalDeltaY);
            }
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
    }
}
