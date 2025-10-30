using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Infrastructure.Ef;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Core.DomainCommon.Validation;
using BobCrm.Api.Application.Queries;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Domain;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置日志到文件 - 存储在项目根目录的logs文件夹
var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "logs");
Directory.CreateDirectory(logsDir);
var logFilePath = Path.Combine(logsDir, $"api_{timestamp}.log");

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Infinite)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

// DbContext (SQLite default; PostgreSQL via config)
var dbProvider = builder.Configuration["Db:Provider"] ?? "sqlite";
var conn = builder.Configuration.GetConnectionString("Default") ?? "Data Source=./data/app.db";
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (dbProvider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
    {
        opt.UseNpgsql(conn, npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "public"));
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }
    else
    {
        opt.UseSqlite(conn);
    }
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

// Persist DataProtection Keys (session reconnect, stable cookies/tokens after restart)
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>();

// JWT
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "dev-secret-change-in-prod-1234567890");
builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();
builder.Services.AddScoped<IRefreshTokenStore, EfRefreshTokenStore>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<ILocalization, EfLocalization>();
// Map base DbContext to AppDbContext for generic repositories/UoW
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IValidationPipeline, ValidationPipeline>();
builder.Services.AddScoped<IBusinessValidator<UpdateCustomerDto>, UpdateCustomerBusinessValidator>();
builder.Services.AddScoped<ICommonValidator<UpdateCustomerDto>, UpdateCustomerCommonValidator>();
builder.Services.AddScoped<IPersistenceValidator<UpdateCustomerDto>, UpdateCustomerPersistenceValidator>();
builder.Services.AddScoped<ICustomerQueries, CustomerQueries>();
builder.Services.AddScoped<IFieldQueries, FieldQueries>();
builder.Services.AddScoped<ILayoutQueries, LayoutQueries>();

// CORS (dev friendly; tighten in production)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// 配置详细日志
app.Logger.LogInformation("============================================");
app.Logger.LogInformation("Application starting at {Time}, log file: {LogFile}", DateTime.Now, logFilePath);
app.Logger.LogInformation("============================================");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseCors();

// Auto-initialize database on startup (dev-friendly, idempotent)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseInitializer.InitializeAsync(db);
    
    // Seed test data (development only)
    try
    {
        await TestDataSeeder.SeedTestDataAsync(db);
    }
    catch { }
    
    // Seed admin user/role and grant access to all customers
    try
    {
        var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await rm.RoleExistsAsync("admin"))
        {
            await rm.CreateAsync(new IdentityRole("admin"));
        }
        var admin = await um.FindByNameAsync("admin");
        if (admin == null)
        {
            admin = new IdentityUser { UserName = "admin", Email = "admin@local", EmailConfirmed = true };
            await um.CreateAsync(admin, "Admin@12345");
            await um.AddToRoleAsync(admin, "admin");
        }
        else
        {
            if (!await um.IsInRoleAsync(admin, "admin")) await um.AddToRoleAsync(admin, "admin");
            if (!admin.EmailConfirmed)
            {
                admin.EmailConfirmed = true; await um.UpdateAsync(admin);
            }
        }
        // Grant admin access to all customers
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<CustomerAccess>>();
        var repoCust = scope.ServiceProvider.GetRequiredService<IRepository<Customer>>();
        var custIds = repoCust.Query().Select(c => c.Id).ToList();
        foreach (var cid in custIds)
        {
            var exists = repo.Query(a => a.CustomerId == cid && a.UserId == admin.Id).Any();
            if (!exists)
            {
                await repo.AddAsync(new CustomerAccess { CustomerId = cid, UserId = admin.Id, CanEdit = true });
            }
        }
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
    }
    catch { }
}

// Auth endpoints
app.MapPost("/api/auth/register", async (UserManager<IdentityUser> um, IEmailSender email, RegisterDto dto, LinkGenerator links, HttpContext http) =>
{
    var user = new IdentityUser { UserName = dto.username, Email = dto.email, EmailConfirmed = false };
    var result = await um.CreateAsync(user, dto.password);
    if (!result.Succeeded) return Results.BadRequest(result.Errors);
    var code = await um.GenerateEmailConfirmationTokenAsync(user);
    var url = links.GetUriByName(http, "Activate", new { userId = user.Id, code });
    await email.SendAsync(dto.email, "Activate your account", $"Click to activate: {url}");
    return Results.Ok(new { status = "ok" });
});

