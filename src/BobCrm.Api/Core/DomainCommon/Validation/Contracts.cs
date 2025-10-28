using Microsoft.AspNetCore.Http;

namespace BobCrm.Api.Core.DomainCommon.Validation;

public interface IBusinessValidator<T>
{
    IEnumerable<ValidationError> Validate(T model);
}

public interface ICommonValidator<T>
{
    IEnumerable<ValidationError> Validate(T model);
}

public interface IPersistenceValidator<T>
{
    Task<IEnumerable<ValidationError>> ValidateAsync(T model, CancellationToken ct = default);
}

public interface IValidationPipeline
{
    Task<IResult?> ValidateAsync<T>(T model, HttpContext http, CancellationToken ct = default);
}

