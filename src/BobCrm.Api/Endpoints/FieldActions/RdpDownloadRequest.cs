namespace BobCrm.Api.Endpoints;

/// <summary>
/// RDP下载请求
/// </summary>
public record RdpDownloadRequest
{
    public string Host { get; init; } = string.Empty;
    public int? Port { get; init; } = 3389;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? Domain { get; init; }
    public int? Width { get; init; } = 1920;
    public int? Height { get; init; } = 1080;
    public string? Gateway { get; init; }
    public bool? RedirectDrives { get; init; }
    public bool? RedirectClipboard { get; init; } = true;
    public bool? RedirectPrinters { get; init; }
    public bool? RedirectComPorts { get; init; }
    public bool? RedirectSmartCards { get; init; }
    public bool? RedirectAudio { get; init; } = true;
}