app.MapGet("/api/auth/activate", async (UserManager<IdentityUser> um, string userId, string code) =>
{
    var user = await um.FindByIdAsync(userId);
    if (user == null) return Results.NotFound();
    var res = await um.ConfirmEmailAsync(user, code);
    return res.Succeeded ? Results.Ok(new { status = "ok" }) : Results.BadRequest(res.Errors);
}).WithName("Activate");

app.MapPost("/api/auth/login", async (UserManager<IdentityUser> um, SignInManager<IdentityUser> sm, IRefreshTokenStore rts, IConfiguration cfg, LoginDto dto, ILocalization loc, HttpContext http, ILogger<Program> logger) =>
{
    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Auth] Login attempt: username='{dto.username}', remote={http.Connection.RemoteIpAddress}");
    logger.LogInformation("[Auth] Login attempt, usernameOrEmail={username}, remote={ip}", dto.username ?? "(null)", http.Connection.RemoteIpAddress);
    
    if (string.IsNullOrWhiteSpace(dto.username))
    {
        var msg = "[Auth] Invalid login request - username is empty";
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}");
        logger.LogWarning(msg);
        return Results.BadRequest(new { error = "Username is required" });
    }
    
    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Auth] Looking for user: username='{dto.username}'");
    var user = await um.FindByNameAsync(dto.username) ?? await um.FindByEmailAsync(dto.username);
    if (user == null)
    {
        var msg = $"[Auth] User not found: username='{dto.username}'";
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}");
        logger.LogWarning(msg);
        
        // 列出所有用户以便调试
        var allUsers = um.Users.Select(u => $"{u.UserName} ({u.Email})").ToList();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Auth] All users in database: {string.Join(", ", allUsers)}");
        
        return Results.Json(new { error = "Invalid username or password" }, statusCode: 401);
    }
    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Auth] Found user: username='{user.UserName}', email='{user.Email}', emailConfirmed={user.EmailConfirmed}");
    
    if (!user.EmailConfirmed)
    {
        var lang = LangHelper.GetLang(http);
        var msg = $"[Auth] Email not confirmed for user '{user.UserName}'";
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}");
        logger.LogWarning(msg);
        return Results.BadRequest(new { error = loc.T("ERR_EMAIL_NOT_CONFIRMED", lang) });
    }
    
    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Auth] Checking password for user '{user.UserName}'");
    var validPassword = await um.CheckPasswordAsync(user, dto.password);
    if (!validPassword)
    {
        var msg = $"[Auth] Password check failed for user '{user.UserName}'";
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}");
        logger.LogWarning(msg);
        return Results.Json(new { error = "Invalid username or password" }, statusCode: 401);
    }
    var tokens = await IssueTokensAsync(cfg, user, rts, key);
    logger.LogInformation("[Auth] Login success for user {user}", user.UserName);
    return Results.Json(new { accessToken = tokens.accessToken, refreshToken = tokens.refreshToken, user = new { id = user.Id, username = user.UserName, role = "user" } });
});

app.MapPost("/api/auth/refresh", async (IConfiguration cfg, IRefreshTokenStore rts, UserManager<IdentityUser> um, RefreshDto dto) =>
{
    var stored = await rts.ValidateAsync(dto.refreshToken);
    if (stored == null) return Results.Unauthorized();
    var user = await um.FindByIdAsync(stored.UserId);
    if (user == null) return Results.Unauthorized();
    await rts.RevokeAsync(dto.refreshToken);
    var tokens = await IssueTokensAsync(cfg, user, rts, key);
    return Results.Json(new { accessToken = tokens.accessToken, refreshToken = tokens.refreshToken });
});

app.MapPost("/api/auth/logout", async (IRefreshTokenStore rts, LogoutDto dto) =>
{
    if (!string.IsNullOrWhiteSpace(dto.refreshToken)) await rts.RevokeAsync(dto.refreshToken);
    return Results.Ok(new { status = "ok" });
}).RequireAuthorization();

app.MapGet("/api/auth/session", (ClaimsPrincipal user) =>
{
    if (user?.Identity?.IsAuthenticated == true)
        return Results.Ok(new { valid = true, user = new { id = user.FindFirstValue(ClaimTypes.NameIdentifier), username = user.Identity!.Name } });
    return Results.Ok(new { valid = false });
}).RequireAuthorization();

