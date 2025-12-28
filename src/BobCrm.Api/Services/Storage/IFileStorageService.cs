using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BobCrm.Api.Services.Storage;

/// <summary>
/// 文件存储服务接口。
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// 上传文件，并返回对象存储中的对象 Key。
    /// </summary>
    Task<string> UploadAsync(IFormFile file, string? objectKeyPrefix = null, CancellationToken ct = default);

    /// <summary>
    /// 获取文件流与 Content-Type。
    /// </summary>
    Task<(Stream Stream, string ContentType)> GetAsync(string objectKey, CancellationToken ct = default);

    /// <summary>
    /// 删除文件。
    /// </summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);

    /// <summary>
    /// 生成预签名下载 URL。
    /// </summary>
    string GetPresignedDownloadUrl(string objectKey, TimeSpan? expiresIn = null);
}
