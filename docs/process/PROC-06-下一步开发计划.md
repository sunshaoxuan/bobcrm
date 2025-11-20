# 下一步开发计划

## 概述
本文档详细规划了 BobCRM v0.7.0 ~ v1.0 的开发任务，重点关注：
1. **动态枚举系统入口**：提供菜单访问与权限控制
2. **可视化菜单编辑器**：实现拖拽式菜单层级配置
3. **系统实体迁移补偿**：为 Users / Roles 实现被移除的功能组件

## 任务优先级

| 编号 | 任务名称 | 优先级 | 估算时间 | 依赖 |
|------|----------|--------|----------|------|
| T1 | 动态枚举管理入口 | 高 | 1-2天 | 无 |
| T2 | 可视化菜单编辑器 MVP | 高 | 3-5天 | 无 |
| T3 | UserRoleAssignment 组件 | 中 | 2-3天 | 无 |
| T4 | RolePermissionTree 组件 | 中 | 2-3天 | 无 |
| T5 | Organization 实体迁移 | 低 | 1-2天 | T3, T4 |
| T6 | 文档补全与同步 | 持续 | - | T1-T5 |

---

## T1: 动态枚举管理入口

### 目标
在系统菜单中添加 **枚举管理** 入口，使管理员可以直接访问 `/system/enums` 页面。

### 任务清单
- [ ] 在 `MenuService` 或静态菜单配置中注册菜单项：
  - 路径：`/system/enums`
  - 图标：`UnorderedListOutlined` 或 `DatabaseOutlined`
  - 功能码：`Menu_EnumManagement`
  - 父级：系统管理
- [ ] 为 `EnumManagement.razor` 添加权限检查（`@attribute [Authorize]` + `RequiredFunctionCode = "Menu_EnumManagement"`）
- [ ] 在 `PermissionSeeder` 中为系统管理员角色分配 `Menu_EnumManagement` 功能码
- [ ] 验证：
  - 管理员登录后可在侧边栏看到 **枚举管理** 入口
  - 非管理员用户看不到该菜单项
  - 点击后正确跳转到 `/system/enums` 页面

### 验收标准
- 管理员可通过侧边栏访问枚举管理页面
- 页面显示所有枚举列表（使用现有的 `EnumManagement.razor`）
- 权限控制正常工作

---

## T2: 可视化菜单编辑器 MVP

### 目标
实现拖拽式菜单编辑器，支持层级配置、权限码关联、保存与预览。

### 架构设计
参考 [ARCH-24-紧凑型顶部菜单导航设计](../design/ARCH-24-紧凑型顶部菜单导航设计.md) 和 [ARCH-24-实施计划](ARCH-24-紧凑型顶部菜单导航-实施计划.md)。

### 任务清单

#### 2.1 后端 API
- [ ] 创建 `MenuEditorService`（或扩展现有 `MenuService`）：
  - `GetMenuTreeAsync()`：获取完整菜单树结构（包括系统菜单和自定义菜单）
  - `SaveMenuTreeAsync(TreeNode[] nodes)`：保存整个菜单树
  - `EnsureFunctionCodesAsync()`：自动为菜单项生成/更新功能码
- [ ] 定义 DTO：
  - `MenuNodeDto`：包含 `Id`, `ParentId`, `Name`, `Icon`, `Route`, `Order`, `FunctionCode`, `Children`
  - `MenuLinkType`：枚举（实体路由 / 外部 URL / 自定义页面）
- [ ] 实现权限码自动关联逻辑：
  - 保存菜单时调用 `PermissionService.EnsureFunctionCode(menuItem.FunctionCode)`
  - 为每个菜单项生成默认功能码（如 `Menu_{EntityName}` 或 `Menu_{CustomName}`）

#### 2.2 前端组件
- [ ] 创建 `MenuEditor.razor`：
  - 使用 `AntDesign.Tree` 组件展示菜单树
  - 集成拖拽功能（`Draggable="true"`）
  - 提供 **新增**、**编辑**、**删除** 按钮
- [ ] 创建 `MenuNodeForm.razor`：
  - 编辑单个菜单节点的表单
  - 字段：名称、图标选择器、链接类型、路由/URL、功能码、是否启用
- [ ] 实现图标选择器（参考 Ant Design 图标库）
- [ ] 实现保存逻辑：
  - 调用 `MenuEditorService.SaveMenuTreeAsync`
  - 成功后触发 `Sidebar` 刷新（通过事件或 SignalR）

#### 2.3 权限与菜单集成
- [ ] 在侧边栏中添加 **菜单编辑器** 入口（仅管理员可见）
- [ ] 实现 `MenuChanged` 事件：保存后通知 `Sidebar` 刷新
- [ ] 权限校验：确保只有拥有 `Menu_Editor` 功能码的用户可以访问

### 验收标准
- 管理员可以拖拽排序菜单项
- 可以新增/编辑/删除菜单节点
- 保存后侧边栏立即更新
- 功能码自动生成并同步到权限表

---

