using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Base;
using BobCrm.Api.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace BobCrm.Api.Application.Queries;

public class FieldQueries : IFieldQueries
{
    private readonly IRepository<FieldDefinition> _repo;
    private readonly ILocalization _loc;
    private readonly IHttpContextAccessor _http;
    public FieldQueries(IRepository<FieldDefinition> repo, ILocalization loc, IHttpContextAccessor http)
    { _repo = repo; _loc = loc; _http = http; }
    public List<object> GetDefinitions()
    {
        var defs = _repo.Query().ToList();
        var lang = LangHelper.GetLang(_http.HttpContext!);
        return defs.Select(f => new
        {
            key = f.Key,
            label = _loc.T(f.DisplayName, lang),
            type = f.DataType,
            required = f.Required,
            validation = f.Validation,
            defaultValue = f.DefaultValue,
            tags = string.IsNullOrWhiteSpace(f.Tags) ? new string[0] : System.Text.Json.JsonSerializer.Deserialize<string[]>(f.Tags!)!,
            actions = NormalizeActions(f.Actions, lang)
        }).Cast<object>().ToList();
    }

    private object[] NormalizeActions(string? json, string lang)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<object>();
        try
        {
            var arr = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(json!) ?? Array.Empty<System.Text.Json.JsonElement>();
            var list = new List<object>();
            foreach (var el in arr)
            {
                var icon = el.TryGetProperty("icon", out var i) ? i.GetString() : null;
                var titleKey = el.TryGetProperty("titleKey", out var t) ? t.GetString() : null;
                var title = string.IsNullOrWhiteSpace(titleKey) ? null : _loc.T(titleKey!, lang);
                var type = el.TryGetProperty("type", out var ty) ? ty.GetString() : null;
                var action = el.TryGetProperty("action", out var ac) ? ac.GetString() : null;
                list.Add(new { icon, title, titleKey, type, action });
            }
            return list.ToArray();
        }
        catch { return Array.Empty<object>(); }
    }
}
