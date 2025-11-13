using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Domain.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

public class AccessService
{
    private readonly AppDbContext _db;

    private record FunctionSeed(string Code, string Name, string? Route, string? Icon, bool IsMenu, int SortOrder, string? ParentCode);

    private static readonly FunctionSeed[] DefaultFunctionSeeds =
    [
        new("APP.ROOT", "Application", null, "appstore", true, 0, null),
        new("APP.DASHBOARD", "Dashboard", "/", "home", true, 10, "APP.ROOT"),
        new("APP.CUSTOMERS", "Customers", "/customers", "team", true, 20, "APP.ROOT"),
        new("APP.CUSTOMERS.CREATE", "Create Customer", null, "plus", false, 21, "APP.CUSTOMERS"),
        new("APP.CUSTOMERS.EDIT", "Edit Customer", null, "edit", false, 22, "APP.CUSTOMERS"),
        new("APP.ENTITY", "Entity Definitions", "/entity-definitions", "profile", true, 30, "APP.ROOT"),
        new("APP.ENTITY.PUBLISH", "Publish Entity", null, "cloud-upload", false, 31, "APP.ENTITY"),
        new("APP.TEMPLATES", "Templates", "/templates", "appstore", true, 40, "APP.ROOT"),
        new("APP.ORGANIZATIONS", "Organizations", "/organizations", "cluster", true, 50, "APP.ROOT"),
        new("APP.ROLES", "Roles", "/roles", "lock", true, 55, "APP.ROOT"),
        new("APP.ROLES.CREATE", "Create Role", null, "plus", false, 56, "APP.ROLES"),
        new("APP.ROLES.EDIT", "Edit Role", null, "edit", false, 57, "APP.ROLES"),
        new("APP.ROLES.PERMISSIONS", "Configure Permissions", null, "safety", false, 58, "APP.ROLES"),
        new("APP.SETTINGS", "Settings", "/settings", "setting", true, 60, "APP.ROOT")
    ];

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
        await SeedFunctionTreeAsync(ct);

        var allFunctionIds = await _db.FunctionNodes
            .OrderBy(f => f.SortOrder)
            .Select(f => f.Id)
            .ToListAsync(ct);

        var adminRole = await _db.RoleProfiles
            .Include(r => r.Functions)
            .Include(r => r.DataScopes)
            .FirstOrDefaultAsync(r => r.IsSystem, ct);

        if (adminRole == null)
        {
            adminRole = new RoleProfile
            {
                Code = "SYS.ADMIN",
                Name = "System Administrator",
                Description = "Has every function and data scope",
                IsEnabled = true,
                IsSystem = true,
                OrganizationId = null,
                Functions = allFunctionIds.Select(id => new RoleFunctionPermission
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

            _db.RoleProfiles.Add(adminRole);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            var existingFunctionIds = adminRole.Functions.Select(f => f.FunctionId).ToHashSet();
            var missingFunctions = allFunctionIds.Where(id => !existingFunctionIds.Contains(id))
                .Select(id => new RoleFunctionPermission
                {
                    RoleId = adminRole.Id,
                    FunctionId = id
                }).ToList();
            var roleChanged = false;
            if (missingFunctions.Count > 0)
            {
                adminRole.Functions.AddRange(missingFunctions);
                roleChanged = true;
            }

            if (!adminRole.DataScopes.Any())
            {
                adminRole.DataScopes.Add(new RoleDataScope
                {
                    RoleId = adminRole.Id,
                    EntityName = "*",
                    ScopeType = RoleDataScopeTypes.All
                });
                roleChanged = true;
            }

            if (roleChanged)
            {
                await _db.SaveChangesAsync(ct);
            }
        }

        var sysAdminUsers = await _db.Users
            .Where(u => u.NormalizedUserName == "ADMIN" || u.Email == "admin@local")
            .ToListAsync(ct);

        if (sysAdminUsers.Count > 0)
        {
            var assignmentsAdded = false;
            foreach (var user in sysAdminUsers)
            {
                var exists = await _db.RoleAssignments.AnyAsync(a =>
                    a.UserId == user.Id &&
                    a.RoleId == adminRole.Id &&
                    a.OrganizationId == null, ct);
                if (!exists)
                {
                    _db.RoleAssignments.Add(new RoleAssignment
                    {
                        UserId = user.Id,
                        RoleId = adminRole.Id,
                        OrganizationId = null
                    });
                    assignmentsAdded = true;
                }
            }

            if (assignmentsAdded)
            {
                await _db.SaveChangesAsync(ct);
            }
        }
    }

    private async Task SeedFunctionTreeAsync(CancellationToken ct)
    {
        var existing = await _db.FunctionNodes.ToDictionaryAsync(f => f.Code, f => f, ct);
        foreach (var seed in DefaultFunctionSeeds)
        {
            if (!existing.TryGetValue(seed.Code, out var node))
            {
                node = new FunctionNode { Code = seed.Code };
                _db.FunctionNodes.Add(node);
                existing[seed.Code] = node;
            }

            node.Name = seed.Name;
            node.Route = seed.Route;
            node.Icon = seed.Icon;
            node.IsMenu = seed.IsMenu;
            node.SortOrder = seed.SortOrder;
        }

        await _db.SaveChangesAsync(ct);

        var refreshed = await _db.FunctionNodes.ToDictionaryAsync(f => f.Code, f => f, ct);
        foreach (var seed in DefaultFunctionSeeds)
        {
            var node = refreshed[seed.Code];
            Guid? parentId = null;
            if (!string.IsNullOrWhiteSpace(seed.ParentCode) && refreshed.TryGetValue(seed.ParentCode, out var parent))
            {
                parentId = parent.Id;
            }

            if (node.ParentId != parentId)
            {
                node.ParentId = parentId;
            }
        }

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(ct);
        }
    }
}
