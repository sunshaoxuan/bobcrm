using BobCrm.Api.Base;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Core.Persistence;

namespace BobCrm.Api.Core.DomainCommon.Validation;

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
