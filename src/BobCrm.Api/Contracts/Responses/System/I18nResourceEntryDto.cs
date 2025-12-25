namespace BobCrm.Api.Contracts.Responses.System;

public sealed class I18nResourceEntryDto
{
    public string Key { get; set; } = string.Empty;

    public string Culture { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public bool IsProtectedKey { get; set; }
}
