using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BobCrm.App.Services;

public class InteractionState
{
    public record ToastMessage(Guid Id, string Title, string? Body, ToastTone Tone, DateTime CreatedAt);
    public enum ToastTone { Info, Success, Warning, Danger }

    public record StickyAction(string Label, InteractionActionTone Tone, Func<Task>? Callback);
    public enum InteractionActionTone { Primary, Default, Ghost }

    private readonly List<ToastMessage> _toasts = new();
    private readonly List<StickyAction> _stickyActions = new();

    public bool IsBulkBarVisible { get; private set; }
    public string BulkBarMessage { get; private set; } = string.Empty;
    public string BulkPrimaryLabel { get; private set; } = "执行";
    public string BulkSecondaryLabel { get; private set; } = "取消";
    public Func<Task>? BulkPrimaryAction { get; private set; }
    public Func<Task>? BulkSecondaryAction { get; private set; }

    public bool IsStickyBarVisible { get; private set; }
    public string StickyBarMessage { get; private set; } = string.Empty;

    public IReadOnlyList<ToastMessage> Toasts => _toasts;
    public IReadOnlyList<StickyAction> StickyActions => _stickyActions;

    public event Action? OnChanged;

    public void ShowBulkBar(string message, string primaryLabel, Func<Task> primary, string? secondaryLabel = null, Func<Task>? secondary = null)
    {
        IsBulkBarVisible = true;
        BulkBarMessage = message;
        BulkPrimaryLabel = primaryLabel;
        BulkPrimaryAction = primary;
        BulkSecondaryLabel = secondaryLabel ?? "取消";
        BulkSecondaryAction = secondary ?? (() => Task.CompletedTask);
        Notify();
    }

    public void HideBulkBar()
    {
        if (!IsBulkBarVisible) return;
        IsBulkBarVisible = false;
        BulkPrimaryAction = null;
        BulkSecondaryAction = null;
        Notify();
    }

    public void ShowStickyBar(string message, IEnumerable<StickyAction> actions)
    {
        IsStickyBarVisible = true;
        StickyBarMessage = message;
        _stickyActions.Clear();
        _stickyActions.AddRange(actions);
        Notify();
    }

    public void HideStickyBar()
    {
        if (!IsStickyBarVisible) return;
        IsStickyBarVisible = false;
        _stickyActions.Clear();
        Notify();
    }

    public Guid PushToast(string title, string? body = null, ToastTone tone = ToastTone.Info, int maxCount = 3)
    {
        var toast = new ToastMessage(Guid.NewGuid(), title, body, tone, DateTime.UtcNow);
        _toasts.Insert(0, toast);
        while (_toasts.Count > maxCount)
        {
            _toasts.RemoveAt(_toasts.Count - 1);
        }
        Notify();
        return toast.Id;
    }

    public void DismissToast(Guid id)
    {
        if (_toasts.RemoveAll(t => t.Id == id) > 0)
        {
            Notify();
        }
    }

    private void Notify() => OnChanged?.Invoke();
}
