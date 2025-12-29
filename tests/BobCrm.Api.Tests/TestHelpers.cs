using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BobCrm.Api.Infrastructure;

namespace BobCrm.Api.Tests;

public static class TestHelpers
{
    public static AppDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        
        return new AppDbContext(options);
    }
    public static async Task<JsonElement> ReadAsJsonAsync(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.Clone();
    }

    public static JsonElement UnwrapData(this JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("data", out var data)) return data;
            if (root.TryGetProperty("Data", out var dataPascal)) return dataPascal;
        }
        return root;
    }

    public static async Task<JsonElement> ReadDataAsJsonAsync(this HttpResponseMessage response)
        => (await response.ReadAsJsonAsync()).UnwrapData();

    public static async Task<T?> ReadDataAsync<T>(this HttpResponseMessage response, JsonSerializerOptions? options = null)
    {
        var data = await response.ReadDataAsJsonAsync();
        return JsonSerializer.Deserialize<T>(data.GetRawText(), options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public static async Task<(string accessToken, string refreshToken)> LoginAsAdminAsync(this HttpClient client)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "Admin@12345" });
        if (!res.IsSuccessStatusCode)
        {
            var errorContent = await res.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Admin login failed with status {res.StatusCode}: {errorContent}");
        }
        var json = (await res.ReadAsJsonAsync()).UnwrapData();
        var access = json.GetProperty("accessToken").GetString()!;
        var refresh = json.GetProperty("refreshToken").GetString()!;
        return (access, refresh);
    }

    public static void UseBearer(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<(string userId, string username, string accessToken)> CreateAndLoginUserAsync(
        this HttpClient client, 
        IServiceProvider services)
    {
        var username = $"user_{Guid.NewGuid():N}";
        var email = $"{username}@local";
        var password = "User@12345";
        
        var reg = await client.PostAsJsonAsync("/api/auth/register", new { username, password, email });
        reg.EnsureSuccessStatusCode();
        
        using (var scope = services.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var u = await um.FindByNameAsync(username);
            var code = await um.GenerateEmailConfirmationTokenAsync(u!);
            var act = await client.GetAsync($"/api/auth/activate?userId={Uri.EscapeDataString(u!.Id)}&code={Uri.EscapeDataString(code)}");
            act.EnsureSuccessStatusCode();
            
            var login = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
            login.EnsureSuccessStatusCode();
            
            var json = (await login.ReadAsJsonAsync()).UnwrapData();
            var access = json.GetProperty("accessToken").GetString()!;
            return (u!.Id, username, access);
        }
    }
}