// Business APIs (Customers, Fields, Layout) via query services (no direct DbContext)
app.MapGet("/api/customers", (ICustomerQueries q) => Results.Json(q.GetList()))
    .RequireAuthorization();

app.MapGet("/api/customers/{id:int}", (int id, ICustomerQueries q) =>
{
    var detail = q.GetDetail(id);
    return detail is null ? Results.NotFound() : Results.Json(detail);
}).RequireAuthorization();

// Customer access management (admin only)
app.MapGet("/api/customers/{id:int}/access", async (int id, AppDbContext db, ClaimsPrincipal user) =>
{
    var name = user.Identity?.Name ?? string.Empty;
    var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) && !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        return Results.StatusCode(403);
    var list = await db.CustomerAccesses.AsNoTracking().Where(a => a.CustomerId == id).Select(a => new { a.UserId, a.CanEdit }).ToListAsync();
    return Results.Json(list);
}).RequireAuthorization();

app.MapPost("/api/customers/{id:int}/access", async (int id, AppDbContext db, ClaimsPrincipal user, AccessUpsert body) =>
{
    var name = user.Identity?.Name ?? string.Empty;
    var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) && !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        return Results.StatusCode(403);
    var entity = await db.CustomerAccesses.FirstOrDefaultAsync(a => a.CustomerId == id && a.UserId == body.userId);
    if (entity == null)
    {
        db.CustomerAccesses.Add(new CustomerAccess { CustomerId = id, UserId = body.userId, CanEdit = body.canEdit });
    }
    else
    {
        entity.CanEdit = body.canEdit;
        db.CustomerAccesses.Update(entity);
    }
    await db.SaveChangesAsync();
    return Results.Ok(new { status = "ok" });
}).RequireAuthorization();

app.MapGet("/api/fields", (IFieldQueries q) => Results.Json(q.GetDefinitions()))
    .RequireAuthorization();

// I18N endpoints
app.MapGet("/api/i18n/resources", (AppDbContext db) =>
{
    var list = db.LocalizationResources.AsNoTracking().ToList();
    return Results.Json(list);
}).RequireAuthorization();

app.MapGet("/api/i18n/{lang}", (string lang, AppDbContext db) =>
{
    lang = (lang ?? "ja").ToLowerInvariant();
    var query = db.LocalizationResources.AsNoTracking();
    var dict = new Dictionary<string, string>();
    foreach (var r in query)
    {
        var val = lang switch
        {
            "ja" => r.JA ?? r.ZH ?? r.EN ?? r.Key,
            "en" => r.EN ?? r.JA ?? r.ZH ?? r.Key,
            "zh" => r.ZH ?? r.JA ?? r.EN ?? r.Key,
            _ => r.JA ?? r.ZH ?? r.EN ?? r.Key
        };
        dict[r.Key] = val;
    }
    return Results.Json(dict);
});

// Development-only: debug endpoint to list all users
app.MapGet("/api/debug/users", async (UserManager<IdentityUser> um) =>
{
    if (!app.Environment.IsDevelopment()) return Results.StatusCode(403);
    var userList = new List<object>();
    foreach (var u in um.Users)
    {
        var hasPassword = await um.HasPasswordAsync(u);
        userList.Add(new { id = u.Id, username = u.UserName, email = u.Email, emailConfirmed = u.EmailConfirmed, hasPassword });
    }
    return Results.Ok(userList);
});

// Development-only: reset setup (delete admin user to allow re-initialization)
app.MapPost("/api/debug/reset-setup", async (UserManager<IdentityUser> um, RoleManager<IdentityRole> rm) =>
{
    if (!app.Environment.IsDevelopment()) return Results.StatusCode(403);
    
    var admin = await um.FindByNameAsync("admin");
    if (admin != null)
    {
        var result = await um.DeleteAsync(admin);
        if (result.Succeeded)
        {
            return Results.Ok(new { status = "ok", message = "Admin user deleted. You can now re-initialize from setup page." });
        }
        else
        {
            return Results.BadRequest(new { error = "Failed to delete admin user", details = string.Join("; ", result.Errors.Select(e => e.Description)) });
        }
    }
    
    return Results.Ok(new { status = "ok", message = "No admin user found. Ready for initialization." });
});

