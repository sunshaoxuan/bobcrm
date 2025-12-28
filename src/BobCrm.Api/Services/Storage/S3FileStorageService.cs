using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BobCrm.Api.Services.Storage;

/// <summary>
/// S3/MinIO 文件存储实现。
/// </summary>
public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly S3Options _options;

    /// <summary>
    /// 使用配置创建 S3/MinIO 存储服务（内部创建 <see cref="IAmazonS3"/> 客户端）。
    /// </summary>
    /// <param name="options">S3 连接与存储配置。</param>
    public S3FileStorageService(IOptions<S3Options> options)
    {
        _options = options.Value;
        var config = new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrl,
            ForcePathStyle = true, // MinIO 需要 Path-style
            AuthenticationRegion = _options.Region
        };
        var creds = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);
        _s3 = new AmazonS3Client(creds, config);
    }

    /// <summary>
    /// 使用配置与外部提供的 <see cref="IAmazonS3"/> 客户端创建服务（便于测试与 DI）。
    /// </summary>
    /// <param name="options">S3 连接与存储配置。</param>
    /// <param name="s3">S3 客户端。</param>
    public S3FileStorageService(IOptions<S3Options> options, IAmazonS3 s3)
    {
        _options = options.Value;
        _s3 = s3;
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(IFormFile file, string? objectKeyPrefix = null, CancellationToken ct = default)
    {
        if (file.Length == 0) throw new ArgumentException("Empty file");
        var key = BuildObjectKey(file.FileName, objectKeyPrefix);
        using var stream = file.OpenReadStream();
        var put = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = file.ContentType ?? "application/octet-stream",
        };
        await _s3.PutObjectAsync(put, ct);
        return key;
    }

    /// <inheritdoc />
    public async Task<(Stream Stream, string ContentType)> GetAsync(string objectKey, CancellationToken ct = default)
    {
        var resp = await _s3.GetObjectAsync(_options.BucketName, objectKey, ct);
        return (resp.ResponseStream, resp.Headers.ContentType ?? "application/octet-stream");
    }

    /// <inheritdoc />
    public Task DeleteAsync(string objectKey, CancellationToken ct = default)
        => _s3.DeleteObjectAsync(_options.BucketName, objectKey, ct);

    /// <inheritdoc />
    public string GetPresignedDownloadUrl(string objectKey, TimeSpan? expiresIn = null)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromMinutes(15)),
            Verb = HttpVerb.GET
        };

        return _s3.GetPreSignedURL(request);
    }

    private static string BuildObjectKey(string fileName, string? prefix)
    {
        var safeName = Path.GetFileName(fileName);
        var date = DateTime.UtcNow.ToString("yyyy/MM/dd");
        return string.IsNullOrWhiteSpace(prefix)
            ? $"uploads/{date}/{Guid.NewGuid():N}-{safeName}"
            : $"{prefix.TrimEnd('/')}/{date}/{Guid.NewGuid():N}-{safeName}";
    }
}

