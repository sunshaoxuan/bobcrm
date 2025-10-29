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
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

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

app.MapPost("/api/auth/login", async (UserManager<IdentityUser> um, SignInManager<IdentityUser> sm, IRefreshTokenStore rts, IConfiguration cfg, LoginDto dto) =>
{
    var user = await um.FindByNameAsync(dto.username) ?? await um.FindByEmailAsync(dto.username);
    if (user == null) return Results.Unauthorized();
    if (!user.EmailConfirmed) return Results.BadRequest(new { error = "Email not confirmed" });
    var pass = await sm.CheckPasswordSignInAsync(user, dto.password, false);
    if (!pass.Succeeded) return Results.Unauthorized();
    var tokens = await IssueTokensAsync(cfg, user, rts, key);
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
    lang = (lang ?? "zh").ToLowerInvariant();
    var query = db.LocalizationResources.AsNoTracking();
    var dict = new Dictionary<string, string>();
    foreach (var r in query)
    {
        var val = lang switch
        {
            "ja" => r.JA ?? r.ZH ?? r.EN ?? r.Key,
            "en" => r.EN ?? r.ZH ?? r.JA ?? r.Key,
            _ => r.ZH ?? r.JA ?? r.EN ?? r.Key
        };
        dict[r.Key] = val;
    }
    return Results.Json(dict);
}).RequireAuthorization();

// languages list
app.MapGet("/api/i18n/languages", (AppDbContext db) =>
{
    // static list for now; could be a table later
    var langs = new[] { new { code = "ja", name = "日本語" }, new { code = "zh", name = "中文" }, new { code = "en", name = "English" } };
    return Results.Json(langs);
}).RequireAuthorization();

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
    HttpContext http) =>
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
        return ApiErrors.Concurrency("version mismatch");

    var defs = repoDef.Query().ToDictionary(d => d.Key, d => d);
    foreach (var f in dto.fields)
    {
        if (string.IsNullOrWhiteSpace(f.key))
            return ApiErrors.Validation("field key required");
        if (!defs.TryGetValue(f.key, out var def))
            return ApiErrors.Business($"unknown field: {f.key}");

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

app.MapPost("/api/layout/{customerId:int}", async (int customerId, ClaimsPrincipal user, IRepository<UserLayout> repoLayout, IUnitOfWork uow, System.Text.Json.JsonElement layout, HttpContext http, string? scope) =>
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
        return ApiErrors.Validation("layout body required");
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
    GenerateLayoutRequest req) =>
{
    if (req.tags == null || req.tags.Length == 0)
        return ApiErrors.Validation("tags required");

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

// First-run admin setup endpoint. Unsafe to expose broadly; allows configuring admin
// only when the default password still works, or when admin user does not exist yet.
app.MapPost("/api/setup/admin", async (
    UserManager<IdentityUser> um,
    RoleManager<IdentityRole> rm,
    SignInManager<IdentityUser> sm,
    AdminSetupDto dto) =>
{
    if (!await rm.RoleExistsAsync("admin"))
    {
        await rm.CreateAsync(new IdentityRole("admin"));
    }
    var admin = await um.FindByNameAsync("admin");
    if (admin == null)
    {
        admin = new IdentityUser { UserName = dto.username, Email = dto.email, EmailConfirmed = true };
        var cr = await um.CreateAsync(admin, dto.password);
        if (!cr.Succeeded) return Results.BadRequest(cr.Errors);
        await um.AddToRoleAsync(admin, "admin");
        return Results.Ok(new { status = "created" });
    }
    else
    {
        // Only allow update if default password still valid (considered uninitialized)
        var canOverride = (await sm.CheckPasswordSignInAsync(admin, "Admin@12345", false)).Succeeded;
        if (!canOverride)
            return Results.StatusCode(403);

        admin.UserName = dto.username;
        admin.Email = dto.email;
        admin.EmailConfirmed = true;
        var ur = await um.UpdateAsync(admin);
        if (!ur.Succeeded) return Results.BadRequest(ur.Errors);
        if (await um.HasPasswordAsync(admin))
            await um.RemovePasswordAsync(admin);
        var pr = await um.AddPasswordAsync(admin, dto.password);
        if (!pr.Succeeded) return Results.BadRequest(pr.Errors);
        return Results.Ok(new { status = "updated" });
    }
});

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


