using Microsoft.AspNetCore.Http;

namespace BobCrm.Api.Core.DomainCommon.Validation;

public interface IValidationPipeline
{
    Task<IResult?> ValidateAsync<T>(T model, HttpContext http, CancellationToken ct = default);
}
