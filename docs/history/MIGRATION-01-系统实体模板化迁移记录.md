# 系统实体模板化迁移记录

## 目标
记录系统实体（Customer、User、Role 等）从硬编码页面迁移到模板驱动视图的过程、路由改动以及已移除的功能。

## 迁移概览
| 实体 | 旧路由 | 新路由 | 迁移状态 | 关键改动 |
|------|--------|--------|----------|----------|
| **Customer** | `/customers` | `/customer` | ✅ 已完成 | `Customer.EntityRoute` 改为 `customer`，`Customers.razor` 改为 `ListTemplateHost EntityType="customer"`，详情页使用 `PageLoader` (`/customer/{id}`) |
| **User** | `/users` | `/user` | ✅ 已完成 | `SystemUser.EntityRoute` 改为 `user`，`Users.razor` 改为 `ListTemplateHost EntityType="user"`，详情页使用 `PageLoader` (`/user/{id}`) |
| **Role** | `/roles` | `/role` | ✅ 已完成 | `RoleProfile.EntityRoute` 改为 `role`，`Roles.razor` 改为 `ListTemplateHost EntityType="role"`，详情页使用 `PageLoader` (`/role/{id}`) |
| **Organization** | `/organizations` | `/organization` | ⬜ 待迁移 | - |

## 已完成的工作

### 1. Customer 实体迁移（v0.6.0）
- **路由更新**：`EntityRoute` 改为 `customer`（单数形式）
- **列表页面迁移**：`Customers.razor` 改为使用 `ListTemplateHost EntityType="customer"`
- **DataGridRuntime 增强**：
  - 实现 `RowActionsJson` 解析，支持 Edit/Delete 按钮
  - 实现行点击导航到详情页（`/customer/{id}`）
  - 确保 `ApiEndpoint` 正确映射到实体 API（`/api/customers`）
- **自动模板生成**：`EntityDefinitionSynchronizer` 在同步实体时自动创建模板和绑定

### 2. User 实体迁移（v0.6.0）
- **路由更新**：`EntityRoute` 改为 `user`
- **列表页面迁移**：`Users.razor` 改为使用 `ListTemplateHost EntityType="user"`
- **详情页统一**：通过通用 `PageLoader` 组件加载实体详情（`/user/{id}`）
- **移除功能**：角色分配 UI 已移除（见下文"待补偿功能"）

### 3. Role 实体迁移（v0.6.0）
- **路由更新**：`EntityRoute` 改为 `role`
- **列表页面迁移**：`Roles.razor` 改为使用 `ListTemplateHost EntityType="role"`
- **详情页统一**：通过通用 `PageLoader` 组件加载实体详情（`/role/{id}`）
- **移除功能**：权限树 UI 已移除（见下文"待补偿功能"）

## 待补偿功能（计划中）

| 功能 | 原位置 | 说明 | 计划补偿方案 | 优先级 |
|------|--------|------|--------------|--------|
| **角色分配** | `Users.razor` 中的角色分配按钮 | 已从硬编码 UI 中移除 | 实现 `UserRoleAssignment` 组件，在用户详情页（模板）中嵌入 | 高 |
| **权限树** | `Roles.razor` 中的权限树 UI | 已移除 | 实现 `RolePermissionTree` 组件，在角色详情页（模板）中嵌入 | 高 |
| **组织树** | `Organizations` 页面（如存在） | 可能需要特殊的树形展示 | 实现 `OrganizationTree` 组件或使用自定义模板 Widget | 中 |

## 技术细节

### 路由规范
- 统一使用 **单数形式** 作为 `EntityRoute`（如 `customer`、`user`、`role`）
- 列表页：`/{entity}`（如 `/customer`）
- 详情页：`/{entity}/{id}`（如 `/customer/123`）
- 编辑页：`/{entity}/{id}/edit`（可选，目前通过详情页内嵌编辑）

### PageLoader 组件
- 统一处理实体详情页的加载逻辑
- 根据 `EntityType` 参数动态加载对应的模板
- 支持路由参数（如 `id`）的传递

### 模板自动生成
`EntityDefinitionSynchronizer.EnsureTemplatesAndBindingsAsync` 会为每个系统实体自动生成：
- **List 模板**：包含 `DataGridWidget`，自动配置列和行操作
- **Detail 模板**：包含 `FormWidget`，展示实体详细信息
- **Edit 模板**：包含可编辑的 `FormWidget`

## 后续步骤
1. **实现 `UserRoleAssignment` 组件**：
   - 提供角色分配 UI
   - 支持多选、搜索、权限校验
   - 可嵌入到用户详情模板中
2. **实现 `RolePermissionTree` 组件**：
   - 提供权限树编辑功能
   - 使用 Ant Design `Tree` 组件
   - 支持批量授权/撤销
3. **迁移 Organization 实体**：
   - 评估是否需要特殊的树形展示组件
   - 实现组织树拖拽排序功能
4. **更新模板生成器**：
   - 为特殊实体（User、Role）生成包含自定义组件的模板
   - 支持模板中嵌入自定义 Widget

## 参考文档
- [ARCH-22-标准实体模板化与权限联动设计](../design/ARCH-22-标准实体模板化与权限联动设计.md)
- [ROADMAP](../ROADMAP.md) - 1.1 系统实体迁移
- [CHANGELOG](../../CHANGELOG.md) - v0.6.0 客户列表模板化迁移
