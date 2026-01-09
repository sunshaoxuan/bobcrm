using System.Reflection;
using AntDesign;
using BobCrm.App.Models.Widgets;

namespace BobCrm.App.Services.Widgets;

/// <summary>
/// 注册所有页面可用的控件类型，集中管理图标、默认标签与创建逻辑。
/// 说明：这里的“写死”并非偷工减料，而是前端的白名单 + 工厂模式，
/// 用来：
/// 1) 防止运行态注入任意未知组件（安全性、稳定性）；
/// 2) 为每个类型附带图标、多语 Label Key 和默认属性；
/// 3) 后续如需动态扩展，可改为从后端/配置加载清单，再统一注册到 _definitions。
/// </summary>
public static class WidgetRegistry
{
    private static readonly Dictionary<string, WidgetDefinition> _definitions;

    public static IReadOnlyList<WidgetDefinition> BasicWidgets { get; }
    public static IReadOnlyList<WidgetDefinition> LayoutWidgets { get; }
    public static IReadOnlyList<WidgetDefinition> DataWidgets { get; }

    static WidgetRegistry()
    {
        try
        {
            var builtIn = new[]
            {
                new WidgetDefinition("textbox", "LBL_TEXTBOX", IconType.Outline.Edit, WidgetCategory.Basic, () => new TextboxWidget()),
                new WidgetDefinition("text", "LBL_TEXTBOX", IconType.Outline.Edit, WidgetCategory.Basic, () => new TextboxWidget()),
                new WidgetDefinition("number", "LBL_NUMBER", IconType.Outline.FieldNumber, WidgetCategory.Basic, () => new NumberWidget()),
                new WidgetDefinition("select", "LBL_SELECT", IconType.Outline.Select, WidgetCategory.Basic, () => new SelectWidget()),
                new WidgetDefinition("checkbox", "LBL_CHECKBOX", IconType.Outline.CheckSquare, WidgetCategory.Basic, () => new CheckboxWidget()),
                new WidgetDefinition("radio", "LBL_RADIO", IconType.Outline.DotChart, WidgetCategory.Basic, () => new RadioWidget()),
                new WidgetDefinition("listbox", "LBL_LISTBOX", IconType.Outline.UnorderedList, WidgetCategory.Basic, () => new ListboxWidget()),
                new WidgetDefinition("textarea", "LBL_TEXTAREA", IconType.Outline.FileText, WidgetCategory.Basic, () => new TextareaWidget()),
                new WidgetDefinition("calendar", "LBL_CALENDAR", IconType.Outline.Calendar, WidgetCategory.Basic, () => new CalendarWidget()),
                new WidgetDefinition("date", "LBL_CALENDAR", IconType.Outline.Calendar, WidgetCategory.Basic, () => new CalendarWidget()),
                new WidgetDefinition("button", "LBL_BUTTON", IconType.Outline.Inbox, WidgetCategory.Basic, () => new ButtonWidget()),
                new WidgetDefinition("label", "LBL_LABEL", IconType.Outline.FileText, WidgetCategory.Basic, () => new LabelWidget()),
                new WidgetDefinition("enumselector", "LBL_SELECT", IconType.Outline.Select, WidgetCategory.Basic, () => new SelectWidget()),

                new WidgetDefinition("section", "LBL_SECTION", IconType.Outline.AppstoreAdd, WidgetCategory.Layout, () => new SectionWidget()),
                new WidgetDefinition("panel", "LBL_PANEL", IconType.Outline.Appstore, WidgetCategory.Layout, () => new PanelWidget()),
                new WidgetDefinition("card", "LBL_CARD", IconType.Outline.Container, WidgetCategory.Layout, () => new CardWidget()),
                new WidgetDefinition("grid", "LBL_GRID", IconType.Outline.BorderOuter, WidgetCategory.Layout, () => new GridWidget()),
                new WidgetDefinition("frame", "LBL_FRAME", IconType.Outline.BorderOuter, WidgetCategory.Layout, () => new FrameWidget()),
                new WidgetDefinition("tabbox", "LBL_TABBOX", IconType.Outline.Appstore, WidgetCategory.Layout, () => new TabContainerWidget()),
                new WidgetDefinition("tab", "LBL_TAB", IconType.Outline.Tag, WidgetCategory.Layout, () => new TabWidget()),

                // 数据控件
                new WidgetDefinition("datagrid", "LBL_DATAGRID", IconType.Outline.Table, WidgetCategory.Data, () => new DataGridWidget()),
                new WidgetDefinition("subform", "LBL_SUBFORM", IconType.Outline.Subnode, WidgetCategory.Data, () => new SubFormWidget()),
                new WidgetDefinition("orgtree", "LBL_ORGTREE", IconType.Outline.Apartment, WidgetCategory.Data, () => new OrganizationTreeWidget()),
                new WidgetDefinition("permtree", "LBL_PERMTREE", IconType.Outline.SafetyCertificate, WidgetCategory.Data, () => new RolePermissionTreeWidget()),
                new WidgetDefinition("userrole", "LBL_USERROLE", IconType.Outline.UserSwitch, WidgetCategory.Data, () => new UserRoleAssignmentWidget()),
            };

            var dynamic = DiscoverWidgets().ToList();
            var merged = builtIn
                .Concat(dynamic)
                .GroupBy(d => d.Type.ToLowerInvariant())
                .Select(g => g.First())
                .ToList();

            _definitions = merged.ToDictionary(d => d.Type.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);
            BasicWidgets = merged.Where(d => d.Category == WidgetCategory.Basic).ToList();
            LayoutWidgets = merged.Where(d => d.Category == WidgetCategory.Layout && d.Type != "tab").ToList();
            DataWidgets = merged.Where(d => d.Category == WidgetCategory.Data).ToList();

            Console.WriteLine($"[WidgetRegistry] Initialized with {merged.Count} widgets. Registered types: {string.Join(", ", _definitions.Keys)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WidgetRegistry] CRITICAL ERROR during initialization: {ex.Message}\n{ex.StackTrace}");
            // Fallback to avoid complete crash
            _definitions = new Dictionary<string, WidgetDefinition>(StringComparer.OrdinalIgnoreCase);
            BasicWidgets = new List<WidgetDefinition>();
            LayoutWidgets = new List<WidgetDefinition>();
            DataWidgets = new List<WidgetDefinition>();
        }
    }

