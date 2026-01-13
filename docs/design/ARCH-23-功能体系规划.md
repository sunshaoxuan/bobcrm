# ARCH-23 功能体系规划

> 版本：0.2（2025-11-13）  
> 目的：梳理 OneCRM 的 3 级功能蓝图，标记现有实现与缺口，作为后续开发/验收基线。

---

## 1. 背景与目标

- **统一导航**：后续菜单、权限、模板分配都以同一套功能树为依据，解决“页面写死/权限散落”的问题。  
- **可观测性**：通过状态标记（✅ 已上线｜🟡 进行中｜⚪ 未启动）快速识别交付差距。  
- **规划驱动**：将功能树映射到产品路线图与开发计划，支持分阶段上线。
- **系统基础设施**：定义统一的邮件、消息与任务调度接口。

## 2. 三层功能树

| 领域（第 1 级） | 模块（第 2 级） | 功能（第 3 级） | 状态 | 说明 |
| --- | --- | --- | --- | --- |
| 1. 系统管理 | 1.1 系统设置 | 1.1.1 系统设置 | ⚪ | 仅有基础 Settings API，占位待实现配置 UI |
|  | 1.2 邮件与消息 | 1.2.1 邮件服务器<br>1.2.2 系统通知<br>1.2.3 消息模板 | ⚪ | 暂无实现 |
|  | 1.3 实体管理 | 1.3.1 业务实体编辑 | ✅ | `EntityDefinitionEdit/Publish`、动态实体管线已上线 |
|  | 1.4 模板管理 | 1.4.1 模板设计（表单设计器）<br>1.4.2 模板分配（节点/用户） | 🟡 | Designer 已上线；模板绑定与运行态刚接入，分配 UI 未做 |
|  | 1.5 日志管理 | 1.5.1 用户使用记录<br>1.5.2 系统日志 | ⚪ | 仅保留数据库/Serilog 日志，无 UI |
|  | 1.6 人工智能 | 1.6.1 模型设置<br>1.6.2 工作流程<br>1.6.3 智能体 | ⚪ | 未启动 |
| 2. 基本设置 | 2.1 组织管理 | 2.1.1 组织档案 | ✅ | `OrganizationManagement.razor` + API 已上线 |
|  |  | 2.1.2 角色管理 | ✅ | 角色档案页面（角色信息 + 权限树）已上线，支持 FunctionNodes 权限分配 |
|  | 2.2 用户与权限 | 2.2.1 用户档案 | ✅ | `/api/users` + `Users.razor`（列表/详情/创建/密码+状态编辑）已上线，并复用权限树路由引导 |
|  |  | 2.2.2 角色权限分配 | ✅ | `BAS.AUTH.ROLE.PERM` 菜单打开 `/roles`，角色档案页提供 FunctionNodes 权限树与数据范围维护 |
|  |  | 2.2.3 用户角色分配 | ✅ | `BAS.AUTH.USER.ROLE` 菜单指向 `/users`，用户档案内置角色面板并限制为拥有此功能码的人员可操作 |
| 3. 客户关系 | 3.1 基本档案 | 3.1.1 客户主档<br>3.1.2 合约管理<br>3.1.3 实施管理<br>3.1.4 运维管理 | 🟡 | 客户主档有初版列表/详情模板；其他子模块未实现 |
|  | 3.2 计划管理 | 3.2.1 计划任务<br>3.2.2 日程安排<br>3.2.3 作业执行<br>3.2.4 作业看板 | ⚪ | 未启动 |
| 4. 知识库 | 4.1 问与答 | 4.1.1 常见问答<br>4.1.2 客户问答库<br>4.1.3 答复模板 | ⚪ | 未启动 |
|  | 4.2 产品与补丁 | 4.2.1 产品版本<br>4.2.2 补丁管理<br>4.2.3 客户部署记录 | ⚪ | 未启动 |
|  | 4.3 文档与手册 | 4.3.1 产品文档<br>4.3.2 操作手册 | ⚪ | 未启动 |
| 5. 工作协作 | 5.1 工作记录 | 5.1.1 个人记录<br>5.1.2 工作日志 | ⚪ | 未启动 |
|  | 5.2 文件管理 | 5.2.1 附件管理<br>5.2.2 评论历史 | ⚪ | 未启动 |