// Development-only: reset current admin password quickly when locked out
app.MapPost("/api/admin/reset-password", async (UserManager<IdentityUser> um, RoleManager<IdentityRole> rm, AdminResetPasswordDto dto) =>
{
    if (!app.Environment.IsDevelopment()) return Results.StatusCode(403);
    if (!await rm.RoleExistsAsync("admin")) return Results.NotFound(new { error = "admin role not found" });
    var admins = await um.GetUsersInRoleAsync("admin");
    var user = admins.FirstOrDefault();
    if (user == null) return Results.NotFound(new { error = "admin user not found" });
    if (await um.HasPasswordAsync(user))
    {
        var rmv = await um.RemovePasswordAsync(user);
        if (!rmv.Succeeded) return Results.BadRequest(new { error = string.Join("; ", rmv.Errors.Select(e => e.Description)) });
    }
    var add = await um.AddPasswordAsync(user, dto.password);
    if (!add.Succeeded) return Results.BadRequest(new { error = string.Join("; ", add.Errors.Select(e => e.Description)) });
    user.EmailConfirmed = true; await um.UpdateAsync(user);
    return Results.Ok(new { status = "ok", user = new { user = user.UserName, email = user.Email } });
});

// languages list
app.MapGet("/api/i18n/languages", (AppDbContext db) =>
{
    var list = db.LocalizationLanguages.AsNoTracking().Select(l => new { code = l.Code, name = l.NativeName }).ToList();
    if (list.Count == 0)
    {
        list = new[] { new { code = "ja", name = "日本語" }, new { code = "zh", name = "中文" }, new { code = "en", name = "English" } }.ToList();
    }
    return Results.Json(list);
});

// Tags overview for quick layout
app.MapGet("/api/fields/tags", (IRepository<FieldDefinition> repoDef) =>
{
    var defs = repoDef.Query().ToList();
    var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    foreach (var d in defs)
    {
        if (string.IsNullOrWhiteSpace(d.Tags)) continue;
        try
        {
            var tags = System.Text.Json.JsonSerializer.Deserialize<string[]>(d.Tags!) ?? Array.Empty<string>();
            foreach (var t in tags)
            {
                if (string.IsNullOrWhiteSpace(t)) continue;
                var k = t.Trim();
                if (!dict.ContainsKey(k)) dict[k] = 0;
                dict[k]++;
            }
        }
        catch { }
    }
    var list = dict.OrderBy(kv => kv.Key).Select(kv => new { tag = kv.Key, count = kv.Value }).ToList();
    return Results.Json(list);
}).RequireAuthorization();

app.MapPut("/api/customers/{id:int}", async (
    int id,
    UpdateCustomerDto dto,
    IRepository<Customer> repoCustomer,
    IRepository<FieldDefinition> repoDef,
    IRepository<FieldValue> repoVal,
    IRepository<CustomerAccess> repoAccess,
    IUnitOfWork uow,
    IValidationPipeline pipe,
    HttpContext http,
    ILocalization loc) =>
{
    var vr = await pipe.ValidateAsync(dto, http);
    if (vr is not null) return vr;

    var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    var anyAccessDefined = repoAccess.Query(a => a.CustomerId == id).Any();
    if (anyAccessDefined)
    {
        var canEdit = repoAccess.Query(a => a.CustomerId == id && a.UserId == uid && a.CanEdit).Any();
        if (!canEdit) return Results.StatusCode(403);
    }

    var c = repoCustomer.Query(x => x.Id == id).FirstOrDefault();
    if (c == null) return Results.NotFound();
    // If client provides expectedVersion, enforce optimistic concurrency; otherwise allow update
    if (dto.expectedVersion.HasValue && dto.expectedVersion.Value != c.Version)
    {
        var lang = LangHelper.GetLang(http);
        return ApiErrors.Concurrency(loc.T("ERR_CONCURRENCY", lang));
    }

    var defs = repoDef.Query().ToDictionary(d => d.Key, d => d);
    foreach (var f in dto.fields)
    {
        if (string.IsNullOrWhiteSpace(f.key))
        {
            var lang = LangHelper.GetLang(http);
            return ApiErrors.Validation(loc.T("ERR_FIELD_KEY_REQUIRED", lang));
        }
        if (!defs.TryGetValue(f.key, out var def))
        {
            var lang = LangHelper.GetLang(http);
            return ApiErrors.Business($"{loc.T("ERR_UNKNOWN_FIELD", lang)}: {f.key}");
        }

        var json = System.Text.Json.JsonSerializer.Serialize(f.value);
        var val = new FieldValue { CustomerId = id, FieldDefinitionId = def.Id, Value = json, Version = c.Version + 1 };
        await repoVal.AddAsync(val);
    }

    // version bump (common behavior placeholder)
    c.Version += 1;
    repoCustomer.Update(c);
    await uow.SaveChangesAsync();
    return Results.Json(new { status = "success", newVersion = c.Version });
}).RequireAuthorization();

