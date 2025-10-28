using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Domain;

namespace BobCrm.Api.Application.Queries;

public interface ICustomerQueries
{
    List<object> GetList();
    object? GetDetail(int id);
}

public class CustomerQueries : ICustomerQueries
{
    private readonly IRepository<Customer> _repoCustomer;
    private readonly IRepository<FieldDefinition> _repoDef;
    private readonly IRepository<FieldValue> _repoVal;

    public CustomerQueries(IRepository<Customer> repoCustomer, IRepository<FieldDefinition> repoDef, IRepository<FieldValue> repoVal)
    {
        _repoCustomer = repoCustomer; _repoDef = repoDef; _repoVal = repoVal;
    }

    public List<object> GetList()
        => _repoCustomer.Query().Select(c => new { id = c.Id, code = c.Code, name = c.Name }).Cast<object>().ToList();

    public object? GetDetail(int id)
    {
        var c = _repoCustomer.Query(x => x.Id == id).FirstOrDefault();
        if (c == null) return null;
        var defs = _repoDef.Query().ToList();
        var values = _repoVal.Query(v => v.CustomerId == id).OrderByDescending(v => v.Version).ToList();
        var fields = defs.Select(d => new
        {
            key = d.Key,
            label = d.DisplayName,
            type = d.DataType,
            value =
                values.FirstOrDefault(v => v.FieldDefinitionId == d.Id)?.Value is string s && s.StartsWith("\"")
                    ? s.Trim('"')
                    : values.FirstOrDefault(v => v.FieldDefinitionId == d.Id)?.Value ?? d.DefaultValue,
            required = d.Required,
            validation = d.Validation
        }).ToArray();
        return new { id = c.Id, code = c.Code, name = c.Name, version = c.Version, fields };
    }
}
