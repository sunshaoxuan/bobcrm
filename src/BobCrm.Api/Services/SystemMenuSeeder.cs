using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

/// <summary>
/// 系统菜单种子数据初始化服务
/// 负责在系统启动时确保所有系统菜单项存在于数据库中
/// </summary>
public class SystemMenuSeeder
{
    private readonly AppDbContext _db;

    public SystemMenuSeeder(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 确保所有系统菜单已初始化
    /// </summary>
    public async Task EnsureSystemMenusAsync()
    {
        // 1. 确保根节点存在
        var root = await EnsureRootNodeAsync();

        // 2. 确保系统管理(SYS)领域节点存在
        var sysDomain = await EnsureDomainNodeAsync(root, "SYS", "系统管理", "setting", 900);

        // 3. 建模与枚举分组（原实体管理）
        var modelingDomain = await EnsureMenuNodeAsync(
            sysDomain,
            "SYS.ENTITY",
            "模型与枚举",
            null,
            "database",
            10,
            new Dictionary<string, string?>
            {
                ["zh"] = "模型与枚举",
                ["ja"] = "モデルと列挙",
                ["en"] = "Modeling & Enums"
            });

        // 5. 枚举管理（搬迁自 SYS.ENUM）
        var enumMenu = await EnsureMenuNodeAsync(
            modelingDomain,
            "SYS.ENTITY.ENUM",
            "枚举管理",
            "/system/enums",
            "unordered-list",
            10,
            new Dictionary<string, string?>
            {
                ["zh"] = "枚举管理",
                ["ja"] = "列挙管理",
                ["en"] = "Enum Management"
            });

        // 4. 业务实体编辑
        var entityEditor = await EnsureMenuNodeAsync(
            modelingDomain,
            "SYS.ENTITY.EDITOR",
            "业务实体编辑",
            "/entity-definitions",
            "profile",
            20,
            new Dictionary<string, string?>
            {
                ["zh"] = "实体管理",
                ["ja"] = "エンティティ管理",
                ["en"] = "Entity Management"
            });

        // 6. 菜单编辑器仍放在系统设置下
        var menuEditorMenu = await EnsureMenuNodeAsync(sysDomain, "SYS.SET.MENU", "菜单编辑器", "/menus", "menu", 20);

        // 7. 确保管理员角色拥有这些权限
        await EnsureAdminPermissionAsync(entityEditor);
        await EnsureAdminPermissionAsync(enumMenu);
        await EnsureAdminPermissionAsync(menuEditorMenu);

        await _db.SaveChangesAsync();
    }

    private async Task<FunctionNode> EnsureRootNodeAsync()
    {
        var root = await _db.FunctionNodes.FirstOrDefaultAsync(f => f.Code == "APP.ROOT");
        if (root == null)
        {
            root = new FunctionNode
            {
                Code = "APP.ROOT",
                Name = "应用根节点",
                Icon = "appstore",
                IsMenu = true,
                SortOrder = 0
            };
            _db.FunctionNodes.Add(root);
            await _db.SaveChangesAsync(); // Save immediately to get Id
        }
        return root;
    }

    private async Task<FunctionNode> EnsureDomainNodeAsync(FunctionNode root, string code, string name, string icon, int sortOrder)
    {
        var node = await _db.FunctionNodes.FirstOrDefaultAsync(f => f.Code == code);
        var display = new Dictionary<string, string?>
        {
            ["zh"] = name,
            ["en"] = "System Management",
            ["ja"] = "システム管理"
        };
        if (node == null)
        {
            node = new FunctionNode
            {
                ParentId = root.Id,
                Code = code,
                Name = name,
                Icon = icon,
                IsMenu = true,
                SortOrder = sortOrder,
                DisplayName = display
            };
            _db.FunctionNodes.Add(node);
            await _db.SaveChangesAsync();
        }
        else
        {
            // Ensure parent is correct
            if (node.ParentId != root.Id)
            {
                node.ParentId = root.Id;
            }
            node.Name = name;
            node.Icon = icon;
            node.SortOrder = sortOrder;
            node.DisplayName = display;
        }
        return node;
    }

    private async Task<FunctionNode> EnsureMenuNodeAsync(
        FunctionNode parent,
        string code,
        string name,
        string? route,
        string icon,
        int sortOrder,
        Dictionary<string, string?>? display = null)
    {
        // 兼容旧的 SYS.ENUM 代码，迁移到新的 SYS.ENTITY.ENUM 结构
        var legacyCode = code == "SYS.ENTITY.ENUM" ? "SYS.ENUM" : null;
        var node = await _db.FunctionNodes.FirstOrDefaultAsync(f => f.Code == code);
        if (node == null && legacyCode != null)
        {
            node = await _db.FunctionNodes.FirstOrDefaultAsync(f => f.Code == legacyCode);
        }

        if (node == null)
        {
            node = new FunctionNode
            {
                ParentId = parent.Id,
                Code = code,
                Name = name,
                Route = route,
                Icon = icon,
                IsMenu = true,
                SortOrder = sortOrder,
                DisplayName = display ?? new Dictionary<string, string?>
                {
                    ["zh"] = name,
                    ["en"] = name,
                    ["ja"] = name
                }
            };
            _db.FunctionNodes.Add(node);
            await _db.SaveChangesAsync();
        }
        else
        {
            // Update properties
            node.ParentId = parent.Id;
            node.Route = route;
            node.Icon = icon;
            node.SortOrder = sortOrder;
            node.Code = code;
            node.Name = name;
            node.DisplayName = display ?? node.DisplayName;
        }

        // Clean up any legacy SYS.ENUM duplicates once the new node is in place
        var legacy = await _db.FunctionNodes
            .Where(f => f.Code == "SYS.ENUM" && f.Id != node.Id)
            .ToListAsync();
        if (legacy.Count > 0)
        {
            _db.FunctionNodes.RemoveRange(legacy);
        }
        return node;
    }

    private async Task EnsureAdminPermissionAsync(FunctionNode menuNode)
    {
        // Find admin role (assuming "Administrator" or "admin")
        var adminRole = await _db.RoleProfiles.FirstOrDefaultAsync(r => r.Name == "Administrator" || r.Name == "admin");
        if (adminRole == null)
        {
            return;
        }

        var permission = await _db.RoleFunctionPermissions
            .FirstOrDefaultAsync(p => p.RoleId == adminRole.Id && p.FunctionId == menuNode.Id);

        if (permission == null)
        {
            permission = new RoleFunctionPermission
            {
                RoleId = adminRole.Id,
                FunctionId = menuNode.Id
            };
            _db.RoleFunctionPermissions.Add(permission);
        }
    }
}
