namespace BobCrm.Api.Core.DomainCommon;

/// <summary>
/// Central registry for System Error Codes.
/// Naming convention: UPPER_SNAKE_CASE
/// </summary>
public static class ErrorCodes
{
    // Common / Infrastructure
    public const string InternalError = "INTERNAL_ERROR";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string InvalidArgument = "INVALID_ARGUMENT";
    public const string InvalidOperation = "INVALID_OPERATION";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string RequestCancelled = "REQUEST_CANCELLED";
    public const string NotImplemented = "NOT_IMPLEMENTED";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    
    // Business Common
    public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";
    public const string PersistenceError = "PERSISTENCE_ERROR";
    public const string DomainError = "DOMAIN_ERROR";
    public const string EntityExists = "ENTITY_EXISTS";

    // Module: Auth
    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string AuthUserNotFound = "AUTH_USER_NOT_FOUND";
    public const string AuthTokenInvalid = "AUTH_TOKEN_INVALID";
    public const string AuthLoginExpired = "AUTH_LOGIN_EXPIRED";

    // Module: System
    public const string SystemAdminImmutable = "SYS_ADMIN_IMMUTABLE";
    public const string SystemRoleImmutable = "SYS_ROLE_IMMUTABLE";

    // Module: Entity/Templates
    public const string TemplateRouteMismatch = "TEMPLATE_ROUTE_MISMATCH";
    public const string CodeConflict = "CODE_CONFLICT";
    public const string DataLoadFailed = "DATA_LOAD_FAILED";

    // Module: Entity definition
    public const string FieldProtectedBySource = "FIELD_PROTECTED_BY_SOURCE";
}
