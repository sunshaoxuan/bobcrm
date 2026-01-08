using System.Net.Http;
using BobCrm.App.Services;
using BobCrm.App.Tests.TestHelpers;

namespace BobCrm.App.Tests;

public class AuthServiceRefreshTests
{
    [Fact]
    public async Task TryRefreshAsync_WhenJsNotReadyForRefreshToken_DoesNotClearTokens()
    {
        var js = new RecordingJsInteropService();
        js.SetResult("bobcrm.getLocalStorageItem", "refreshToken", success: false, value: null);

        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5200") };
        var auth = new AuthService(new SimpleHttpClientFactory(httpClient), js, TimeProvider.System);

        var ok = await auth.TryRefreshAsync();

        Assert.False(ok);
        Assert.DoesNotContain(js.VoidCalls, c => c.Identifier == "bobcrm.removeLocalStorageItem" && c.Arg0 == "accessToken");
        Assert.DoesNotContain(js.VoidCalls, c => c.Identifier == "bobcrm.removeLocalStorageItem" && c.Arg0 == "refreshToken");
    }

    [Fact]
    public async Task TryRefreshAsync_WhenRefreshTokenMissing_DoesNotClearTokens()
    {
        var js = new RecordingJsInteropService();
        js.SetResult("bobcrm.getLocalStorageItem", "refreshToken", success: true, value: null);

        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5200") };
        var auth = new AuthService(new SimpleHttpClientFactory(httpClient), js, TimeProvider.System);

        var ok = await auth.TryRefreshAsync();

        Assert.False(ok);
        Assert.DoesNotContain(js.VoidCalls, c => c.Identifier == "bobcrm.removeLocalStorageItem" && c.Arg0 == "accessToken");
        Assert.DoesNotContain(js.VoidCalls, c => c.Identifier == "bobcrm.removeLocalStorageItem" && c.Arg0 == "refreshToken");
    }

    private sealed class RecordingJsInteropService : IJsInteropService
    {
        private readonly Dictionary<(string Identifier, string? Arg0), (bool Success, object? Value)> _results = new();

        public List<(string Identifier, string? Arg0)> VoidCalls { get; } = new();

        public void SetResult(string identifier, string? arg0, bool success, object? value) =>
            _results[(identifier, arg0)] = (success, value);

        public Task<(bool Success, T? Value)> TryInvokeAsync<T>(string identifier, params object?[]? args)
        {
            var arg0 = args is { Length: > 0 } ? args[0]?.ToString() : null;
            if (_results.TryGetValue((identifier, arg0), out var result))
            {
                return Task.FromResult((result.Success, (T?)result.Value));
            }

            return Task.FromResult<(bool, T?)>((true, default));
        }

        public Task<bool> TryInvokeVoidAsync(string identifier, params object?[]? args)
        {
            var arg0 = args is { Length: > 0 } ? args[0]?.ToString() : null;
            VoidCalls.Add((identifier, arg0));
            return Task.FromResult(true);
        }

        public Task<bool> TryInvokeVoidWithToastAsync(string identifier, Func<string> errorMessageFactory, params object?[]? args) =>
            TryInvokeVoidAsync(identifier, args);
    }
}

