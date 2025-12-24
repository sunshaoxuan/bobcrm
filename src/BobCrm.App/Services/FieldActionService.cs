using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using BobCrm.App.Utils;

namespace BobCrm.App.Services;

/// <summary>
/// 字段动作执行服务
/// </summary>
public class FieldActionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJSRuntime _js;
    private readonly AuthService _auth;
    private readonly ILogger<FieldActionService> _logger;

    public FieldActionService(IHttpClientFactory httpClientFactory, IJSRuntime js, AuthService auth, ILogger<FieldActionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _js = js;
        _auth = auth;
        _logger = logger;
    }

    /// <summary>
    /// 执行字段动作
    /// </summary>
    public async Task<bool> ExecuteActionAsync(string actionType, string? fieldValue, Dictionary<string, object>? parameters = null)
    {
        try
        {
            return actionType switch
            {
                "downloadRdp" => await DownloadRdpAsync(fieldValue, parameters),
                "openLink" => await OpenLinkAsync(fieldValue),
                "copy" => await CopyToClipboardAsync(fieldValue),
                "mailto" => await OpenMailtoAsync(fieldValue, parameters),
                _ => throw new NotSupportedException($"不支持的动作类型: {actionType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FieldActionService] 执行动作失败 ({ActionType})", actionType);
            return false;
        }
    }

    /// <summary>
    /// 下载RDP文件
    /// </summary>
    private async Task<bool> DownloadRdpAsync(string? fieldValue, Dictionary<string, object>? parameters)
    {
        if (string.IsNullOrWhiteSpace(fieldValue))
            return false;

        try
        {
            // 解析RDS字段值（可能是JSON格式）
            RdpConfig config;
            try
            {
                config = JsonSerializer.Deserialize<RdpConfig>(fieldValue) ?? new RdpConfig();
            }
            catch
            {
                // 如果不是JSON，假设是简单的主机名
                config = new RdpConfig { Host = fieldValue };
            }

            // 合并参数
            if (parameters != null)
            {
                if (parameters.TryGetValue("username", out var username))
                    config.Username = username?.ToString();
                if (parameters.TryGetValue("domain", out var domain))
                    config.Domain = domain?.ToString();
            }

            // 调用后端API生成RDP文件（使用已认证的客户端）
            var client = await _auth.CreateClientWithAuthAsync();

            var response = await client.PostAsJsonAsync("/api/actions/rdp/download", new
            {
                host = config.Host,
                port = config.Port ?? 3389,
                username = config.Username,
                password = config.Password,
                domain = config.Domain,
                width = config.Width ?? 1920,
                height = config.Height ?? 1080,
                gateway = config.Gateway,
                redirectDrives = config.RedirectDrives,
                redirectClipboard = config.RedirectClipboard ?? true,
                redirectPrinters = config.RedirectPrinters,
                redirectComPorts = config.RedirectComPorts,
                redirectSmartCards = config.RedirectSmartCards,
                redirectAudio = config.RedirectAudio ?? true
            });

            if (response.IsSuccessStatusCode)
            {
                // 获取文件内容
                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') 
                    ?? $"{FileNameHelper.SanitizeFileName(config.Host)}_{DateTime.UtcNow:yyyyMMddHHmmss}.rdp";

                // 使用JavaScript触发下载
                await _js.InvokeVoidAsync("downloadFile", fileName, "application/x-rdp", fileBytes);
                return true;
            }
            else
            {
                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("[FieldActionService] RDP下载失败: {StatusCode}, {ErrorText}", response.StatusCode, errorText);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FieldActionService] RDP下载异常");
            return false;
        }
    }

    /// <summary>
    /// 打开链接
    /// </summary>
    private async Task<bool> OpenLinkAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            // 确保URL有协议
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                url = "https://" + url;

            await _js.InvokeVoidAsync("open", url, "_blank");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FieldActionService] 打开链接失败");
            return false;
        }
    }

    /// <summary>
    /// 复制到剪贴板
    /// </summary>
    private async Task<bool> CopyToClipboardAsync(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        try
        {
            await _js.InvokeVoidAsync("navigator.clipboard.writeText", text);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FieldActionService] 复制失败");
            return false;
        }
    }

    /// <summary>
    /// 打开邮件客户端
    /// </summary>
    private async Task<bool> OpenMailtoAsync(string? email, Dictionary<string, object>? parameters)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // 构建mailto链接（使用已认证的客户端）
            var client = await _auth.CreateClientWithAuthAsync();

            var requestBody = new Dictionary<string, string> { ["email"] = email };
            
            if (parameters != null)
            {
                if (parameters.TryGetValue("subject", out var subject))
                    requestBody["subject"] = subject?.ToString() ?? string.Empty;
                if (parameters.TryGetValue("body", out var body))
                    requestBody["body"] = body?.ToString() ?? string.Empty;
                if (parameters.TryGetValue("cc", out var cc))
                    requestBody["cc"] = cc?.ToString() ?? string.Empty;
                if (parameters.TryGetValue("bcc", out var bcc))
                    requestBody["bcc"] = bcc?.ToString() ?? string.Empty;
            }

            var response = await client.PostAsJsonAsync("/api/actions/mailto/generate", requestBody);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (result.TryGetProperty("link", out var linkElement))
                {
                    var mailtoLink = linkElement.GetString();
                    if (!string.IsNullOrEmpty(mailtoLink))
                    {
                        await _js.InvokeVoidAsync("location.assign", mailtoLink);
                        return true;
                    }
                }
            }

            // 如果API失败，使用简单的mailto链接
            await _js.InvokeVoidAsync("location.assign", $"mailto:{email}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FieldActionService] 打开邮件失败");
            return false;
        }
    }

}

