namespace BobCrm.App.Services;

/// <summary>
/// 全局Toast通知服务
/// </summary>
public class ToastService
{
    private readonly List<ToastMessage> _messages = new();
    private readonly int _maxVisibleMessages = 3;
    private readonly int _autoHideDuration = 3000; // 3秒

    public event Action? OnChange;

    public void Success(string message) => AddMessage(message, ToastType.Success);
    public void Error(string message) => AddMessage(message, ToastType.Error);
    public void Warning(string message) => AddMessage(message, ToastType.Warning);
    public void Info(string message) => AddMessage(message, ToastType.Info);

    public IReadOnlyList<ToastMessage> GetVisibleMessages()
        => _messages.Take(_maxVisibleMessages).ToList();

    public int GetAutoHideDuration() => _autoHideDuration;

    public void RemoveMessage(Guid id)
    {
        var message = _messages.FirstOrDefault(m => m.Id == id);
        if (message != null)
        {
            _messages.Remove(message);
            NotifyStateChanged();
        }
    }

    private void AddMessage(string message, ToastType type)
    {
        _messages.Insert(0, new ToastMessage
        {
            Id = Guid.NewGuid(),
            Message = message,
            Type = type,
            CreatedAt = DateTime.UtcNow
        });

        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

