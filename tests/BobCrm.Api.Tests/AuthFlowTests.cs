using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BobCrm.Api.Tests;

public class AuthFlowTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public AuthFlowTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_Activate_Login_Succeeds()
    {
        var client = _factory.CreateClient();
        var username = $"user_{Guid.NewGuid():N}";
        var email = $"{username}@local";
        var password = "User@12345";

        var reg = await client.PostAsJsonAsync("/api/auth/register", new { username, password, email });
        reg.EnsureSuccessStatusCode();

        // Generate activation code via DI and call activate endpoint
        using var scope = _factory.Services.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await um.FindByNameAsync(username);
        Assert.NotNull(user);
        var code = await um.GenerateEmailConfirmationTokenAsync(user!);
        var act = await client.GetAsync($"/api/auth/activate?userId={Uri.EscapeDataString(user!.Id)}&code={Uri.EscapeDataString(code)}");
        act.EnsureSuccessStatusCode();

        // login
        var login = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        login.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement;
        var token = json.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
    }
}

