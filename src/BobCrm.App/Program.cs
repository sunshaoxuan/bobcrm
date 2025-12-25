using AntDesign;
using BobCrm.App.Components;
using BobCrm.App.Services;
using BobCrm.App.Services.Multilingual;
using Microsoft.AspNetCore.Components.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
// 开发期输出详细的电路错误，便于定位渲染异常导致的断链
builder.Services.Configure<CircuitOptions>(o => o.DetailedErrors = true);
builder.Services.AddAntDesign();
// Http client for API (strict separation)
var apiBase = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:5200";
builder.Services.AddTransient<BobCrm.App.Services.LangHeaderHandler>();
builder.Services.AddHttpClient("api", c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<BobCrm.App.Services.LangHeaderHandler>();
builder.Services.AddScoped<BobCrm.App.Services.AuthService>();
builder.Services.AddScoped<BobCrm.App.Services.OrganizationService>();
builder.Services.AddScoped<BobCrm.App.Services.FieldService>();
builder.Services.AddScoped<BobCrm.App.Services.FieldActionService>();
builder.Services.AddScoped<BobCrm.App.Services.AccessService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<BobCrm.App.Services.MenuService>();
builder.Services.AddScoped<BobCrm.App.Services.UserService>();
builder.Services.AddScoped<BobCrm.App.Services.TemplateRuntimeClient>();
builder.Services.AddScoped<BobCrm.App.Services.TemplateBindingService>();
builder.Services.AddScoped<BobCrm.App.Services.I18nService>();
// Multilingual text resolution services
builder.Services.Configure<MultilingualOptions>(options =>
{
    options.DefaultLanguage = "ja";
    options.FallbackLanguages = new List<string> { "en", "zh" };
});
builder.Services.AddScoped<ILanguageContext, I18nLanguageContext>();
builder.Services.AddScoped<IMultilingualTextResolver, MultilingualTextResolver>();
builder.Services.AddScoped<BobCrm.App.Services.PreferencesService>();
builder.Services.AddScoped<BobCrm.App.Services.ThemeState>();
builder.Services.AddScoped<BobCrm.App.Services.LayoutState>();
builder.Services.AddScoped<BobCrm.App.Services.InteractionState>();
builder.Services.AddScoped<BobCrm.App.Services.ToastService>();
// 动态实体系统服务
builder.Services.AddScoped<BobCrm.App.Services.EntityDefinitionService>();
builder.Services.AddScoped<BobCrm.App.Services.EntityDomainService>();
builder.Services.AddScoped<BobCrm.App.Services.DynamicEntityService>();
builder.Services.AddScoped<BobCrm.App.Services.EnumDefinitionService>();
builder.Services.AddScoped<BobCrm.App.Services.AuditLogService>();
builder.Services.AddScoped<BobCrm.App.Services.BackgroundJobService>();
builder.Services.AddSingleton<BobCrm.App.Services.Widgets.Rendering.IDesignWidgetContentRenderer, BobCrm.App.Services.Widgets.Rendering.DesignWidgetContentRenderer>();
builder.Services.AddSingleton<BobCrm.App.Services.Widgets.Rendering.IDesignContainerRenderer, BobCrm.App.Services.Widgets.Rendering.DesignContainerRenderer>();
builder.Services.AddSingleton<BobCrm.App.Services.Widgets.Rendering.IRuntimeWidgetRenderer, BobCrm.App.Services.Widgets.Rendering.RuntimeWidgetRenderer>();
builder.Services.AddSingleton<BobCrm.App.Services.Designer.IWidgetPropertyProvider, BobCrm.App.Services.Designer.WidgetPropertyProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// API proxy - forward /api requests to API server
app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"), apiApp =>
{
    apiApp.Run(async context =>
    {
        var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient("api");

        var requestPath = context.Request.Path.Value + context.Request.QueryString;
        var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), requestPath);

        // Copy request headers
        foreach (var header in context.Request.Headers)
        {
            // Skip pseudo-headers, Host, and content headers that will be set automatically
            if (!header.Key.StartsWith(":") && 
                header.Key != "Host" && 
                header.Key != "Transfer-Encoding" && 
                header.Key != "Content-Length")
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        // Copy request body for POST/PUT - check for body content regardless of ContentLength header
        if (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "PATCH")
        {
            var memoryStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(memoryStream);
            
            if (memoryStream.Length > 0)
            {
                memoryStream.Position = 0;
                requestMessage.Content = new StreamContent(memoryStream);
                if (context.Request.ContentType != null)
                {
                    try
                    {
                        // 安全解析 ContentType，处理 charset 参数
                        var contentType = context.Request.ContentType;
                        if (System.Net.Http.Headers.MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
                        {
                            requestMessage.Content.Headers.ContentType = mediaType;
                        }
                    }
                    catch
                    {
                        // 如果解析失败，尝试只设置基本的 content type
                        try
                        {
                            var baseType = context.Request.ContentType.Split(';')[0].Trim();
                            requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(baseType);
                        }
                        catch { }
                    }
                }
            }
        }

        var response = await client.SendAsync(requestMessage);

        // Copy response
        context.Response.StatusCode = (int)response.StatusCode;
        
        // Copy response headers (excluding content headers)
        foreach (var header in response.Headers)
        {
            // 跳过 Transfer-Encoding，由 ASP.NET Core 自动处理
            if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                continue;
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        
        // Copy content headers (excluding those that conflict)
        foreach (var header in response.Content.Headers)
        {
            // 跳过 Content-Length 和 Transfer-Encoding，由 ASP.NET Core 自动处理
            if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                continue;
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        await response.Content.CopyToAsync(context.Response.Body);
    });
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
