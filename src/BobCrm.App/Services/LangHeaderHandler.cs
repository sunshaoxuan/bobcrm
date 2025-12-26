using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BobCrm.App.Services;

public class LangHeaderHandler : DelegatingHandler
{
    private readonly IJsInteropService _js;

    public LangHeaderHandler(IJsInteropService js) => _js = js;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var (_, lang) = await _js.TryInvokeAsync<string?>("bobcrm.getCookie", "lang");
        lang = string.IsNullOrWhiteSpace(lang) ? "ja" : lang;
        if (request.Headers.Contains("X-Lang")) request.Headers.Remove("X-Lang");
        request.Headers.Add("X-Lang", lang.ToLowerInvariant());
        return await base.SendAsync(request, cancellationToken);
    }
}
