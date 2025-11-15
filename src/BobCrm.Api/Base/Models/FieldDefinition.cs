using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Base;

public class FieldDefinition
{
    public int Id { get; set; }
    [Required, MaxLength(64)] public string Key { get; set; } = string.Empty;
    [Required, MaxLength(256)] public string DisplayName { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string DataType { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public string? Actions { get; set; }
    public bool Required { get; set; }
    [MaxLength(512)] public string? Validation { get; set; }
    public string? DefaultValue { get; set; }
}
