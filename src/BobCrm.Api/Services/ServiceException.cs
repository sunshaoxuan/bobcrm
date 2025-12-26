namespace BobCrm.Api.Services;

public class ServiceException : Exception
{
    public string ErrorCode { get; }

    public ServiceException(string message, string errorCode = "OPERATION_FAILED") : base(message)
    {
        ErrorCode = errorCode;
    }
}
