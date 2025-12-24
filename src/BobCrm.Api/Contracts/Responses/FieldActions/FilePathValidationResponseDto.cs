namespace BobCrm.Api.Contracts.Responses.FieldActions;

/// <summary>
/// 文件路径校验结果。
/// </summary>
public class FilePathValidationResponseDto
{
    /// <summary>
    /// 是否存在。
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// 类型：url/file/directory/notfound。
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 说明信息（可选）。
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 文件大小（字节，可选）。
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// 文件扩展名（可选）。
    /// </summary>
    public string? Extension { get; set; }

    /// <summary>
    /// 最后修改时间（可选，本地时间）。
    /// </summary>
    public DateTime? LastModified { get; set; }
}

