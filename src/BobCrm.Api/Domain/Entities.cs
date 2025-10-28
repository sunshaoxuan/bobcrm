using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Domain;

public class Customer
{
    public int Id { get; set; }
    [Required, MaxLength(64)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string? ExtData { get; set; } // json/jsonb
}

public class CustomerAccess
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool CanEdit { get; set; }
}

public class FieldDefinition
{
    public int Id { get; set; }
    [Required, MaxLength(64)] public string Key { get; set; } = string.Empty;
    [Required, MaxLength(256)] public string DisplayName { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string DataType { get; set; } = string.Empty;
    public string? Tags { get; set; } // json/jsonb
    public string? Actions { get; set; } // json/jsonb
}

public class FieldValue
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int FieldDefinitionId { get; set; }
    public string? Value { get; set; } // json/jsonb or scalar as string
    public int Version { get; set; }
}

public class UserLayout
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string? LayoutJson { get; set; } // json/jsonb
}

public class LocalizationResource
{
    [Key, MaxLength(256)] public string Key { get; set; } = string.Empty;
    public string? ZH { get; set; }
    public string? JA { get; set; }
    public string? EN { get; set; }
}

