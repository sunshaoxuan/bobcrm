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
using BobCrm.Api.Domain;

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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IValidationPipeline, ValidationPipeline>();
builder.Services.AddScoped<IBusinessValidator<UpdateCustomerDto>, UpdateCustomerBusinessValidator>();
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

// Auto-migrate database on startup (both providers), with EnsureCreated fallback and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try { db.Database.Migrate(); }
    catch { db.Database.EnsureCreated(); }

    if (!db.Customers.Any())
    {
        db.Customers.AddRange(
            new Customer { Code = "C001", Name = "客户A", Version = 1 },
            new Customer { Code = "C002", Name = "客户B", Version = 1 }
        );
        if (!db.FieldDefinitions.Any())
        {
            db.FieldDefinitions.Add(new FieldDefinition
            {
                Key = "email",
                DisplayName = "邮箱",
                DataType = "email",
                Tags = "[\"常用\"]",
                Actions = "[{\"icon\":\"mail\",\"title\":\"发邮件\",\"type\":\"click\",\"action\":\"mailto\"}]"
            });
        }
        db.SaveChanges();
    }
    // Postgres JSONB indexes
    var isNpgsql = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
    if (isNpgsql)
    {
        try
        {
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS idx_fieldvalues_value_gin ON \"FieldValues\" USING GIN (\"Value\");");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS idx_fielddefinitions_tags_gin ON \"FieldDefinitions\" USING GIN (\"Tags\");");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS idx_fielddefinitions_actions_gin ON \"FieldDefinitions\" USING GIN (\"Actions\");");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS idx_userlayouts_layoutjson_gin ON \"UserLayouts\" USING GIN (\"LayoutJson\");");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS idx_fieldvalues_customer_field ON \"FieldValues\" (\"CustomerId\", \"FieldDefinitionId\");");
        }
        catch { /* best-effort */ }
    }
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

app.MapGet("/api/fields", (IFieldQueries q) => Results.Json(q.GetDefinitions()))
    .RequireAuthorization();

app.MapPut("/api/customers/{id:int}", async (
    int id,
    UpdateCustomerDto dto,
    IRepository<Customer> repoCustomer,
    IRepository<FieldDefinition> repoDef,
    IRepository<FieldValue> repoVal,
    IUnitOfWork uow,
    IValidationPipeline pipe,
    HttpContext http) =>
{
    var vr = await pipe.ValidateAsync(dto, http);
    if (vr is not null) return vr;

    var c = repoCustomer.Query(x => x.Id == id).FirstOrDefault();
    if (c == null) return Results.NotFound();

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

app.MapGet("/api/layout/{customerId:int}", (int customerId, ClaimsPrincipal user, ILayoutQueries q) =>
{
    var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    return Results.Json(q.GetUserLayout(uid, customerId));
}).RequireAuthorization();

app.MapPost("/api/layout/{customerId:int}", async (int customerId, ClaimsPrincipal user, IRepository<UserLayout> repoLayout, IUnitOfWork uow, System.Text.Json.JsonElement layout) =>
{
    var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    var entity = repoLayout.Query(x => x.UserId == uid && x.CustomerId == customerId).FirstOrDefault();
    var json = layout.GetRawText();
    if (string.IsNullOrWhiteSpace(json))
        return ApiErrors.Validation("layout body required");
    if (entity == null)
    {
        entity = new UserLayout { UserId = uid, CustomerId = customerId, LayoutJson = json };
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

class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options), IDataProtectionKeyContext
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAccess> CustomerAccesses => Set<CustomerAccess>();
    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();
    public DbSet<FieldValue> FieldValues => Set<FieldValue>();
    public DbSet<UserLayout> UserLayouts => Set<UserLayout>();
    public DbSet<LocalizationResource> LocalizationResources => Set<LocalizationResource>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
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
}

record RegisterDto(string username, string password, string email);
record LoginDto(string username, string password);
record RefreshDto(string refreshToken);
record LogoutDto(string refreshToken);
public record UpdateCustomerDto(List<FieldDto> fields, int? expectedVersion);
public record FieldDto(string key, object value);

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
