namespace BobCrm.Api.Base.Aggregates;

public class ValidationException : Exception
{
    public List<ValidationError> Errors { get; }

    public ValidationException(List<ValidationError> errors) : base("ERR_VALIDATION_FAILED")
    {
        Errors = errors;
    }

    public ValidationException(string messageKey, params object[] args) : base(messageKey)
    {
        Errors = new List<ValidationError> { new ValidationError("General", messageKey, args) };
    }
}
