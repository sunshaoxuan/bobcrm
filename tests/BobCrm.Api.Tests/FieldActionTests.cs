using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BobCrm.Api.Tests;

/// <summary>
/// 字段动作集成测试
/// </summary>
public class FieldActionTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public FieldActionTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task RdpDownload_WithValidHost_ReturnsRdpFile()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var request = new
        {
            host = "192.168.1.100",
            port = 3389,
            username = "administrator",
            width = 1920,
            height = 1080
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/actions/rdp/download", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/x-rdp", response.Content.Headers.ContentType?.MediaType);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("full address:s:192.168.1.100", content);
        Assert.Contains("username:s:administrator", content);
        Assert.Contains("desktopwidth:i:1920", content);
        Assert.Contains("desktopheight:i:1080", content);
    }

    [Fact]
    public async Task RdpDownload_WithCustomPort_IncludesPortInAddress()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var request = new
        {
            host = "remote.server.com",
            port = 13389
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/actions/rdp/download", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("full address:s:remote.server.com:13389", content);
    }

    [Fact]
    public async Task RdpDownload_WithoutHost_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var request = new
        {
            host = "",
            port = 3389
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/actions/rdp/download", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(error.TryGetProperty("code", out var code));
        Assert.Equal("ERR_RDP_HOST_REQUIRED", code.GetString());
    }

    [Fact]
    public async Task RdpDownload_WithDomain_IncludesDomainSetting()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var request = new
        {
            host = "192.168.1.100",
            username = "admin",
            domain = "MYDOMAIN"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/actions/rdp/download", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("domain:s:MYDOMAIN", content);
    }

    [Fact]
    public async Task RdpDownload_WithRedirectOptions_IncludesRedirectSettings()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var request = new
        {
            host = "192.168.1.100",
            redirectDrives = true,
            redirectClipboard = true,
            redirectPrinters = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/actions/rdp/download", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("redirectdrives:i:1", content);
        Assert.Contains("redirectclipboard:i:1", content);
        Assert.Contains("redirectprinters:i:1", content);
    }

    [Fact]
    public async Task FileValidate_WithExistingFile_ReturnsExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        // 使用一个肯定存在的文件（当前测试DLL）
        var testFilePath = typeof(FieldActionTests).Assembly.Location;
        
        var request = new { path = testFilePath };

        // Act
        var response = await client.PostAsJsonAsync("/api/actions/file/validate", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("exists", out var exists));
        Assert.True(exists.GetBoolean());
        Assert.True(result.TryGetProperty("type", out var type));
        Assert.Equal("file", type.GetString());
    }

    [Fact]
    public async Task FileValidate_WithNonExistingFile_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var request = new { path = "C:\\NonExisting\\File\\Path\\test.txt" };

        // Act
        var response = await client.PostAsJsonAsync("/api/actions/file/validate", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("exists", out var exists));
        Assert.False(exists.GetBoolean());
    }

    [Fact]
    public async Task FileValidate_WithUrl_ReturnsUrlType()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var request = new { path = "https://example.com/file.txt" };

        // Act
        var response = await client.PostAsJsonAsync("/api/actions/file/validate", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("type", out var type));
        Assert.Equal("url", type.GetString());
    }

    [Fact]
    public async Task MailtoGenerate_WithEmail_ReturnsMailtoLink()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var request = new { email = "test@example.com" };

        // Act
        var response = await client.PostAsJsonAsync("/api/actions/mailto/generate", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("link", out var link));
        var linkStr = link.GetString();
        Assert.NotNull(linkStr);
        // 邮箱地址会被URL编码（@变成%40）
        Assert.True(linkStr.StartsWith("mailto:test") && linkStr.Contains("example.com"));
    }

    [Fact]
    public async Task MailtoGenerate_WithSubjectAndBody_IncludesQueryParams()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var request = new 
        { 
            email = "test@example.com",
            subject = "Test Subject",
            body = "Test Body"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/actions/mailto/generate", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("link", out var link));
        var linkStr = link.GetString();
        Assert.NotNull(linkStr);
        Assert.Contains("subject=", linkStr);
        Assert.Contains("body=", linkStr);
    }

    [Fact]
    public async Task MailtoGenerate_WithoutEmail_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var request = new { email = "" };

        // Act
        var response = await client.PostAsJsonAsync("/api/actions/mailto/generate", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(error.TryGetProperty("code", out var code));
        Assert.Equal("ERR_EMAIL_REQUIRED", code.GetString());
    }

    [Fact]
    public async Task FieldActions_RequireAuthentication()
    {
        // Arrange
        var client = _factory.CreateClient();
        // 不登录

        // Act & Assert - RDP下载需要认证
        var rdpResponse = await client.PostAsJsonAsync("/api/actions/rdp/download", new { host = "test" });
        Assert.Equal(HttpStatusCode.Unauthorized, rdpResponse.StatusCode);

        // Act & Assert - 文件验证需要认证
        var fileResponse = await client.PostAsJsonAsync("/api/actions/file/validate", new { path = "test" });
        Assert.Equal(HttpStatusCode.Unauthorized, fileResponse.StatusCode);

        // Act & Assert - Mailto生成需要认证
        var mailtoResponse = await client.PostAsJsonAsync("/api/actions/mailto/generate", new { email = "test@test.com" });
        Assert.Equal(HttpStatusCode.Unauthorized, mailtoResponse.StatusCode);
    }
}

