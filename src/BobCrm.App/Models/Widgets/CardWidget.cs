namespace BobCrm.App.Models.Widgets;

/// <summary>
/// Card 卡片控件 - 用于分组展示表单字段
/// 支持标题、折叠、边框等配置
/// </summary>
public class CardWidget : ContainerWidget
{
    public CardWidget()
    {
        Type = "card";
        Label = "LBL_CARD";
        Width = 100;
        WidthUnit = "%";
        HeightUnit = "auto";

        // 卡片默认布局配置
        ContainerLayout = new ContainerLayoutOptions
        {
            Mode = ContainerLayoutMode.Flow,
            FlexDirection = "column",
            FlexWrap = false,
            JustifyContent = "flex-start",
            AlignItems = "stretch",
            Gap = 12,
            Padding = 16,
            BackgroundColor = "#ffffff",
            BorderRadius = 8,
            BorderStyle = "solid",
            BorderColor = "#e8e8e8",
            BorderWidth = 1
        };
    }

    /// <summary>容器内布局选项</summary>
    public ContainerLayoutOptions ContainerLayout { get; set; } = new();

    /// <summary>卡片标题</summary>
    public string? Title { get; set; }

    /// <summary>是否显示标题</summary>
    public bool ShowTitle { get; set; } = true;

    /// <summary>是否可折叠</summary>
    public bool Collapsible { get; set; } = false;

    /// <summary>是否默认展开（当 Collapsible = true 时有效）</summary>
    public bool DefaultExpanded { get; set; } = true;

    /// <summary>标题栏背景色</summary>
    public string? HeaderBackgroundColor { get; set; } = "#fafafa";

    /// <summary>是否显示阴影</summary>
    public bool ShowShadow { get; set; } = false;

    /// <summary>
    /// 获取 Card 控件的属性元数据
    /// </summary>
    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        var properties = base.GetPropertyMetadata();

        properties.AddRange(new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new()
            {
                PropertyPath = "Title",
                Label = "PROP_TITLE",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text,
                Placeholder = "PROP_CARD_TITLE_PLACEHOLDER",
                Group = "PROP_GROUP_BASIC"
            },
            new()
            {
                PropertyPath = "ShowTitle",
                Label = "PROP_SHOW_TITLE",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean,
                Group = "PROP_GROUP_DISPLAY"
            },
            new()
            {
                PropertyPath = "Collapsible",
                Label = "PROP_COLLAPSIBLE",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean,
                Group = "PROP_GROUP_BEHAVIOR"
            },
            new()
            {
                PropertyPath = "DefaultExpanded",
                Label = "PROP_DEFAULT_EXPANDED",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean,
                Group = "PROP_GROUP_BEHAVIOR",
                VisibleWhen = "Collapsible",
                VisibleWhenValue = true
            },
            new()
            {
                PropertyPath = "ShowShadow",
                Label = "PROP_SHOW_SHADOW",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean,
                Group = "PROP_GROUP_DISPLAY"
            },
            new()
            {
                PropertyPath = "HeaderBackgroundColor",
                Label = "PROP_HEADER_BG_COLOR",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Color,
                Placeholder = "#fafafa",
                Group = "PROP_GROUP_STYLE"
            },
            new()
            {
                PropertyPath = "ContainerLayout.Gap",
                Label = "PROP_GAP",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number,
                Min = 0,
                Max = 48,
                Group = "PROP_GROUP_LAYOUT"
            },
            new()
            {
                PropertyPath = "ContainerLayout.Padding",
                Label = "PROP_PADDING",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number,
                Min = 0,
                Max = 48,
                Group = "PROP_GROUP_LAYOUT"
            },
            new()
            {
                PropertyPath = "ContainerLayout.BackgroundColor",
                Label = "PROP_BACKGROUND_COLOR",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Color,
                Placeholder = "#ffffff",
                Group = "PROP_GROUP_STYLE"
            },
            new()
            {
                PropertyPath = "ContainerLayout.BorderRadius",
                Label = "PROP_BORDER_RADIUS",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number,
                Min = 0,
                Max = 24,
                Group = "PROP_GROUP_STYLE"
            },
            new()
            {
                PropertyPath = "ContainerLayout.BorderColor",
                Label = "PROP_BORDER_COLOR",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Color,
                Placeholder = "#e8e8e8",
                Group = "PROP_GROUP_STYLE"
            },
            new()
            {
                PropertyPath = "ContainerLayout.BorderWidth",
                Label = "PROP_BORDER_WIDTH",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number,
                Min = 0,
                Max = 5,
                Group = "PROP_GROUP_STYLE"
            }
        });

        return properties;
    }

    public override string GetDefaultCodePrefix()
    {
        return "card";
    }

    public override void RenderDesign(DesignRenderContext context)
    {
        var builder = context.Builder;

        // Card 设计态预览
        var cardStyle = $"width:100%; background:{ContainerLayout.BackgroundColor}; " +
                       $"border:{ContainerLayout.BorderWidth}px {ContainerLayout.BorderStyle} {ContainerLayout.BorderColor}; " +
                       $"border-radius:{ContainerLayout.BorderRadius}px; " +
                       $"overflow:hidden; " +
                       (ShowShadow ? "box-shadow:0 2px 8px rgba(0,0,0,0.1);" : "");

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", cardStyle);

        // 标题栏
        if (!string.IsNullOrWhiteSpace(Title) && ShowTitle)
        {
            builder.OpenElement(2, "div");
            builder.AddAttribute(3, "style", $"padding:12px 16px; background:{HeaderBackgroundColor}; " +
                                            $"border-bottom:1px solid {ContainerLayout.BorderColor}; " +
                                            $"font-weight:600; font-size:14px; display:flex; justify-content:space-between; align-items:center;");
            builder.OpenElement(4, "span");
            builder.AddContent(5, context.Localize(Title));
            builder.CloseElement();

            // 折叠图标（如果可折叠）
            if (Collapsible)
            {
                builder.OpenElement(6, "span");
                builder.AddAttribute(7, "style", "font-size:12px; color:#999;");
                builder.AddContent(8, DefaultExpanded ? "▼" : "▶");
                builder.CloseElement();
            }
            builder.CloseElement();
        }

        // 内容区域
        builder.OpenElement(9, "div");
        builder.AddAttribute(10, "style", $"padding:{ContainerLayout.Padding}px; " +
                                         $"display:flex; flex-direction:{ContainerLayout.FlexDirection}; " +
                                         $"gap:{ContainerLayout.Gap}px; min-height:60px;");

        // 显示"拖放控件到这里"占位符
        if (Children == null || !Children.Any())
        {
            builder.OpenElement(11, "div");
            builder.AddAttribute(12, "style", "width:100%; padding:20px; text-align:center; color:#999; " +
                                             "border:2px dashed #d9d9d9; border-radius:4px; background:#fafafa;");
            builder.AddContent(13, "拖放控件到这里");
            builder.CloseElement();
        }

        builder.CloseElement(); // 内容区域
        builder.CloseElement(); // Card
    }
}
