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

    /// <summary>
    /// 显示成功消息
    /// </summary>
    public void Success(string message)
    {
        AddMessage(new ToastMessage
        {
            Id = Guid.NewGuid(),
            Message = message,
            Type = ToastType.Success,
            CreatedAt = DateTime.Now
        });
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    public void Error(string message)
    {
        AddMessage(new ToastMessage
        {
            Id = Guid.NewGuid(),
            Message = message,
            Type = ToastType.Error,
            CreatedAt = DateTime.Now
        });
    }

    /// <summary>
    /// 显示警告消息
    /// </summary>
    public void Warning(string message)
    {
        AddMessage(new ToastMessage
        {
            Id = Guid.NewGuid(),
            Message = message,
            Type = ToastType.Warning,
            CreatedAt = DateTime.Now
        });
    }

    /// <summary>
    /// 显示信息消息
    /// </summary>
    public void Info(string message)
    {
        AddMessage(new ToastMessage
        {
            Id = Guid.NewGuid(),
            Message = message,
            Type = ToastType.Info,
            CreatedAt = DateTime.Now
        });
    }

    /// <summary>
    /// 获取当前可见的消息（最多3个）
    /// </summary>
    public IReadOnlyList<ToastMessage> GetVisibleMessages()
    {
        return _messages.Take(_maxVisibleMessages).ToList();
    }

    /// <summary>
    /// 获取自动隐藏时长（毫秒）
    /// </summary>
    public int GetAutoHideDuration() => _autoHideDuration;

    /// <summary>
    /// 移除消息
    /// </summary>
    public void RemoveMessage(Guid id)
    {
        var message = _messages.FirstOrDefault(m => m.Id == id);
        if (message != null)
        {
            _messages.Remove(message);
            NotifyStateChanged();
        }
    }

    private void AddMessage(ToastMessage message)
    {
        _messages.Insert(0, message);
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

/// <summary>
/// Toast消息
/// </summary>
public class ToastMessage
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Toast消息类型
/// </summary>
public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}
