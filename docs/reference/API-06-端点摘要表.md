# 端点分析摘要表

## 按端点文件快速参考

### 1. AccessEndpoints.cs (12 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 获取功能树 | GET | /api/access/functions | 匿名 (Results.Ok) | 高 | 无 |
| 获取用户功能 | GET | /api/access/functions/me | 匿名 (Results.Ok) | 中 | 无 |
| 创建功能 | POST | /api/access/functions | FunctionNodeDto (Results.Ok) | 低 | CreateFunctionRequest |
| 获取角色 | GET | /api/access/roles | RoleProfile (Results.Ok) | 中 | 无 |
| 创建角色 | POST | /api/access/roles | RoleProfile (Results.Ok) | 中 | CreateRoleRequest |
| 获取角色详情 | GET | /api/access/roles/{roleId} | 匿名 (Results.Json) | 高 | 无 |
| 更新角色 | PUT | /api/access/roles/{roleId} | 匿名 (Results.Ok) | 高 | UpdateRoleRequest |
| 删除角色 | DELETE | /api/access/roles/{roleId} | NoContent | 低 | 无 |
| 更新权限 | PUT | /api/access/roles/{roleId}/permissions | 匿名 (Results.Ok) | 高 | UpdatePermissionsRequest |
| 创建分配 | POST | /api/access/assignments | RoleAssignment (Results.Ok) | 中 | AssignRoleRequest |
| 获取用户分配 | GET | /api/access/assignments/user/{userId} | 匿名 (Results.Ok) | 高 | 无 |
| 删除分配 | DELETE | /api/access/assignments/{assignmentId} | NoContent | 低 | 无 |

---

### 2. AdminEndpoints.cs (5 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 数据库健康检查 | GET | /api/admin/db/health | 匿名 (Results.Json) | 高 | 无 |
| 重建数据库 | POST | /api/admin/db/recreate | 匿名 (Results.Ok) | 中 | 无 |
| 调试列出用户 | GET | /api/debug/users | 匿名 (Results.Ok) | 高 | 无 |
| 调试重置设置 | POST | /api/debug/reset-setup | 匿名 (Results.Ok) | 中 | 无 |
| 重置密码 | POST | /api/admin/reset-password | 匿名 (Results.Ok) | 高 | ResetPasswordDto |

---

### 3. AuthEndpoints.cs (8 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 注册 | POST | /api/auth/register | 匿名 (Results.Ok) | 中 | RegisterDto |
| 激活 | GET | /api/auth/activate | 匿名 (Results.Ok) | 中 | 无 (查询参数) |
| 登录 | POST | /api/auth/login | 匿名 (Results.Json) | 高 | LoginDto |
| 刷新令牌 | POST | /api/auth/refresh | 匿名 (Results.Json) | 高 | RefreshDto |
| 登出 | POST | /api/auth/logout | 匿名 (Results.Ok) | 中 | LogoutDto |
| 会话检查 | GET | /api/auth/session | 匿名 (Results.Ok) | 高 | 无 |
| 获取当前用户 | GET | /api/auth/me | 匿名 (Results.Ok) | 高 | 无 |
| 修改密码 | POST | /api/auth/change-password | 匿名 (Results.Ok) | 高 | ChangePasswordDto |

---

### 4. CustomerEndpoints.cs (6 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 列出客户 | GET | /api/customers | Results.Json (IQuery) | 中 | 无 |
| 获取客户 | GET | /api/customers/{id} | Results.Json (IQuery) | 中 | 无 |
| 创建客户 | POST | /api/customers | 匿名 (Results.Json) | 高 | CreateCustomerDto |
| 更新客户 | PUT | /api/customers/{id} | 匿名 (Results.Json) | 高 | UpdateCustomerDto |
| 获取访问列表 | GET | /api/customers/{id}/access | 匿名 (Results.Json) | 高 | 无 |
| 更新访问 | POST | /api/customers/{id}/access | 匿名 (Results.Ok) | 中 | AccessUpsertDto |

---

