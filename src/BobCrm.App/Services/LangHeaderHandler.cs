using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BobCrm.App.Services;

public class LangHeaderHandler : DelegatingHandler
{
    private readonly IJSRuntime _js;
    public LangHeaderHandler(IJSRuntime js) => _js = js;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var lang = await _js.InvokeAsync<string?>("localStorage.getItem", "lang");
            if (!string.IsNullOrWhiteSpace(lang))
            {
                if (request.Headers.Contains("X-Lang")) request.Headers.Remove("X-Lang");
                request.Headers.Add("X-Lang", lang.ToLowerInvariant());
            }
        }
        catch { }
        return await base.SendAsync(request, cancellationToken);
    }
}

