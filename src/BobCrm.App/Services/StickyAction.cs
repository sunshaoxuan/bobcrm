namespace BobCrm.App.Services;

public record StickyAction(string Label, InteractionActionTone Tone, Func<Task>? Callback);
