using System.Net;
using System.Net.Http.Json;
using BobCrm.Api.Endpoints;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BobCrm.Api.Tests;

public class FieldActionEndpointsFinalSprintTests
{
    private static WebApplicationFactory<Program> CreateTestAuthFactory()
    {
        return new TestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(new TestAuthState("user", "user"));

                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, LayoutTestAuthHandler>("Test", _ => { });

                services.PostConfigureAll<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultScheme = "Test";
                });
            });
        });
    }

    [Fact]
    public async Task FieldActions_WithoutAuth_ShouldReturn401()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/actions/file/validate", new FileValidationRequest { Path = "x" });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DownloadRdp_WhenHostMissing_ShouldReturn400()
    {
        using var factory = CreateTestAuthFactory();
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/actions/rdp/download", new RdpDownloadRequest { Host = "" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DownloadRdp_WhenHostProvided_ShouldReturnFile()
    {
        using var factory = CreateTestAuthFactory();
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/actions/rdp/download", new RdpDownloadRequest
        {
            Host = "example",
            Port = 3390,
            Username = "u",
            Domain = "d",
            Password = "p",
            RedirectDrives = true
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/x-rdp");
    }

    [Fact]
    public async Task ValidateFilePath_ShouldHandleBadInputAndExistingFiles()
    {
        using var factory = CreateTestAuthFactory();
        var client = factory.CreateClient();

        (await client.PostAsJsonAsync("/api/actions/file/validate", new FileValidationRequest { Path = "" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var url = await client.PostAsJsonAsync("/api/actions/file/validate", new FileValidationRequest { Path = "https://example.com/a" });
        url.StatusCode.Should().Be(HttpStatusCode.OK);
        (await url.ReadDataAsJsonAsync()).GetProperty("type").GetString().Should().Be("url");

        (await client.PostAsJsonAsync("/api/actions/file/validate", new FileValidationRequest { Path = "relative\\path" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var tempFile = Path.GetTempFileName();
        try
        {
            var file = await client.PostAsJsonAsync("/api/actions/file/validate", new FileValidationRequest { Path = tempFile });
            file.StatusCode.Should().Be(HttpStatusCode.OK);
            (await file.ReadDataAsJsonAsync()).GetProperty("type").GetString().Should().Be("file");

            var dir = await client.PostAsJsonAsync("/api/actions/file/validate", new FileValidationRequest { Path = Path.GetTempPath() });
            dir.StatusCode.Should().Be(HttpStatusCode.OK);
            (await dir.ReadDataAsJsonAsync()).GetProperty("type").GetString().Should().Be("directory");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    [Fact]
    public async Task GenerateMailtoLink_ShouldValidateAndBuildQuery()
    {
        using var factory = CreateTestAuthFactory();
        var client = factory.CreateClient();

        (await client.PostAsJsonAsync("/api/actions/mailto/generate", new MailtoRequest { Email = "" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var resp = await client.PostAsJsonAsync("/api/actions/mailto/generate", new MailtoRequest
        {
            Email = "a@b.com",
            Subject = "S",
            Body = "B",
            Cc = "c@d.com",
            Bcc = "e@f.com"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resp.ReadDataAsJsonAsync()).GetProperty("link").GetString().Should().Contain("mailto:");
    }
}

