namespace BobCrm.Api.Contracts.Responses.EntityAggregate;

/// <summary>
/// 聚合校验错误条目。
/// </summary>
public class EntityAggregateValidationErrorDto
{
    public string PropertyPath { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

