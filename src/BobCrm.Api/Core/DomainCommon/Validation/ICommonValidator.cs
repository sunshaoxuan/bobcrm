namespace BobCrm.Api.Core.DomainCommon.Validation;

public interface ICommonValidator<T>
{
    IEnumerable<ValidationError> Validate(T model);
}
