namespace BobCrm.Api.Core.DomainCommon;

public record ValidationResult(bool IsValid, IReadOnlyList<ValidationError> Errors)
{
    public static ValidationResult Ok() => new(true, Array.Empty<ValidationError>());
    public static ValidationResult Fail(params ValidationError[] errors) => new(false, errors);
}
