namespace BobCrm.Api.Base.Aggregates;

public class DomainException : Exception
{
    public string MessageKey { get; }
    public object[] Args { get; }

    public DomainException(string messageKey, params object[] args) : base(messageKey)
    {
        MessageKey = messageKey;
        Args = args ?? Array.Empty<object>();
    }

    public DomainException(string messageKey, Exception innerException, params object[] args) : base(messageKey, innerException)
    {
        MessageKey = messageKey;
        Args = args ?? Array.Empty<object>();
    }
}