### 5. DynamicEntityEndpoints.cs (7 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 查询实体 | POST | /api/dynamic-entities/{type}/query | 匿名 (Results.Ok) | 高 | QueryRequest |
| 按 ID 获取 | GET | /api/dynamic-entities/{type}/{id} | 动态 (Results.Ok) | 中 | 无 |
| 原始查询 | POST | /api/dynamic-entities/raw/{table}/query | 匿名 (Results.Ok) | 高 | QueryRequest |
| 创建 | POST | /api/dynamic-entities/{type} | 动态 (Results.Created) | 中 | Dictionary<string,object> |
| 更新 | PUT | /api/dynamic-entities/{type}/{id} | 动态 (Results.Ok) | 中 | Dictionary<string,object> |
| 删除 | DELETE | /api/dynamic-entities/{type}/{id} | NoContent | 低 | 无 |
| 计数 | POST | /api/dynamic-entities/{type}/count | 匿名 (Results.Ok) | 高 | CountRequest |

---

### 6. EntityAggregateEndpoints.cs (6 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 获取聚合 | GET | /api/entity-aggregates/{id} | 匿名 (Results.Ok) | 高 | 无 |
| 保存聚合 | POST | /api/entity-aggregates | 匿名 (Results.Ok) | 高 | SaveEntityDefinitionAggregateRequest |
| 验证聚合 | POST | /api/entity-aggregates/validate | 匿名 (Results.Ok) | 高 | SaveEntityDefinitionAggregateRequest |
| 删除子实体 | DELETE | /api/entity-aggregates/sub-entities/{id} | NoContent | 低 | 无 |
| 预览元数据 | GET | /api/entity-aggregates/{id}/metadata-preview | Results.Content (JSON) | 中 | 无 |
| 预览代码 | GET | /api/entity-aggregates/{id}/code-preview | 匿名 (Results.Ok) | 高 | 无 |

---

### 7. EntityDefinitionEndpoints.cs (25 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 获取可用 | GET | /api/entities | 匿名 (Results.Json) | 高 | 无 |
| 获取所有 | GET | /api/entities/all | 匿名 (Results.Json) | 高 | 无 |
| 验证路由 | GET | /api/entities/{route}/validate | 匿名 (Results.Json) | 高 | 无 |
| 列出定义 | GET | /api/entity-definitions | 匿名 (Results.Json) | 高 | 无 |
| 获取定义 | GET | /api/entity-definitions/{id} | 匿名 (Results.Json) | 高 | 无 |
| 按类型获取 | GET | /api/entity-definitions/by-type/{type} | EntityDefinition (Results.Json) | 中 | 无 |
| 检查引用 | GET | /api/entity-definitions/{id}/referenced | 匿名 (Results.Json) | 高 | 无 |
| 创建 | POST | /api/entity-definitions | EntityDefinition (Results.Created) | 中 | CreateEntityDefinitionDto |
| 更新 | PUT | /api/entity-definitions/{id} | EntityDefinition (Results.Ok) | 中 | UpdateEntityDefinitionDto |
| 删除 | DELETE | /api/entity-definitions/{id} | NoContent | 低 | 无 |
| 发布 | POST | /api/entity-definitions/{id}/publish | 匿名 (Results.Ok) | 高 | 无 |
| 发布变更 | POST | /api/entity-definitions/{id}/publish-changes | 匿名 (Results.Ok) | 高 | 无 |
| 预览 DDL | GET | /api/entity-definitions/{id}/preview-ddl | 匿名 (Results.Ok) | 高 | 无 |
| 获取 DDL 历史 | GET | /api/entity-definitions/{id}/ddl-history | 匿名 (Results.Ok) | 高 | 无 |
| 生成代码 | GET | /api/entity-definitions/{id}/generate-code | 匿名 (Results.Ok) | 高 | 无 |
| 编译 | POST | /api/entity-definitions/{id}/compile | 匿名 (Results.Ok) | 高 | 无 |
| 批量编译 | POST | /api/entity-definitions/compile-batch | 匿名 (Results.Ok) | 高 | CompileBatchDto |
| 验证代码 | GET | /api/entity-definitions/{id}/validate-code | 匿名 (Results.Ok) | 高 | 无 |
| 获取已加载 | GET | /api/entity-definitions/loaded-entities | 匿名 (Results.Ok) | 高 | 无 |
| 获取类型信息 | GET | /api/entity-definitions/type-info/{type} | 匿名 (Results.Ok) | 高 | 无 |
| 卸载 | DELETE | /api/entity-definitions/loaded-entities/{type} | 匿名 (Results.Ok) | 高 | 无 |
| 重新编译 | POST | /api/entity-definitions/{id}/recompile | 匿名 (Results.Ok) | 高 | 无 |

---

