namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 实体接口 DTO
/// </summary>
public class EntityInterfaceDto
{
    public Guid Id { get; set; }
    public string InterfaceType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
