using BobCrm.App.Components.Shared;
using BobCrm.Api.Abstractions;
using Microsoft.JSInterop;

namespace BobCrm.App.Services;

public class JsInteropService : IJsInteropService
{
    private readonly IJSRuntime _js;
    private readonly ILogger<JsInteropService> _logger;
    private readonly ToastService _toastService;
    private readonly II18nService _i18n;

    public JsInteropService(
        IJSRuntime js,
        ILogger<JsInteropService> logger,
        ToastService toastService,
        II18nService i18n)
    {
        _js = js;
        _logger = logger;
        _toastService = toastService;
        _i18n = i18n;
    }

    public async Task<(bool Success, T? Value)> TryInvokeAsync<T>(string identifier, params object?[]? args)
    {
        try
        {
            var result = await _js.InvokeAsync<T>(identifier, args ?? Array.Empty<object?>());
            return (true, result);
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JS Interop Error (InvokeAsync) calling {Identifier}", identifier);
            return (false, default);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("JS Interop Canceled calling {Identifier}", identifier);
            return (false, default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected Error calling JS {Identifier}", identifier);
            return (false, default);
        }
    }

    public async Task<bool> TryInvokeVoidAsync(string identifier, params object?[]? args)
    {
        try
        {
            await _js.InvokeVoidAsync(identifier, args ?? Array.Empty<object?>());
            return true;
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JS Interop Error (InvokeVoidAsync) calling {Identifier}", identifier);
            return false;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("JS Interop Canceled calling {Identifier}", identifier);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected Error calling JS {Identifier}", identifier);
            return false;
        }
    }

    public async Task<bool> TryInvokeVoidWithToastAsync(string identifier, string errorI18nKey, params object?[]? args)
    {
        var success = await TryInvokeVoidAsync(identifier, args);
        if (!success)
        {
            var msg = _i18n.T(errorI18nKey);
            if (string.IsNullOrWhiteSpace(msg)) msg = errorI18nKey;
            _toastService.Error(msg);
        }
        return success;
    }
}
