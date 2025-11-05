namespace BobCrm.Api.Utils;

/// <summary>
/// 文件名处理工具类
/// </summary>
public static class FileNameHelper
{
    /// <summary>
    /// 清理文件名中的非法字符
    /// </summary>
    /// <param name="fileName">原始文件名</param>
    /// <returns>清理后的文件名</returns>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
    }
}

