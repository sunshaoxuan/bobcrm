namespace BobCrm.Api.Core.DomainCommon.Validation;

public interface IBusinessValidator<T>
{
    IEnumerable<ValidationError> Validate(T model);
}
