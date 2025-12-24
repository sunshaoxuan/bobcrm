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
