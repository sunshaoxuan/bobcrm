using Microsoft.AspNetCore.Http;

namespace BobCrm.Api.Core.DomainCommon;

public static class ApiErrors
{
    public static IResult Validation(string message, params ValidationError[] details)
        => Results.Json(new { code = "ValidationFailed", message, details }, statusCode: 400);

    public static IResult Business(string message, params ValidationError[] details)
        => Results.Json(new { code = "BusinessRuleViolation", message, details }, statusCode: 400);

    public static IResult Persistence(string message, params ValidationError[] details)
        => Results.Json(new { code = "PersistenceError", message, details }, statusCode: 500);
}

