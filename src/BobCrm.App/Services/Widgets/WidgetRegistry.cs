using AntDesign;
using BobCrm.App.Models.Widgets;

namespace BobCrm.App.Services.Widgets;

/// <summary>
/// 注册所有页面可用的控件类型，集中管理图标、默认标签与创建逻辑。
/// </summary>
public static class WidgetRegistry
{
    public enum WidgetCategory
    {
        Basic,
        Layout
    }

    public record WidgetDefinition(
        string Type,
        string LabelKey,
        string Icon,
        WidgetCategory Category,
        Func<DraggableWidget> Factory);

    private static readonly Dictionary<string, WidgetDefinition> _definitions;

    public static IReadOnlyList<WidgetDefinition> BasicWidgets { get; }
    public static IReadOnlyList<WidgetDefinition> LayoutWidgets { get; }

    static WidgetRegistry()
    {
        var defs = new[]
        {
            new WidgetDefinition("textbox", "LBL_TEXTBOX", IconType.Outline.Edit, WidgetCategory.Basic, () => new TextboxWidget()),
            new WidgetDefinition("number", "LBL_NUMBER", IconType.Outline.FieldNumber, WidgetCategory.Basic, () => new NumberWidget()),
            new WidgetDefinition("select", "LBL_SELECT", IconType.Outline.Select, WidgetCategory.Basic, () => new SelectWidget()),
            new WidgetDefinition("checkbox", "LBL_CHECKBOX", IconType.Outline.CheckSquare, WidgetCategory.Basic, () => new CheckboxWidget()),
            new WidgetDefinition("radio", "LBL_RADIO", IconType.Outline.DotChart, WidgetCategory.Basic, () => new RadioWidget()),
            new WidgetDefinition("listbox", "LBL_LISTBOX", IconType.Outline.UnorderedList, WidgetCategory.Basic, () => new ListboxWidget()),
            new WidgetDefinition("textarea", "LBL_TEXTAREA", IconType.Outline.FileText, WidgetCategory.Basic, () => new TextareaWidget()),
            new WidgetDefinition("calendar", "LBL_CALENDAR", IconType.Outline.Calendar, WidgetCategory.Basic, () => new CalendarWidget()),
            new WidgetDefinition("button", "LBL_BUTTON", IconType.Outline.Inbox, WidgetCategory.Basic, () => new ButtonWidget()),
            new WidgetDefinition("label", "LBL_LABEL", IconType.Outline.FileText, WidgetCategory.Basic, () => new LabelWidget()),

            new WidgetDefinition("section", "LBL_SECTION", IconType.Outline.AppstoreAdd, WidgetCategory.Layout, () => new SectionWidget()),
            new WidgetDefinition("panel", "LBL_PANEL", IconType.Outline.Appstore, WidgetCategory.Layout, () => new PanelWidget()),
            new WidgetDefinition("grid", "LBL_GRID", IconType.Outline.BorderOuter, WidgetCategory.Layout, () => new GridWidget()),
            new WidgetDefinition("frame", "LBL_FRAME", IconType.Outline.BorderOuter, WidgetCategory.Layout, () => new FrameWidget()),
            new WidgetDefinition("tabbox", "LBL_TABBOX", IconType.Outline.Appstore, WidgetCategory.Layout, () => new TabContainerWidget()),
            new WidgetDefinition("tab", "LBL_TAB", IconType.Outline.Tag, WidgetCategory.Layout, () => new TabWidget()),
        };

        _definitions = defs.ToDictionary(d => d.Type.ToLowerInvariant());
        BasicWidgets = defs.Where(d => d.Category == WidgetCategory.Basic).ToList();
        LayoutWidgets = defs.Where(d => d.Category == WidgetCategory.Layout && d.Type != "tab").ToList(); // tab 是内部使用
    }

    public static WidgetDefinition GetDefinition(string type)
    {
        if (!_definitions.TryGetValue(type.ToLowerInvariant(), out var def))
            throw new InvalidOperationException($"Unknown widget type: {type}");
        return def;
    }

    /// <summary>
    /// 创建新的控件实例，并设置公共默认属性。
    /// </summary>
    public static DraggableWidget Create(string type, string? labelOverride = null)
    {
        var def = GetDefinition(type);
        var widget = def.Factory();

        widget.Id = Guid.NewGuid().ToString();
        widget.Type = def.Type;
        widget.Label = labelOverride ?? def.LabelKey;
        widget.Width = 48;
        widget.WidthUnit = "%";
        widget.Visible = true;

        if (widget is ContainerWidget container && container.Children == null)
        {
            container.Children = new List<DraggableWidget>();
        }

        if (widget is TabContainerWidget tabContainer)
        {
            EnsureTabContainerDefaults(tabContainer);
        }
        else if (widget is TabWidget tabWidget && tabWidget.Children == null)
        {
            tabWidget.Children = new List<DraggableWidget>();
        }

        return widget;
    }

    private static void EnsureTabContainerDefaults(TabContainerWidget tabContainer)
    {
        tabContainer.Children ??= new List<DraggableWidget>();

        if (!tabContainer.Children.OfType<TabWidget>().Any())
        {
            var tab1 = new TabWidget { Label = "Tab 1", IsDefault = true };
            var tab2 = new TabWidget { Label = "Tab 2" };
            tabContainer.Children.Add(tab1);
            tabContainer.Children.Add(tab2);
        }

        var firstTab = tabContainer.Children.OfType<TabWidget>().First();
        tabContainer.ActiveTabId = firstTab.TabId;
    }
}
