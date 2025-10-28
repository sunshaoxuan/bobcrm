using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Domain;

public class Customer
{
    public int Id { get; set; }
    [Required, MaxLength(64)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string? ExtData { get; set; }
}