## T3: UserRoleAssignment 组件

### 目标
为用户详情页提供角色分配功能，替代之前在 `Users.razor` 中硬编码的 UI。

### 任务清单
- [ ] 创建 `UserRoleAssignment.razor` 组件：
  - 接收 `UserId` 参数
  - 显示用户当前拥有的角色列表
  - 提供 **添加角色** 和 **移除角色** 功能
- [ ] 后端 API（如不存在）：
  - `GET /api/users/{id}/roles`：获取用户角色
  - `POST /api/users/{id}/roles/{roleId}`：添加角色
  - `DELETE /api/users/{id}/roles/{roleId}`：移除角色
- [ ] 集成到用户详情模板：
  - 在 `DefaultTemplateGenerator` 中为 User 实体生成包含 `UserRoleAssignment` 的详情模板
  - 或在 `PageLoader` 中动态嵌入组件

### 验收标准
- 用户详情页显示角色分配组件
- 可以添加/移除角色
- 权限正确控制（只有拥有 `User_AssignRole` 功能码的用户可操作）

---

## T4: RolePermissionTree 组件

### 目标
为角色详情页提供权限树编辑功能，替代之前在 `Roles.razor` 中的权限树 UI。

### 任务清单
- [ ] 创建 `RolePermissionTree.razor` 组件：
  - 接收 `RoleId` 参数
  - 使用 `AntDesign.Tree` 展示权限树（按模块/功能分组）
  - 支持复选框（已授权的权限显示为选中状态）
- [ ] 后端 API（如不存在）：
  - `GET /api/roles/{id}/permissions`：获取角色权限
  - `POST /api/roles/{id}/permissions`：批量更新角色权限
- [ ] 实现批量授权/撤销功能
- [ ] 集成到角色详情模板

### 验收标准
- 角色详情页显示权限树组件
- 可以勾选/取消勾选权限
- 支持批量操作（如"全选"、"清空"）
- 保存后立即生效

---

## T5: Organization 实体迁移

### 目标
将 Organization 实体迁移到模板驱动视图，并提供组织树拖拽排序功能。

### 任务清单
- [ ] 修改 `Organization.EntityRoute` 为 `organization`（单数）
- [ ] 重构 `Organizations.razor` 使用 `ListTemplateHost EntityType="organization"`
- [ ] 评估是否需要自定义 Widget：
  - 如需树形展示，创建 `OrganizationTreeWidget`
  - 或在列表模板中使用 `TreeTable` 组件
- [ ] 实现拖拽排序（如需要）
- [ ] 更新 `PROC-07-系统实体模板化迁移记录.md`

### 验收标准
- 组织列表页使用模板驱动
- 支持树形展示（如适用）
- 原有功能不受影响

---

## T6: 文档补全与同步

### 持续任务
- [ ] 每完成一个任务后，更新对应的文档：
  - `GUIDE-XX-XXX.md`：使用指南
  - `PROC-07-系统实体模板化迁移记录.md`：迁移记录
  - `ROADMAP.md`：路线图进度
  - `CHANGELOG.md`：变更日志
- [ ] 在 `PROC-00-文档索引.md` 中注册新文档
- [ ] 确保所有文档遵循项目命名规范：`分类-编号-标题.md`

---

## 里程碑

| 里程碑 | 包含任务 | 完成标志 | 预计时间 |
|--------|----------|----------|----------|
| **M1: 功能入口完善** | T1 | 枚举管理入口在侧边栏可访问 | 1周 |
| **M2: 菜单编辑器 MVP** | T2 | 可视化菜单编辑器上线，支持拖拽和保存 | 2周 |
| **M3: 系统实体补偿** | T3, T4 | Users 和 Roles 页面功能完整 | 2周 |
| **M4: v1.0 准备** | T5, T6 | 所有系统实体完成迁移，文档齐全 | 3周 |

---

## 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| **拖拽组件性能问题**（大量菜单项时） | 用户体验下降 | 使用虚拟滚动（Ant Design Tree 支持）；限制菜单层级深度 |
| **权限码同步失败** | 菜单可见但无权限访问，或反之 | 在保存菜单时进行事务处理，确保菜单与权限码原子性更新 |
| **组件嵌入模板的技术难度** | 自定义组件无法在模板中渲染 | 扩展 `WidgetRegistry`，支持动态组件注册；或使用 RenderFragment |
| **文档漂移** | 代码与文档不一致 | PR 检查清单中强制要求文档更新；CI 检查文档链接有效性 |

---

## 参考文档
- [ROADMAP](../ROADMAP.md)
- [ARCH-24-紧凑型顶部菜单导航设计](../design/ARCH-24-紧凑型顶部菜单导航设计.md)
- [ARCH-24-实施计划](ARCH-24-紧凑型顶部菜单导航-实施计划.md)
- [ARCH-26-动态枚举系统设计](../design/ARCH-26-动态枚举系统设计.md)
- [PROC-07-系统实体模板化迁移记录](PROC-07-系统实体模板化迁移记录.md)
