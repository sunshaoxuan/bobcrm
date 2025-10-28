using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// DbContext (SQLite default; D3 will add PostgreSQL)
var dbProvider = builder.Configuration["Db:Provider"] ?? "sqlite";
var conn = builder.Configuration.GetConnectionString("Default") ?? "Data Source=./data/app.db";
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (dbProvider == "sqlite") opt.UseSqlite(conn);
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

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

class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
}

record RegisterDto(string username, string password, string email);
record LoginDto(string username, string password);
record RefreshDto(string refreshToken);
record LogoutDto(string refreshToken);

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
