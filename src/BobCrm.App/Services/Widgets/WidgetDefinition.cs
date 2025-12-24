using BobCrm.App.Models.Widgets;

namespace BobCrm.App.Services.Widgets;

public record WidgetDefinition(
    string Type,
    string LabelKey,
    string Icon,
    WidgetCategory Category,
    Func<DraggableWidget> Factory);
