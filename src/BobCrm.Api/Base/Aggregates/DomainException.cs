namespace BobCrm.Api.Base.Aggregates;

using System;

/// <summary>
/// 领域异常
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
