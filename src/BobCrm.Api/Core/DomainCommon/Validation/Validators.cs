using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Domain;

namespace BobCrm.Api.Core.DomainCommon.Validation;

// Business validation for updating customer fields
public class UpdateCustomerBusinessValidator : IBusinessValidator<UpdateCustomerDto>
{
    public IEnumerable<ValidationError> Validate(UpdateCustomerDto model)
    {
        if (model.fields == null || model.fields.Count == 0)
            yield return new ValidationError("fields", "Required", "fields required");
        else
        {
            foreach (var f in model.fields)
            {
                if (string.IsNullOrWhiteSpace(f.key))
                    yield return new ValidationError("key", "Required", "field key required");
            }
        }
    }
}

public class UpdateCustomerPersistenceValidator : IPersistenceValidator<UpdateCustomerDto>
{
    private readonly IRepository<FieldDefinition> _repoDef;
    public UpdateCustomerPersistenceValidator(IRepository<FieldDefinition> repoDef) => _repoDef = repoDef;
    public Task<IEnumerable<ValidationError>> ValidateAsync(UpdateCustomerDto model, CancellationToken ct = default)
    {
        var errors = new List<ValidationError>();
        if (model.fields != null && model.fields.Count > 0)
        {
            var defs = _repoDef.Query().ToDictionary(d => d.Key, d => d);
            foreach (var f in model.fields)
            {
                if (!defs.ContainsKey(f.key))
                    errors.Add(new ValidationError("key", "UnknownField", $"unknown field: {f.key}"));
            }
        }
        return Task.FromResult<IEnumerable<ValidationError>>(errors);
    }
}

// Common validation: required and regex
public class UpdateCustomerCommonValidator : ICommonValidator<UpdateCustomerDto>
{
    private readonly IRepository<FieldDefinition> _repoDef;
    public UpdateCustomerCommonValidator(IRepository<FieldDefinition> repoDef) => _repoDef = repoDef;

    public IEnumerable<ValidationError> Validate(UpdateCustomerDto model)
    {
        var defs = _repoDef.Query().ToList();
        var byKey = (model.fields ?? new List<FieldDto>()).ToDictionary(x => x.key, x => x.value);
        foreach (var d in defs)
        {
            if (d.Required)
            {
                if (!byKey.TryGetValue(d.Key, out var val) || val is null || string.IsNullOrWhiteSpace(val.ToString()))
                    yield return new ValidationError(d.Key, "Required", $"{d.DisplayName} is required");
            }
            if (!string.IsNullOrWhiteSpace(d.Validation) && byKey.TryGetValue(d.Key, out var v) && v is not null)
            {
                var text = v.ToString() ?? string.Empty;
                bool patternError = false;
                bool match = true;
                try { match = System.Text.RegularExpressions.Regex.IsMatch(text, d.Validation!); }
                catch { patternError = true; }
                if (patternError)
                    yield return new ValidationError(d.Key, "InvalidPattern", $"invalid validation pattern for {d.Key}");
                else if (!match)
                    yield return new ValidationError(d.Key, "InvalidFormat", $"{d.DisplayName} format invalid");
            }
        }
    }
}