app.MapGet("/api/layout/{customerId:int}", (int customerId, ClaimsPrincipal user, ILayoutQueries q, string? scope) =>
{
    var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    scope ??= "effective";
    return Results.Json(q.GetLayout(uid, customerId, scope));
}).RequireAuthorization();

app.MapPost("/api/layout/{customerId:int}", async (int customerId, ClaimsPrincipal user, IRepository<UserLayout> repoLayout, IUnitOfWork uow, System.Text.Json.JsonElement layout, HttpContext http, string? scope, ILocalization loc) =>
{
    var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    var saveScope = (scope ?? "user").ToLowerInvariant();
    var targetUserId = saveScope == "default" ? "__default__" : uid;
    if (saveScope == "default")
    {
        var name = user.Identity?.Name ?? string.Empty;
        var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) && !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.StatusCode(403);
    }
    var entity = repoLayout.Query(x => x.UserId == targetUserId && x.CustomerId == customerId).FirstOrDefault();
    var json = layout.GetRawText();
    if (string.IsNullOrWhiteSpace(json))
    {
        var lang = LangHelper.GetLang(http);
        return ApiErrors.Validation(loc.T("ERR_LAYOUT_BODY_REQUIRED", lang));
    }
    if (entity == null)
    {
        entity = new UserLayout { UserId = targetUserId, CustomerId = customerId, LayoutJson = json };
        await repoLayout.AddAsync(entity);
    }
    else
    {
        entity.LayoutJson = json;
        repoLayout.Update(entity);
    }
    await uow.SaveChangesAsync();
    return Results.Ok(new { status = "ok" });
}).RequireAuthorization();

app.MapDelete("/api/layout/{customerId:int}", async (int customerId, ClaimsPrincipal user, IRepository<UserLayout> repoLayout, IUnitOfWork uow, string? scope) =>
{
    var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    var delScope = (scope ?? "user").ToLowerInvariant();
    var targetUserId = delScope == "default" ? "__default__" : uid;
    if (delScope == "default")
    {
        var name = user.Identity?.Name ?? string.Empty;
        var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) && !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.StatusCode(403);
    }
    var entity = repoLayout.Query(x => x.UserId == targetUserId && x.CustomerId == customerId).FirstOrDefault();
    if (entity != null)
    {
        repoLayout.Remove(entity);
        await uow.SaveChangesAsync();
    }
    return Results.Ok(new { status = "ok" });
}).RequireAuthorization();

// Generate layout from tags (flow or free) and optionally save
app.MapPost("/api/layout/{customerId:int}/generate", async (
    int customerId,
    ClaimsPrincipal user,
    IRepository<FieldDefinition> repoDef,
    IRepository<UserLayout> repoLayout,
    IUnitOfWork uow,
    GenerateLayoutRequest req,
    ILocalization loc,
    HttpContext http) =>
{
    if (req.tags == null || req.tags.Length == 0)
    {
        var lang = LangHelper.GetLang(http);
        return ApiErrors.Validation(loc.T("ERR_TAGS_REQUIRED", lang));
    }

    var mode = string.Equals(req.mode, "free", StringComparison.OrdinalIgnoreCase) ? "free" : "flow";
    var defs = repoDef.Query().ToList();
    var tagSet = new HashSet<string>(req.tags.Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)), StringComparer.OrdinalIgnoreCase);

    bool HasTag(FieldDefinition d)
    {
        if (string.IsNullOrWhiteSpace(d.Tags)) return false;
        try
        {
            var tags = System.Text.Json.JsonSerializer.Deserialize<string[]>(d.Tags!) ?? Array.Empty<string>();
            return tags.Any(t => tagSet.Contains(t));
        }
        catch { return false; }
    }

    var withTag = defs.Where(HasTag).ToList();
    var others = defs.Except(withTag).ToList();
    var ordered = withTag.Concat(others).ToList();

    var items = new Dictionary<string, object?>();
    if (mode == "flow")
    {
        for (int i = 0; i < ordered.Count; i++)
        {
            var d = ordered[i];
            items[d.Key] = new { order = i, w = 6 };
        }
    }
    else // free
    {
        var columns = 12; var w = 3; var h = 1; var perRow = Math.Max(1, columns / w);
        for (int i = 0; i < ordered.Count; i++)
        {
            var d = ordered[i];
            var col = i % perRow; var row = i / perRow;
            items[d.Key] = new { x = col * w, y = row * h, w, h };
        }
    }

    var jsonObj = new { mode, items };

    if (req.save == true)
    {
        var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var scope = (req.scope ?? "user").ToLowerInvariant();
        var targetUserId = scope == "default" ? "__default__" : uid;
        if (scope == "default")
        {
            var name = user.Identity?.Name ?? string.Empty;
            var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) && !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                return Results.StatusCode(403);
        }

        var json = System.Text.Json.JsonSerializer.Serialize(jsonObj);
        var entity = repoLayout.Query(x => x.UserId == targetUserId && x.CustomerId == customerId).FirstOrDefault();
        if (entity == null)
        {
            entity = new UserLayout { UserId = targetUserId, CustomerId = customerId, LayoutJson = json };
            await repoLayout.AddAsync(entity);
        }
        else
        {
            entity.LayoutJson = json;
            repoLayout.Update(entity);
        }
        await uow.SaveChangesAsync();
    }

    return Results.Json(jsonObj);
}).RequireAuthorization();

