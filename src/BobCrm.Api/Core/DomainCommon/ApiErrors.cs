using Microsoft.AspNetCore.Http;
using BobCrm.Api.Contracts;

namespace BobCrm.Api.Core.DomainCommon;

public static class ApiErrors
{
    private static Dictionary<string, string[]> ToDetailsDictionary(ValidationError[] details)
    {
        var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in details)
        {
            var field = string.IsNullOrWhiteSpace(d.Field) ? "_global" : d.Field;
            if (!dict.TryGetValue(field, out var list))
            {
                list = new List<string>();
                dict[field] = list;
            }
            list.Add(d.Message);
        }

        return dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    public static IResult Validation(string message, params ValidationError[] details)
        => Results.Json(
            ApiResponseExtensions.ErrorResponse(
                ErrorCodes.ValidationFailed,
                message,
                details is { Length: > 0 } ? ToDetailsDictionary(details) : null),
            statusCode: StatusCodes.Status400BadRequest);

    public static IResult Business(string message, params ValidationError[] details)
        => Results.Json(
            ApiResponseExtensions.ErrorResponse(
                ErrorCodes.BusinessRuleViolation,
                message,
                details is { Length: > 0 } ? ToDetailsDictionary(details) : null),
            statusCode: StatusCodes.Status400BadRequest);

    public static IResult Persistence(string message, params ValidationError[] details)
        => Results.Json(
            ApiResponseExtensions.ErrorResponse(
                ErrorCodes.PersistenceError,
                message,
                details is { Length: > 0 } ? ToDetailsDictionary(details) : null),
            statusCode: StatusCodes.Status500InternalServerError);

    public static IResult Concurrency(string message = "version mismatch")
        => Results.Json(
            ApiResponseExtensions.ErrorResponse(
                ErrorCodes.ConcurrencyConflict,
                message,
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["version"] = new[] { message } }),
            statusCode: StatusCodes.Status409Conflict);
}
