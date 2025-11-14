namespace BobCrm.Api.Domain.Models.Metadata;

public class FieldSourceEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; } = 100;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
