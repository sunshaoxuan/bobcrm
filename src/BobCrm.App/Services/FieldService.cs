using System.Net.Http.Json;

namespace BobCrm.App.Services;

public class FieldService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AuthService _auth;
    public FieldService(IHttpClientFactory httpFactory, AuthService auth)
    { _httpFactory = httpFactory; _auth = auth; }

    public async Task<List<FieldDefinitionDto>> GetDefinitionsAsync(CancellationToken ct = default)
    {
        var resp = await _auth.GetWithRefreshAsync("/api/fields");
        resp.EnsureSuccessStatusCode();
        var defs = await resp.Content.ReadFromJsonAsync<List<FieldDefinitionDto>>(cancellationToken: ct);
        return defs ?? new List<FieldDefinitionDto>();
    }

    public async Task<List<TagInfo>> GetTagsAsync(CancellationToken ct = default)
    {
        var resp = await _auth.GetWithRefreshAsync("/api/fields/tags");
        resp.EnsureSuccessStatusCode();
        var tags = await resp.Content.ReadFromJsonAsync<List<TagInfo>>(cancellationToken: ct);
        return tags ?? new List<TagInfo>();
    }

    public async Task<string> GenerateLayoutAsync(int customerId, IEnumerable<string> tags, string mode = "flow", bool save = false, string scope = "user", CancellationToken ct = default)
    {
        var http = await _auth.CreateClientWithAuthAsync();
        var body = new { tags = tags.ToArray(), mode, save, scope };
        var resp = await http.PostAsJsonAsync($"/api/layout/{customerId}/generate", body, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        return json;
    }

    public class FieldDefinitionDto
    {
        public string key { get; set; } = string.Empty;
        public string label { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public bool required { get; set; }
        public string? validation { get; set; }
        public string? defaultValue { get; set; }
        public string[] tags { get; set; } = Array.Empty<string>();
        public ActionDef[] actions { get; set; } = Array.Empty<ActionDef>();
    }

    public class ActionDef
    {
        public string? icon { get; set; }
        public string? title { get; set; }
        public string? type { get; set; }
        public string? action { get; set; }
    }

    public class TagInfo
    {
        public string tag { get; set; } = string.Empty;
        public int count { get; set; }
    }
}
