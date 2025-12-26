using System.Net;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Contracts;
using System.Text.Json;

using BobCrm.Api.Abstractions;

namespace BobCrm.Api.Tests.Infrastructure;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock = new();
    private readonly Mock<IWebHostEnvironment> _envMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<ILocalization> _localizationMock = new();
    private readonly DefaultHttpContext _httpContext;
    private readonly GlobalExceptionHandler _handler;

    public GlobalExceptionHandlerTests()
    {
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
        
        _serviceProviderMock.Setup(x => x.GetService(typeof(ILocalization))).Returns(_localizationMock.Object);
        _httpContext.RequestServices = _serviceProviderMock.Object;

        _envMock.SetupGet(x => x.EnvironmentName).Returns("Production");
        _localizationMock.Setup(x => x.T(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((key, _) => $"LOC_{key}");

        _handler = new GlobalExceptionHandler(_loggerMock.Object, _envMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidationException_ReturnsValidationFailed()
    {
        var ex = new BobCrm.Api.Base.Aggregates.ValidationException("Validation failed", new[] { new BobCrm.Api.Core.DomainCommon.ValidationError("Field", "Error", "Detail") });
        
        await _handler.TryHandleAsync(_httpContext, ex, CancellationToken.None);
        
        Assert.Equal(400, _httpContext.Response.StatusCode);
        var response = await ReadErrorResponse();
        Assert.Equal(ErrorCodes.ValidationFailed, response.Code);
        Assert.Equal("LOC_ERR_VALIDATION_FAILED", response.Message);
    }

    [Fact]
    public async Task HandleAsync_KeyNotFoundException_ReturnsNotFound()
    {
        var ex = new KeyNotFoundException("Item not found");
        
        await _handler.TryHandleAsync(_httpContext, ex, CancellationToken.None);
        
        Assert.Equal(404, _httpContext.Response.StatusCode);
        var response = await ReadErrorResponse();
        Assert.Equal(ErrorCodes.NotFound, response.Code);
        Assert.Equal("LOC_ERR_NOT_FOUND", response.Message);
    }

    [Fact]
    public async Task HandleAsync_UnauthorizedAccessException_ReturnsUnauthorized()
    {
        var ex = new UnauthorizedAccessException("Access denied");
        
        await _handler.TryHandleAsync(_httpContext, ex, CancellationToken.None);
        
        Assert.Equal(401, _httpContext.Response.StatusCode);
        var response = await ReadErrorResponse();
        Assert.Equal(ErrorCodes.Unauthorized, response.Code);
        Assert.Equal("LOC_ERR_UNAUTHORIZED", response.Message);
    }
    
    [Fact]
    public async Task HandleAsync_DomainException_ReturnsCustomErrorCode()
    {
        var ex = new BobCrm.Api.Core.DomainCommon.DomainException("Custom Error", "CUSTOM_ERR_001");
        
        await _handler.TryHandleAsync(_httpContext, ex, CancellationToken.None);
        
        Assert.Equal(400, _httpContext.Response.StatusCode);
        var response = await ReadErrorResponse();
        Assert.Equal("CUSTOM_ERR_001", response.Code);
    }

    private async Task<ErrorResponse> ReadErrorResponse()
    {
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        return JsonSerializer.Deserialize<ErrorResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}
