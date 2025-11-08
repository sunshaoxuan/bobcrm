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
    public string ServiceUrl { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string BucketName { get; set; } = "bobcrm";
    public string Region { get; set; } = "us-east-1"; // MinIO ignores but AWS SDK requires a value
}

