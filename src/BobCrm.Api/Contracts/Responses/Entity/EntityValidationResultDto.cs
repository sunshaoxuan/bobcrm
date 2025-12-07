namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 实体验证结果 DTO
/// </summary>
public class EntityValidationResultDto
{
    public bool IsValid { get; set; }
    public string EntityRoute { get; set; } = string.Empty;
    public EntityDefinitionDto? Entity { get; set; }
}
