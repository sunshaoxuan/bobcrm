using System.ComponentModel.DataAnnotations;
using BobCrm.Api.Domain.Attributes;

namespace BobCrm.Api.Domain;

public class Customer
{
    public int Id { get; set; }
    [Required, MaxLength(64)] public string Code { get; set; } = string.Empty;
    
    [Required, MaxLength(256)]
    [Localizable(Required = false, MaxLength = 256, Hint = "客户名称")]
    public string Name { get; set; } = string.Empty;
    
    public int Version { get; set; } = 1;
    public string? ExtData { get; set; }
}

