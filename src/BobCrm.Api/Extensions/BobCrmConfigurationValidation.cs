using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BobCrm.Api.Extensions;

public static class BobCrmConfigurationValidation
{
    public static void ValidateJwt(IConfiguration configuration, IHostEnvironment environment)
    {
        var jwtKey = configuration["Jwt:Key"];
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        if (environment.IsDevelopment())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("Jwt:Key is required in non-development environments.");
        }

        if (jwtKey.Trim().Length < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters in non-development environments.");
        }

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer is required in non-development environments.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Jwt:Audience is required in non-development environments.");
        }
    }

    public static string[] GetCorsAllowedOrigins(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return Array.Empty<string>();
        }

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                             ?? configuration["Cors:AllowedOrigins"]?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                             ?? Array.Empty<string>();

        if (allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must be configured in non-development environments.");
        }

        return allowedOrigins;
    }
}
