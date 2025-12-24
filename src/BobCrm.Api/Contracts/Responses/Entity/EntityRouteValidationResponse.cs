namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// EntityRoute 校验响应 DTO。
/// 用于描述实体路由是否合法，以及（可选）返回命中的实体元数据描述符。
/// </summary>
public class EntityRouteValidationResponse
{
    /// <summary>
    /// 路由是否有效。
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 解析/校验后的实体路由（例如 <c>customer</c>）。
    /// </summary>
    public string EntityRoute { get; set; } = string.Empty;

    /// <summary>
    /// 可选的实体元数据描述符。
    /// 说明：该字段在 Batch 2 阶段保持为 <see cref="object"/>，后续可替换为更强类型的元数据 DTO。
    /// </summary>
    public object? Entity { get; set; }
}

