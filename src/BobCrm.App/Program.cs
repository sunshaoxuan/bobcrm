using AntDesign;
using BobCrm.App.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAntDesign();
// Http client for API (strict separation)
var apiBase = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:5200";
builder.Services.AddTransient<BobCrm.App.Services.LangHeaderHandler>();
builder.Services.AddHttpClient("api", c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<BobCrm.App.Services.LangHeaderHandler>();
builder.Services.AddScoped<BobCrm.App.Services.AuthService>();
builder.Services.AddScoped<BobCrm.App.Services.FieldService>();
builder.Services.AddScoped<BobCrm.App.Services.AccessService>();
builder.Services.AddScoped<BobCrm.App.Services.I18nService>();

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
