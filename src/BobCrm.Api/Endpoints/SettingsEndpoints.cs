using System.Security.Claims;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Services.Settings;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Core.DomainCommon;

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
            return Results.Ok(new SuccessResponse<SystemSettingsDto>(new SystemSettingsDto(
                system.CompanyName,
                system.DefaultTheme,
                system.DefaultPrimaryColor,
                system.DefaultLanguage,
                system.DefaultHomeRoute,
                system.DefaultNavMode,
                system.TimeZoneId,
                system.AllowSelfRegistration,
                system.SmtpHost,
                system.SmtpPort,
                system.SmtpUsername,
                system.SmtpEnableSsl,
                system.SmtpFromAddress,
                system.SmtpDisplayName,
                !string.IsNullOrWhiteSpace(system.SmtpPasswordEncrypted))));
        })
        .WithName("GetSystemSettings")
        .WithSummary("Get system settings")
        .Produces<SuccessResponse<SystemSettingsDto>>(StatusCodes.Status200OK);

        systemGroup.MapPut("/", async (UpdateSystemSettingsRequest request, SettingsService svc) =>
        {
            var updated = await svc.UpdateSystemSettingsAsync(request);
            return Results.Ok(new SuccessResponse<SystemSettingsDto>(new SystemSettingsDto(
                updated.CompanyName,
                updated.DefaultTheme,
                updated.DefaultPrimaryColor,
                updated.DefaultLanguage,
                updated.DefaultHomeRoute,
                updated.DefaultNavMode,
                updated.TimeZoneId,
                updated.AllowSelfRegistration,
                updated.SmtpHost,
                updated.SmtpPort,
                updated.SmtpUsername,
                updated.SmtpEnableSsl,
                updated.SmtpFromAddress,
                updated.SmtpDisplayName,
                !string.IsNullOrWhiteSpace(updated.SmtpPasswordEncrypted))));
        })
        .WithName("UpdateSystemSettings")
        .WithSummary("Update system settings")
        .Produces<SuccessResponse<SystemSettingsDto>>(StatusCodes.Status200OK);

        systemGroup.MapPost("/smtp/test", async (
            SendTestEmailRequest request,
            SettingsService svc,
            IEmailSender email) =>
        {
            if (string.IsNullOrWhiteSpace(request.To))
            {
                return Results.BadRequest(new ErrorResponse("Recipient is required", "SMTP_TEST_RECIPIENT_REQUIRED"));
            }

            var system = await svc.GetSystemSettingsAsync();
            if (string.IsNullOrWhiteSpace(system.SmtpHost) || string.IsNullOrWhiteSpace(system.SmtpFromAddress))
            {
                return Results.BadRequest(new ErrorResponse("SMTP not configured", "SMTP_NOT_CONFIGURED"));
            }

            try
            {
                await email.SendAsync(request.To.Trim(), "SMTP Test", $"This is a test email from {system.CompanyName}.");
                return Results.Ok(new SuccessResponse("Sent"));
            }
            catch (Exception ex)
            {
                throw new DomainException(ex.Message, "SMTP_TEST_FAILED");
            }
        })
        .WithName("SendSmtpTestEmail")
        .WithSummary("Send a test email using current SMTP settings")
        .Produces<SuccessResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

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
            return Results.Ok(new SuccessResponse<UserSettingsSnapshotDto>(snapshot));
        })
        .WithName("GetUserSettings")
        .WithSummary("Get user settings with defaults")
        .Produces<SuccessResponse<UserSettingsSnapshotDto>>(StatusCodes.Status200OK);

        userGroup.MapPut("/", async (UpdateUserSettingsRequest request, ClaimsPrincipal user, SettingsService svc) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid))
            {
                return Results.Unauthorized();
            }

            var snapshot = await svc.UpdateUserSettingsAsync(uid, request);
            return Results.Ok(new SuccessResponse<UserSettingsSnapshotDto>(snapshot));
        })
        .WithName("UpdateUserSettings")
        .WithSummary("Update user-specific settings")
        .Produces<SuccessResponse<UserSettingsSnapshotDto>>(StatusCodes.Status200OK);

        return app;
    }
}
