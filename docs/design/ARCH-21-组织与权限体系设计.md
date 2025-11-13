# ARCH-21 组织与权限体系设计

## 1. 引言
组织与权限体系用于表达“一个用户在某组织下拥有哪些功能与数据范围”。本设计涵盖：

- 组织树（OrganizationNodes）的建模与 API；
- 角色档案、功能树、数据范围以及用户角色分配；
- 默认种子（菜单、系统管理员、admin 绑定）；
- 与实体定义/权限检查的集成点。

## 2. 设计目标
1. 以树形结构管理组织：单根、多层级、同级 Code 唯一；
2. 通过角色档案绑定组织 + 功能 + 数据范围；
3. 将菜单/功能树与权限授权统一建模；
4. 启动时自动补齐系统管理员链条，避免“有账号但无权限”的问题；
5. 提供清晰 API 与测试策略，支持后续 UI 管理页面。

## 3. 组织档案
### 3.1 模型
| 字段 | 说明 |
| --- | --- |
| `Id` | 主键 |
| `Code` | 同级唯一编码 |
| `Name` | 节点名称 |
| `ParentId` | 父节点，为空则为根 |
| `Level` | 层级深度 |
| `PathCode` | 按 `01.02.03` 递增编码 |
| `SortOrder` | 同级排序 |

### 3.2 规则
- 根节点唯一；
- 删除前必须无子节点；
- PathCode 由服务计算，不允许客户端传入；
- 前端采用“左树右表 + 行内编辑”；
- API：`GET /api/organizations/tree`、`POST /api/organizations`、`PUT /api/organizations/{id}`、`DELETE /api/organizations/{id}`。

## 4. 权限模型
### 4.1 核心实体
| 实体 | 说明 |
| --- | --- |
| `FunctionNode` | 菜单/功能节点，包含 Code/Name/Route/Icon/IsMenu/SortOrder/ParentId |
| `RoleProfile` | 角色档案，字段：`OrganizationId?`、`Code`、`Name`、`Description`、`IsSystem`、`IsEnabled` |
| `RoleFunctionPermission` | 角色-功能映射（RoleId, FunctionId） |
| `RoleDataScope` | 角色-实体数据范围 |
| `RoleAssignment` | 用户-角色-组织绑定 |

### 4.2 数据范围类型
| 类型 | 含义 |
| --- | --- |
| All | 全部数据 |
| Self | 自己创建/拥有 |
| Organization | 当前组织 |
| OrgSubTree | 当前组织及子孙 |
| Custom | 自定义 DSL/OData 表达式 |

## 5. 功能树
### 5.1 默认节点
在 `AccessService` 中定义 `DefaultFunctionSeeds`，包括：

```
APP.ROOT
 ├─ APP.DASHBOARD ("/")
 ├─ APP.CUSTOMERS ("/customers")
 │   ├─ APP.CUSTOMERS.CREATE
 │   └─ APP.CUSTOMERS.EDIT
 ├─ APP.ENTITY ("/entity-definitions")
 ├─ APP.TEMPLATES ("/templates")
 ├─ APP.ORGANIZATIONS ("/organizations")
 └─ APP.SETTINGS ("/settings")
```

这些节点驱动：
- 左侧导航 / 面包屑；
- 权限授权粒度；
- 未来功能管理 UI。

### 5.2 API
- `GET /api/access/functions`：树形结构；
- `POST /api/access/functions`：新增节点；
- 后续可扩展 PUT/DELETE 供后台维护。

## 6. 角色与数据范围
### 6.1 创建角色
`POST /api/access/roles` 接收 `CreateRoleRequest`：
- 基础信息（OrganizationId, Code, Name...）；
- `FunctionIds`：多选功能节点；
- `DataScopes`：数组，每条包含 `EntityName + ScopeType + FilterExpression`。

服务端负责：
- 校验同组织下 Code 唯一；
- 批量添加 `RoleFunctionPermission`；
- 序列化 `RoleDataScope`。

### 6.2 用户角色分配
`POST /api/access/assignments`（`AssignRoleRequest`）：
- `UserId`、`RoleId`、`OrganizationId?`、`ValidFrom/To`；
- 不允许重复（同用户+角色+组织）。

查询 `(UserId, OrganizationId)` 时，取所有角色合并功能、合并数据范围（Allow/Deny 逻辑可扩展）。

## 7. 默认种子链条
在应用启动阶段：
1. **功能树种子**：缺少节点时自动补齐，父子关系依据 `ParentCode`，幂等更新。
2. **系统管理员角色**：`SYS.ADMIN` 若不存在则创建；存在时补齐缺失的功能/数据范围。
3. **admin 用户绑定**：找到用户名 `admin` 或邮箱 `admin@local` 的用户，写入 `RoleAssignments`（OrganizationId = null）。
4. **组织接口**：实体勾选 `IOrganizational` 时仅生成 `OrganizationId` 字段，由权限模块根据 RoleAssignment 限定。

## 8. 集成
### 8.1 与实体定义
- 实体中 `OrganizationId` 外键引用 `OrganizationNodes`；
- 发布/运行时根据 RoleAssignment 限制可访问组织；
- `FunctionNode` Code 与前端菜单一致，防止“功能存在但无菜单”。

### 8.2 与认证
- 登录成功后根据用户所选组织查询 `RoleAssignment` → 生成 Claims/缓存；
- 404/403 由统一授权中间件处理（后续任务）。

## 9. 测试策略
| 层级 | 重点 |
| --- | --- |
| 单元测试 | `OrganizationService`（PathCode、唯一约束）、`AccessService`（角色创建、管理员种子） |
| 集成测试 | `/api/organizations`、`/api/access` CRUD、默认种子幂等 |
| 前端测试 | 组织管理 UI（展开/收起/新增/删除）、角色配置页面（未来） |

## 10. 运维提示
- 每次部署需运行 `dotnet ef database update`（新增 Role/Function 表）；
- 首次启动需访问 `/api/setup/admin` 完成 admin 初始化；
- 若需重置菜单，可清空 `FunctionNodes` 并重启，AccessService 会重新种子。
