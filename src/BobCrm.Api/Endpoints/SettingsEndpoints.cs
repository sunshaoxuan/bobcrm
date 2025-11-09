using System.Security.Claims;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Services.Settings;

namespace BobCrm.Api.Endpoints;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var settingsGroup = app.MapGroup("/api/settings")
            .WithTags("Settings")
            .WithOpenApi();

        var systemGroup = settingsGroup.MapGroup("/system")
            .RequireAuthorization(policy => policy.RequireRole("admin"));

        systemGroup.MapGet("/", async (SettingsService svc) =>
        {
            var system = await svc.GetSystemSettingsAsync();
            return Results.Ok(new SystemSettingsDto(
                system.CompanyName,
                system.DefaultTheme,
                system.DefaultPrimaryColor,
                system.DefaultLanguage,
                system.DefaultHomeRoute,
                system.DefaultNavMode,
                system.TimeZoneId,
                system.AllowSelfRegistration));
        })
        .WithName("GetSystemSettings")
        .WithSummary("Get system settings");

        systemGroup.MapPut("/", async (UpdateSystemSettingsRequest request, SettingsService svc) =>
        {
            var updated = await svc.UpdateSystemSettingsAsync(request);
            return Results.Ok(new SystemSettingsDto(
                updated.CompanyName,
                updated.DefaultTheme,
                updated.DefaultPrimaryColor,
                updated.DefaultLanguage,
                updated.DefaultHomeRoute,
                updated.DefaultNavMode,
                updated.TimeZoneId,
                updated.AllowSelfRegistration));
        })
        .WithName("UpdateSystemSettings")
        .WithSummary("Update system settings");

        var userGroup = settingsGroup.MapGroup("/user")
            .RequireAuthorization();

        userGroup.MapGet("/", async (ClaimsPrincipal user, SettingsService svc) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid))
            {
                return Results.Unauthorized();
            }

            var snapshot = await svc.GetUserSettingsAsync(uid);
            return Results.Ok(snapshot);
        })
        .WithName("GetUserSettings")
        .WithSummary("Get user settings with defaults");

        userGroup.MapPut("/", async (UpdateUserSettingsRequest request, ClaimsPrincipal user, SettingsService svc) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid))
            {
                return Results.Unauthorized();
            }

            var snapshot = await svc.UpdateUserSettingsAsync(uid, request);
            return Results.Ok(snapshot);
        })
        .WithName("UpdateUserSettings")
        .WithSummary("Update user-specific settings");

        return app;
    }
}
