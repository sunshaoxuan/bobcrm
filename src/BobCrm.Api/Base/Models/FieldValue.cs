namespace BobCrm.Api.Base;

public class FieldValue
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int FieldDefinitionId { get; set; }
    public string? Value { get; set; }
    public int Version { get; set; }
}

