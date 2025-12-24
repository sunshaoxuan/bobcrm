namespace BobCrm.App.Services;

public class ToastMessage
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; } = ToastType.Info;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
