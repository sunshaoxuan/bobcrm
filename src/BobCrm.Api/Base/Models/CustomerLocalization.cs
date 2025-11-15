using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BobCrm.Api.Base;

public class CustomerLocalization : ILocalizationData
{
    [Key, Column(Order = 0)]
    public int CustomerId { get; set; }
    
    [Key, Column(Order = 1), MaxLength(8)]
    public string Language { get; set; } = string.Empty; // "ja", "zh", "en"
    
    [MaxLength(256)]
    public string? Name { get; set; }
    
    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }
    
    // ILocalizationData implementation
    public int EntityId { get => CustomerId; set => CustomerId = value; }
    
    public string? GetLocalizedValue(string propertyName)
    {
        return propertyName switch
        {
            nameof(Name) => Name,
            _ => null
        };
    }
}
