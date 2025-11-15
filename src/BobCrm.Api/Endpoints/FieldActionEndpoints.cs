using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Base;
using BobCrm.Api.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BobCrm.Api.Endpoints;

public static class FieldActionEndpoints
{
    public static void MapFieldActionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/actions")
            .RequireAuthorization()
            .WithTags("FieldActions")
            .WithOpenApi();

        // RDP文件下载
        group.MapPost("/rdp/download", DownloadRdp)
            .WithName("DownloadRdp")
            .WithSummary("生成并下载RDP文件");

        // 文件路径验证
        group.MapPost("/file/validate", ValidateFilePath)
            .WithName("ValidateFilePath")
            .WithSummary("验证文件路径是否存在");

        // Mailto链接生成
        group.MapPost("/mailto/generate", GenerateMailtoLink)
            .WithName("GenerateMailtoLink")
            .WithSummary("生成mailto链接");
    }

    /// <summary>
    /// 生成并返回RDP文件
    /// </summary>
    private static IResult DownloadRdp([FromBody] RdpDownloadRequest request)
    {
        try
        {
            // 验证必填字段
            if (string.IsNullOrWhiteSpace(request.Host))
                return Results.BadRequest(new { code = "ERR_RDP_HOST_REQUIRED", message = "主机地址不能为空" });

            var rdpContent = GenerateRdpContent(request);
            var fileName = $"{FileNameHelper.SanitizeFileName(request.Host)}_{DateTime.Now:yyyyMMddHHmmss}.rdp";
            
            // 返回文件
            var bytes = Encoding.UTF8.GetBytes(rdpContent);
            return Results.File(bytes, "application/x-rdp", fileName);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { code = "ERR_RDP_GENERATE_FAILED", message = $"生成RDP文件失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 生成RDP文件内容
    /// </summary>
    private static string GenerateRdpContent(RdpDownloadRequest request)
    {
        var sb = new StringBuilder();
        
        // RDP 文件基础配置
        sb.AppendLine("screen mode id:i:2"); // 全屏模式
        sb.AppendLine($"use multimon:i:0");
        sb.AppendLine($"desktopwidth:i:{request.Width ?? 1920}");
        sb.AppendLine($"desktopheight:i:{request.Height ?? 1080}");
        sb.AppendLine("session bpp:i:32"); // 颜色深度
        sb.AppendLine("winposstr:s:0,3,0,0,800,600");
        sb.AppendLine("compression:i:1");
        sb.AppendLine("keyboardhook:i:2");
        sb.AppendLine("audiocapturemode:i:0");
        sb.AppendLine("videoplaybackmode:i:1");
        sb.AppendLine("connection type:i:7");
        sb.AppendLine("networkautodetect:i:1");
        sb.AppendLine("bandwidthautodetect:i:1");
        sb.AppendLine("displayconnectionbar:i:1");
        sb.AppendLine("enableworkspacereconnect:i:0");
        sb.AppendLine("disable wallpaper:i:0");
        sb.AppendLine("allow font smoothing:i:0");
        sb.AppendLine("allow desktop composition:i:0");
        sb.AppendLine("disable full window drag:i:1");
        sb.AppendLine("disable menu anims:i:1");
        sb.AppendLine("disable themes:i:0");
        sb.AppendLine("disable cursor setting:i:0");
        sb.AppendLine("bitmapcachepersistenable:i:1");
        
        // 主机和端口
        var host = request.Host;
        if (request.Port.HasValue && request.Port.Value != 3389)
            host = $"{host}:{request.Port}";
        sb.AppendLine($"full address:s:{host}");
        
        // 用户名
        if (!string.IsNullOrWhiteSpace(request.Username))
            sb.AppendLine($"username:s:{request.Username}");
        
        // 域（如果有）
        if (!string.IsNullOrWhiteSpace(request.Domain))
            sb.AppendLine($"domain:s:{request.Domain}");
        
        // 密码加密（可选）
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var encryptedPassword = EncryptRdpPassword(request.Password);
            sb.AppendLine($"password 51:b:{encryptedPassword}");
        }
        
        // 其他选项
        sb.AppendLine("authentication level:i:0"); // 不验证服务器身份
        sb.AppendLine("prompt for credentials:i:0");
        sb.AppendLine("negotiate security layer:i:1");
        
        // 驱动器重定向
        if (request.RedirectDrives ?? false)
        {
            sb.AppendLine("redirectdrives:i:1");
        }
        
        // 剪贴板重定向
        if (request.RedirectClipboard ?? true)
        {
            sb.AppendLine("redirectclipboard:i:1");
        }
        
        // 打印机重定向
        if (request.RedirectPrinters ?? false)
        {
            sb.AppendLine("redirectprinters:i:1");
        }
        
        // COM端口重定向
        if (request.RedirectComPorts ?? false)
        {
            sb.AppendLine("redirectcomports:i:1");
        }
        
        // 智能卡重定向
        if (request.RedirectSmartCards ?? false)
        {
            sb.AppendLine("redirectsmartcards:i:1");
        }
        
        // 音频重定向
        sb.AppendLine($"audiomode:i:{(request.RedirectAudio ?? true ? 0 : 2)}"); // 0=播放到本地, 2=不播放
        
        // 网关设置（如果有）
        if (!string.IsNullOrWhiteSpace(request.Gateway))
        {
            sb.AppendLine($"gatewayhostname:s:{request.Gateway}");
            sb.AppendLine("gatewayusagemethod:i:1");
            sb.AppendLine("gatewayprofileusagemethod:i:1");
            sb.AppendLine("gatewaycredentialssource:i:0");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// 加密RDP密码（使用Windows Data Protection API格式）
    /// 注意：这是简化版本，仅用于演示，实际生产环境建议不保存密码或使用更安全的方式
    /// </summary>
    private static string EncryptRdpPassword(string password)
    {
        try
        {
            // RDP密码需要使用特定的加密方式
            // 这里使用Base64编码作为简化实现
            // 实际生产环境应该使用Windows Credential Manager或不保存密码
            var bytes = Encoding.Unicode.GetBytes(password);
            return Convert.ToBase64String(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 验证文件路径
    /// </summary>
    private static IResult ValidateFilePath([FromBody] FileValidationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Path))
                return Results.BadRequest(new { code = "ERR_PATH_REQUIRED", message = "文件路径不能为空" });

            // 安全检查：只允许验证本地文件路径
            if (request.Path.StartsWith("http://") || request.Path.StartsWith("https://"))
                return Results.Ok(new { exists = true, type = "url", message = "URL路径" });

            // 检查路径格式
            if (!Uri.TryCreate(request.Path, UriKind.Absolute, out var uri) && 
                !Path.IsPathRooted(request.Path))
                return Results.BadRequest(new { code = "ERR_INVALID_PATH", message = "无效的文件路径格式" });

            // 检查文件是否存在
            var exists = File.Exists(request.Path);
            var isDirectory = Directory.Exists(request.Path);

            if (exists)
            {
                var fileInfo = new FileInfo(request.Path);
                return Results.Ok(new 
                { 
                    exists = true, 
                    type = "file",
                    size = fileInfo.Length,
                    extension = fileInfo.Extension,
                    lastModified = fileInfo.LastWriteTime
                });
            }
            else if (isDirectory)
            {
                return Results.Ok(new { exists = true, type = "directory" });
            }
            else
            {
                return Results.Ok(new { exists = false, type = "notfound", message = "文件或目录不存在" });
            }
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { code = "ERR_VALIDATION_FAILED", message = $"验证失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 生成mailto链接
    /// </summary>
    private static IResult GenerateMailtoLink([FromBody] MailtoRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return Results.BadRequest(new { code = "ERR_EMAIL_REQUIRED", message = "邮箱地址不能为空" });

            var mailto = $"mailto:{Uri.EscapeDataString(request.Email)}";
            var queryParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(request.Subject))
                queryParts.Add($"subject={Uri.EscapeDataString(request.Subject)}");

            if (!string.IsNullOrWhiteSpace(request.Body))
                queryParts.Add($"body={Uri.EscapeDataString(request.Body)}");

            if (!string.IsNullOrWhiteSpace(request.Cc))
                queryParts.Add($"cc={Uri.EscapeDataString(request.Cc)}");

            if (!string.IsNullOrWhiteSpace(request.Bcc))
                queryParts.Add($"bcc={Uri.EscapeDataString(request.Bcc)}");

            if (queryParts.Count > 0)
                mailto += "?" + string.Join("&", queryParts);

            return Results.Ok(new { link = mailto });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { code = "ERR_MAILTO_FAILED", message = $"生成mailto链接失败: {ex.Message}" });
        }
    }
}

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

/// <summary>
/// 文件验证请求
/// </summary>
public record FileValidationRequest
{
    public string Path { get; init; } = string.Empty;
}

/// <summary>
/// Mailto请求
/// </summary>
public record MailtoRequest
{
    public string Email { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public string? Cc { get; init; }
    public string? Bcc { get; init; }
}