### 8. FieldActionEndpoints.cs (3 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 下载 RDP | POST | /api/actions/rdp/download | File (application/x-rdp) | 低 | RdpDownloadRequest |
| 验证文件 | POST | /api/actions/file/validate | 匿名 (Results.Ok) | 高 | FileValidationRequest |
| 生成 Mailto | POST | /api/actions/mailto/generate | 匿名 (Results.Ok) | 高 | MailtoRequest |

---

### 9. FileEndpoints.cs (3 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 上传 | POST | /api/files/upload | 匿名 (Results.Ok) | 高 | Form file |
| 下载 | GET | /api/files/{*key} | Stream | 低 | 无 |
| 删除 | DELETE | /api/files/{*key} | NoContent | 低 | 无 |

---

### 10. I18nEndpoints.cs (4 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 获取版本 | GET | /api/i18n/version | 匿名 (Results.Json) | 高 | 无 |
| 获取资源 | GET | /api/i18n/resources | List<LocalizationResource> | 中 | 无 |
| 获取字典 | GET | /api/i18n/{lang} | Dictionary<string,string> | 中 | 无 |
| 获取语言 | GET | /api/i18n/languages | 匿名 (Results.Json) | 高 | 无 |

---

### 11. LayoutEndpoints.cs (14 个端点) [6 个已废弃]

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 获取字段定义 | GET | /api/fields | Results.Json (IQuery) | 中 | 无 |
| 获取字段标签 | GET | /api/fields/tags | 匿名 (Results.Json) | 高 | 无 |
| 获取布局 [已废弃] | GET | /api/layout/{id} | Results.Json (IQuery) | 中 | 无 |
| 保存布局 [已废弃] | POST | /api/layout/{id} | 匿名 (Results.Ok) | 中 | JsonElement |
| 删除布局 [已废弃] | DELETE | /api/layout/{id} | 匿名 (Results.Ok) | 中 | 无 |
| 获取布局 | GET | /api/layout | Results.Json (IQuery) | 中 | 无 |
| 获取布局别名 | GET | /api/layout/customer | Results.Json (IQuery) | 中 | 无 |
| 保存布局 | POST | /api/layout | 匿名 (Results.Ok) | 中 | JsonElement |
| 保存布局别名 | POST | /api/layout/customer | 匿名 (Results.Ok) | 中 | JsonElement |
| 删除布局 | DELETE | /api/layout | 匿名 (Results.Ok) | 中 | 无 |
| 删除布局别名 | DELETE | /api/layout/customer | 匿名 (Results.Ok) | 中 | 无 |
| 按实体获取 | GET | /api/layout/entity/{type} | Results.Json (IQuery) | 中 | 无 |
| 按实体保存 | POST | /api/layout/entity/{type} | 匿名 (Results.Ok) | 中 | JsonElement |
| 按实体删除 | DELETE | /api/layout/entity/{type} | 匿名 (Results.Ok) | 中 | 无 |
| 生成 | POST | /api/layout/{id}/generate | 匿名 (Results.Json) | 高 | GenerateLayoutRequest |

---

### 12. OrganizationEndpoints.cs (4 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 获取树 | GET | /api/organizations/tree | 匿名 (Results.Ok) | 中 | 无 |
| 创建 | POST | /api/organizations | 匿名 (Results.Ok) | 中 | CreateOrganizationRequest |
| 更新 | PUT | /api/organizations/{id} | 匿名 (Results.Ok) | 中 | UpdateOrganizationRequest |
| 删除 | DELETE | /api/organizations/{id} | Results.Ok | 中 | 无 |

---

### 13. SettingsEndpoints.cs (4 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 获取系统设置 | GET | /api/settings/system | SystemSettingsDto (Results.Ok) | 低 | 无 |
| 更新系统设置 | PUT | /api/settings/system | SystemSettingsDto (Results.Ok) | 低 | UpdateSystemSettingsRequest |
| 获取用户设置 | GET | /api/settings/user | UserSettingsSnapshot (Results.Ok) | 低 | 无 |
| 更新用户设置 | PUT | /api/settings/user | UserSettingsSnapshot (Results.Ok) | 低 | UpdateUserSettingsRequest |

---

### 14. SetupEndpoints.cs (2 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 获取管理员信息 | GET | /api/setup/admin | 匿名 (Results.Ok) | 高 | 无 |
| 设置管理员 | POST | /api/setup/admin | 匿名 (Results.Ok) | 中 | AdminSetupDto |

---

