using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BobCrm.Api.Services.Storage;

public interface IFileStorageService
{
    Task<string> UploadAsync(IFormFile file, string? objectKeyPrefix = null, CancellationToken ct = default);
    Task<(Stream Stream, string ContentType)> GetAsync(string objectKey, CancellationToken ct = default);
    Task DeleteAsync(string objectKey, CancellationToken ct = default);
}

public class S3Options
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}
