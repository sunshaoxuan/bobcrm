namespace BobCrm.Api.Domain;

public class LocalizationLanguage
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
}

