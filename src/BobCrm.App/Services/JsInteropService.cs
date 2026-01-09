using Microsoft.JSInterop;

namespace BobCrm.App.Services;

public class JsInteropService : IJsInteropService
{
    private readonly IJSRuntime _js;
    private readonly ILogger<JsInteropService> _logger;
    private readonly ToastService _toastService;

    public JsInteropService(
        IJSRuntime js,
        ILogger<JsInteropService> logger,
        ToastService toastService)
    {
        _js = js;
        _logger = logger;
        _toastService = toastService;
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
            if (IsPrerenderingError(ex) || ex is JSDisconnectedException)
            {
                return (false, default);
            }

            _logger.LogError(ex, "Unexpected Error calling JS {Identifier}", identifier);
            return (false, default);
        }
    }

    private bool IsPrerenderingError(Exception ex)
    {
        return ex is InvalidOperationException && (ex.Message.Contains("statically rendered") || ex.Message.Contains("prerendering")); 
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
            if (IsPrerenderingError(ex) || ex is JSDisconnectedException)
            {
                return false;
            }

            _logger.LogError(ex, "Unexpected Error calling JS {Identifier}", identifier);
            return false;
        }
    }

    public async Task<bool> TryInvokeVoidWithToastAsync(string identifier, Func<string> errorMessageFactory, params object?[]? args)
    {
        var success = await TryInvokeVoidAsync(identifier, args);
        if (!success)
        {
            string msg;
            try
            {
                msg = errorMessageFactory();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error message factory failed for JS toast, identifier={Identifier}", identifier);
                msg = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(msg))
            {
                msg = "JS interop failed.";
            }

            _toastService.Error(msg);
        }
        return success;
    }
}
