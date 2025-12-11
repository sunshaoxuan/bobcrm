using BobCrm.App.Services.Widgets;
using BobCrm.App.Services.Widgets.Rendering;
using AntDesign;

namespace BobCrm.App.Models.Widgets;

[WidgetMetadata("tabbox", "LBL_TABBOX", "Outline.Appstore", WidgetRegistry.WidgetCategory.Layout)]
/// <summary>
/// Tabs 容器控件
/// </summary>
public class TabContainerWidget : ContainerWidget
{
    public TabContainerWidget()
    {
        Type = "tabbox";
        Label = "LBL_TABBOX";
        Width = 100;
        WidthUnit = "%";
        HeightUnit = "auto";
        Children = new List<DraggableWidget>();
    }

    /// <summary>默认激活的 TabId</summary>
    public string? ActiveTabId { get; set; }

    /// <summary>是否带动画切换</summary>
    public bool Animated { get; set; } = true;

    /// <summary>标签是否可居中显示</summary>
    public bool Centered { get; set; } = false;

    /// <summary>
    /// 运行态渲染：使用 RuntimeContainerRenderer.RenderTabContainer
    /// </summary>
    public override void RenderRuntime(RuntimeRenderContext context)
    {
        RuntimeContainerRenderer.RenderTabContainer(
            context.Builder,
            this,
            context.Mode,
            (child, mode) => context.RenderChild(child),
            // 获取当前激活的 Tab
            container => 
            {
                var tabs = container.Children?.OfType<TabWidget>().ToList();
                if (tabs == null || !tabs.Any()) return null;
                
                // 1. Try to find by ActiveTabId
                var active = tabs.FirstOrDefault(t => t.TabId == container.ActiveTabId);
                
                // 2. Fallback to IsDefault
                if (active == null) active = tabs.FirstOrDefault(t => t.IsDefault);
                
                // 3. Fallback to first
                if (active == null) active = tabs.First();
                
                return active;
            },
            (w, mode) => WidgetStyleHelper.GetRuntimeWidgetStyle(w, mode)
        );
    }

    /// <summary>
    /// 获取 TabContainer 控件的属性元数据
    /// </summary>
    public override List<BobCrm.App.Models.Designer.WidgetPropertyMetadata> GetPropertyMetadata()
    {
        var properties = base.GetPropertyMetadata();

        properties.AddRange(new List<BobCrm.App.Models.Designer.WidgetPropertyMetadata>
        {
            new() { PropertyPath = "Animated", Label = "PROP_ANIMATED", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Centered", Label = "PROP_CENTERED", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Boolean },
            new() { PropertyPath = "Size", Label = "PROP_SIZE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Select,
                Options = new List<BobCrm.App.Models.Designer.PropertyOption>
                {
                    new() { Value = "small", Label = "PROP_SIZE_SMALL" },
                    new() { Value = "default", Label = "PROP_SIZE_DEFAULT" },
                    new() { Value = "large", Label = "PROP_SIZE_LARGE" }
                }
            },
            new() { PropertyPath = "Type", Label = "PROP_TYPE", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Select,
                Options = new List<BobCrm.App.Models.Designer.PropertyOption>
                {
                    new() { Value = "line", Label = "PROP_TAB_TYPE_LINE" },
                    new() { Value = "card", Label = "PROP_TAB_TYPE_CARD" }
                }
            },
            new() { PropertyPath = "TabPosition", Label = "PROP_TAB_POSITION", EditorType = BobCrm.App.Models.Designer.PropertyEditorType.Select,
                Options = new List<BobCrm.App.Models.Designer.PropertyOption>
                {
                    new() { Value = "top", Label = "PROP_POSITION_TOP" },
                    new() { Value = "bottom", Label = "PROP_POSITION_BOTTOM" },
                    new() { Value = "left", Label = "PROP_POSITION_LEFT" },
                    new() { Value = "right", Label = "PROP_POSITION_RIGHT" }
                }
            }
        });

        return properties;
    }

    public override string GetDefaultCodePrefix()
    {
        return "tabcontainer";
    }
}
