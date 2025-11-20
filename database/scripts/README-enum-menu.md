# 枚举管理菜单初始化脚本

## 📋 说明

本脚本用于在 BobCRM 系统中添加"枚举定义"菜单项，使用户可以通过系统菜单访问枚举管理功能。

## 📍 菜单位置

```
系统设置 (SYS)
  └─ 实体管理 (SYS.ENTITY)
      └─ 枚举定义 (SYS.ENTITY.ENUM)  ← 新增菜单
```

## 🎯 功能

- **菜单代码**: `SYS.ENTITY.ENUM`
- **路由**: `/enums`
- **图标**: `ordered-list`
- **多语言**: 通过 `MENU_SYS_ENTITY_ENUM` 资源键支持中日英三语

## 🚀 使用方法

### 前置条件
1. 数据库已完成初始化（存在 `FunctionNodes` 表）
2. 根节点 `APP.ROOT` 已创建
3. PostgreSQL 数据库（脚本使用 `gen_random_uuid()`）

### 执行步骤

#### 方法 1: 通过 psql 命令行
```bash
# 连接到数据库
psql -U your_username -d bobcrm

# 执行脚本
\i database/scripts/add-enum-menu-node.sql

# 或者直接执行
psql -U your_username -d bobcrm -f database/scripts/add-enum-menu-node.sql
```

#### 方法 2: 通过 pgAdmin
1. 打开 pgAdmin 并连接到 bobcrm 数据库
2. 打开查询工具（Query Tool）
3. 加载 `add-enum-menu-node.sql` 文件
4. 点击执行（F5）

#### 方法 3: 通过 Docker
```bash
# 如果数据库运行在 Docker 中
docker exec -i bobcrm-pg psql -U postgres -d bobcrm < database/scripts/add-enum-menu-node.sql
```

### 验证安装

执行脚本后，查询结果会显示菜单节点信息：

```
 FunctionCode    | NameKey             | Route  | Icon         | SortOrder | ParentCode
-----------------+---------------------+--------+--------------+-----------+------------
 SYS             | MENU_SYS            | NULL   | setting      | 1000      | APP.ROOT
 SYS.ENTITY      | MENU_SYS_ENTITY     | NULL   | database     | 100       | SYS
 SYS.ENTITY.ENUM | MENU_SYS_ENTITY_ENUM| /enums | ordered-list | 20        | SYS.ENTITY
```

## 🔍 脚本功能详解

### 1. 安全检查
- 检查根节点是否存在
- 检查枚举菜单是否已存在（避免重复插入）
- 使用事务保证数据一致性

### 2. 节点创建
脚本会自动创建缺失的父节点：
- ✅ `SYS` 域节点（如果不存在）
- ✅ `SYS.ENTITY` 模块节点（如果不存在）
- ✅ `SYS.ENTITY.ENUM` 菜单节点（目标节点）

### 3. 输出信息
执行过程中会输出详细的 NOTICE 信息：
```
NOTICE:  Found root node: ...
NOTICE:  Found SYS domain node: ...
NOTICE:  Found entity management module: ...
NOTICE:  Successfully created enum menu node: ...
NOTICE:  Menu path: 系统设置 > 实体管理 > 枚举定义
NOTICE:  Route: /enums
NOTICE:  === Enum menu node installation complete ===
```

## ⚠️ 注意事项

1. **重复执行**: 脚本是幂等的，可以安全地重复执行。如果菜单已存在，会跳过创建。

2. **数据库权限**: 执行脚本需要具有 INSERT 权限的数据库用户。

3. **前置依赖**:
   - ✅ `i18n-resources.json` 中已添加 `MENU_SYS_ENTITY_ENUM` 资源键
   - ✅ 前端路由 `/enums` 已配置（EnumDefinitions.razor）

4. **角色权限**: 脚本创建菜单后，还需要配置角色权限才能让用户访问。参考下一节。

## 🔐 配置角色权限

菜单创建后，需要为角色分配访问权限：

### 方法 1: 通过系统界面
1. 登录系统并导航到 "角色管理"
2. 选择需要授权的角色（如 SYS.ADMIN）
3. 在权限树中勾选 "系统设置 > 实体管理 > 枚举定义"
4. 保存权限配置

### 方法 2: 直接SQL（管理员）
```sql
-- 为 SYS.ADMIN 角色添加枚举管理权限
INSERT INTO "RoleFunctionPermissions" (
    "Id",
    "RoleProfileId",
    "FunctionNodeId",
    "CanView",
    "CanCreate",
    "CanEdit",
    "CanDelete",
    "CreatedAt"
)
SELECT
    gen_random_uuid(),
    rp."Id",
    fn."Id",
    true,  -- CanView
    true,  -- CanCreate
    true,  -- CanEdit
    true,  -- CanDelete
    NOW()
FROM "RoleProfiles" rp
CROSS JOIN "FunctionNodes" fn
WHERE rp."Code" = 'SYS.ADMIN'
  AND fn."Code" = 'SYS.ENTITY.ENUM'
  AND NOT EXISTS (
      SELECT 1 FROM "RoleFunctionPermissions" rfp
      WHERE rfp."RoleProfileId" = rp."Id"
        AND rfp."FunctionNodeId" = fn."Id"
  );
```

## 🧪 测试验证

### 1. 数据库验证
```sql
-- 检查菜单节点
SELECT * FROM "FunctionNodes"
WHERE "Code" = 'SYS.ENTITY.ENUM';

-- 检查多语言资源
SELECT * FROM "I18nResources"
WHERE "Code" = 'MENU_SYS_ENTITY_ENUM';
```

### 2. 前端验证
1. 重启前端应用
2. 登录系统
3. 导航到 "系统设置 > 实体管理"
4. 应看到 "枚举定义" 菜单项
5. 点击后应跳转到 `/enums` 页面

## 📚 相关文件

- **SQL脚本**: `database/scripts/add-enum-menu-node.sql`
- **i18n资源**: `src/BobCrm.Api/Resources/i18n-resources.json`
- **前端页面**: `src/BobCrm.App/Components/Pages/EnumDefinitions.razor`
- **实施文档**: `docs/examples/ENUM-UI-INTEGRATION-SUMMARY.md`

## 🔄 回滚（如需删除菜单）

```sql
-- 删除枚举菜单节点（谨慎操作）
DELETE FROM "RoleFunctionPermissions"
WHERE "FunctionNodeId" IN (
    SELECT "Id" FROM "FunctionNodes"
    WHERE "Code" = 'SYS.ENTITY.ENUM'
);

DELETE FROM "FunctionNodes"
WHERE "Code" = 'SYS.ENTITY.ENUM';
```

## 📞 问题反馈

如果脚本执行出现问题，请检查：
1. PostgreSQL 版本是否支持 `gen_random_uuid()`（9.4+）
2. 数据库是否已完成基础初始化
3. 用户是否有足够的权限

---

**Last Updated**: 2025-11-19
**Version**: 1.0.0
