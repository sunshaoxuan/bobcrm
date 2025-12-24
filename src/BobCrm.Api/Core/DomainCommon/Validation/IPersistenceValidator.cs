namespace BobCrm.Api.Core.DomainCommon.Validation;

public interface IPersistenceValidator<T>
{
    Task<IEnumerable<ValidationError>> ValidateAsync(T model, CancellationToken ct = default);
}
