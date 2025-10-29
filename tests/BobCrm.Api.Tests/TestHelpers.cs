using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BobCrm.Api.Tests;

public static class TestHelpers
{
    public static async Task<(string accessToken, string refreshToken)> LoginAsAdminAsync(this HttpClient client)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "Admin@12345" });
        res.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        var access = json.GetProperty("accessToken").GetString()!;
        var refresh = json.GetProperty("refreshToken").GetString()!;
        return (access, refresh);
    }

    public static void UseBearer(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

