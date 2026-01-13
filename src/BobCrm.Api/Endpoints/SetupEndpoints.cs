using Microsoft.AspNetCore.Identity;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Responses.Setup;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 系统初始化设置相关端点
/// </summary>
public static class SetupEndpoints
{
    public static IEndpointRouteBuilder MapSetupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/setup")
            .WithTags("系统设置")
            .WithOpenApi();

        // 获取当前管理员信息
        group.MapGet("/admin", async (
            UserManager<IdentityUser> um,
            RoleManager<IdentityRole> rm,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            logger.LogDebug("[Setup] Admin info requested");
            var lang = LangHelper.GetLang(http);
            
            var adminRole = await rm.FindByNameAsync("admin");
            if (adminRole == null)
            {
                logger.LogInformation("[Setup] Admin role does not exist");
                return Results.Ok(new SuccessResponse<AdminInfoDto>(new AdminInfoDto { Exists = false }));
            }

            // 查找管理员角色中的任何用户
            var adminUsers = await um.GetUsersInRoleAsync("admin");
            var admin = adminUsers.FirstOrDefault();

            if (admin == null)
            {
                logger.LogInformation("[Setup] No admin user found");
                return Results.Ok(new SuccessResponse<AdminInfoDto>(new AdminInfoDto { Exists = false }));
            }

            logger.LogInformation("[Setup] Admin user exists: {Username}", admin.UserName);
            return Results.Ok(new SuccessResponse<AdminInfoDto>(new AdminInfoDto
            {
                Username = admin.UserName ?? string.Empty,
                Email = admin.Email ?? string.Empty,
                Exists = true
            }));
        })
        .WithName("GetAdminInfo")
        .WithSummary("获取管理员信息")
        .WithDescription("检查系统是否已初始化管理员账户")
        .Produces<SuccessResponse<AdminInfoDto>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // 首次运行管理员设置
        group.MapPost("/admin", async (
            UserManager<IdentityUser> um,
            RoleManager<IdentityRole> rm,
            SignInManager<IdentityUser> sm,
            AdminSetupDto dto,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            logger.LogInformation("[Setup] Admin configuration request: username={Username}, email={Email}", dto.Username, dto.Email);
            
            if (!await rm.RoleExistsAsync("admin"))
            {
                await rm.CreateAsync(new IdentityRole("admin"));
                logger.LogInformation("[Setup] Admin role created");
            }

            // 检查是否已存在管理员用户（优先 username=admin，其次取 admin 角色中的任意用户，避免重复创建导致 UNIQUE 冲突）
            var existingAdmin = await um.FindByNameAsync("admin");
            if (existingAdmin == null)
            {
                var adminsInRole = await um.GetUsersInRoleAsync("admin");
                existingAdmin = adminsInRole.FirstOrDefault();
            }
            IdentityUser? adminUser = null;

            if (existingAdmin != null)
            {
                // 检查是否为未初始化的默认管理员（email = admin@local 且默认密码有效）
                var isDefaultAdmin = existingAdmin.Email == "admin@local";
                var defaultPasswordWorks = false;
                if (isDefaultAdmin)
                {
                    defaultPasswordWorks = (await sm.CheckPasswordSignInAsync(existingAdmin, "Admin@12345", false)).Succeeded;
                }

                // 如果是默认管理员且未自定义，允许更新
                if (isDefaultAdmin && defaultPasswordWorks)
                {
                    adminUser = existingAdmin;
                    logger.LogInformation("[Setup] Found default admin, will update");
                }
                else if (isDefaultAdmin && !defaultPasswordWorks)
                {
                    // 默认管理员存在但密码已更改 - 如果邮箱仍是 admin@local 则允许更新
                    adminUser = existingAdmin;
                    logger.LogInformation("[Setup] Found modified default admin, will update");
                }
                else
                {
                    // 管理员存在但已自定义 - 检查是否可以覆盖
                    var canOverride = (await sm.CheckPasswordSignInAsync(existingAdmin, "Admin@12345", false)).Succeeded;
                    if (!canOverride)
                    {
                        logger.LogWarning("[Setup] Override denied: existing admin not default and default password invalid");
                        return Results.Json(new ErrorResponse(loc.T("MSG_SETTINGS_ADMIN_ONLY", lang), "FORBIDDEN"), statusCode: StatusCodes.Status403Forbidden);
                    }
                    adminUser = existingAdmin;
                    logger.LogInformation("[Setup] Found customized admin with default password, will update");
                }
            }

            if (adminUser == null)
            {
                // 不存在管理员用户，创建新的
                adminUser = new IdentityUser { UserName = dto.Username, Email = dto.Email, EmailConfirmed = true };
                var cr = await um.CreateAsync(adminUser, dto.Password);
                if (!cr.Succeeded)
                {
                    var errors = string.Join("; ", cr.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    logger.LogError("[Setup] Failed to create admin: {Errors}", errors);
                    return Results.BadRequest(new ErrorResponse(
                        loc.T("ERR_SETUP_CREATE_ADMIN_FAILED", lang),
                        new Dictionary<string, string[]> { { "errors", new[] { errors } } },
                        "SETUP_CREATE_FAILED"));
                }
                await um.AddToRoleAsync(adminUser, "admin");
                logger.LogInformation("[Setup] Admin created successfully: {Username}", dto.Username);
                return Results.Ok(ApiResponseExtensions.SuccessResponse(loc.T("MSG_SETUP_ADMIN_CREATED", lang)));
            }
            else
            {
                // 更新现有管理员用户
                adminUser.UserName = dto.Username;
                adminUser.Email = dto.Email;
                adminUser.EmailConfirmed = true;
                // 重置锁定计数器以避免意外的登录失败
                adminUser.AccessFailedCount = 0;
                adminUser.LockoutEnabled = false;
                adminUser.LockoutEnd = null;
                var ur = await um.UpdateAsync(adminUser);
                if (!ur.Succeeded)
                {
                    var errors = string.Join("; ", ur.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    logger.LogError("[Setup] Failed to update admin profile: {Errors}", errors);
                    return Results.BadRequest(new ErrorResponse(
                        loc.T("ERR_SETUP_UPDATE_ADMIN_FAILED", lang),
                        new Dictionary<string, string[]> { { "errors", new[] { errors } } },
                        "SETUP_UPDATE_FAILED"));
                }

                // 更新密码
                if (await um.HasPasswordAsync(adminUser))
                {
                    var removeResult = await um.RemovePasswordAsync(adminUser);
                    if (!removeResult.Succeeded)
                    {
                        var errors = string.Join("; ", removeResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                        logger.LogError("[Setup] Failed to remove old password: {Errors}", errors);
                        return Results.BadRequest(new ErrorResponse(
                            loc.T("ERR_SETUP_REMOVE_PASSWORD_FAILED", lang),
                            new Dictionary<string, string[]> { { "errors", new[] { errors } } },
                            "SETUP_UPDATE_FAILED"));
                    }
                }

                var pr = await um.AddPasswordAsync(adminUser, dto.Password);
                if (!pr.Succeeded)
                {
                    var errors = string.Join("; ", pr.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    logger.LogError("[Setup] Failed to set new password: {Errors}", errors);
                    return Results.BadRequest(new ErrorResponse(
                        loc.T("ERR_SETUP_SET_PASSWORD_FAILED", lang),
                        new Dictionary<string, string[]> { { "errors", new[] { errors } } },
                        "SETUP_UPDATE_FAILED"));
                }
                await um.UpdateSecurityStampAsync(adminUser);
                if (!await um.IsInRoleAsync(adminUser, "admin"))
                {
                    await um.AddToRoleAsync(adminUser, "admin");
                }
                logger.LogInformation("[Setup] Admin updated successfully: {Username}", dto.Username);
                return Results.Ok(ApiResponseExtensions.SuccessResponse(loc.T("MSG_SETUP_ADMIN_UPDATED", lang)));
            }
        })
        .WithName("SetupAdmin")
        .WithSummary("配置管理员账户")
        .WithDescription("创建或更新系统管理员账户")
        .Produces<SuccessResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .AllowAnonymous();

        return app;
    }
}
