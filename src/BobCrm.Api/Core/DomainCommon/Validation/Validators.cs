using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Base;
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Core.DomainCommon.Validation;

// Business validation for updating customer fields
public class UpdateCustomerBusinessValidator : IBusinessValidator<UpdateCustomerDto>
{
    public IEnumerable<ValidationError> Validate(UpdateCustomerDto model)
    {
        // 允许仅更新基础属性（code/name），此时 fields 可为空
        var hasBasicChange = !(string.IsNullOrWhiteSpace(model.Code) && string.IsNullOrWhiteSpace(model.Name));
        if ((model.Fields == null || model.Fields.Count == 0))
        {
            if (!hasBasicChange)
            {
                // 既无字段也无基础属性修改 -> 无事可做
                yield return new ValidationError("fields", "FieldsRequired", "");
            }
            yield break;
        }

        foreach (var f in model.Fields)
        {
            if (string.IsNullOrWhiteSpace(f.Key))
                yield return new ValidationError("key", "Required", "");
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
        if (model.Fields != null && model.Fields.Count > 0)
        {
            var defs = _repoDef.Query().ToDictionary(d => d.Key, d => d);
            foreach (var f in model.Fields)
            {
                if (!defs.ContainsKey(f.Key))
                    errors.Add(new ValidationError("key", "UnknownField", f.Key));
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
        var byKey = (model.Fields ?? new List<FieldDto>()).ToDictionary(x => x.Key, x => x.Value);
        foreach (var d in defs)
        {
            if (d.Required)
            {
                if (!byKey.TryGetValue(d.Key, out var val) || val is null || string.IsNullOrWhiteSpace(val.ToString()))
                    yield return new ValidationError(d.Key, "Required", "");
            }
            if (!string.IsNullOrWhiteSpace(d.Validation) && byKey.TryGetValue(d.Key, out var v) && v is not null)
            {
                var text = v.ToString() ?? string.Empty;
                bool patternError = false;
                bool match = true;
                try { match = System.Text.RegularExpressions.Regex.IsMatch(text, d.Validation!); }
                catch { patternError = true; }
                if (patternError)
                    yield return new ValidationError(d.Key, "InvalidPattern", "");
                else if (!match)
                    yield return new ValidationError(d.Key, "InvalidFormat", "");
            }
        }
    }
}