### 15. TemplateEndpoints.cs (9 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 列出模板 | GET | /api/templates | 匿名 (Results.Json) | 高 | 无 |
| 获取模板 | GET | /api/templates/{id} | FormTemplate (Results.Json) | 中 | 无 |
| 创建 | POST | /api/templates | FormTemplate (Results.Created) | 中 | CreateTemplateRequest |
| 更新 | PUT | /api/templates/{id} | FormTemplate (Results.Ok) | 中 | UpdateTemplateRequest |
| 删除 | DELETE | /api/templates/{id} | 匿名 (Results.Ok) | 中 | 无 |
| 获取有效 | GET | /api/templates/effective/{type} | FormTemplate (Results.Json) | 中 | 无 |
| 获取绑定 | GET | /api/templates/bindings/{type} | TemplateBinding (ToDto) | 中 | 无 |
| 更新绑定 | PUT | /api/templates/bindings | TemplateBinding (ToDto) | 中 | UpsertTemplateBindingRequest |
| 构建运行时 | POST | /api/templates/runtime/{type} | 匿名 (Results.Ok) | 高 | TemplateRuntimeRequest |

---

### 16. UserEndpoints.cs (5 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 列出用户 | GET | /api/users | Mapped (Results.Ok) | 中 | 无 |
| 获取用户 | GET | /api/users/{id} | UserDetailDto (Results.Ok) | 低 | 无 |
| 创建 | POST | /api/users | UserDetailDto (Results.Ok) | 低 | CreateUserRequest |
| 更新 | PUT | /api/users/{id} | UserDetailDto (Results.Ok) | 低 | UpdateUserRequest |
| 更新角色 | PUT | /api/users/{id}/roles | 匿名 (Results.Ok) | 高 | UpdateUserRolesRequest |

---

### 17. EntityAdvancedFeaturesController.cs (6 个端点)

| 端点 | 方法 | 路由 | 返回类型 | 风险等级 | 请求 DTO |
|----------|--------|-------|-------------|-----------|-------------|
| 获取子项 | GET | /api/entity-advanced/{id}/children | 匿名 (Ok()) | 高 | 无 |
| 配置主从 | POST | /api/entity-advanced/{id}/configure-master-detail | 匿名 (Ok()) | 高 | MasterDetailConfigRequest |
| 生成 AggVO | POST | /api/entity-advanced/{id}/generate-aggvo | 匿名 (Ok()) | 高 | 无 |
| 评估迁移 | POST | /api/entity-advanced/{id}/evaluate-migration | 匿名 (Ok()) | 高 | List<FieldMetadata> |
| 获取主候选 | GET | /api/entity-advanced/master-candidates | 匿名 (Ok()) | 高 | 无 |
| 获取从候选 | GET | /api/entity-advanced/detail-candidates | 匿名 (Ok()) | 高 | 无 |

---

## 统计摘要

| 指标 | 计数 | 百分比 |
|--------|-------|-----------|
| **端点总数** | 90 | 100% |
| **高风险 (匿名对象)** | 45 | 50% |
| **中风险 (动态/混合)** | 30 | 33% |
| **低风险 (类型化 DTO)** | 15 | 17% |
| **有请求 DTO** | 55 | 61% |
| **无请求 DTO** | 35 | 39% |
| **已废弃端点** | 6 | 7% |
| **NoContent 响应** | 8 | 9% |

---

## 按文件风险分布

### 严重优先级
- **AuthEndpoints.cs**: 75% 高风险 (6/8)
- **EntityDefinitionEndpoints.cs**: 90% 高风险 (18/20)
- **AccessEndpoints.cs**: 67% 高风险 (8/12)
- **EntityAggregateEndpoints.cs**: 83% 高风险 (5/6)
- **EntityAdvancedFeaturesController.cs**: 100% 高风险 (6/6)

### 高优先级
- **CustomerEndpoints.cs**: 67% 高风险 (4/6)
- **DynamicEntityEndpoints.cs**: 57% 高风险 (4/7)
- **FieldActionEndpoints.cs**: 67% 高风险 (2/3)

### 中优先级
- **TemplateEndpoints.cs**: 33% 高风险 (3/9)
- **LayoutEndpoints.cs**: 21% 高风险 (3/14)
- **AdminEndpoints.cs**: 60% 高风险 (3/5)

### 低优先级
- **UserEndpoints.cs**: 20% 高风险 (1/5)
- **SettingsEndpoints.cs**: 0% 高风险 (0/4)
