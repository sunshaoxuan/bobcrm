namespace BobCrm.Api.Base.Aggregates;

public class ValidationError
{
    public string PropertyPath { get; }
    public string MessageKey { get; }
    public object[] Args { get; }

    public string Message => MessageKey;

    public ValidationError(string propertyPath, string messageKey, params object[] args)
    {
        PropertyPath = propertyPath;
        MessageKey = messageKey;
        Args = args ?? Array.Empty<object>();
    }
}
