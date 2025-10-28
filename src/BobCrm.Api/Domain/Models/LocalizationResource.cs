using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Domain;

public class LocalizationResource
{
    [Key, MaxLength(256)] public string Key { get; set; } = string.Empty;
    public string? ZH { get; set; }
    public string? JA { get; set; }
    public string? EN { get; set; }
}

