using System;
using System.Collections.Generic;
using System.Linq;
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
        new("APP.ROOT", "应用根节点", null, "appstore", true, 0, null),

        // 1. 系统管理
        new("SYS", "系统管理", null, "setting", true, 10, "APP.ROOT"),
        new("SYS.SET", "系统设置", null, "setting", true, 11, "SYS"),
        new("SYS.SET.CONFIG", "系统设置", "/settings", "setting", true, 111, "SYS.SET"),
        new("SYS.MSG", "邮件与消息", null, "mail", true, 12, "SYS"),
        new("SYS.MSG.MAIL", "邮件服务器", null, "mail", true, 121, "SYS.MSG"),
        new("SYS.MSG.NOTIFY", "系统通知", null, "notification", false, 122, "SYS.MSG"),
        new("SYS.MSG.TEMPLATE", "消息模板", null, "book", false, 123, "SYS.MSG"),
        new("SYS.ENTITY", "实体管理", null, "profile", true, 13, "SYS"),
        new("SYS.ENTITY.EDITOR", "业务实体编辑", "/entity-definitions", "profile", true, 131, "SYS.ENTITY"),
        new("SYS.TEMPLATE", "模板管理", null, "appstore", true, 14, "SYS"),
        new("SYS.TEMPLATE.DESIGN", "模板设计", "/templates", "appstore", true, 141, "SYS.TEMPLATE"),
        new("SYS.TEMPLATE.ASSIGN", "模板分配", null, "branches", false, 142, "SYS.TEMPLATE"),
        new("SYS.LOG", "日志管理", null, "audit", true, 15, "SYS"),
        new("SYS.LOG.USER", "用户使用记录", null, "user", false, 151, "SYS.LOG"),
        new("SYS.LOG.SYSTEM", "系统日志", null, "file-search", false, 152, "SYS.LOG"),
        new("SYS.AI", "人工智能", null, "robot", true, 16, "SYS"),
        new("SYS.AI.MODEL", "模型设置", null, "sliders", false, 161, "SYS.AI"),
        new("SYS.AI.WORKFLOW", "工作流程", null, "branches", false, 162, "SYS.AI"),
        new("SYS.AI.AGENT", "智能体", null, "apartment", false, 163, "SYS.AI"),

        // 2. 基本设置
        new("BAS", "基本设置", null, "appstore", true, 20, "APP.ROOT"),
        new("BAS.ORG", "组织管理", null, "cluster", true, 21, "BAS"),
        new("BAS.ORG.DIRECTORY", "组织档案", "/organizations", "cluster", true, 211, "BAS.ORG"),
        new("BAS.ORG.ROLES", "角色管理", "/roles", "lock", true, 212, "BAS.ORG"),
        new("BAS.AUTH", "用户与权限", null, "team", true, 22, "BAS"),
        new("BAS.AUTH.USERS", "用户档案", "/users", "user", true, 221, "BAS.AUTH"),
        new("BAS.AUTH.ROLE.PERM", "角色权限分配", null, "safety", false, 222, "BAS.AUTH"),
        new("BAS.AUTH.USER.ROLE", "用户角色分配", null, "team", false, 223, "BAS.AUTH"),

        // 3. 客户关系
        new("CRM", "客户关系", null, "team", true, 30, "APP.ROOT"),
        new("CRM.CORE", "基本档案", null, "database", true, 31, "CRM"),
        new("CRM.CORE.ACCOUNTS", "客户主档", "/customers", "team", true, 311, "CRM.CORE"),
        new("CRM.CORE.CONTRACTS", "合约管理", null, "file-done", false, 312, "CRM.CORE"),
        new("CRM.CORE.IMPLEMENT", "实施管理", null, "tool", false, 313, "CRM.CORE"),
        new("CRM.CORE.SERVICE", "运维管理", null, "safety", false, 314, "CRM.CORE"),
        new("CRM.PLAN", "计划管理", null, "calendar", true, 32, "CRM"),
        new("CRM.PLAN.TASKS", "计划任务", null, "schedule", true, 321, "CRM.PLAN"),
        new("CRM.PLAN.SCHEDULE", "日程安排", null, "calendar", false, 322, "CRM.PLAN"),
        new("CRM.PLAN.EXEC", "作业执行", null, "play-circle", false, 323, "CRM.PLAN"),
        new("CRM.PLAN.KANBAN", "作业看板", null, "appstore", false, 324, "CRM.PLAN"),

        // 4. 知识库
        new("KB", "知识库", null, "book", true, 40, "APP.ROOT"),
        new("KB.QA", "问与答", null, "question-circle", true, 41, "KB"),
        new("KB.QA.FAQ", "常见问答", null, "question-circle", true, 411, "KB.QA"),
        new("KB.QA.CUSTOMER", "客户问答库", null, "message", false, 412, "KB.QA"),
        new("KB.QA.REPLY", "答复模板", null, "file-text", false, 413, "KB.QA"),
        new("KB.PROD", "产品与补丁", null, "build", true, 42, "KB"),
        new("KB.PROD.VERSION", "产品版本", null, "barcode", true, 421, "KB.PROD"),
        new("KB.PROD.PATCH", "补丁管理", null, "tool", false, 422, "KB.PROD"),
        new("KB.PROD.DEPLOY", "客户部署记录", null, "file-search", false, 423, "KB.PROD"),
        new("KB.DOC", "文档与手册", null, "book", true, 43, "KB"),
        new("KB.DOC.PRODUCT", "产品文档", null, "book", true, 431, "KB.DOC"),
        new("KB.DOC.MANUAL", "操作手册", null, "read", false, 432, "KB.DOC"),

        // 5. 工作协作
        new("COLLAB", "工作协作", null, "team", true, 50, "APP.ROOT"),
        new("COLLAB.WORK", "工作记录", null, "schedule", true, 51, "COLLAB"),
        new("COLLAB.WORK.PERSONAL", "个人记录", null, "user", true, 511, "COLLAB.WORK"),
        new("COLLAB.WORK.LOG", "工作日志", null, "file-text", false, 512, "COLLAB.WORK"),
        new("COLLAB.FILE", "文件管理", null, "paper-clip", true, 52, "COLLAB"),
        new("COLLAB.FILE.ATTACH", "附件管理", null, "paper-clip", true, 521, "COLLAB.FILE"),
        new("COLLAB.FILE.COMMENT", "评论历史", null, "comment", false, 522, "COLLAB.FILE")
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

    public async Task EnsureFunctionAccessAsync(string userId, string? functionCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(functionCode))
        {
            return;
        }

        if (!await HasFunctionAccessAsync(userId, functionCode, ct))
        {
            throw new UnauthorizedAccessException("User does not have required function permission.");
        }
    }

    public async Task<bool> HasFunctionAccessAsync(string userId, string? functionCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(functionCode))
        {
            return true;
        }

        var normalizedCode = functionCode.Trim();
        var now = DateTime.UtcNow;

        return await _db.RoleAssignments
            .Where(a => a.UserId == userId &&
                        (!a.ValidFrom.HasValue || a.ValidFrom <= now) &&
                        (!a.ValidTo.HasValue || a.ValidTo >= now))
            .SelectMany(a => a.Role!.Functions)
            .Join(_db.FunctionNodes,
                rf => rf.FunctionId,
                fn => fn.Id,
                (rf, fn) => fn.Code)
            .AnyAsync(code => code == normalizedCode, ct);
    }

    public async Task<DataScopeEvaluationResult> EvaluateDataScopeAsync(string userId, string entityName, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var normalizedEntity = entityName.Trim().ToLowerInvariant();

        var scopeBindings = await _db.RoleAssignments
            .Where(a => a.UserId == userId &&
                        (!a.ValidFrom.HasValue || a.ValidFrom <= now) &&
                        (!a.ValidTo.HasValue || a.ValidTo >= now) &&
                        a.Role != null)
            .SelectMany(a => a.Role!.DataScopes
                .Where(ds => ds.EntityName == "*" || ds.EntityName.ToLower() == normalizedEntity)
                .Select(ds => new ScopeBinding(ds, a.OrganizationId)))
            .ToListAsync(ct);

        if (scopeBindings.Any(sb => sb.Scope.ScopeType == RoleDataScopeTypes.All))
        {
            return new DataScopeEvaluationResult(true, Array.Empty<ScopeBinding>());
        }

        return new DataScopeEvaluationResult(false, scopeBindings);
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

public record ScopeBinding(RoleDataScope Scope, Guid? OrganizationId);

public record DataScopeEvaluationResult(bool HasFullAccess, IReadOnlyList<ScopeBinding> Scopes)
{
    public IReadOnlyList<Guid?> OrganizationFilter =>
        Scopes.Select(sb => sb.OrganizationId)
            .Where(id => id.HasValue)
            .Distinct()
            .ToList();
}
