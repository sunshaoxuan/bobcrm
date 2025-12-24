using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BobCrm.Api.Tests;

/// <summary>
/// I18n 缓存、ETag 和版本控制测试
/// </summary>
public class I18nCacheTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public I18nCacheTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task I18n_ETag_Returns_304_When_Not_Modified()
    {
        var client = _factory.CreateClient();

        // 首次请求 - 应该返回 200 和 ETag
        var firstResponse = await client.GetAsync("/api/i18n/ja");
        firstResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.True(firstResponse.Headers.TryGetValues("ETag", out var etagValues));
        var etag = etagValues!.First();
        Assert.False(string.IsNullOrEmpty(etag));

        // 验证返回内容是字典
        var content = await firstResponse.Content.ReadAsStringAsync();
        var dict = JsonDocument.Parse(content).RootElement;
        Assert.Equal(JsonValueKind.Object, dict.ValueKind);

        // 第二次请求 - 携带 If-None-Match，应该返回 304
        var secondRequest = new HttpRequestMessage(HttpMethod.Get, "/api/i18n/ja");
        secondRequest.Headers.Add("If-None-Match", etag);
        var secondResponse = await client.SendAsync(secondRequest);
        
        Assert.Equal(HttpStatusCode.NotModified, secondResponse.StatusCode);
        var secondContent = await secondResponse.Content.ReadAsStringAsync();
        Assert.True(string.IsNullOrEmpty(secondContent), "304 响应应该没有内容");
    }

    [Fact]
    public async Task I18n_Version_Returns_Consistent_Value()
    {
        var client = _factory.CreateClient();

        // 多次请求版本端点，应该返回相同的版本号
        var v1Resp = await client.GetAsync("/api/i18n/version");
        v1Resp.EnsureSuccessStatusCode();
        var v1 = await v1Resp.ReadDataAsJsonAsync();
        var version1 = v1.GetProperty("version").GetInt64();

        await Task.Delay(100); // 短暂延迟

        var v2Resp = await client.GetAsync("/api/i18n/version");
        v2Resp.EnsureSuccessStatusCode();
        var v2 = await v2Resp.ReadDataAsJsonAsync();
        var version2 = v2.GetProperty("version").GetInt64();

        Assert.Equal(version1, version2);
    }

    [Fact]
    public async Task I18n_CacheControl_Header_Present()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/i18n/ja");
        response.EnsureSuccessStatusCode();

        // 验证 Cache-Control 头存在
        Assert.True(response.Headers.TryGetValues("Cache-Control", out var cacheControlValues));
        var cacheControl = cacheControlValues!.First();
        Assert.Contains("public", cacheControl);
        Assert.Contains("max-age", cacheControl);
    }

    [Fact]
    public async Task I18n_Different_Languages_Have_Different_ETags()
    {
        var client = _factory.CreateClient();

        // 请求日语
        var jaResponse = await client.GetAsync("/api/i18n/ja");
        jaResponse.EnsureSuccessStatusCode();
        Assert.True(jaResponse.Headers.TryGetValues("ETag", out var jaEtagValues));
        var jaEtag = jaEtagValues!.First();

        // 请求中文
        var zhResponse = await client.GetAsync("/api/i18n/zh");
        zhResponse.EnsureSuccessStatusCode();
        Assert.True(zhResponse.Headers.TryGetValues("ETag", out var zhEtagValues));
        var zhEtag = zhEtagValues!.First();

        // ETag 应该不同（因为包含语言标识）
        Assert.NotEqual(jaEtag, zhEtag);
    }

    [Fact]
    public async Task I18n_Resources_Endpoint_Requires_Auth()
    {
        var client = _factory.CreateClient();

        // 未认证访问 resources 端点应该返回 401
        var response = await client.GetAsync("/api/i18n/resources");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task I18n_Resources_With_Auth_Returns_ETag()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        // 认证后访问应该返回 200 和 ETag
        var response = await client.GetAsync("/api/i18n/resources");
        response.EnsureSuccessStatusCode();
        
        Assert.True(response.Headers.TryGetValues("ETag", out var etagValues));
        var etag = etagValues!.First();
        Assert.False(string.IsNullOrEmpty(etag));

        // 验证返回的是数组
        var resources = await response.ReadDataAsJsonAsync();
        Assert.Equal(JsonValueKind.Array, resources.ValueKind);
    }
}

