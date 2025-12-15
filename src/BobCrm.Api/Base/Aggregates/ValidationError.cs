namespace BobCrm.Api.Base.Aggregates;

/// <summary>
/// 验证错误
/// </summary>
public class ValidationError
{
    /// <summary>
    /// 属性路径（如 "Root.EntityName" 或 "SubEntity[Lines].Field[ProductCode].DataType"）
    /// </summary>
    public string PropertyPath { get; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string Message { get; }

    public ValidationError(string propertyPath, string message)
    {
        PropertyPath = propertyPath;
        Message = message;
    }
}