> 以上表述为业务蓝图，不同节点可对应一个或多个菜单/页面/模板。

## 3. 实现现状概览

| 功能节点 | 后端 | 前端 | 备注 |
| --- | --- | --- | --- |
| 1.3.1 业务实体编辑 | ✅ `EntityDefinitionEndpoints`, 发布/编译服务 | ✅ `EntityDefinitionEdit/Publish.razor` | 动态实体流程闭环 |
| 1.4.1 模板设计 | ✅ FormTemplate CRUD + Designer API | ✅ `FormDesigner.razor` | 支持 FormTemplate 存储 |
| 1.4.2 模板分配 | ✅ TemplateBinding + RuntimeService | 🟡 PageLoader 接入 runtime，缺分配 UI | 需统一 List/Detail 宿主 |
| 2.1.1 组织档案 | ✅ OrganizationService + API | ✅ `OrganizationManagement.razor` | 已支持树结构 CRUD |
| 2.1.2 角色管理 | ✅ RoleProfile/Function/DataScope/Assignment API | ✅ `Roles.razor`（角色列表+信息+FunctionNodes 权限树） | 同步写回角色-功能/数据范围，并展示系统角色只读提示；入口在“用户与权限 > 角色权限分配” |
| 2.2.1 用户档案 | ✅ `/api/users` CRUD + 角色分配端点 | ✅ `Users.razor`（双栏列表/详情 + 角色勾选 + 创建模态 + 密码管理） | 配套 `UserService` 与 `UserManagementTests` 覆盖查询/创建/角色更新 |
| 3.1.1 客户主档 | ✅ Customer CRUD + 动态字段 | 🟡 `Customers.razor`(静态列表)、`PageLoader`（详情） | 列表需模板化、详情已接 runtime |

其余节点暂为空白或仅有数据库/日志基础，需按路线图补齐。

## 4. 完成库（Reference Implementation）

| 分类 | 可复用资产 | 说明 |
| --- | --- | --- |
| 模板系统 | FormTemplate + TemplateBinding + TemplateRuntime | 已能支撑“实体详情模板化”，正扩展到列表/模块模板 |
| 权限系统 | AccessService（功能树 & 数据范围）、RoleProfile/Assignment | 可用于 1.x、2.x 的所有“权限管理”节点，需要 UI 层封装 |
| 动态实体 | EntityDefinition + DynamicEntityService + PageLoader | 可构建 3.x、4.x 等业务模块的 CRUD |
| 组织能力 | OrganizationNode + IOrganizational 接口 | 允许任意实体勾选组织维度后自动具备 OrgId 字段 |

这些模块构成“完成库”，可快速复用到新的顶层功能，只需补齐 UI/流程。

## 5. 开发规划

| 阶段 | 目标 | 关键工作 |
| --- | --- | --- |
| Phase A（进行中） | 模板化打底 | ① 完成运行态 API + PageLoader 接入（已完成）<br>② 设计 ListTemplateHost 并迁移 Customers 列表 |
| Phase B | 基础设置闭环 | ① 角色管理 UI + 权限树<br>② 用户档案/角色分配界面<br>③ 组织-角色-模板联动（默认模板按组织分配） |
| Phase C | 系统管理增强 | ① 模板分配中心（领域/节点/用户）<br>② 系统设置/日志管理界面<br>③ 邮件与消息基础能力 |
| Phase D | 业务域扩展 | ① 客户关系各子模块<br>② 知识库 / 工作协作初版<br>③ 引入行为 DSL（ARCH-22 Phase3） |

## 6. 后续动作

1. **功能树落地**：将上表转换为 FunctionNode 种子（三级编码，如 `SYS.TEMPLATE.DESIGN`），并与菜单、权限、模板绑定共享。  
2. **路线图同步**：产品/项目会议以本文件为基准，明确每个迭代覆盖哪些节点。  
3. **验收与回归**：上线前更新“状态列”，确保文档与实现一致，成为回归 checklist。

> **进度更新（2025-11-13）**：`AccessService` 中的 `DefaultFunctionSeeds` 已按照本文三层结构写入 `FunctionNodes` 表，支持后续的权限分配与菜单渲染；新的编码前缀（SYS/BAS/CRM/KB/COLLAB）请在角色、模板绑定等场景沿用。

