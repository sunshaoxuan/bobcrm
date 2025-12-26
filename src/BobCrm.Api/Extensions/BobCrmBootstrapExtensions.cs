using System.Text;
using BobCrm.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace BobCrm.Api.Extensions;

public static class BobCrmBootstrapExtensions
{
    public static IServiceCollection AddBobCrmDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dbProvider = configuration["Db:Provider"] ?? "sqlite";
        var conn = configuration.GetConnectionString("Default") ?? "Data Source=./data/app.db";

        services.AddDbContext<AppDbContext>(opt =>
        {
            if (dbProvider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
            {
                opt.UseNpgsql(conn, npg =>
                {
                    npg.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });
                AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            }
            else
            {
                opt.UseSqlite(conn);
            }
        });

        return services;
    }

    public static IServiceCollection AddBobCrmAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        BobCrmConfigurationValidation.ValidateJwt(configuration, environment);

        var jwtKey = configuration["Jwt:Key"];
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        var keyBytes = Encoding.UTF8.GetBytes(jwtKey ?? "dev-secret-change-in-prod-1234567890");
        var validateIssuer = !string.IsNullOrWhiteSpace(issuer);
        var validateAudience = !string.IsNullOrWhiteSpace(audience);

        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ValidateIssuer = validateIssuer,
                ValidateAudience = validateAudience,
                ValidIssuer = issuer,
                ValidAudience = audience,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddBobCrmCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (environment.IsDevelopment())
                {
                    policy.SetIsOriginAllowed(_ => true)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                    return;
                }

                var allowedOrigins = BobCrmConfigurationValidation.GetCorsAllowedOrigins(configuration, environment);
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }
}
