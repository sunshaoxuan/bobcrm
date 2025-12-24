using BobCrm.Api.Base;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Core.Persistence;

namespace BobCrm.Api.Core.DomainCommon.Validation;

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