// Admin/DB endpoints (development only)
app.MapGet("/api/admin/db/health", async (AppDbContext db) =>
{
    if (!app.Environment.IsDevelopment()) return Results.StatusCode(403);
    var provider = db.Database.ProviderName ?? "unknown";
    var canConnect = await db.Database.CanConnectAsync();
    var info = new
    {
        provider,
        canConnect,
        counts = new
        {
            customers = await db.Customers.CountAsync(),
            fieldDefinitions = await db.FieldDefinitions.CountAsync(),
            fieldValues = await db.FieldValues.CountAsync(),
            userLayouts = await db.UserLayouts.CountAsync()
        }
    };
    return Results.Json(info);
});

app.MapPost("/api/admin/db/recreate", async (AppDbContext db) =>
{
    if (!app.Environment.IsDevelopment()) return Results.StatusCode(403);
    await DatabaseInitializer.RecreateAsync(db);
    return Results.Ok(new { status = "recreated" });
});

// Get current admin user info (for setup page display)
app.MapGet("/api/setup/admin", async (UserManager<IdentityUser> um, RoleManager<IdentityRole> rm) =>
{
    var adminRole = await rm.FindByNameAsync("admin");
    if (adminRole == null) return Results.Ok(new { username = "", email = "", exists = false });
    
    // Find any user in admin role
    var adminUsers = await um.GetUsersInRoleAsync("admin");
    var admin = adminUsers.FirstOrDefault();
    
    if (admin == null) return Results.Ok(new { username = "", email = "", exists = false });
    
    return Results.Ok(new { 
        username = admin.UserName ?? "", 
        email = admin.Email ?? "", 
        exists = true 
    });
});

