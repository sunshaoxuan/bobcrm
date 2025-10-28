using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AntDesign;
using BobCrm.App.Components;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configuration defaults for JWT (dev). Override in Production via appsettings.Production.json or env vars.
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Jwt:Key"] = builder.Configuration["Jwt:Key"] ?? "dev-secret-change-in-prod-1234567890",
    ["Jwt:Issuer"] = builder.Configuration["Jwt:Issuer"] ?? "BobCrm",
    ["Jwt:Audience"] = builder.Configuration["Jwt:Audience"] ?? "BobCrmUsers",
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAntDesign();
builder.Services.AddHttpClient();

// JWT Authentication (minimal, to be replaced by Identity later per milestones)
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization();

// In-memory mock data to keep UI alive (aligns with docs shapes)
var customers = new List<object>
{
    new { id = 1, code = "C001", name = "客户A" },
    new { id = 2, code = "C002", name = "客户B" }
};

var customerDetails = new Dictionary<int, object>
{
    [1] = new
    {
        id = 1,
        code = "C001",
        name = "客户A",
        version = 2,
        fields = new object[]
        {
            new { key = "email", label = "邮箱", type = "email", value = "a@b.com" },
            new { key = "rds", label = "RDS连接", type = "rds", value = new { ip = "10.0.0.1", user = "admin" } }
        }
    },
    [2] = new
    {
        id = 2,
        code = "C002",
        name = "客户B",
        version = 1,
        fields = new object[]
        {
            new { key = "email", label = "邮箱", type = "email", value = "b@b.com" }
        }
    }
};

var fieldDefinitions = new List<object>
{
    new
    {
        key = "email",
        label = "邮箱",
        type = "email",
        tags = new [] { "常用" },
        actions = new object[] { new { icon = "mail", title = "发邮件", type = "click", action = "mailto" } }
    }
};

var userLayouts = new Dictionary<(int userId, int customerId), object>();

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
app.UseAuthentication();
app.UseAuthorization();

// Minimal APIs (strictly per docs)
app.MapPost("/api/auth/login", (LoginRequest body) =>
{
    if (body.username == "admin" && body.password == "123456")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "admin")
        };
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: builder.Configuration["Jwt:Issuer"],
            audience: builder.Configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return Results.Json(new { token = jwt, user = new { id = 1, username = "admin", role = "admin" } });
    }
    return Results.Unauthorized();
});

app.MapGet("/api/customers", () => Results.Json(customers)).RequireAuthorization();

app.MapGet("/api/customers/{id:int}", (int id) =>
{
    if (customerDetails.TryGetValue(id, out var detail))
        return Results.Json(detail);
    return Results.NotFound();
}).RequireAuthorization();

app.MapGet("/api/fields", () => Results.Json(fieldDefinitions)).RequireAuthorization();

app.MapPut("/api/customers/{id:int}", (int id, CustomerUpdateRequest req) =>
{
    if (!customerDetails.TryGetValue(id, out var obj)) return Results.NotFound();
    var current = (dynamic)obj;
    int version = current.version;
    var mergedFields = ((IEnumerable<object>)current.fields).ToList();
    foreach (var f in req.fields)
    {
        var idx = mergedFields.FindIndex(x => ((dynamic)x).key == f.key);
        if (idx >= 0) mergedFields[idx] = new { key = f.key, label = ((dynamic)mergedFields[idx]).label, type = ((dynamic)mergedFields[idx]).type, value = f.value };
        else mergedFields.Add(new { key = f.key, label = f.key, type = "text", value = f.value });
    }
    var updated = new
    {
        id = current.id,
        code = current.code,
        name = current.name,
        version = version + 1,
        fields = mergedFields.ToArray()
    };
    customerDetails[id] = updated;
    return Results.Json(new { status = "success", newVersion = version + 1 });
}).RequireAuthorization();

app.MapGet("/api/layout/{customerId:int}", (int customerId, ClaimsPrincipal user) =>
{
    var uid = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    userLayouts.TryGetValue((uid, customerId), out var layout);
    return Results.Json(layout ?? new { });
}).RequireAuthorization();

app.MapPost("/api/layout/{customerId:int}", (int customerId, ClaimsPrincipal user, IDictionary<string, object> layoutJson) =>
{
    var uid = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    userLayouts[(uid, customerId)] = layoutJson;
    return Results.Ok();
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

record LoginRequest(string username, string password);
record CustomerUpdateRequest(List<FieldUpdate> fields)
{
    public List<FieldUpdate> fields { get; set; } = fields;
}
record FieldUpdate(string key, object value);
