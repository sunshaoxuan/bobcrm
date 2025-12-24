namespace BobCrm.App.Services;

/// <summary>
/// RDP配置
/// </summary>
public class RdpConfig
{
    public string Host { get; set; } = string.Empty;
    public int? Port { get; set; } = 3389;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Domain { get; set; }
    public int? Width { get; set; } = 1920;
    public int? Height { get; set; } = 1080;
    public string? Gateway { get; set; }
    public bool? RedirectDrives { get; set; }
    public bool? RedirectClipboard { get; set; } = true;
    public bool? RedirectPrinters { get; set; }
    public bool? RedirectComPorts { get; set; }
    public bool? RedirectSmartCards { get; set; }
    public bool? RedirectAudio { get; set; } = true;
}
