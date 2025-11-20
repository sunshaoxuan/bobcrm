using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
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

        // 3. 确保枚举管理菜单存在
        var enumMenu = await EnsureMenuNodeAsync(sysDomain, "SYS.ENUM", "枚举管理", "/system/enums", "unordered-list", 10);

        // 4. 确保管理员角色拥有此权限
        await EnsureAdminPermissionAsync(enumMenu);

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
                DisplayName = new Dictionary<string, string?>
                {
                    ["zh"] = name,
                    ["en"] = "System Management",
                    ["ja"] = "システム管理"
                }
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
        }
        return node;
    }

    private async Task<FunctionNode> EnsureMenuNodeAsync(FunctionNode parent, string code, string name, string route, string icon, int sortOrder)
    {
        var node = await _db.FunctionNodes.FirstOrDefaultAsync(f => f.Code == code);
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
                DisplayName = new Dictionary<string, string?>
                {
                    ["zh"] = name,
                    ["en"] = "Enum Management",
                    ["ja"] = "列挙型管理"
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
        }
        return node;
    }

    private async Task EnsureAdminPermissionAsync(FunctionNode menuNode)
    {
        // Find admin role (assuming "Administrator" or "admin")
        var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator" || r.Name == "admin");
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
                FunctionId = menuNode.Id,
                CanRead = true,
                CanCreate = true,
                CanUpdate = true,
                CanDelete = true
            };
            _db.RoleFunctionPermissions.Add(permission);
        }
    }
}
