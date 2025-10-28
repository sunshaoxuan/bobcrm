using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BobCrm.Api.Core.DomainCommon.Validation;

public class ValidationPipeline : IValidationPipeline
{
    private readonly IServiceProvider _sp;
    public ValidationPipeline(IServiceProvider sp) => _sp = sp;

    public async Task<IResult?> ValidateAsync<T>(T model, HttpContext http, CancellationToken ct = default)
    {
        // Business validators
        var bValidators = _sp.GetServices<IBusinessValidator<T>>();
        foreach (var v in bValidators)
        {
            var errs = v.Validate(model).ToArray();
            if (errs.Length > 0) return ApiErrors.Business("business validation failed", errs);
        }

        // Common validators
        var cValidators = _sp.GetServices<ICommonValidator<T>>();
        foreach (var v in cValidators)
        {
            var errs = v.Validate(model).ToArray();
            if (errs.Length > 0) return ApiErrors.Validation("validation failed", errs);
        }

        // Persistence validators
        var pValidators = _sp.GetServices<IPersistenceValidator<T>>();
        foreach (var v in pValidators)
        {
            var errs = (await v.ValidateAsync(model, ct)).ToArray();
            if (errs.Length > 0) return ApiErrors.Persistence("persistence validation failed", errs);
        }

        return null;
    }
}

