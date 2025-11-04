namespace BobCrm.Api.Contracts.DTOs;

public record CreateFieldDefDto(
    string Key,
    string DisplayName,
    string DataType,
    bool Required = false,
    object? DefaultValue = null,
    string? Validation = null,
    List<string>? Tags = null,
    List<string>? Actions = null
);

public record UpdateFieldDefDto(
    string DisplayName,
    bool? Required = null,
    object? DefaultValue = null,
    string? Validation = null,
    List<string>? Tags = null,
    List<string>? Actions = null
);

public record SaveUserLayoutDto(Dictionary<string, object> LayoutJson);

public record SaveCustomerLayoutDto(Dictionary<string, object> LayoutJson);

