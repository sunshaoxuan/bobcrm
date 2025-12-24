using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BobCrm.App.Models.Widgets;

namespace BobCrm.App.Services.Designer;

public sealed class DesignerStateStore
{
    private readonly JsonSerializerOptions _jsonOptions;

    public DesignerStateStore(JsonSerializerOptions jsonOptions)
    {
        _jsonOptions = jsonOptions;
    }

    public Stack<string> UndoStack { get; } = new();
    public Stack<string> RedoStack { get; } = new();

    public int MaxSnapshots { get; set; } = 100;

    public bool CanUndo => UndoStack.Count > 1;
    public bool CanRedo => RedoStack.Count > 0;

    public void Reset(IReadOnlyList<DraggableWidget> widgets)
    {
        UndoStack.Clear();
        RedoStack.Clear();
        SaveSnapshot(widgets);
    }

    public void SaveSnapshot(IReadOnlyList<DraggableWidget> widgets)
    {
        var json = JsonSerializer.Serialize(widgets, _jsonOptions);

        if (UndoStack.Count > 0 && string.Equals(UndoStack.Peek(), json, System.StringComparison.Ordinal))
        {
            return;
        }

        UndoStack.Push(json);
        RedoStack.Clear();
        TrimOldSnapshots();
    }

    public List<DraggableWidget>? Undo(IReadOnlyList<DraggableWidget> current)
    {
        if (!CanUndo)
        {
            return null;
        }

        var currentJson = JsonSerializer.Serialize(current, _jsonOptions);
        RedoStack.Push(currentJson);

        // Pop current snapshot
        _ = UndoStack.Pop();
        var previousJson = UndoStack.Peek();
        return Deserialize(previousJson);
    }

    public List<DraggableWidget>? Redo(IReadOnlyList<DraggableWidget> current)
    {
        if (!CanRedo)
        {
            return null;
        }

        var currentJson = JsonSerializer.Serialize(current, _jsonOptions);
        UndoStack.Push(currentJson);
        TrimOldSnapshots();

        var nextJson = RedoStack.Pop();
        return Deserialize(nextJson);
    }

    private List<DraggableWidget>? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<DraggableWidget>>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private void TrimOldSnapshots()
    {
        if (MaxSnapshots <= 0)
        {
            return;
        }

        if (UndoStack.Count <= MaxSnapshots)
        {
            return;
        }

        var snapshots = UndoStack.Reverse().ToList(); // bottom -> top
        var kept = snapshots.Skip(System.Math.Max(0, snapshots.Count - MaxSnapshots)).ToList();

        UndoStack.Clear();
        foreach (var item in kept)
        {
            UndoStack.Push(item);
        }
    }
}
