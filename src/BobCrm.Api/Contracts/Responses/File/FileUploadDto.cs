namespace BobCrm.Api.Contracts.Responses.File;

/// <summary>
/// 文件上传结果。
/// </summary>
public class FileUploadDto
{
    /// <summary>
    /// 文件存储 key。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 访问 URL（相对路径）。
    /// </summary>
    public string Url { get; set; } = string.Empty;
}

