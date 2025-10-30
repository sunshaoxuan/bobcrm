using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BobCrm.Api.Domain;

public class CustomerLocalization
{
    [Key, Column(Order = 0)]
    public int CustomerId { get; set; }
    
    [Key, Column(Order = 1), MaxLength(8)]
    public string Language { get; set; } = string.Empty; // "ja", "zh", "en"
    
    [MaxLength(256)]
    public string? Name { get; set; }
    
    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }
}
