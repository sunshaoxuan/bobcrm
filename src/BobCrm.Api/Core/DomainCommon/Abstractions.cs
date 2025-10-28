namespace BobCrm.Api.Core.DomainCommon;

public interface IValidatable
{
    ValidationResult Validate();
}

public interface IStateful
{
    string State { get; }
    bool CanTransitionTo(string nextState);
}

public interface IDomainEvent { }

public record ValidationError(string Field, string Code, string Message);

public record ValidationResult(bool IsValid, IReadOnlyList<ValidationError> Errors)
{
    public static ValidationResult Ok() => new(true, Array.Empty<ValidationError>());
    public static ValidationResult Fail(params ValidationError[] errors) => new(false, errors);
}

public interface IVersioned
{
    int Version { get; }
}

public interface IAuditable
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
    string? CreatedBy { get; }
    string? UpdatedBy { get; }
}

