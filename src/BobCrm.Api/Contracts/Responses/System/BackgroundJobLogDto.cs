namespace BobCrm.Api.Contracts.Responses.System;

public sealed class BackgroundJobLogDto
{
    public DateTime TimestampUtc { get; set; }

    public string Level { get; set; } = "INFO";

    public string Message { get; set; } = string.Empty;
}
