namespace BobCrm.App.Models;

/// <summary>
/// 实体接口DTO
/// </summary>
public class EntityInterfaceDto
{
    public Guid Id { get; set; }
    public string InterfaceType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}
