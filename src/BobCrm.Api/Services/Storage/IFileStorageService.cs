using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BobCrm.Api.Services.Storage;

public interface IFileStorageService
{
    Task<string> UploadAsync(IFormFile file, string? objectKeyPrefix = null, CancellationToken ct = default);
    Task<(Stream Stream, string ContentType)> GetAsync(string objectKey, CancellationToken ct = default);
    Task DeleteAsync(string objectKey, CancellationToken ct = default);
    string GetPresignedDownloadUrl(string objectKey, TimeSpan? expiresIn = null);
}
