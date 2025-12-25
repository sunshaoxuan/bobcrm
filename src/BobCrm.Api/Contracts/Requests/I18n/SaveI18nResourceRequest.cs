namespace BobCrm.Api.Contracts.Requests.I18n;

public sealed class SaveI18nResourceRequest
{
    public string Key { get; set; } = string.Empty;

    public string Culture { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public bool Force { get; set; }
}
