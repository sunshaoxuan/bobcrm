namespace BobCrm.Api.Contracts.Responses.EntityAggregate;

/// <summary>
/// 聚合校验结果。
/// </summary>
public class EntityAggregateValidationResponseDto
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public string? Code { get; set; }
    public List<EntityAggregateValidationErrorDto> Errors { get; set; } = new();
}