    public static WidgetDefinition GetDefinition(string type)
    {
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Widget type cannot be null or empty", nameof(type));

        // 强制使用小写查找以匹配注册信息
        if (!_definitions.TryGetValue(type.ToLowerInvariant(), out var def))
        {
            var available = string.Join(", ", _definitions.Keys);
            throw new InvalidOperationException($"未知的 Widget 类型: '{type}'。当前已注册类型: [{available}]");
        }
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

    private static IEnumerable<WidgetDefinition> DiscoverWidgets()
    {
        var asm = typeof(WidgetRegistry).Assembly;
        var candidates = asm.GetTypes()
            .Where(t => !t.IsAbstract && typeof(DraggableWidget).IsAssignableFrom(t));

        foreach (var type in candidates)
        {
            var meta = type.GetCustomAttribute<WidgetMetadataAttribute>();
            if (meta == null) continue;
            if (string.IsNullOrWhiteSpace(meta.Type) || string.IsNullOrWhiteSpace(meta.LabelKey)) continue;
            if (type.GetConstructor(Type.EmptyTypes) == null) continue; // 需要无参构造

            Func<DraggableWidget> factory = () => (DraggableWidget)Activator.CreateInstance(type)!;
            // 统一转换为小写以防止大小写问题
            yield return new WidgetDefinition(meta.Type.ToLowerInvariant(), meta.LabelKey, meta.Icon, meta.Category, factory);
        }
    }

    private static void EnsureTabContainerDefaults(TabContainerWidget tabContainer)
    {
        tabContainer.Children ??= new List<DraggableWidget>();

        if (!tabContainer.Children.OfType<TabWidget>().Any())
        {
            var tab1 = new TabWidget { Label = "LBL_TAB_1", IsDefault = true };
            var tab2 = new TabWidget { Label = "LBL_TAB_2" };
            tabContainer.Children.Add(tab1);
            tabContainer.Children.Add(tab2);
        }

        var firstTab = tabContainer.Children.OfType<TabWidget>().First();
        tabContainer.ActiveTabId = firstTab.TabId;
    }
}
