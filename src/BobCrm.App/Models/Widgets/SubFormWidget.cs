using System;
using AntDesign;
using BobCrm.App.Services.Widgets;

namespace BobCrm.App.Models.Widgets;

[WidgetMetadata("subform", "LBL_SUBFORM", "Outline.Subnode", WidgetCategory.Data)]
/// <summary>
/// SubForm 主从表单控件 - 用于处理一对多关系
/// 例如：订单-订单项、客户-联系人等
/// </summary>
public class SubFormWidget : ContainerWidget
{
    public SubFormWidget()
    {
        Type = "subform";
        Label = "LBL_SUBFORM";
        Width = 100;
        WidthUnit = "%";
        HeightUnit = "auto";

        // SubForm 默认布局配置
        ContainerLayout = new ContainerLayoutOptions
        {
            Mode = ContainerLayoutMode.Flow,
            FlexDirection = "column",
            FlexWrap = false,
            JustifyContent = "flex-start",
            AlignItems = "stretch",
            Gap = 8,
            Padding = 12,
            BackgroundColor = "#f5f5f5",
            BorderRadius = 6,
            BorderStyle = "solid",
            BorderColor = "#d9d9d9",
            BorderWidth = 1
        };
    }

    /// <summary>容器内布局选项</summary>
    public ContainerLayoutOptions ContainerLayout { get; set; } = new();

    /// <summary>关联实体类型（子实体）</summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>外键字段名（子实体中指向主实体的字段）</summary>
    public string? ForeignKeyField { get; set; }

    /// <summary>嵌入的子表单模板 ID（可选，用于自定义子表单布局）</summary>
    public int? EmbeddedTemplateId { get; set; }

    /// <summary>是否允许新增子记录</summary>
    public bool AllowAdd { get; set; } = true;

    /// <summary>是否允许编辑子记录</summary>
    public bool AllowEdit { get; set; } = true;

    /// <summary>是否允许删除子记录</summary>
    public bool AllowDelete { get; set; } = true;

    /// <summary>显示模式（table：表格模式，cards：卡片模式）</summary>
    public string DisplayMode { get; set; } = "table";

    /// <summary>最大子记录数（0 表示无限制）</summary>
    public int MaxItems { get; set; } = 0;

    /// <summary>是否显示工具栏</summary>
    public bool ShowToolbar { get; set; } = true;

    /// <summary>子表单标题</summary>
    public string? Title { get; set; }

    /// <summary>是否显示标题</summary>
    public bool ShowTitle { get; set; } = true;

    /// <summary>
    /// 获取 SubForm 控件的属性元数据
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
                Placeholder = "PROP_SUBFORM_TITLE_PLACEHOLDER",
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
                PropertyPath = "RelatedEntityType",
                Label = "PROP_RELATED_ENTITY_TYPE",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.DataSetPicker,
                Group = "PROP_GROUP_DATA",
                HelpText = "PROP_RELATED_ENTITY_TYPE_HELP"
            },
            new()
            {
                PropertyPath = "ForeignKeyField",
                Label = "PROP_FOREIGN_KEY_FIELD",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Text,
                Placeholder = "PROP_FOREIGN_KEY_PLACEHOLDER",
                Group = "PROP_GROUP_DATA"
            },
            new()
            {
                PropertyPath = "EmbeddedTemplateId",
                Label = "PROP_EMBEDDED_TEMPLATE",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number,
                Group = "PROP_GROUP_DATA"
            },
            new()
            {
                PropertyPath = "DisplayMode",
                Label = "PROP_DISPLAY_MODE",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Select,
                Group = "PROP_GROUP_DISPLAY",
                Options = new List<BobCrm.App.Models.Designer.PropertyOption>
                {
                    new() { Value = "table", Label = "PROP_MODE_TABLE" },
                    new() { Value = "cards", Label = "PROP_MODE_CARDS" }
                }
            },
            new()
            {
                PropertyPath = "AllowAdd",
                Label = "PROP_ALLOW_ADD",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean,
                Group = "PROP_GROUP_BEHAVIOR"
            },
            new()
            {
                PropertyPath = "AllowEdit",
                Label = "PROP_ALLOW_EDIT",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean,
                Group = "PROP_GROUP_BEHAVIOR"
            },
            new()
            {
                PropertyPath = "AllowDelete",
                Label = "PROP_ALLOW_DELETE",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean,
                Group = "PROP_GROUP_BEHAVIOR"
            },
            new()
            {
                PropertyPath = "MaxItems",
                Label = "PROP_MAX_ITEMS",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Number,
                Min = 0,
                Max = 1000,
                Group = "PROP_GROUP_BEHAVIOR"
            },
            new()
            {
                PropertyPath = "ShowToolbar",
                Label = "PROP_SHOW_TOOLBAR",
                EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean,
                Group = "PROP_GROUP_DISPLAY"
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
            }
        });

        return properties;
    }

    public override string GetDefaultCodePrefix()
    {
        return "subform";
    }

    public override void RenderRuntime(RuntimeRenderContext context)
    {
        var builder = context.Builder;

        // 使用 SubFormRuntime 组件进行渲染
        builder.OpenComponent<BobCrm.App.Components.Shared.SubFormRuntime>(0);
        builder.AddAttribute(1, "Widget", this);
        builder.AddAttribute(2, "ContainerStyle", "");
        builder.AddAttribute(3, "MasterEntityId", context.Widget.ExtendedProperties?.GetValueOrDefault("MasterEntityId")?.ToString());
        builder.CloseComponent();
    }

    public override Type? RuntimeComponentType => typeof(BobCrm.App.Components.Shared.SubFormRuntime);

    public override void RenderDesign(DesignRenderContext context)
    {
        var builder = context.Builder;

        // 设计态预览
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:12px; background:{context.BackgroundResolver(this)}; border:1px dashed #d9d9d9; border-radius:4px;");

        // 标题
        if (!string.IsNullOrWhiteSpace(Title) && ShowTitle)
        {
            builder.OpenElement(2, "div");
            builder.AddAttribute(3, "style", $"{context.TextStyleResolver(this)} font-weight:bold; margin-bottom:8px;");
            builder.AddContent(4, context.Localize(Title));
            builder.CloseElement();
        }

        // SubForm 占位符
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "background:#fafafa; border:1px solid #e0e0e0; border-radius:4px; padding:16px; text-align:center;");
        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "style", "font-size:12px; color:#999; margin-bottom:4px;");
        builder.AddContent(9, $"SubForm: {RelatedEntityType ?? "未配置"}");
        builder.CloseElement();
        builder.OpenElement(10, "div");
        builder.AddAttribute(11, "style", "font-size:11px; color:#ccc;");
        builder.AddContent(12, $"外键: {ForeignKeyField ?? "未配置"} | 模式: {DisplayMode}");
        builder.CloseElement();
        builder.CloseElement();

        builder.CloseElement();
    }
}