> **补充（2025-11-14）**：`BAS.AUTH.ROLE.PERM` 与 `BAS.AUTH.USER.ROLE` 现已标记为菜单节点，分别映射到 `/roles` 与 `/users`。同一模块下的 API 启用函数码过滤器（`FunctionPermissionFilter`），与菜单曝光保持一致；系统会在首次启动时自动创建 `admin`/`Admin@12345` 账号并绑定 `SYS.ADMIN` 角色，保障从零环境也能访问这些页面。

## 7. 菜单展示机制设计

### 7.1 交互原则
- **领域切换与领域导航分离**：左侧第一层仅显示“领域切换器”（SYS、BAS、CRM、KB、COLLAB 等），保持视觉简洁；切换领域后，右侧/二栏即时刷新该领域的模块+功能列表。
- **三层呈现**：
  - **领域栏**：只显示领域图标+名称 + 当前选中高亮（可折叠为图标列）。
  - **模块面板**：卡片/分组形式展示二级模块，模块标题下列出三级功能菜单。
  - **功能列表**：每个功能条目包含图标、名称、可选描述，支持点击跳转或展开操作。
- **图标必填**：FunctionNode 中 `Icon` 字段成为必填项，UI 上所有菜单均显示对应图标，未配置则显示通用占位符。
- **响应式布局**：窄屏时将领域切换折叠为抽屉，模块/功能以折叠面板呈现。

### 7.2 技术方案
- 前端组件：
  1. `DomainSwitcher`：读取 `FunctionNodes` 的一级节点，渲染为垂直图标栏；交互事件发出 `OnDomainChanged(code)`。
  2. `DomainMenuPanel`：接收当前领域 code，加载其所有子节点（模块/功能），以 `Collapse`/`Card` 方式展示，并按 `SortOrder` 排序。
  3. `MenuTile`：封装图标+标题+描述，支持自定义颜色/背景（后续可与主题联动）。
- 数据来源：沿用 `FunctionNodes` 表，前端通过 `/api/access/functions`（待扩展：返回扁平列表 + 权限过滤）获取当前用户可见的节点。
- 权限控制：仅渲染当前用户拥有的 FunctionNodes，保持菜单与权限一致。

### 7.3 实施计划
1. **后端补充**：
   - 扩展 `AccessEndpoints`：提供 `GET /api/access/functions/tree`（按当前用户返回可见节点，含 Icon/Route）。
   - 保证 FunctionNode Icon 非空（种子已覆盖，可在创建/更新 API 加校验）。
2. **前端开发**：
   - 新增 `DomainSwitcher.razor`、`DomainMenuPanel.razor` 等组件。
   - 在 Shell/Layout 中引入领域切换 + 模块菜单，替换旧的扁平导航。
   - 设计稿需体现高对比、图标统一、悬停动画等美术要求。
3. **测试**：
   - 单元：FunctionNodes 接口权限过滤、Icon 校验。
   - 前端组件测试：领域切换状态、模块展开/收起、无权限/空数据等边界。
   - 回归：验证现有路由（如 `/entity-definitions`、`/templates`）能在新菜单下正确跳转。
4. **提交**：完成菜单组件与 API 后，更新 `CHANGELOG`、`ARCH-23`（状态列）并附截图说明。

### 7.4 当前交付（2025-11-13）
- **后端**：`GET /api/access/functions/me` 会根据用户的角色权限过滤 FunctionNodes，并返回带有 Icon/Route 的三层树结构（含祖先节点）。
- **前端**：`MainLayout` 增加了领域切换 rail 与领域菜单面板，自动根据权限切换领域并展示二级、三级菜单；菜单图标取自 FunctionNodes，可刷新实时更新。
- **权限联动**：只要未授予相应领域/功能的 FunctionNode，菜单中该领域即不会出现，确保“所见即所得”的权限体验。
- **角色管理**：新增 `/roles` 页面，可查看/编辑角色基本信息，并基于完整 FunctionNodes 树勾选权限，提交后与系统菜单联动。

---

> 负责人：产品架构（文档）、平台研发（模板与权限）、业务团队（领域实现）。更新频率：迭代内如有状态变更需同步。***
