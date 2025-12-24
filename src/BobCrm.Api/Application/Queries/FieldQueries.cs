using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Base;
using BobCrm.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using BobCrm.Api.Contracts.Responses.Layout;

namespace BobCrm.Api.Application.Queries;

public class FieldQueries : IFieldQueries
{
    private readonly IRepository<FieldDefinition> _repo;
    private readonly ILocalization _loc;
    private readonly IHttpContextAccessor _http;
    public FieldQueries(IRepository<FieldDefinition> repo, ILocalization loc, IHttpContextAccessor http)
    { _repo = repo; _loc = loc; _http = http; }
    public List<FieldDefinitionResponseDto> GetDefinitions()
    {
        var defs = _repo.Query().ToList();
        var lang = LangHelper.GetLang(_http.HttpContext!);
        return defs.Select(f => new FieldDefinitionResponseDto
        {
            Key = f.Key,
            Label = _loc.T(f.DisplayName, lang),
            Type = f.DataType,
            Required = f.Required,
            Validation = f.Validation,
            DefaultValue = f.DefaultValue,
            Tags = string.IsNullOrWhiteSpace(f.Tags)
                ? new List<string>()
                : (System.Text.Json.JsonSerializer.Deserialize<string[]>(f.Tags!) ?? Array.Empty<string>()).ToList(),
            Actions = NormalizeActions(f.Actions, lang)
        }).ToList();
    }

    private List<FieldActionResponseDto> NormalizeActions(string? json, string lang)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<FieldActionResponseDto>();
        try
        {
            var arr = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(json!) ?? Array.Empty<System.Text.Json.JsonElement>();
            var list = new List<FieldActionResponseDto>();
            foreach (var el in arr)
            {
                var icon = el.TryGetProperty("icon", out var i) ? i.GetString() : null;
                var titleKey = el.TryGetProperty("titleKey", out var t) ? t.GetString() : null;
                var title = string.IsNullOrWhiteSpace(titleKey) ? null : _loc.T(titleKey!, lang);
                var type = el.TryGetProperty("type", out var ty) ? ty.GetString() : null;
                var action = el.TryGetProperty("action", out var ac) ? ac.GetString() : null;
                list.Add(new FieldActionResponseDto
                {
                    Icon = icon,
                    Title = title,
                    TitleKey = titleKey,
                    Type = type,
                    Action = action
                });
            }
            return list;
        }
        catch
        {
            return new List<FieldActionResponseDto>();
        }
    }
}
