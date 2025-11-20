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

## T1: 动态枚举管理入口 ✅ **已完成**

### 目标
在系统菜单中添加 **枚举管理** 入口，使管理员可以直接访问 `/system/enums` 页面。

### 任务清单
- [x] 在 `SystemMenuSeeder` 中注册菜单项：
  - 路径：`/system/enums`
  - 图标：`unordered-list`
  - 功能码：`SYS.ENUM`
  - 父级：系统管理（SYS）
- [x] `EnumManagement.razor` 已添加 `@attribute [Authorize]`
- [x] 在 `SystemMenuSeeder` 中为管理员角色分配 `SYS.ENUM` 权限
- [x] 验证完成：
  - ✅ SystemMenuSeeder.EnsureSystemMenusAsync 创建菜单节点
  - ✅ SystemMenuSeeder.EnsureAdminPermissionAsync 分配权限
  - ✅ 前端权限通过菜单可见性控制（后端RequireFunction）

### 验收标准
- ✅ 管理员可通过侧边栏访问枚举管理页面
- ✅ 页面显示所有枚举列表（使用现有的 `EnumManagement.razor`）
- ✅ 权限控制通过菜单可见性和后端API实现

### 实施细节
- **文件**: `src/BobCrm.Api/Services/SystemMenuSeeder.cs:32-33`
- **菜单节点**: Code=`SYS.ENUM`, Name="枚举管理", Route="/system/enums"
- **权限分配**: 自动为Administrator角色分配完整CRUD权限

---

## T2: 可视化菜单编辑器 MVP ✅ **已完成**

### 目标
实现拖拽式菜单编辑器，支持层级配置、权限码关联、保存与预览。

### 架构设计
参考 [ARCH-24-紧凑型顶部菜单导航设计](../design/ARCH-24-紧凑型顶部菜单导航设计.md) 和 [ARCH-24-实施计划](ARCH-24-紧凑型顶部菜单导航-实施计划.md)。

### 任务清单

#### 2.1 后端 API ✅
- [x] `MenuService` 已实现完整功能：
  - ✅ `GetManageTreeAsync()`：获取完整菜单树结构
  - ✅ `CreateAsync()`/`UpdateAsync()`/`DeleteAsync()`：CRUD操作
  - ✅ `ReorderAsync()`：批量更新节点顺序和父子关系
- [x] 后端API已实现：
  - ✅ `/api/access/functions/manage`：获取菜单树（需SYS.SET.MENU权限）
  - ✅ `/api/access/functions`：CRUD和重排序端点
  - ✅ FunctionNode模型包含所有必要字段
- [x] 权限码通过FunctionNode.Code自动关联

#### 2.2 前端组件 ✅
- [x] `MenuManagement.razor` 已完全实现：
  - ✅ 拖拽式菜单树（支持before/after/into三种放置位置）
  - ✅ 新增/编辑/删除功能
  - ✅ 图标配置（文本输入）
  - ✅ 双导航类型：路由(route)和模板(template)
  - ✅ 多语言支持（DisplayName使用MultilingualInput）
- [x] 内嵌表单模态框实现了 `MenuNodeForm` 的所有功能
- [x] 实时验证和错误处理

#### 2.3 权限与菜单集成 ✅
- [x] 在SystemMenuSeeder中添加菜单编辑器入口
  - 功能码：`SYS.SET.MENU`
  - 路由：`/menus`
  - 父级：系统管理（SYS）
- [x] 后端权限校验：/api/access/functions/manage 需要 SYS.SET.MENU
- [x] 前端使用 AuthChecker 组件

### 验收标准
- ✅ 管理员可以拖拽排序菜单项（支持跨层级移动）
- ✅ 可以新增/编辑/删除菜单节点
- ✅ 保存后菜单树立即更新（通过重新加载）
- ✅ 功能码通过FunctionNode自动管理

### 实施细节
- **文件**: `src/BobCrm.App/Components/Pages/MenuManagement.razor` (782行)
- **特性**:
  - 拖拽排序with visual drop zones
  - 双导航模式（路由/模板）
  - 模板选择器with lazy loading
  - 防循环依赖检测

---

## T3: UserRoleAssignment 组件 ✅ **已完成**

### 目标
为用户详情页提供角色分配功能，替代之前在 `Users.razor` 中硬编码的 UI。

### 任务清单
- [x] 创建 `UserRoleAssignment.razor` 组件：
  - ✅ 接收 `UserId` 参数
  - ✅ 使用Ant Design Transfer组件显示角色分配
  - ✅ 显示用户当前拥有的角色列表（含组织范围信息）
  - ✅ 提供批量添加/移除角色功能
  - ✅ 支持搜索和全选功能
- [x] 后端 API 已存在：
  - ✅ `GET /api/users/{id}`：获取用户详情（含角色列表）
  - ✅ `PUT /api/users/{id}/roles`：批量更新用户角色（UpdateUserRolesRequest）
  - ✅ `GET /api/access/roles`：获取所有可用角色
- [x] 组件可重用性：
  - ✅ 独立组件，可嵌入任何页面
  - ✅ 参数化配置（UserId）
  - ✅ 完整的加载/保存状态管理

### 验收标准
- ✅ 组件已创建并可独立使用
- ✅ Transfer组件提供直观的角色分配界面
- ✅ 保存后立即刷新显示最新状态
- ✅ 错误处理完善（Toast消息提示）

