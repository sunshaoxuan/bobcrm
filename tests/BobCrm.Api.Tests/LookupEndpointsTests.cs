using System.Net.Http.Json;
using System.Text.Json;

namespace BobCrm.Api.Tests;

public class LookupEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public LookupEndpointsTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Lookup_Resolve_Works_For_Customers()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var resp = await client.PostAsJsonAsync("/api/lookups/resolve", new
        {
            target = "customer",
            displayField = "Name",
            ids = new[] { "1" }
        });
        resp.EnsureSuccessStatusCode();

        var data = await resp.ReadDataAsJsonAsync();
        Assert.True(data.ValueKind == JsonValueKind.Object);
        Assert.True(data.TryGetProperty("1", out var name));
        Assert.False(string.IsNullOrWhiteSpace(name.GetString()));
    }
}

