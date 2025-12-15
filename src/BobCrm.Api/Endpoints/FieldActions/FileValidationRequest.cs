namespace BobCrm.Api.Endpoints;

/// <summary>
/// 文件验证请求
/// </summary>
public record FileValidationRequest
{
    public string Path { get; init; } = string.Empty;
}

