using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using BobCrm.Api.Services.Storage;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace BobCrm.Api.Tests;

public class S3FileStorageServiceTests
{
    [Fact]
    public async Task UploadAsync_ShouldPutObject_AndReturnGeneratedKey()
    {
        var s3 = new Mock<IAmazonS3>(MockBehavior.Strict);
        PutObjectRequest? capturedPut = null;

        s3.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => capturedPut = req)
            .ReturnsAsync(new PutObjectResponse());

        var service = CreateService(s3.Object);
        var file = CreateFile("C:\\temp\\report.txt", "text/plain", "hello");

        var key = await service.UploadAsync(file, objectKeyPrefix: "attachments");

        key.Should().StartWith("attachments/");
        key.Should().Contain("report.txt");
        capturedPut.Should().NotBeNull();
        capturedPut!.BucketName.Should().Be("bucket");
        capturedPut.Key.Should().Be(key);
        capturedPut.ContentType.Should().Be("text/plain");
        capturedPut.InputStream.Should().NotBeNull();

        s3.VerifyAll();
    }

    [Fact]
    public async Task UploadAsync_ShouldThrow_WhenEmptyFile()
    {
        var service = CreateService(Mock.Of<IAmazonS3>());
        var file = CreateFile("a.txt", "text/plain", "");

        var act = async () => await service.UploadAsync(file);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Empty file");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnStreamAndContentType()
    {
        var s3 = new Mock<IAmazonS3>(MockBehavior.Strict);
        var response = new GetObjectResponse
        {
            ResponseStream = new MemoryStream(new byte[] { 1, 2, 3 })
        };
        response.Headers.ContentType = "application/json";

        s3.Setup(x => x.GetObjectAsync("bucket", "k1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var service = CreateService(s3.Object);
        var result = await service.GetAsync("k1");

        result.ContentType.Should().Be("application/json");
        result.Stream.Should().NotBeNull();
        await result.Stream.CopyToAsync(Stream.Null);

        s3.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallS3Delete()
    {
        var s3 = new Mock<IAmazonS3>(MockBehavior.Strict);
        s3.Setup(x => x.DeleteObjectAsync("bucket", "k1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse());

        var service = CreateService(s3.Object);
        await service.DeleteAsync("k1");

        s3.VerifyAll();
    }

    [Fact]
    public void GetPresignedDownloadUrl_ShouldCallS3AndReturnUrl()
    {
        var s3 = new Mock<IAmazonS3>(MockBehavior.Strict);
        GetPreSignedUrlRequest? captured = null;

        s3.Setup(x => x.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(req => captured = req)
            .Returns("https://example.test/presigned");

        var service = CreateService(s3.Object);
        var url = service.GetPresignedDownloadUrl("k1", expiresIn: TimeSpan.FromMinutes(2));

        url.Should().Be("https://example.test/presigned");
        captured.Should().NotBeNull();
        captured!.BucketName.Should().Be("bucket");
        captured.Key.Should().Be("k1");
        captured.Verb.Should().Be(HttpVerb.GET);
        (captured.Expires - DateTime.UtcNow).Should().BeLessThan(TimeSpan.FromMinutes(3));

        s3.VerifyAll();
    }

    private static S3FileStorageService CreateService(IAmazonS3 s3)
    {
        var options = Options.Create(new S3Options
        {
            ServiceUrl = "http://localhost:9000",
            AccessKey = "ak",
            SecretKey = "sk",
            BucketName = "bucket",
            Region = "us-east-1"
        });

        return new S3FileStorageService(options, s3);
    }

    private static IFormFile CreateFile(string fileName, string contentType, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
