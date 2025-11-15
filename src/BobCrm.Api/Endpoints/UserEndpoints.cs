using System.Security.Claims;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .RequireAuthorization()
            .WithTags("用户档案")
            .WithOpenApi();
        group.RequireFunction("BAS.AUTH.USERS");

        group.MapGet("", async (
            UserManager<IdentityUser> um,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var users = await um.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync(ct);
            var assignments = await db.RoleAssignments
                .AsNoTracking()
                .Include(a => a.Role)
                .ToListAsync(ct);
            var lookup = assignments
                .GroupBy(a => a.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var list = users.Select(u => ToSummary(u, lookup.TryGetValue(u.Id, out var roles) ? roles : new List<RoleAssignment>()));
            return Results.Ok(list);
        });

        group.MapGet("/{id}", async (
            string id,
            UserManager<IdentityUser> um,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var user = await um.FindByIdAsync(id);
            if (user == null) return Results.NotFound(new { error = "User not found" });

            var roles = await db.RoleAssignments
                .AsNoTracking()
                .Include(a => a.Role)
                .Where(a => a.UserId == id)
                .ToListAsync(ct);

            var detail = ToDetail(user, roles);
            return Results.Ok(detail);
        });

        group.MapPost("", async (
            CreateUserRequest request,
            UserManager<IdentityUser> um,
            AppDbContext db,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            var userName = request.UserName.Trim();
            var email = request.Email.Trim();

            if (await um.FindByNameAsync(userName) != null)
            {
                return Results.BadRequest(new { error = "Username already exists." });
            }

            var user = new IdentityUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = request.EmailConfirmed
            };

            var create = await um.CreateAsync(user);
            if (!create.Succeeded)
            {
                return Results.BadRequest(new { error = string.Join("; ", create.Errors.Select(e => e.Description)) });
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var addPwd = await um.AddPasswordAsync(user, request.Password);
                if (!addPwd.Succeeded)
                {
                    await um.DeleteAsync(user);
                    return Results.BadRequest(new { error = string.Join("; ", addPwd.Errors.Select(e => e.Description)) });
                }
            }

            await UpdateAssignmentsAsync(user.Id, request.Roles, db, ct);
            logger.LogInformation("[Users] Created user {UserName}", userName);

            var detail = await db.RoleAssignments.Where(a => a.UserId == user.Id).Include(a => a.Role).ToListAsync(ct);
            return Results.Ok(ToDetail(user, detail));
        });

        group.MapPut("/{id}", async (
            string id,
            UpdateUserRequest request,
            UserManager<IdentityUser> um,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var user = await um.FindByIdAsync(id);
            if (user == null) return Results.NotFound(new { error = "User not found" });

            var email = request.Email?.Trim();
            if (!string.IsNullOrWhiteSpace(email))
            {
                user.Email = email;
            }
            if (request.EmailConfirmed.HasValue)
            {
                user.EmailConfirmed = request.EmailConfirmed.Value;
            }
            var phone = request.PhoneNumber?.Trim();
            if (!string.IsNullOrWhiteSpace(phone))
            {
                user.PhoneNumber = phone;
            }
            if (request.IsLocked.HasValue)
            {
                if (request.IsLocked.Value)
                {
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                }
                else
                {
                    user.LockoutEnd = null;
                }
            }

            var update = await um.UpdateAsync(user);
            if (!update.Succeeded)
            {
                return Results.BadRequest(new { error = string.Join("; ", update.Errors.Select(e => e.Description)) });
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                if (await um.HasPasswordAsync(user))
                {
                    var remove = await um.RemovePasswordAsync(user);
                    if (!remove.Succeeded)
                    {
                        return Results.BadRequest(new { error = string.Join("; ", remove.Errors.Select(e => e.Description)) });
                    }
                }

                var add = await um.AddPasswordAsync(user, request.Password);
                if (!add.Succeeded)
                {
                    return Results.BadRequest(new { error = string.Join("; ", add.Errors.Select(e => e.Description)) });
                }
            }

            var roles = await db.RoleAssignments.AsNoTracking().Include(a => a.Role).Where(a => a.UserId == id).ToListAsync(ct);
            return Results.Ok(ToDetail(user, roles));
        });

        group.MapPut("/{id}/roles", async (
            string id,
            UpdateUserRolesRequest request,
            UserManager<IdentityUser> um,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var user = await um.FindByIdAsync(id);
            if (user == null) return Results.NotFound(new { error = "User not found" });

            await UpdateAssignmentsAsync(id, request.Roles, db, ct);
            var roles = await db.RoleAssignments.AsNoTracking().Include(a => a.Role).Where(a => a.UserId == id).ToListAsync(ct);
            return Results.Ok(new { success = true, roles = roles.Select(r => new { r.RoleId, r.OrganizationId }) });
        }).RequireFunction("BAS.AUTH.USER.ROLE");

        return app;
    }

    private static UserSummaryDto ToSummary(IdentityUser user, List<RoleAssignment> assignments)
    {
        return new UserSummaryDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd >= DateTimeOffset.UtcNow,
            Roles = assignments.Select(a => new UserRoleAssignmentDto
            {
                RoleId = a.RoleId,
                RoleCode = a.Role?.Code ?? string.Empty,
                RoleName = a.Role?.Name ?? string.Empty,
                OrganizationId = a.OrganizationId
            }).ToList()
        };
    }

    private static UserDetailDto ToDetail(IdentityUser user, List<RoleAssignment> assignments)
    {
        var summary = ToSummary(user, assignments);
        return new UserDetailDto
        {
            Id = summary.Id,
            UserName = summary.UserName,
            Email = summary.Email,
            EmailConfirmed = summary.EmailConfirmed,
            IsLocked = summary.IsLocked,
            Roles = summary.Roles,
            PhoneNumber = user.PhoneNumber,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnd = user.LockoutEnd
        };
    }

    private static async Task UpdateAssignmentsAsync(string userId, IEnumerable<UserRoleAssignmentRequest> requests, AppDbContext db, CancellationToken ct)
    {
        var existing = await db.RoleAssignments.Where(a => a.UserId == userId).ToListAsync(ct);
        db.RoleAssignments.RemoveRange(existing);

        if (requests != null)
        {
            foreach (var role in requests)
            {
                if (role.RoleId == Guid.Empty) continue;
                db.RoleAssignments.Add(new RoleAssignment
                {
                    UserId = userId,
                    RoleId = role.RoleId,
                    OrganizationId = role.OrganizationId
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
