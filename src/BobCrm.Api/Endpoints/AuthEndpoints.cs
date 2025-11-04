using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 认证相关端点
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("认证")
            .WithOpenApi();

        // 用户注册
        group.MapPost("/register", async (
            UserManager<IdentityUser> um,
            IEmailSender email,
            RegisterDto dto,
            LinkGenerator links,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            logger.LogInformation("[Auth] User registration attempt: username={Username}, email={Email}", dto.Username, dto.Email);
            
            var user = new IdentityUser { UserName = dto.Username, Email = dto.Email, EmailConfirmed = false };
            var result = await um.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                logger.LogWarning("[Auth] Registration failed for {Username}: {Errors}", dto.Username, string.Join(", ", result.Errors.Select(e => e.Description)));
                return Results.BadRequest(result.Errors);
            }
            
            var code = await um.GenerateEmailConfirmationTokenAsync(user);
            var url = links.GetUriByName(http, "Activate", new { userId = user.Id, code });
            await email.SendAsync(dto.Email, "Activate your account", $"Click to activate: {url}");
            
            logger.LogInformation("[Auth] Registration successful for {Username}", dto.Username);
            return Results.Ok(ApiResponseExtensions.SuccessResponse("注册成功，请查收邮件激活账户"));
        })
        .WithName("Register")
        .WithSummary("用户注册")
        .WithDescription("注册新用户账号，需要邮箱激活");

        // 激活账户
        group.MapGet("/activate", async (
            UserManager<IdentityUser> um,
            ILogger<Program> logger,
            string userId,
            string code) =>
        {
            logger.LogInformation("[Auth] Account activation attempt: userId={UserId}", userId);
            
            var user = await um.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("[Auth] Activation failed: user not found, userId={UserId}", userId);
                return Results.NotFound();
            }
            
            var res = await um.ConfirmEmailAsync(user, code);
            if (res.Succeeded)
            {
                logger.LogInformation("[Auth] Account activated successfully: username={Username}", user.UserName);
                return Results.Ok(ApiResponseExtensions.SuccessResponse("账户激活成功"));
            }
            
            logger.LogWarning("[Auth] Activation failed for {Username}: {Errors}", user.UserName, string.Join(", ", res.Errors.Select(e => e.Description)));
            return Results.BadRequest(res.Errors);
        })
        .WithName("Activate")
        .WithSummary("激活账户")
        .WithDescription("通过邮件链接激活用户账户");

        // 用户登录
        group.MapPost("/login", async (
            UserManager<IdentityUser> um,
            SignInManager<IdentityUser> sm,
            IRefreshTokenStore rts,
            IConfiguration cfg,
            LoginDto dto,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            logger.LogInformation("[Auth] Login attempt: usernameOrEmail={Username}, remote={Ip}", dto.Username ?? "(null)", http.Connection.RemoteIpAddress);

            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                logger.LogWarning("[Auth] Invalid login request - username is empty");
                return Results.BadRequest(new { error = "Username is required" });
            }

            var user = await um.FindByNameAsync(dto.Username) ?? await um.FindByEmailAsync(dto.Username);
            if (user == null)
            {
                logger.LogWarning("[Auth] User not found: username={Username}", dto.Username);
                return Results.Json(new { error = "Invalid username or password" }, statusCode: 401);
            }

            logger.LogDebug("[Auth] Found user: username={UserName}, email={Email}, emailConfirmed={EmailConfirmed}", 
                user.UserName, user.Email, user.EmailConfirmed);

            if (!user.EmailConfirmed)
            {
                var lang = LangHelper.GetLang(http);
                logger.LogWarning("[Auth] Email not confirmed for user {UserName}", user.UserName);
                return Results.BadRequest(new { error = loc.T("ERR_EMAIL_NOT_CONFIRMED", lang) });
            }

            var validPassword = await um.CheckPasswordAsync(user, dto.Password);
            if (!validPassword)
            {
                logger.LogWarning("[Auth] Password check failed for user {UserName}", user.UserName);
                return Results.Json(new { error = "Invalid username or password" }, statusCode: 401);
            }

            var key = System.Text.Encoding.UTF8.GetBytes(cfg["Jwt:Key"] ?? "dev-secret-change-in-prod-1234567890");
            var tokens = await IssueTokensAsync(cfg, user, rts, key);
            logger.LogInformation("[Auth] Login success for user {UserName}", user.UserName);
            return Results.Json(new
            {
                accessToken = tokens.accessToken,
                refreshToken = tokens.refreshToken,
                user = new { id = user.Id, username = user.UserName, role = "user" }
            });
        })
        .WithName("Login")
        .WithSummary("用户登录")
        .WithDescription("使用用户名或邮箱登录，返回访问令牌和刷新令牌");

        // 刷新令牌
        group.MapPost("/refresh", async (
            IConfiguration cfg,
            IRefreshTokenStore rts,
            UserManager<IdentityUser> um,
            ILogger<Program> logger,
            HttpContext http,
            RefreshDto dto) =>
        {
            var clientIp = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            logger.LogInformation("[Auth] Refresh token request from {Ip}", clientIp);
            
            var stored = await rts.ValidateAsync(dto.RefreshToken);
            if (stored == null)
            {
                logger.LogWarning("[Auth] Invalid or revoked refresh token from {Ip} - token may have been used already", clientIp);
                return Results.Unauthorized();
            }
            
            var user = await um.FindByIdAsync(stored.UserId);
            if (user == null)
            {
                logger.LogWarning("[Auth] User not found for refresh token, userId={UserId}, ip={Ip}", stored.UserId, clientIp);
                return Results.Unauthorized();
            }
            
            logger.LogInformation("[Auth] Revoking old refresh token for user {UserName}", user.UserName);
            await rts.RevokeAsync(dto.RefreshToken);
            
            var key = System.Text.Encoding.UTF8.GetBytes(cfg["Jwt:Key"] ?? "dev-secret-change-in-prod-1234567890");
            var tokens = await IssueTokensAsync(cfg, user, rts, key);
            
            logger.LogInformation("[Auth] Token refreshed successfully for user {UserName} from {Ip}", user.UserName, clientIp);
            return Results.Json(new { accessToken = tokens.accessToken, refreshToken = tokens.refreshToken });
        })
        .WithName("RefreshToken")
        .WithSummary("刷新令牌")
        .WithDescription("使用刷新令牌获取新的访问令牌");

        // 登出
        group.MapPost("/logout", async (
            IRefreshTokenStore rts, 
            ILogger<Program> logger,
            LogoutDto dto) =>
        {
            if (!string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                await rts.RevokeAsync(dto.RefreshToken);
                logger.LogInformation("[Auth] User logged out");
            }
            return Results.Ok(ApiResponseExtensions.SuccessResponse("登出成功"));
        })
        .RequireAuthorization()
        .WithName("Logout")
        .WithSummary("用户登出")
        .WithDescription("撤销刷新令牌，登出用户");

        // 会话验证
        group.MapGet("/session", (ClaimsPrincipal user) =>
        {
            if (user?.Identity?.IsAuthenticated == true)
                return Results.Ok(new
                {
                    valid = true,
                    user = new { id = user.FindFirstValue(ClaimTypes.NameIdentifier), username = user.Identity!.Name }
                });
            return Results.Ok(new { valid = false });
        })
        .RequireAuthorization()
        .WithName("Session")
        .WithSummary("会话验证")
        .WithDescription("验证当前用户会话是否有效");

        // 获取当前用户信息
        group.MapGet("/me", async (ClaimsPrincipal user, UserManager<IdentityUser> um) =>
        {
            if (user?.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(uid))
                return Results.Unauthorized();

            var identityUser = await um.FindByIdAsync(uid);
            if (identityUser == null)
                return Results.Unauthorized();

            var roles = await um.GetRolesAsync(identityUser);

            return Results.Ok(new
            {
                id = identityUser.Id,
                username = identityUser.UserName,
                email = identityUser.Email,
                emailConfirmed = identityUser.EmailConfirmed,
                roles = roles.ToList()
            });
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("获取当前用户信息")
        .WithDescription("返回当前已认证用户的详细信息，包括ID、用户名、邮箱和角色");

        return app;
    }

    /// <summary>
    /// 生成 JWT 访问令牌和刷新令牌
    /// </summary>
    private static async Task<(string accessToken, string refreshToken)> IssueTokensAsync(
        IConfiguration cfg,
        IdentityUser user,
        IRefreshTokenStore rts,
        byte[] key)
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
}