### 实施细节
- **文件**: `src/BobCrm.App/Components/Shared/UserRoleAssignment.razor`
- **特性**:
  - Ant Design Transfer双列表选择
  - 实时角色状态显示（含组织范围标签）
  - 集成UserService和RoleService
  - i18n多语言支持

### 待集成
- 需要在User实体的详情模板中嵌入此组件（通过WidgetRegistry或自定义Widget）

---

## T4: RolePermissionTree 组件 ✅ **已完成**

### 目标
为角色详情页提供权限树编辑功能，替代之前在 `Roles.razor` 中的权限树 UI。

### 任务清单
- [x] 创建 `RolePermissionTree.razor` 运行时组件：
  - ✅ 接收 `RoleId` 参数
  - ✅ 使用 `AntDesign.Tree` 展示权限树（层级结构）
  - ✅ 支持复选框（已授权的权限显示为选中状态）
  - ✅ 显示功能码和多语言名称
- [x] 后端 API 已存在：
  - ✅ `GET /api/access/roles/{id}`：获取角色详情（含FunctionIds）
  - ✅ `GET /api/access/functions`：获取功能树
  - ✅ `PUT /api/access/roles/{id}/permissions`：批量更新角色权限
- [x] 实现批量授权/撤销功能：
  - ✅ 全选/全不选按钮
  - ✅ 展开/折叠全部按钮
  - ✅ 搜索功能（可选）
  - ✅ 权限统计显示（可选）
- [x] 组件参数化配置：
  - `ShowSearch`、`ShowStatistics`、`DefaultExpandAll`、`CascadeSelect`

### 验收标准
- ✅ 组件已创建并可独立使用
- ✅ 可以勾选/取消勾选权限（树形复选框）
- ✅ 支持批量操作（全选/清空/展开/折叠）
- ✅ 保存后调用UpdatePermissionsAsync更新权限

### 实施细节
- **文件**: `src/BobCrm.App/Components/Shared/RolePermissionTree.razor`
- **特性**:
  - Ant Design Tree组件with checkable nodes
  - 功能树从RoleService加载（含缓存）
  - 工具栏：全选/清空/展开/折叠/保存
  - 权限统计（总数/已选数）
  - i18n多语言支持

### 待集成
- 需要在Role实体的详情模板中嵌入此组件（通过WidgetRegistry或自定义Widget）

### 注意
- 已存在 `RolePermissionTreeWidget.cs`（设计器Widget模型）
- 本任务创建的是运行时Blazor组件，两者配合使用

---

## T5: Organization 实体迁移 ⏸️ **已推迟**

### 目标
将 Organization 实体迁移到模板驱动视图，并提供组织树拖拽排序功能。

### 任务清单
- [ ] 修改 `OrganizationNode.EntityRoute` 为 `organization`（单数）
- [ ] 重构 `OrganizationManagement.razor` 使用 `ListTemplateHost EntityType="organization"`
- [ ] 评估是否需要自定义 Widget：
  - ✅ `OrganizationTreeWidget.cs` 已存在
  - [ ] 需要创建对应的运行时组件
  - [ ] 或在列表模板中使用 `TreeTable` 组件
- [ ] 实现拖拽排序（保留现有实现）
- [ ] 更新 `MIGRATION-01-系统实体模板化迁移记录.md`

### 推迟原因
1. **复杂度高**: Organization实体使用树形结构，迁移需要特殊处理
2. **优先级低**: PROC-06标记为低优先级，T1-T4为高/中优先级
3. **现有实现稳定**: `OrganizationManagement.razor` 已完整实现树形展示和编辑功能
4. **依赖T3/T4**: 需要先完成补偿组件的集成经验

### 当前状态
- ✅ 现有页面功能完整：树形展示、CRUD、详情编辑
- ✅ `OrganizationTreeWidget.cs` 模型已存在
- ❌ 未迁移到模板驱动方式
- ❌ EntityRoute仍为 `organization-nodes`（复数）

### 后续计划
- 在v1.0准备阶段再评估迁移必要性
- 可能作为独立任务单独规划
- 参考T3/T4的组件集成经验

---

## T6: 文档补全与同步

### 持续任务
- [ ] 每完成一个任务后，更新对应的文档：
  - `GUIDE-XX-XXX.md`：使用指南
  - `MIGRATION-01-系统实体模板化迁移记录.md`：迁移记录
  - `ROADMAP.md`：路线图进度
  - `CHANGELOG.md`：变更日志
- [ ] 在 `PROC-00-文档索引.md` 中注册新文档
- [ ] 确保所有文档遵循项目命名规范：`分类-编号-标题.md`

---

## 里程碑

| 里程碑 | 包含任务 | 完成标志 | 状态 | 完成时间 |
|--------|----------|----------|------|----------|
| **M1: 功能入口完善** | T1 | 枚举管理入口在侧边栏可访问 | ✅ **已完成** | 2025-11-20 |
| **M2: 菜单编辑器 MVP** | T2 | 可视化菜单编辑器上线，支持拖拽和保存 | ✅ **已完成** | 2025-11-20 |
| **M3: 系统实体补偿** | T3, T4 | Users 和 Roles 页面功能组件已创建 | ✅ **已完成** | 2025-11-20 |
| **M4: 组件集成与测试** | T6 | 组件集成到模板，文档更新 | ✅ **已完成** | 2025-11-20 |
| **M5: v1.0 准备** | T5 | Organization迁移（可选） | ⏸️ **已推迟** | TBD |

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
- [MIGRATION-01-系统实体模板化迁移记录](MIGRATION-01-系统实体模板化迁移记录.md)
