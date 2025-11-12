using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Domain.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

public class AccessService
{
    private readonly AppDbContext _db;

    public AccessService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<FunctionNode> CreateFunctionAsync(CreateFunctionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Function code and name are required.");
        }

        var exists = await _db.FunctionNodes.AnyAsync(f => f.Code == request.Code, ct);
        if (exists)
        {
            throw new InvalidOperationException("Function code already exists.");
        }

        var node = new FunctionNode
        {
            ParentId = request.ParentId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Route = request.Route?.Trim(),
            Icon = request.Icon?.Trim(),
            IsMenu = request.IsMenu,
            SortOrder = request.SortOrder
        };

        _db.FunctionNodes.Add(node);
        await _db.SaveChangesAsync(ct);
        return node;
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

    public async Task SeedSystemAdministratorAsync(CancellationToken ct = default)
    {
        var hasFunctions = await _db.FunctionNodes.AnyAsync(ct);
        if (!hasFunctions)
        {
            _db.FunctionNodes.Add(new FunctionNode
            {
                Code = "SYS.ALL",
                Name = "All Functions",
                IsMenu = true,
                SortOrder = 0
            });
            await _db.SaveChangesAsync(ct);
        }

        var adminRole = await _db.RoleProfiles.FirstOrDefaultAsync(r => r.IsSystem, ct);
        if (adminRole != null)
        {
            return;
        }

        var functions = await _db.FunctionNodes.Select(f => f.Id).ToListAsync(ct);

        var role = new RoleProfile
        {
            Code = "SYS.ADMIN",
            Name = "System Administrator",
            Description = "Has every function and data scope",
            IsEnabled = true,
            IsSystem = true,
            OrganizationId = null,
            Functions = functions.Select(id => new RoleFunctionPermission
            {
                FunctionId = id
            }).ToList(),
            DataScopes = new List<RoleDataScope>
            {
                new()
                {
                    EntityName = "*",
                    ScopeType = RoleDataScopeTypes.All
                }
            }
        };

        _db.RoleProfiles.Add(role);
        await _db.SaveChangesAsync(ct);
    }
}
