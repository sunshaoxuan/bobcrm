using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.DTOs.Access;
using BobCrm.Api.Contracts.Requests.Access;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

public class RoleService
{
    private readonly AppDbContext _db;

    public RoleService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RoleProfile> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Role code and name are required.");
        }

        var exists = await _db.RoleProfiles
            .AnyAsync(r => r.Code == request.Code && r.OrganizationId == request.OrganizationId, ct);
        if (exists)
        {
            throw new InvalidOperationException("Role code already exists within the organization.");
        }

        var role = new RoleProfile
        {
            OrganizationId = request.OrganizationId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description,
            IsEnabled = request.IsEnabled,
            IsSystem = false
        };

        if (request.FunctionIds?.Count > 0)
        {
            var functions = await _db.FunctionNodes
                .Where(f => request.FunctionIds.Contains(f.Id))
                .Select(f => f.Id)
                .ToListAsync(ct);
            role.Functions = functions.Select(f => new RoleFunctionPermission
            {
                Role = role,
                FunctionId = f
            }).ToList();
        }

        if (request.DataScopes?.Count > 0)
        {
            role.DataScopes = request.DataScopes.Select(ds => new RoleDataScope
            {
                Role = role,
                EntityName = ds.EntityName,
                ScopeType = ds.ScopeType,
                FilterExpression = ds.FilterExpression
            }).ToList();
        }

        _db.RoleProfiles.Add(role);
        await _db.SaveChangesAsync(ct);
        return role;
    }

    public async Task<RoleAssignment> AssignRoleAsync(AssignRoleRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new InvalidOperationException("UserId is required.");
        }

        var exists = await _db.RoleAssignments.AnyAsync(a =>
            a.UserId == request.UserId &&
            a.RoleId == request.RoleId &&
            a.OrganizationId == request.OrganizationId, ct);

        if (exists)
        {
            throw new InvalidOperationException("Assignment already exists.");
        }

        var assignment = new RoleAssignment
        {
            UserId = request.UserId,
            RoleId = request.RoleId,
            OrganizationId = request.OrganizationId,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo
        };

        _db.RoleAssignments.Add(assignment);
        await _db.SaveChangesAsync(ct);
        return assignment;
    }

    public async Task<RoleProfile> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var role = await _db.RoleProfiles.FindAsync(new object[] { roleId }, ct);
        if (role == null)
            throw new KeyNotFoundException("Role not found.");

        if (role.IsSystem)
            throw new InvalidOperationException("System role is immutable.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            role.Name = request.Name;
        if (request.Description is not null)
            role.Description = request.Description;
        if (request.IsEnabled.HasValue)
            role.IsEnabled = request.IsEnabled.Value;

        await _db.SaveChangesAsync(ct);
        await _db.Entry(role).Collection(r => r.Functions).LoadAsync(ct);
        await _db.Entry(role).Collection(r => r.DataScopes).LoadAsync(ct);
        return role;
    }

    public async Task DeleteRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        var role = await _db.RoleProfiles
            .Include(r => r.Functions)
            .Include(r => r.DataScopes)
            .Include(r => r.Assignments)
            .FirstOrDefaultAsync(r => r.Id == roleId, ct);

        if (role == null)
            throw new KeyNotFoundException("Role not found.");

        if (role.IsSystem)
            throw new InvalidOperationException("System role is immutable.");

        if (role.Assignments?.Any() == true)
            throw new InvalidOperationException("Role has active assignments.");

        _db.RoleProfiles.Remove(role);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateRolePermissionsAsync(Guid roleId, UpdatePermissionsRequest request, CancellationToken ct = default)
    {
        var role = await _db.RoleProfiles
            .Include(r => r.Functions)
            .Include(r => r.DataScopes)
            .FirstOrDefaultAsync(r => r.Id == roleId, ct);

        if (role == null)
            throw new KeyNotFoundException("Role not found.");

        // Update function permissions
        _db.RoleFunctionPermissions.RemoveRange(role.Functions);
        var templateSelections = request.FunctionPermissions?
            .Where(fp => fp.FunctionId != Guid.Empty)
            .GroupBy(fp => fp.FunctionId)
            .ToDictionary(
                g => g.Key,
                g => g.Last().TemplateBindingId);

        var finalFunctionIds = new HashSet<Guid>(request.FunctionIds ?? Enumerable.Empty<Guid>());
        if (templateSelections != null)
        {
            foreach (var functionId in templateSelections.Keys)
            {
                finalFunctionIds.Add(functionId);
            }
        }

        if (finalFunctionIds.Count > 0)
        {
            role.Functions = finalFunctionIds.Select(fid => new RoleFunctionPermission
            {
                RoleId = roleId,
                FunctionId = fid,
                TemplateBindingId = templateSelections != null && templateSelections.TryGetValue(fid, out var bindingId)
                    ? bindingId
                    : null
            }).ToList();
        }
        else
        {
            role.Functions = new List<RoleFunctionPermission>();
        }

        // Update data scopes
        _db.RoleDataScopes.RemoveRange(role.DataScopes);
        if (request.DataScopes?.Count > 0)
        {
            role.DataScopes = request.DataScopes.Select(ds => new RoleDataScope
            {
                Role = role,
                EntityName = ds.EntityName,
                ScopeType = ds.ScopeType,
                FilterExpression = ds.FilterExpression
            }).ToList();
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAssignmentAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await _db.RoleAssignments.FindAsync(new object[] { assignmentId }, ct);
        if (assignment == null)
           throw new KeyNotFoundException("Assignment not found.");

        _db.RoleAssignments.Remove(assignment);
        await _db.SaveChangesAsync(ct);
    }
}
