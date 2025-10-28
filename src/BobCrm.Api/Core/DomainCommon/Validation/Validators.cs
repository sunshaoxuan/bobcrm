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
