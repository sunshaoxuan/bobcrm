namespace BobCrm.Api.Core.DomainCommon;

public record ValidationError(string Field, string Code, string Message);