// First-run admin setup endpoint. Unsafe to expose broadly; allows configuring admin
// only when the default password still works, or when admin user does not exist yet.
app.MapPost("/api/setup/admin", async (
    UserManager<IdentityUser> um,
    RoleManager<IdentityRole> rm,
    SignInManager<IdentityUser> sm,
    AdminSetupDto dto,
    ILogger<Program> logger) =>
{
    logger.LogInformation("[Setup] Request to configure admin: username={username}, email={email}", dto.username, dto.email);
    if (!await rm.RoleExistsAsync("admin"))
    {
        await rm.CreateAsync(new IdentityRole("admin"));
    }
    
    // Check if there's already an admin user with the default credentials (uninitialized state)
    var existingAdmin = await um.FindByNameAsync("admin");
    IdentityUser? adminUser = null;
    
    if (existingAdmin != null)
    {
        // Check if it's the default uninitialized admin (email = admin@local and default password works)
        var isDefaultAdmin = existingAdmin.Email == "admin@local";
        var defaultPasswordWorks = false;
        if (isDefaultAdmin)
        {
            defaultPasswordWorks = (await sm.CheckPasswordSignInAsync(existingAdmin, "Admin@12345", false)).Succeeded;
        }
        
        // If it's the default admin that hasn't been customized, allow update
        if (isDefaultAdmin && defaultPasswordWorks)
        {
            adminUser = existingAdmin;
        }
        else if (isDefaultAdmin && !defaultPasswordWorks)
        {
            // Default admin exists but password was changed - still allow if email is still admin@local
            adminUser = existingAdmin;
        }
        else
        {
            // Admin exists but is customized - check if we should update it
            // For setup purposes, allow updating the default "admin" user if password is still default
            var canOverride = (await sm.CheckPasswordSignInAsync(existingAdmin, "Admin@12345", false)).Succeeded;
            if (!canOverride)
            {
                logger.LogWarning("[Setup] Override denied: existing admin not default and default password invalid");
                return Results.StatusCode(403); // Cannot update - admin already configured
            }
            adminUser = existingAdmin;
        }
    }
    
    if (adminUser == null)
    {
        // No admin user exists, create new one
        adminUser = new IdentityUser { UserName = dto.username, Email = dto.email, EmailConfirmed = true };
        var cr = await um.CreateAsync(adminUser, dto.password);
        if (!cr.Succeeded)
        {
            var errors = string.Join("; ", cr.Errors.Select(e => $"{e.Code}: {e.Description}"));
            logger.LogError("[Setup] Failed to create admin: {errors}", errors);
            return Results.BadRequest(new { error = "创建管理员用户失败", details = errors });
        }
        await um.AddToRoleAsync(adminUser, "admin");
        logger.LogInformation("[Setup] Admin created: {username}", dto.username);
        return Results.Ok(new { status = "created", username = dto.username, email = dto.email });
    }
    else
    {
        // Update existing admin user
        adminUser.UserName = dto.username;
        adminUser.Email = dto.email;
        adminUser.EmailConfirmed = true;
        // Reset lockout counters to avoid unexpected login failure
        adminUser.AccessFailedCount = 0;
        adminUser.LockoutEnabled = false;
        adminUser.LockoutEnd = null;
        var ur = await um.UpdateAsync(adminUser);
        if (!ur.Succeeded)
        {
            var errors = string.Join("; ", ur.Errors.Select(e => $"{e.Code}: {e.Description}"));
            logger.LogError("[Setup] Failed to update admin profile: {errors}", errors);
            return Results.BadRequest(new { error = "更新管理员用户失败", details = errors });
        }
        
        // Update password
        if (await um.HasPasswordAsync(adminUser))
        {
            var removeResult = await um.RemovePasswordAsync(adminUser);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join("; ", removeResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                logger.LogError("[Setup] Failed to remove old password: {errors}", errors);
                return Results.BadRequest(new { error = "移除旧密码失败", details = errors });
            }
        }

        var pr = await um.AddPasswordAsync(adminUser, dto.password);
        if (!pr.Succeeded)
        {
            var errors = string.Join("; ", pr.Errors.Select(e => $"{e.Code}: {e.Description}"));
            logger.LogError("[Setup] Failed to set new password: {errors}", errors);
            return Results.BadRequest(new { error = "设置新密码失败", details = errors });
        }
        await um.UpdateSecurityStampAsync(adminUser);
        logger.LogInformation("[Setup] Admin updated successfully: {username}", dto.username);
        return Results.Ok(new { status = "updated", username = dto.username, email = dto.email });
    }
});

// NOTE: Duplicate older endpoint removed. We already mapped GET /api/setup/admin
// above using RoleManager to return any user in the 'admin' role. Keeping only
// one mapping avoids returning "exists=false" after username changes away from
// "admin".

app.Run();

static async Task<(string accessToken, string refreshToken)> IssueTokensAsync(IConfiguration cfg, IdentityUser user, IRefreshTokenStore rts, byte[] key)
{
    var issuer = cfg["Jwt:Issuer"];
    var audience = cfg["Jwt:Audience"];
    var accessMinutes = int.TryParse(cfg["Jwt:AccessMinutes"], out var m) ? m : 60;
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
        new Claim(ClaimTypes.Email, user.Email ?? "")
    };
    var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
    var jwt = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddMinutes(accessMinutes), signingCredentials: creds);
    var access = new JwtSecurityTokenHandler().WriteToken(jwt);
    var refresh = await rts.CreateAsync(user.Id, DateTime.UtcNow.AddDays(int.TryParse(cfg["Jwt:RefreshDays"], out var d) ? d : 7));
    return (access, refresh);
}

class AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor accessor) : IdentityDbContext<IdentityUser>(options), IDataProtectionKeyContext
{
    private readonly IHttpContextAccessor _http = accessor;
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAccess> CustomerAccesses => Set<CustomerAccess>();
    public DbSet<CustomerLocalization> CustomerLocalizations => Set<CustomerLocalization>();
    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();
    public DbSet<FieldValue> FieldValues => Set<FieldValue>();
    public DbSet<UserLayout> UserLayouts => Set<UserLayout>();
    public DbSet<LocalizationResource> LocalizationResources => Set<LocalizationResource>();
    public DbSet<LocalizationLanguage> LocalizationLanguages => Set<LocalizationLanguage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        // shadow audit properties
        void Audit<T>() where T : class
        {
            b.Entity<T>().Property<DateTime>("CreatedAt");
            b.Entity<T>().Property<DateTime>("UpdatedAt");
            b.Entity<T>().Property<string>("CreatedBy").HasMaxLength(128);
            b.Entity<T>().Property<string>("UpdatedBy").HasMaxLength(128);
        }
        Audit<Customer>();
        Audit<FieldDefinition>();
        Audit<FieldValue>();
        Audit<UserLayout>();

        var isNpgsql = Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        if (isNpgsql)
        {
            b.Entity<Customer>(e => e.Property(x => x.ExtData).HasColumnType("jsonb"));
            b.Entity<FieldDefinition>(e =>
            {
                e.Property(x => x.Tags).HasColumnType("jsonb");
                e.Property(x => x.Actions).HasColumnType("jsonb");
            });
            b.Entity<FieldValue>(e => e.Property(x => x.Value).HasColumnType("jsonb"));
            b.Entity<UserLayout>(e => e.Property(x => x.LayoutJson).HasColumnType("jsonb"));
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAudit();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAudit();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAudit()
    {
        var uid = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var now = DateTime.UtcNow;
        foreach (var e in ChangeTracker.Entries())
        {
            if (e.State == EntityState.Added)
            {
                if (e.Metadata.FindProperty("CreatedAt") != null) e.Property("CreatedAt").CurrentValue = now;
                if (e.Metadata.FindProperty("CreatedBy") != null) e.Property("CreatedBy").CurrentValue = uid;
            }
            if (e.State == EntityState.Added || e.State == EntityState.Modified)
            {
                if (e.Metadata.FindProperty("UpdatedAt") != null) e.Property("UpdatedAt").CurrentValue = now;
                if (e.Metadata.FindProperty("UpdatedBy") != null) e.Property("UpdatedBy").CurrentValue = uid;
            }
        }
    }
}

record RegisterDto(string username, string password, string email);
record LoginDto(string username, string password);
record RefreshDto(string refreshToken);
record LogoutDto(string refreshToken);
public record UpdateCustomerDto(List<FieldDto> fields, int? expectedVersion);
public record FieldDto(string key, object value);
public record GenerateLayoutRequest(string[] tags, string? mode, bool? save, string? scope);
public record AccessUpsert(string userId, bool canEdit);
public record AdminSetupDto(string username, string email, string password);
public record AdminResetPasswordDto(string password);

class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
}

interface IRefreshTokenStore
{
    Task<string> CreateAsync(string userId, DateTime expiresAt);
    Task<RefreshToken?> ValidateAsync(string token);
    Task RevokeAsync(string token);
}

class EfRefreshTokenStore(AppDbContext db) : IRefreshTokenStore
{
    public async Task<string> CreateAsync(string userId, DateTime expiresAt)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        db.RefreshTokens.Add(new RefreshToken { UserId = userId, Token = token, ExpiresAt = expiresAt });
        await db.SaveChangesAsync();
        return token;
    }

    public async Task<RefreshToken?> ValidateAsync(string token)
    {
        var rt = await db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token);
        if (rt == null) return null;
        if (rt.RevokedAt != null || rt.ExpiresAt <= DateTime.UtcNow) return null;
        return rt;
    }

    public async Task RevokeAsync(string token)
    {
        var rt = await db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token);
        if (rt != null)
        {
            rt.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }
}

class ConsoleEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string body)
    {
        Console.WriteLine($"[EMAIL] To:{to} Subject:{subject} Body:{body}");
        return Task.CompletedTask;
    }
}

interface IEmailSender { Task SendAsync(string to, string subject, string body); }

// Enable WebApplicationFactory<Program> from test project
public partial class Program { }
