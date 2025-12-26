using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs.User;
using BobCrm.Api.Contracts.Requests.User;
using BobCrm.Api.Contracts.Responses.User;
using BobCrm.Api.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

public class UserAppService : IUserAppService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AppDbContext _db;
    private readonly ILocalization _loc;
    private readonly ILogger<UserAppService> _logger;

    public UserAppService(
        UserManager<IdentityUser> userManager,
        AppDbContext db,
        ILocalization loc,
        ILogger<UserAppService> logger)
    {
        _userManager = userManager;
        _db = db;
        _loc = loc;
        _logger = logger;
    }

    public async Task<List<UserSummaryDto>> GetUsersAsync(CancellationToken ct = default)
    {
        var users = await _userManager.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync(ct);
        var assignments = await _db.RoleAssignments
            .AsNoTracking()
            .Include(a => a.Role)
            .ToListAsync(ct);
        var lookup = assignments
            .GroupBy(a => a.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var list = users
            .Select(u => ToSummary(u, lookup.TryGetValue(u.Id, out var roles) ? roles : new List<RoleAssignment>()))
            .ToList();
            
        return list;
    }

    public async Task<UserDetailDto> GetUserAsync(string id, string? lang = null, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) 
            throw new KeyNotFoundException(_loc.T("ERR_USER_NOT_FOUND", lang ?? ""));

        var roles = await _db.RoleAssignments
            .AsNoTracking()
            .Include(a => a.Role)
            .Where(a => a.UserId == id)
            .ToListAsync(ct);

        return ToDetail(user, roles);
    }

    public async Task<UserDetailDto> CreateUserAsync(CreateUserRequest request, string? lang = null, CancellationToken ct = default)
    {
        var userName = request.UserName.Trim();
        var email = request.Email.Trim();

        if (await _userManager.FindByNameAsync(userName) != null)
        {
            throw new ServiceException(_loc.T("ERR_USERNAME_EXISTS", lang ?? ""), "USERNAME_EXISTS");
        }

        var user = new IdentityUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = request.EmailConfirmed
        };

        var create = await _userManager.CreateAsync(user);
        if (!create.Succeeded)
        {
            var errors = string.Join("; ", create.Errors.Select(e => e.Description));
            throw new ServiceException(string.Format(_loc.T("ERR_USER_CREATE_FAILED", lang ?? ""), errors), "USER_CREATE_FAILED");
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var addPwd = await _userManager.AddPasswordAsync(user, request.Password);
            if (!addPwd.Succeeded)
            {
                await _userManager.DeleteAsync(user); // Rollback
                var errors = string.Join("; ", addPwd.Errors.Select(e => e.Description));
                throw new ServiceException(string.Format(_loc.T("ERR_USER_ADD_PASSWORD_FAILED", lang ?? ""), errors), "USER_CREATE_FAILED");
            }
        }

        await UpdateAssignmentsAsync(user.Id, request.Roles, ct);
        _logger.LogInformation("[Users] Created user {UserName}", userName);

        var detail = await _db.RoleAssignments.Where(a => a.UserId == user.Id).Include(a => a.Role).ToListAsync(ct);
        return ToDetail(user, detail);
    }

    public async Task<UserDetailDto> UpdateUserAsync(string id, UpdateUserRequest request, string? lang = null, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException(_loc.T("ERR_USER_NOT_FOUND", lang ?? ""));

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

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            var errors = string.Join("; ", update.Errors.Select(e => e.Description));
            throw new ServiceException(string.Format(_loc.T("ERR_USER_UPDATE_FAILED", lang ?? ""), errors), "USER_UPDATE_FAILED");
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (await _userManager.HasPasswordAsync(user))
            {
                var remove = await _userManager.RemovePasswordAsync(user);
                if (!remove.Succeeded)
                {
                    var errors = string.Join("; ", remove.Errors.Select(e => e.Description));
                    throw new ServiceException(string.Format(_loc.T("ERR_USER_REMOVE_PASSWORD_FAILED", lang ?? ""), errors), "USER_UPDATE_FAILED");
                }
            }

            var add = await _userManager.AddPasswordAsync(user, request.Password);
            if (!add.Succeeded)
            {
                var errors = string.Join("; ", add.Errors.Select(e => e.Description));
                throw new ServiceException(string.Format(_loc.T("ERR_USER_ADD_PASSWORD_FAILED", lang ?? ""), errors), "USER_UPDATE_FAILED");
            }
        }

        var roles = await _db.RoleAssignments.AsNoTracking().Include(a => a.Role).Where(a => a.UserId == id).ToListAsync(ct);
        return ToDetail(user, roles);
    }

    public async Task<UserRolesUpdateResponse> UpdateUserRolesAsync(string id, UpdateUserRolesRequest request, string? lang = null, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
             throw new KeyNotFoundException(_loc.T("ERR_USER_NOT_FOUND", lang ?? ""));

        await UpdateAssignmentsAsync(id, request.Roles, ct);
        var roles = await _db.RoleAssignments.AsNoTracking().Include(a => a.Role).Where(a => a.UserId == id).ToListAsync(ct);

        return new UserRolesUpdateResponse
        {
            Success = true,
            Roles = roles
                .Select(r => new UserRoleAssignmentDto
                {
                    RoleId = r.RoleId,
                    RoleCode = r.Role?.Code ?? string.Empty,
                    RoleName = r.Role?.Name ?? string.Empty,
                    OrganizationId = r.OrganizationId
                })
                .ToList()
        };
    }

    private async Task UpdateAssignmentsAsync(string userId, IEnumerable<UserRoleAssignmentRequest> requests, CancellationToken ct)
    {
        var existing = await _db.RoleAssignments.Where(a => a.UserId == userId).ToListAsync(ct);
        _db.RoleAssignments.RemoveRange(existing);

        if (requests != null)
        {
            foreach (var role in requests)
            {
                if (role.RoleId == Guid.Empty) continue;
                _db.RoleAssignments.Add(new RoleAssignment
                {
                    UserId = userId,
                    RoleId = role.RoleId,
                    OrganizationId = role.OrganizationId
                });
            }
        }

        await _db.SaveChangesAsync(ct);
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
}
