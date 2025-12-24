namespace BobCrm.Api.Core.DomainCommon;

public interface IValidatable
{
    ValidationResult Validate();
}
