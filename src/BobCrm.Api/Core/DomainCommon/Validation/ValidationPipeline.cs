using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Domain;

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
            if (errs.Length > 0)
            {
                var loc = _sp.GetRequiredService<ILocalization>();
                var lang = LangHelper.GetLang(http);
                var msg = loc.T("ERR_BUSINESS_VALIDATION_FAILED", lang);
                var details = LocalizeErrors(errs, http, loc, lang);
                return ApiErrors.Business(msg, details);
            }
        }

        // Common validators
        var cValidators = _sp.GetServices<ICommonValidator<T>>();
        foreach (var v in cValidators)
        {
            var errs = v.Validate(model).ToArray();
            if (errs.Length > 0)
            {
                var loc = _sp.GetRequiredService<ILocalization>();
                var lang = LangHelper.GetLang(http);
                var msg = loc.T("ERR_VALIDATION_FAILED", lang);
                var details = LocalizeErrors(errs, http, loc, lang);
                return ApiErrors.Validation(msg, details);
            }
        }

        // Persistence validators
        var pValidators = _sp.GetServices<IPersistenceValidator<T>>();
        foreach (var v in pValidators)
        {
            var errs = (await v.ValidateAsync(model, ct)).ToArray();
            if (errs.Length > 0)
            {
                var loc = _sp.GetRequiredService<ILocalization>();
                var lang = LangHelper.GetLang(http);
                var msg = loc.T("ERR_PERSISTENCE_VALIDATION_FAILED", lang);
                var details = LocalizeErrors(errs, http, loc, lang);
                return ApiErrors.Persistence(msg, details);
            }
        }

        return null;
    }

    private ValidationError[] LocalizeErrors(ValidationError[] errs, HttpContext http, ILocalization loc, string lang)
    {
        var repoDef = _sp.GetService<IRepository<FieldDefinition>>();
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (repoDef != null)
            {
                foreach (var d in repoDef.Query().ToList())
                {
                    var label = loc.T(d.DisplayName, lang);
                    map[d.Key] = string.IsNullOrWhiteSpace(label) ? d.Key : label;
                }
            }
        }
        catch { }

        string LabelFor(string field)
            => (!string.IsNullOrWhiteSpace(field) && map.TryGetValue(field, out var v)) ? v : field;

        var list = new List<ValidationError>(errs.Length);
        foreach (var e in errs)
        {
            string msg = e.Message;
            switch (e.Code)
            {
                case "FieldsRequired":
                    msg = loc.T("VAL_FIELDS_REQUIRED", lang);
                    break;
                case "Required":
                    if (string.Equals(e.Field, "fields", StringComparison.OrdinalIgnoreCase))
                        msg = loc.T("VAL_FIELDS_REQUIRED", lang);
                    else
                        msg = string.Format(loc.T("VAL_REQUIRED", lang), LabelFor(e.Field));
                    break;
                case "InvalidPattern":
                    msg = string.Format(loc.T("VAL_INVALID_PATTERN", lang), LabelFor(e.Field));
                    break;
                case "InvalidFormat":
                    msg = string.Format(loc.T("VAL_INVALID_FORMAT", lang), LabelFor(e.Field));
                    break;
                case "UnknownField":
                    var which = string.IsNullOrWhiteSpace(e.Message) ? e.Field : e.Message;
                    msg = string.Format(loc.T("VAL_UNKNOWN_FIELD", lang), which);
                    break;
            }
            list.Add(new ValidationError(e.Field, e.Code, msg));
        }
        return list.ToArray();
    }
}
