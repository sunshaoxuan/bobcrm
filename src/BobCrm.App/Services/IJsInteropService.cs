using Microsoft.JSInterop;

namespace BobCrm.App.Services;

/// <summary>
/// Provides a safe wrapper around IJSRuntime with unified error handling and logging.
/// </summary>
public interface IJsInteropService
{
    /// <summary>
    /// Safely invokes a JavaScript function that returns a value.
    /// Captures exceptions, logs them, and optionally shows a user-facing error message.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="identifier">The JavaScript function identifier.</param>
    /// <param name="args">Arguments for the function.</param>
    /// <returns>A tuple containing Success status and the Result (if successful).</returns>
    Task<(bool Success, T? Value)> TryInvokeAsync<T>(string identifier, params object?[]? args);

    /// <summary>
    /// Safely invokes a void JavaScript function.
    /// Captures exceptions, logs them, and optionally shows a user-facing error message.
    /// </summary>
    /// <param name="identifier">The JavaScript function identifier.</param>
    /// <param name="args">Arguments for the function.</param>
    /// <returns>True if successful, False if an exception occurred.</returns>
    Task<bool> TryInvokeVoidAsync(string identifier, params object?[]? args);

    /// <summary>
    /// Safely invokes a void JavaScript function and shows an error toast if it fails.
    /// </summary>
    Task<bool> TryInvokeVoidWithToastAsync(string identifier, string errorI18nKey, params object?[]? args);
}
