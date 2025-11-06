namespace BobCrm.App.Models.Widgets;

/// <summary>
/// Section/Block 容器控件
/// 可配置的多块容器，块内可放子控件，支持背景色、大小与对齐
/// </summary>
public class SectionWidget : ContainerWidget
{
    public SectionWidget()
    {
        Type = "section";
        Label = "Section";

        // 容器外表现（作为父容器中的一个项）
        Width = 100;
        WidthUnit = "%";
        Height = 200;
        HeightUnit = "px";

        // 容器内布局配置
        ContainerLayout = new ContainerLayoutOptions
        {
            Mode = ContainerLayoutMode.Flow,
            FlexDirection = "row",
            FlexWrap = true,
            JustifyContent = "flex-start",
            AlignItems = "flex-start",
            Gap = 8,
            Padding = 12,
            BackgroundColor = "#f5f5f5",
            BorderRadius = 8,
            BorderStyle = "solid",
            BorderColor = "#d9d9d9",
            BorderWidth = 1
        };
    }

    /// <summary>
    /// 容器内布局选项
    /// </summary>
    public ContainerLayoutOptions ContainerLayout { get; set; } = new ContainerLayoutOptions();

    /// <summary>
    /// Section 特有属性：标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 是否显示标题
    /// </summary>
    public bool ShowTitle { get; set; } = false;

    /// <summary>
    /// 是否可折叠
    /// </summary>
    public bool Collapsible { get; set; } = false;

    /// <summary>
    /// 是否默认折叠
    /// </summary>
    public bool Collapsed { get; set; } = false;

    /// <summary>
    /// 判断某个属性是否可编辑
    /// </summary>
    public override bool CanEditProperty(string propertyName)
    {
        return propertyName switch
        {
            // Section 支持编辑所有通用属性
            "Width" => true,
            "Height" => true,
            "Title" => true,
            "ShowTitle" => true,
            "Collapsible" => true,
            "BackgroundColor" => true,
            "Padding" => true,
            "Gap" => true,
            "JustifyContent" => true,
            "AlignItems" => true,
            "FlexDirection" => true,
            "FlexWrap" => true,
            "BorderRadius" => true,
            "BorderColor" => true,
            "BorderWidth" => true,
            _ => base.CanEditProperty(propertyName)
        };
    }

    /// <summary>
    /// 获取 Section 控件的属性元数据
    /// </summary>
    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        return new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Title", Label = "PROP_TITLE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text, Placeholder = "PROP_SECTION_TITLE_PLACEHOLDER" },
            new() { PropertyPath = "ShowTitle", Label = "PROP_SHOW_TITLE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Collapsible", Label = "PROP_COLLAPSIBLE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Collapsed", Label = "PROP_COLLAPSED_DEFAULT", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean, VisibleWhen = "Collapsible", VisibleWhenValue = true },
            new() { PropertyPath = "ContainerLayout.Gap", Label = "PROP_GAP", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 0, Max = 48, Group = "PROP_GROUP_LAYOUT" },
            new() { PropertyPath = "ContainerLayout.Padding", Label = "PROP_PADDING", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 0, Max = 48, Group = "PROP_GROUP_LAYOUT" },
            new() { PropertyPath = "ContainerLayout.BackgroundColor", Label = "PROP_BACKGROUND_COLOR", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Color, Placeholder = "#f5f5f5", Group = "PROP_GROUP_LAYOUT" },
            new() { PropertyPath = "ContainerLayout.BorderRadius", Label = "PROP_BORDER_RADIUS", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number, Min = 0, Max = 24, Group = "PROP_GROUP_LAYOUT" }
        };
    }
}
