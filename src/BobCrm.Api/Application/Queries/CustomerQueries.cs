using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Base;
using System.Security.Claims;
using BobCrm.Api.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace BobCrm.Api.Application.Queries;

public interface ICustomerQueries
{
    List<object> GetList();
    object? GetDetail(int id);
}

public class CustomerQueries : ICustomerQueries
{
    private readonly IRepository<Customer> _repoCustomer;
    private readonly IRepository<CustomerLocalization> _repoLocalization;
    private readonly IRepository<FieldDefinition> _repoDef;
    private readonly IRepository<FieldValue> _repoVal;
    private readonly IRepository<CustomerAccess> _repoAccess;
    private readonly IHttpContextAccessor _http;
    private readonly ILocalization _loc;

    public CustomerQueries(IRepository<Customer> repoCustomer, IRepository<CustomerLocalization> repoLocalization, IRepository<FieldDefinition> repoDef, IRepository<FieldValue> repoVal, IRepository<CustomerAccess> repoAccess, IHttpContextAccessor http, ILocalization loc)
    {
        _repoCustomer = repoCustomer; _repoLocalization = repoLocalization; _repoDef = repoDef; _repoVal = repoVal; _repoAccess = repoAccess; _http = http; _loc = loc;
    }

    public List<object> GetList()
    {
        var uid = _http.HttpContext?.User?.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? string.Empty;
        var accessIds = _repoAccess.Query(a => a.UserId == uid).Select(a => a.CustomerId).ToList();
        var hasAccessRows = accessIds.Count > 0;
        var q = hasAccessRows ? _repoCustomer.Query(c => accessIds.Contains(c.Id)) : _repoCustomer.Query();
        
        // Get current language for localization
        var lang = LangHelper.GetLang(_http.HttpContext!);
        
        // Get localized names for all customers
        var customers = q.ToList();
        Dictionary<int, string?> localizedNames = new();
        
        // Try to get localized names if table exists
        try
        {
            localizedNames = _repoLocalization.Query()
                .Where(l => l.Language == lang)
                .ToDictionary(l => l.CustomerId, l => l.Name);
        }
        catch
        {
            // Table doesn't exist yet, use default names
        }
        
        return customers.Select(c => new 
        { 
            id = c.Id, 
            code = c.Code, 
            // Use localized name if available, otherwise fall back to default name
            name = localizedNames.ContainsKey(c.Id) && !string.IsNullOrEmpty(localizedNames[c.Id])
                ? localizedNames[c.Id]
                : c.Name 
        }).Cast<object>().ToList();
    }

    public object? GetDetail(int id)
    {
        var uid = _http.HttpContext?.User?.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? string.Empty;
        var hasSpecificAccess = _repoAccess.Query(a => a.CustomerId == id).Any();
        if (hasSpecificAccess)
        {
            var canView = _repoAccess.Query(a => a.CustomerId == id && a.UserId == uid).Any();
            if (!canView) return null;
        }
        var c = _repoCustomer.Query(x => x.Id == id).FirstOrDefault();
        if (c == null) return null;
        var defs = _repoDef.Query().ToList();
        var lang = LangHelper.GetLang(_http.HttpContext!);
        var values = _repoVal.Query(v => v.CustomerId == id).OrderByDescending(v => v.Version).ToList();
        
        // Get localized name
        string? displayName = c.Name;
        try
        {
            var localizedName = _repoLocalization.Query()
                .FirstOrDefault(l => l.CustomerId == id && l.Language == lang);
            if (localizedName != null && !string.IsNullOrEmpty(localizedName.Name))
            {
                displayName = localizedName.Name;
            }
        }
        catch
        {
            // Table doesn't exist yet, use default name
        }
        
        var fields = defs.Select(d => new
        {
            key = d.Key,
            label = _loc.T(d.DisplayName, lang),
            type = d.DataType,
            value =
                values.FirstOrDefault(v => v.FieldDefinitionId == d.Id)?.Value is string s && s.StartsWith("\"")
                    ? s.Trim('"')
                    : values.FirstOrDefault(v => v.FieldDefinitionId == d.Id)?.Value ?? d.DefaultValue,
            required = d.Required,
            validation = d.Validation
        }).ToArray();
        return new { id = c.Id, code = c.Code, name = displayName, version = c.Version, fields };
    }
}
