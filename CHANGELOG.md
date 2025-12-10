# Changelog

本文档记录 BobCRM 项目的所有重要变更。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

---

## [未发布] - 进行中

### Fixed
- **模板运行链路修复** (PLAN-06):
  - 统一模板 JSON 格式：所有环节使用数组式 Widget JSON（含 children / layout），`WidgetJsonConverter` 支持 `WidgetRegistry` 和递归反序列化。
  - 运行时解析升级：`ContainerWidget` 及其子类（Panel, Section, Card 等）实现递归 `RenderRuntime`，`PageLoader` 适配新 JSON 格式并支持嵌套布局。
  - 设计器能力增强：`PropertyEditor` 新增 `DataSetPicker` 和 `FieldPicker`，支持数据绑定配置。
  - 运行态数据收集：`EditValueManager` 支持递归收集嵌套控件的字段值，修复保存时数据丢失问题。
  - 视觉兜底：PageLoader/ListTemplateHost 使用 design token 外观，统一空态/错误态与重试按钮，模板加载失败使用 I18n 文案。
- **列表页体验**：
  - 行内操作按钮图标化（查看/编辑/删除），使用标准 `IconActionButton` 组件，横向排列且悬停显示多语提示，避免拉高行高。
  - 客户列表移除非编辑态的保存类按钮，仅保留导出与新建入口。
  - DataGrid 本地化列标题与动作，多语兜底；分页兜底为 PageSize=20，始终显示分页栏。
  - E2E 截图更新（login/list/detail/edit），见 `artifacts/step1-login.png` ~ `step4-edit-save.png`。
- **Template Regeneration**: Fixed "Regenerate Defaults" button unresponsiveness and timestamp behavior
  - Added missing `<AntContainer />` component to `MainLayout.razor` to enable Ant Design modals
  - Fixed i18n key from non-existent `BTN_CONFIRM` to `BTN_OK` in confirmation modal
  - **Refactored force update logic**: Added `force` parameter to `IDefaultTemplateGenerator.EnsureTemplatesAsync()`
    - System startup: Templates only update if content actually changed (timestamps preserved)
    - Manual regeneration: Passing `force: true` ensures timestamps update even if content unchanged
    - `AdminEndpoints` now explicitly passes `force: true` for user-triggered regeneration
  - Resolved issue where template timestamps incorrectly matched service restart time
  - Files modified:
    - `src/BobCrm.Application/Templates/IDefaultTemplateGenerator.cs`
    - `src/BobCrm.Api/Services/DefaultTemplateGenerator.cs`
    - `src/BobCrm.Api/Services/DefaultTemplateService.cs`
    - `src/BobCrm.Api/Endpoints/AdminEndpoints.cs`
    - `src/BobCrm.App/Components/Layout/MainLayout.razor`
    - `src/BobCrm.App/Components/Pages/Templates.razor`
    - `tests/BobCrm.Api.Tests/EntityPublishingAndDDLTests.cs`
- **模板重置链路**：硬重置/全量重置现在覆盖单复数路由匹配（如 user/users），删除全量模板与绑定并重建系统双模板，返回/日志包含当前模板明细（Id/用途/来源/更新时间），便于核对是否生效；前端硬重置改用顶部消息提示结果。

### Added
- 公共端点 `/api/entities/{entityType}/definition`：返回实体字段与接口投影，支持大小写、`entity_` 前缀、单复数候选；供 Form Designer/实体树加载系统实体。
- 文档：新增《GUIDE-07 模板默认与设计器更新说明》（模板生成、设计器、多语展示的更新要点与验证清单）。
- Form Designer：DataGrid 设计态预览组件，展示列/搜索/刷新/分页占位。

### Changed
- 默认模板生成：模板名改用 i18n Key `TEMPLATE_NAME_{USAGE}_{ENTITY}`；List 模板 DataGrid 默认带列、搜索、刷新、分页；Edit 模板新增 Back/Cancel 按钮；生成前强制调用 `GetInitialDefinition` 补全元数据。
- 前端设计器：初始化即加载实体/字段（带路由候选与重试），空的 List 模板自动补 DataGrid 并提示；头部新增 Cancel/Return；实体结构加载失败给出友好提示。
- 实体结构树：改用 Ant Design Tree，支持图标/标签保留与实体引用字段懒加载；模板列表与 DataGrid 运行态：模板名/实体标签按 i18n Key 或模式翻译；无列配置时使用默认/占位列，避免空表渲染。
- 设计器 Tooltip：统一使用语义文本色，避免浅底场景下字段标签（如 *Required）出现白底白字。
- Form Designer：组件面板改为手风琴模式，默认只展开“实体结构”，切换分组时自动收起其他分组，减少滚动。
- 模板列表页：默认按实体分组，管理员可在每个实体分组处一键再生成默认模板（前端确认，后端管理端点支持单实体/全量重建并更新绑定）。
- Playwright 模板链路脚本：支持 BASE_URL/HEADLESS/SLOWMO/SCREENSHOT_DIR 配置，错误截图输出到 artifacts，便于 STD-06 复现。

---

## [0.8.0] - 2025-11-21

### Added
#### 字段级权限系统（Field-Level Security）
- **后端实现** (`src/BobCrm.Api/`):
  - 新增 `FieldPermission` 实体模型（`Base/Models/FieldPermission.cs`）：
    - 字段：`RoleId`, `EntityType`, `FieldName`, `CanRead`, `CanWrite`, `Remarks`
    - 实现 `IAuditableEntity` 接口，支持审计字段
    - 复合唯一索引：`(RoleId, EntityType, FieldName)`
  - 新增 `IFieldPermissionService` 接口和实现（`Services/FieldPermissionService.cs`）：
    - 12个核心方法：权限查询、批量更新、删除、用户权限检查
    - 多角色权限聚合策略：Union（最宽松权限优先）
    - **多层缓存优化**（CRITICAL性能修复）：
      - 用户角色缓存（5分钟 TTL）
      - 字段权限缓存（5分钟 TTL）
      - 性能提升：100字段 = 200查询 → 2查询
    - 自动缓存失效：权限修改时清除相关缓存
  - 新增 11个 REST API 端点（`Endpoints/FieldPermissionEndpoints.cs`）：
    - `GET /api/field-permissions/role/{roleId}` - 获取角色所有权限
    - `GET /api/field-permissions/role/{roleId}/entity/{entityType}` - 获取角色对特定实体的权限
    - `POST /api/field-permissions/role/{roleId}/entity/{entityType}/bulk` - 批量更新权限
    - `GET /api/field-permissions/user/entity/{entityType}/readable-fields` - 获取用户可读字段
    - `GET /api/field-permissions/user/entity/{entityType}/writable-fields` - 获取用户可写字段
    - 其他查询、单项更新、删除端点
  - 新增运行时字段过滤服务（`Services/FieldFilterService.cs`）：
    - JSON 字段过滤：自动过滤 API 响应中用户无权访问的字段
    - 写入验证：验证用户是否有权修改提交的字段
    - 递归过滤：支持嵌套对象和数组
  - 新增便捷扩展方法（`Utils/FieldFilterExtensions.cs`）：
    - `FilteredOkAsync()` - 返回过滤后的成功响应
    - `ValidateWritePermissionsAsync()` - 验证写入权限
    - `OrElseAsync()` - 链式 API 支持
  - 数据库迁移（`Migrations/20251121000000_AddFieldPermissions.cs`）：
    - 创建 `FieldPermissions` 表
    - 外键关联到 `RoleProfiles` 表（CASCADE删除）
    - 默认值：`CanRead=true`, `CanWrite=false`
  - EF Core 配置（`Infrastructure/Ef/Configurations/FieldPermissionConfiguration.cs`）：
    - 复合唯一索引配置
    - 字段长度限制：`EntityType`, `FieldName` (128字符)
    - 审计字段配置

- **前端实现** (`src/BobCrm.App/`):
  - 新增 `RoleFieldPermissions` 组件（`Components/Shared/RoleFieldPermissions.razor`，~300行）：
    - 折叠面板（Collapse）展示实体分组
    - 表格（Table）显示字段权限
    - 复选框切换 `CanRead` / `CanWrite` 权限
    - 备注（Remarks）输入框
    - 批量保存功能
    - 统计信息显示：总字段数、可读/可写字段数
    - 空状态提示：无实体或无字段时的友好提示
  - 集成到 `RolePermissionTree` 组件：
    - 使用 Ant Design `Tabs` 组件实现标签页界面
    - Tab 1：功能权限（原有的树形权限选择器）
    - Tab 2：字段权限（新增的字段权限管理器）
    - 统一的权限管理入口
  - 新增 16个多语言资源键（`src/BobCrm.Api/Resources/i18n-resources.json`）：
    - `LBL_FIELD_PERMISSIONS`: "字段权限" / "フィールド権限" / "Field Permissions"
    - `LBL_FUNCTION_PERMISSIONS`: "功能权限" / "機能権限" / "Function Permissions"
    - `LBL_CAN_READ`: "可读" / "読取可" / "Can Read"
    - `LBL_CAN_WRITE`: "可写" / "書込可" / "Can Write"
    - `LBL_REMARKS`: "备注" / "備考" / "Remarks"
    - `LBL_TOTAL_FIELDS`, `LBL_READABLE_FIELDS`, `LBL_WRITABLE_FIELDS` 等统计标签
    - `MSG_NO_ENTITIES`, `MSG_NO_FIELDS` 等提示消息
    - `MSG_PERMISSIONS_SAVED` 等操作反馈消息

- **文档**：
  - 新增字段级权限使用示例（`docs/examples/EX-04-字段级权限使用示例.md`，450+行）：
    - 5个使用场景示例
    - API 配置指南
    - 最佳实践和安全考虑
    - 故障排查指南

### Changed
#### 架构重构
- **模板服务提取** (`src/BobCrm.Api/`):
  - 新增 `ITemplateService` 接口（`Abstractions/ITemplateService.cs`）：
    - 定义 8个核心方法：模板查询、创建、更新、删除、复制、应用、获取有效模板
  - 新增 `TemplateService` 实现（`Services/TemplateService.cs`，~400行）：
    - 从 `TemplateEndpoints.cs` 提取所有业务逻辑
    - 增强错误处理：异常驱动（`KeyNotFoundException`, `InvalidOperationException`）
    - 改进日志记录：Information 和 Warning 级别
    - 支持"系统默认"和"用户默认"模板管理
  - 重构 `TemplateEndpoints` (`Endpoints/TemplateEndpoints.cs`):
    - 代码减少 41%（873行 → 515行）
    - 端点层仅负责 HTTP 请求/响应
    - 所有业务逻辑委托给 `TemplateService`
    - CRUD 端点代码减少 76%
  - 服务注册（`Program.cs`）：
    - 注册 `ITemplateService` 为 Scoped 生命周期
    - 注册 `IFieldPermissionService` 为 Scoped 生命周期
    - 注册 `FieldFilterService` 为 Scoped 生命周期

#### 性能优化
- **FieldPermissionService 缓存实现**（CRITICAL修复）:
  - 问题：N+1查询问题导致严重性能下降
    - 修复前：每个字段检查 = 2次数据库查询
    - 示例：100个字段的列表 = 200次数据库查询
  - 解决方案：多层缓存策略
    - `GetUserRoleIdsAsync()`：缓存用户角色ID（5分钟）
    - `GetUserEntityPermissionsAsync()`：缓存用户对实体的所有字段权限（5分钟）
    - 权限聚合在缓存层完成（Union策略）
  - 性能提升：
    - 100个字段的列表：200查询 → 2查询（99%减少）
    - 首次加载：1次角色查询 + 1次权限查询
    - 后续5分钟内：完全命中缓存，0数据库查询
  - 缓存失效策略：
    - 权限修改时自动清除相关角色缓存
    - 使用 `IMemoryCache.Remove()` 精确失效

### Fixed
- **字段权限 UI 缺失**：
  - 问题：v0.8.0 初版仅实现后端，无 UI 配置入口
  - 修复：集成 `RoleFieldPermissions` 到 `RolePermissionTree`，提供标签页界面
- **权限查询性能问题**：
  - 问题：`GetUserFieldPermissionAsync` 无缓存，导致 N+1 查询
  - 修复：实现多层缓存，性能提升 99%
- **复合索引验证**：
  - 确认 `FieldPermissions` 表存在复合唯一索引 `IX_FieldPermissions_Role_Entity_Field`
  - 优化查询性能和数据完整性

### Technical Details
#### 提交记录
- `bdd058e`: feat: extract TemplateService from TemplateEndpoints (TASK-01)
- `71d582a`: feat: implement Field-Level Security backend (TASK-03)
- `8d7d4ec`: feat: implement runtime field filtering for Field-Level Security (TASK-03)
- `5443213`: feat: add Field Permissions UI component (TASK-03)
- `729812e`: feat: add database migration for Field Permissions (TASK-03)
- `9d41524`: perf: implement multi-level caching for FieldPermissionService (FIX-01)
- `422238d`: feat: integrate Field Permissions into RolePermissionTree (FIX-03)

#### 代码审查响应
- **REVIEW-06**: 初始代码审查，识别缺失的缓存、UI集成
- **REVIEW-07**: 验证所有修复通过，v0.8.0 发布候选版本稳定

---

## [0.7.0] - 2025-11-20

### Added
#### 图标选择器组件
- **新增 IconSelector 组件** (`src/BobCrm.App/Components/Shared/IconSelector.razor`)：
  - 可视化图标选择器，替代文本输入方式
  - 内置 120+ 个 Ant Design Blazor 常用图标（Outline 系列）
  - 实现图标搜索功能：支持按图标名称和标签搜索过滤
  - 网格布局显示：每行自适应显示，最小宽度 80px
  - 图标预览：选中状态高亮显示，鼠标悬停预览效果
  - 向后兼容：支持手动输入图标名称，输入框带图标预览
  - 下拉面板：点击"选择"按钮展开/收起图标选择面板
  - 响应式设计：支持浅色/深色主题自动切换
  - 多语言支持：界面文本全部使用 i18n 资源键
  - 集成到 `MenuManagement.razor`：菜单图标字段使用新选择器
- **新增多语言资源键**：
  - `BTN_SELECT`: "选择" / "選択" / "Select"
  - `LBL_ICON_NAME`: "图标名称" / "アイコン名" / "Icon Name"
  - `LBL_SEARCH_ICON`: "搜索图标" / "アイコンを検索" / "Search icon"
  - `TXT_NO_ICONS_FOUND`: "未找到匹配的图标" / "一致するアイコンが見つかりません" / "No icons found"

#### 菜单导入/导出功能
- **后端 API** (`src/BobCrm.Api/Endpoints/AccessEndpoints.cs`):
  - 新增 `GET /api/access/functions/export`：导出完整菜单树为 JSON 格式
    - 递归构建菜单树结构（包含多语言 DisplayName、图标、路由、模板等）
    - 导出格式包含版本号和导出时间戳
  - 新增 `POST /api/access/functions/import`：导入菜单树到数据库
    - 支持冲突检测：检查功能码是否已存在
    - 支持两种合并策略：`skip`（跳过冲突项）和 `replace`（覆盖冲突项）
    - 递归导入菜单树，自动处理父子关系和排序
- **前端 UI** (`src/BobCrm.App/Components/Pages/MenuManagement.razor`):
  - 添加"导出"按钮：点击后生成 JSON 文件并自动下载（文件名格式：`menus-export-yyyyMMdd-HHmmss.json`）
  - 添加"导入"按钮：打开导入对话框，支持拖拽或点击上传 JSON 文件
  - 导入对话框两步流程：
    - 步骤1：上传文件（最大 10MB，仅支持 .json 格式）
    - 步骤2：预览导入内容，显示冲突提示和合并策略选择
  - 冲突检测：显示已存在的功能码列表，支持选择"跳过冲突项"或"覆盖冲突项"
  - 导入结果反馈：显示已导入和已跳过的菜单数量
- **服务层** (`src/BobCrm.App/Services/MenuService.cs`):
  - 新增 `ExportMenusAsync()`：调用导出 API，返回 JSON 数据
  - 新增 `CheckImportConflictsAsync()`：检查导入冲突，返回冲突功能码列表
  - 新增 `ImportMenusAsync()`：执行导入操作，支持合并策略参数
  - 新增数据模型：`MenuImportData`, `MenuImportNode`, `ImportResult`, `ImportErrorResponse`
- **新增多语言资源键**（共 17 个）：
  - 按钮：`BTN_IMPORT`, `BTN_CONFIRM_IMPORT`
  - 标签：`LBL_IMPORT_MENU`, `LBL_IMPORT_TOTAL`, `LBL_IMPORTED`, `LBL_MERGE_STRATEGY`, `LBL_REPLACE_CONFLICTS`, `LBL_SKIP_CONFLICTS`, `LBL_SKIPPED`, `LBL_STEP1_UPLOAD`, `LBL_STEP2_PREVIEW`
  - 消息：`MSG_EXPORT_SUCCESS`, `MSG_EXPORT_FAILED`, `MSG_FILE_TOO_LARGE`, `MSG_FILE_PARSE_ERROR`, `MSG_IMPORT_CONFLICTS`, `MSG_IMPORT_SUCCESS`, `MSG_IMPORT_FAILED`, `MSG_INVALID_IMPORT_FILE`
  - 文本：`TXT_MORE`

### Changed
#### 文档更新
- **菜单编辑器使用指南完善**：
  - 更新 `docs/guides/GUIDE-06-菜单编辑器使用指南.md`，从"计划中"状态改为"已实现"
  - 编写完整的使用步骤：访问方式、新增/编辑/删除节点、拖拽排序（before/after/into三种位置）
  - 添加菜单配置说明：基本配置、导航类型配置（路由/模板）、图标配置
  - 添加10个常见问题（FAQ）：菜单刷新、图标选择、拖拽限制、权限集成、功能码规范等
  - 添加3个使用流程示例：创建完整的客户管理模块菜单、调整菜单顺序、提升子节点为根节点
  - 添加技术细节说明：实现文件、关键特性（循环依赖检测、批量排序更新、多语言支持、模板绑定）
  - 文档反映 `MenuManagement.razor` (782行) 的实际实现

---

## [0.6.0] - 2025-11-20

### Added
#### 客户列表模板化迁移
- **后端增强**：
  - `EntityDefinitionSynchronizer` 添加模板和绑定自动生成功能
  - 在同步实体定义后自动调用 `EnsureTemplatesAndBindingsAsync`，确保系统实体（如Customer）存在模板（列表、详情、编辑）和绑定
  - `DefaultTemplateGenerator` 在生成List模板时自动添加 `RowActionsJson`，包含Edit和Delete操作
- **前端增强**：
  - `DataGridRuntime.razor` 实现完整的行操作功能
    - 解析并渲染 `RowActionsJson` 中定义的操作按钮（Edit、Delete）
    - 实现行点击导航到详情页（`/customer/{id}`）
    - 实现Edit按钮导航和Delete按钮删除功能
    - 确保 `ApiEndpoint` 正确映射到实体API（`/api/customers`）
  - `Customers.razor` 已迁移到使用 `ListTemplateHost` 和 `DataGridWidget` 的模板驱动方法
- **自动化测试**：
  - 验证 `TemplateRuntimeEndpoint` 返回 "customer" 的种子模板
  - 验证 `DataGridWidget` 的序列化/反序列化
  - 所有单元测试通过（379个通过，13个跳过）

#### 动态枚举系统完善
- **DTO扩展**：
  - `CreateEnumDefinitionRequest`添加`IsEnabled`属性支持启用状态控制
  - `UpdateEnumDefinitionRequest`添加`Options`属性支持直接更新枚举选项
- **前端集成**：
  - `EntityDefinitionDto.cs`添加`FieldDataType.Enum`常量
  - `EntityDefinitionEdit.razor`完整集成枚举字段类型选择
  - 新增"枚举定义"列显示关联的枚举
  - 实现枚举选择器UI，支持从已定义枚举列表中选择
  - 添加`GetEnumDisplayName`辅助方法支持多语言显示
- **多语言资源**：添加18个枚举相关i18n资源键
  - 标签：`LBL_ENUM_DEFINITION`, `LBL_ENUM_CODE`, `LBL_ENUM_OPTIONS`, `LBL_OPTION_VALUE`, `LBL_COLOR_TAG`, `LBL_CREATE_ENUM`, `LBL_EDIT_ENUM`
  - 按钮：`BTN_NEW_ENUM`, `BTN_ADD_OPTION`
  - 消息：`MSG_ENUM_CREATE_SUCCESS`, `MSG_ENUM_UPDATE_SUCCESS`, `MSG_ENUM_DELETE_SUCCESS`, `MSG_ENUM_LOAD_FAILED`, `MSG_ENUM_SAVE_FAILED`, `MSG_ENUM_CODE_REQUIRED`, `MSG_OPTION_VALUE_REQUIRED`, `MSG_CONFIRM_DELETE_ENUM`, `MSG_ENUM_IN_USE`  
  - 菜单：`MENU_SYS_ENTITY_ENUM`
  
#### 用户与角色模板化集成
- **组件集成**：
  - `UserRoleAssignmentWidget`：集成到 User 实体详情模板，提供可视化的角色分配界面
  - `RolePermissionTreeWidget`：集成到 Role 实体详情模板，提供树形权限配置界面
- **模板生成**：
  - `DefaultTemplateGenerator` 自动识别 User/Role 实体并注入专用 Widget
  - `WidgetRegistry` 注册 `userrole` and `permtree` 控件类型

### Changed
- **API路径统一**：所有枚举API端点统一使用`/api/enums`前缀
- **路由规范**：枚举管理页面路由统一为`/system/enums`
- **EnumDefinitionService**：更新所有API调用路径适配新的端点结构

### Fixed
- **类型安全**：修正Dictionary和MultilingualTextDto之间的类型转换
- **表单绑定**：优化MultilingualInput组件与后端DTO的数据绑定
- **编译修复**：解决 `EnumDefinitionEdit.razor` 和 `EnumDefinitions.razor` 中的构建错误，恢复完整功能

---

#### 2025-11-19
- **枚举系统前后端UI集成完成**：为动态枚举系统添加完整的前端管理界面
  - **前端常量与i18n资源**：
    - 添加 `FieldDataType.Enum` 常量到前端模型
    - 新增 18 个枚举相关的多语言资源键（LBL_ENUM_DEFINITION、BTN_NEW_ENUM、MSG_ENUM_CREATE_SUCCESS 等）
  - **枚举管理页面**：
    - `EnumDefinitions.razor`：枚举列表管理页面，支持搜索、状态筛选、查看、编辑、删除功能
    - `EnumDefinitionEdit.razor`：枚举创建/编辑页面，支持多语言显示名、枚举选项管理、颜色标签配置
    - 实现行内编辑枚举选项，支持添加、编辑、删除操作
    - 查看详情 Modal：只读模式展示枚举完整信息（基本信息、所有语言的显示名、选项列表）
  - **菜单系统集成**：
    - 添加 `MENU_SYS_ENTITY_ENUM` 菜单资源键（中日英三语）
    - 提供 SQL 初始化脚本 `add-enum-menu-node.sql`，在"系统设置 > 实体管理"下注册枚举菜单
    - 菜单代码 `SYS.ENTITY.ENUM`，路由 `/enums`，图标 `ordered-list`
  - **数据展示优化**：
    - `DataGridRuntime.razor` 改进枚举值显示，使用当前语言而非硬编码英文
    - 支持单选和多选枚举值的正确解析与展示
  - **删除保护**：枚举删除时检查引用完整性，防止删除正在被字段使用的枚举

### Changed
- **DataGridRuntime.razor**：注入 `I18nService`，枚举显示名从硬编码 "en" 改为使用 `I18n.CurrentLang`

- **Default template automation**：实体发布后自动生成 Detail/Edit/List 模板并注册菜单挂点，形成"发布即上线"的闭环。
- **Template & menu management UI**：模板中心支持绑定切换；菜单管理支持多语标题、模板/功能双模式指派及拖拽排序。
- **Role-level template authorization**：`RoleFunctionPermission` 现可记录 `TemplateBindingId`，角色可在同一菜单下授权不同模板。
- **Dynamic entity host enhancements**：`DynamicEntityData.razor` 引入弹窗式创建/编辑、字段级校验与批量刷新。
- **Template designer progress tracker**：新增 `docs/process/TMP-template-designer-progress.md`，追踪阶段性交付与待办。
- **ARCH-22 Phase A – Data source infrastructure**：
  - 后端：落地 `DataSet` / `QueryDefinition` / `PermissionFilter` / `DataSourceTypeEntry` 模型与 DTO，新增 `DataSetService`、`DataSetEndpoints`、`IDataSourceHandler` 策略体系以及 `AddDataSourceInfrastructure` 迁移。
  - 前端：实现 `DataGridWidget`、`OrganizationTreeWidget`、`RolePermissionTreeWidget`、`WidgetJsonConverter`、`ListTemplateHost`、`DataGridRuntime`，并扩展 `PropertyEditorType` 与 `WidgetRegistry` 的 Data 类别。
  - 运行态：所有模板/数据集 API 调用改用 `AuthService` 获取带凭据的 HttpClient，确保鉴权链路一致。

#### 2025-11-16
- **Template binding management page**：新增 `/templates/bindings` 页面，支持实体/用途筛选、系统/个人绑定切换与权限提示。
- **Menu management APIs**：暴露菜单 CRUD、排序、模板绑定端点，并在 UI 中展示模板元数据与导航模式选择。
- **Entity publish template pipeline**：`EntityPublishingService` 集成 `DefaultTemplateService` 与 `EntityMenuRegistrar`，发布时自动生成模板及菜单节点。
- **DefaultTemplateGenerator & service**：实现 `IDefaultTemplateGenerator` 与 `DefaultTemplateService`，统一生成/持久化 Detail/Edit/List 模板。
- **Function tree builder**：新增 `FunctionTreeBuilder` 服务与多语/模板字段扩展，使角色与菜单 API 可返回模板配置信息。
- **Role permission template support**：`UpdatePermissionsRequest` 与 Role API/页面现可提交模板绑定 ID。
- **Dynamic entity CRUD modal**：`DynamicEntityData.razor` 增加页面内模态框创建/编辑能力。
- **Docs & audits**：`ARCH-22`、`PROC-04` 等文档记录 11/16 模板-权限闭环审计与风险。

### Changed
- **ARCH-22 文档**：新增"实施进度"章节，并在 5.1 小节补充"设计器控件覆盖计划""数据源与条件绑定"。
- **PROC-04 差距审计**：记录 2025-11-16 模板-权限闭环阶段审计及后续风险。
- **TMP 进度表**：标记 Phase A 后端基础设施与运行态组件为完成，保留 FormDesigner 数据源 UI、PageLoader、种子数据及测试为待办。

### Fixed
#### 2025-11-18
- **测试基础设施全面增强**：统一测试数据库策略，解决僵尸进程和数据库隔离问题。
  - **TestWebAppFactory & MenuWorkflowAppFactory 对齐**：
    - MenuWorkflowAppFactory 从 InMemory 数据库切换到 Postgres，使用随机数据库名（`bobcrm_test_{guid}`）确保完全隔离。
    - 两个 Factory 现采用相同策略：CreateHost 前创建数据库→应用迁移→Dispose 时删除数据库。
    - 添加 SemaphoreSlim 锁机制，防止并发测试冲突。
  - **DatabaseInitializerTests 改进**：
    - 新增 CreateDatabaseAsync() 辅助方法，在 RecreateAsync() 前显式创建数据库。
    - **新增 RecreateAsync 测试覆盖**：添加专门测试验证数据库重建逻辑（drop + create + migrate）。
    - 测试前后自动清理，确保数据库不残留。
  - **超时与僵尸进程处理**：
    - 新增 `scripts/run-tests-with-timeout.ps1`，提供10分钟超时保护、自动清理 testhost 进程、详细日志输出。
    - 支持按目标运行（solution/api/app），并传递正确的退出码给 CI/CD。
  - **测试验证**：
    - DatabaseInitializerTests: 7/7 通过（包含新的 RecreateAsync 测试） ✓
    - AggVOSystemTests: 9/9 通过 ✓
    - SystemSettings 表正确创建，无"relation does not exist"错误 ✓

#### 2025-11-17
- **测试基础设施修复**：彻底解决测试初始化和数据库迁移问题。
  - **TestWebAppFactory.cs**：在应用启动前强制重建测试数据库（使用原始SQL终止连接、删除并重建数据库），解决 I18nEndpoints 启动时查询 SystemSettings 表失败的问题。
  - **Migration重建**：删除所有11个冲突的旧migrations（存在DisplayName列重复定义等问题），创建全新的 InitialCreate migration，提供干净的迁移基线。
  - **测试编译修复**：修复 TestNavigationManager → FakeNavigationManager（2处），TemplateBindingId → TemplateId 属性名（2处），匿名数组类型推断错误（1处），缺失 using/方法/字段（4处）。
  - **测试结果**：编译 0 errors 0 warnings，测试通过率 62.3%（218/350），migration冲突完全消除。

- **全面清除编译警告**：修复 API/App 项目所有 33 个编译警告，实现零警告零错误编译状态。
  - **CS1998 异步方法警告**（3处）：EntityDataSourceHandler.cs 移除 async，使用 Task.FromResult() 返回同步结果。
  - **CS8620 可空引用类型警告**（15处）：AppDbContext.cs jsonConverter 调用添加 `!` 操作符（13处），DynamicEntityData.razor payload 参数添加 `!`（2处）。
  - **CS8602 空引用警告**（1处）：ListTemplateHost.razor _widgets!.Count 添加 `!` 操作符。
  - **ASP0006 RenderTreeBuilder 警告**（27处）：RolePermissionTreeWidget.cs（20处）与 OrganizationTreeWidget.cs（7处）将动态 sequence 参数改为硬编码整数字面量，遵循 Blazor 性能最佳实践。

#### 2025-11-16
- **MenuManagement.razor**：恢复 `RadioGroup` 回调逻辑，移除无效的 `DragEventArgs.PreventDefault()`，统一 `Message` API 调用。
- **DynamicEntityData.razor**：为 Form 添加 `Model` 绑定并显式声明 `EventCallback` Lambda 类型，修复泛型推断问题。
- **TemplateBindings.razor**：修复 CSS `@media` 转义、补齐 using，并通过 `AuthService` 访问模板运行态 API。
- **TemplateBindingService**：统一返回 `IReadOnlyList<FormTemplate>`，消除 `??` 操作符类型不匹配。
- **运行态组件认证**：`ListTemplateHost` 与 `DataGridRuntime` 改用 `AuthService` 获取认证 HttpClient，确保模板/数据集 API 均能鉴权。

---

## [0.5.13] - 2025-11-15

### Added
- **全局Toast通知系统**：新增统一的消息通知机制，替代分散的消息提示
  - 创建 `ToastService` 全局服务，提供 Success/Error/Warning/Info 四种消息类型
  - `GlobalToast.razor` 组件，顶部居中显示，最多显示 3 条消息
  - 3 秒自动隐藏，带有滑动和淡入动画
  - 色彩编码：成功（绿色）、错误（红色）、警告（黄色）、信息（蓝色）
  - 支持图标显示，浅色/深色主题适配
  - 集成到实体定义编辑页面，提供清晰的保存成功/失败反馈

### Fixed
- **实体字段保存失败问题**：修复实体定义编辑页面新增字段后保存消失的问题
  - 完整实现 `EntityDefinitionEndpoints.cs` 中的字段更新逻辑（原为占位代码）
  - 新增字段不再使用前端生成的临时 GUID，避免 `DbUpdateConcurrencyException`
  - 实现基于 `Source` 的字段保护级别：
    * System 字段：仅允许更新显示名和排序
    * Interface 字段：允许更新显示名、排序和默认值
    * Custom 字段：可更新大部分属性
  - 实现字段软删除机制：
    * 添加 `IsDeleted`、`DeletedAt`、`DeletedBy` 字段到 `FieldMetadata`
    * 删除时标记而非物理删除，必填字段软删除后自动改为可空
    * 查询时自动过滤已删除字段
  - 修复 DDL 预览不显示新增字段的问题（字段现在正确保存到数据库）

---

## [0.5.12] - 2025-11-14

### Added
- **紧凑型顶部菜单导航系统**（第一版发布）：
  - 设计文档：创建 [ARCH-24-紧凑型顶部菜单导航设计](docs/design/ARCH-24-紧凑型顶部菜单导航设计.md)，规划将左侧固定导航改为顶部折叠菜单
  - **基础结构**：完成 `DomainSelector` 和 `MenuPanel` 组件、扩展 `LayoutState` 服务、移除左侧固定导航栏
  - **交互优化**：
    * 创建 `MenuButton` 组件，封装菜单按钮和面板，通过 JavaScript 动态计算位置实现精确对齐
    * 领域选择器移至右侧用户名旁边，改为仅图标显示，优化视觉层次
    * 菜单面板改为 4 列网格布局（最大宽度 600px），提升性能和浏览体验
    * 实现失焦自动关闭：两个菜单都添加透明覆盖层，点击外部区域自动收起
    * 移除"返回领域菜单"按钮，简化交互流程
    * 菜单面板覆盖层从 header 下方开始，不遮盖顶部导航栏
  - **特点**：内容区域占满全宽，菜单按需展开，最大化空间利用率
  - **后续计划**：将进行美化和功能增强
- **系统实体同步增强**：`EntityDefinitionSynchronizer` 现在会自动更新现有系统实体的 `Source` 字段，确保 Customer、OrganizationNode、RoleProfile 等系统实体及其字段的 Source 标记正确为 "System"
- **字段档案**：新增 `FieldDataTypes` / `FieldSources` 档案表、实体模型与初始化脚本，统一存储字段类型/来源（含多语描述）。实体编辑器、字段校验等位置均可直接读取档案数据，后续扩展无需修改枚举或代码

### Changed
- **菜单定位机制**：从全局居中的 Modal 定位改为基于按钮位置的动态定位，解决菜单与触发按钮不对齐的问题
- **领域选择器样式**：移除 transform scale 动画，改用 opacity 避免触发元素尺寸变化导致的位置跳动
- **z-index 层级**：domain-selector (1500) > menu-panel (1001) > overlays (1000/1400)，确保正确的层叠顺序
- **命名空间统一**：将所有相关命名空间、默认占位字符串与示例脚本更新为 `BobCrm.Base.*`，同步修复系统实体同步器、数据库初始化测试与文档示例，确保"Domain" 仅用于业务领域档案

### Fixed
- **实体定义列表 API 契约不匹配**：修复 `/api/entity-definitions` 端点返回的 JSON 结构与前端 DTO 不匹配的问题
  - 前端 `EntityDefinitionDto` 添加 `FieldCount` 属性
  - API 列表端点的 `Interfaces` 字段从字符串数组改为完整对象数组（包含 Id, InterfaceType, IsEnabled）
  - 确保 API 响应能被前端正确反序列化，系统实体（Customer、OrganizationNode、RoleProfile）现在可以在实体定义管理页面正确显示
- **Customer 实体字段缺少 Source 标记**：为 Customer 实体的所有字段（Id, Code, Name, Version, ExtData）添加 `Source = FieldSource.System`，与 OrganizationNode 和 RoleProfile 保持一致

---

## [0.5.11] - 2025-11-13

### Added
- **角色与权限框架**：新增 `RoleProfile`、`FunctionNode`、`RoleFunctionPermission`、`RoleDataScope`、`RoleAssignment`、`AccessService` 及 `/api/access` 端点，覆盖角色建档、功能授权、数据范围设置与用户-角色绑定。
- **系统默认种子**：启动时自动生成标准功能树（Dashboard / Customers / Entity / Settings 等）、`SYS.ADMIN` 角色以及 `admin`→`SYS.ADMIN` 绑定，保证默认账号可访问整套功能。
- **模板绑定基础设施**：FormTemplate 新增 UsageType/Tags/RequiredFunctionCode 字段，引入 `TemplateBinding` 模型、绑定服务与 `/api/templates/bindings`、`/api/templates/runtime` 端点，为系统实体页面的模板化铺路。
- **模板运行态（前端）**：Blazor `PageLoader` 通过 `TemplateRuntimeClient` 优先消费 `/api/templates/runtime`，显示模板信息与数据范围，缺省时自动回退旧版模板 API。
- **角色管理界面**：新增 `/roles` 页面，提供角色列表、基础信息编辑与 FunctionNodes 权限树分配功能。
- **默认管理员预置**：应用启动自动确保 `admin/Admin@12345` 存在并绑定 `SYS.ADMIN` 角色，方便从零数据库直接进入系统。
- **用户档案**：实现 `/api/users` 查询/创建/更新/角色分配端点、`UserService` 以及 `/users` 页面（列表、详情、角色勾选、密码/状态管理），并补充 `UserManagementTests` 验证 API 行为。

### Changed
- **文档体系**：按照“设计 / 指南 / 参考 / 历史 / 流程 / 示例”重构目录结构，更新根 `README.md` 与 `docs/PROC-00-文档索引.md` 并新增 `docs/guides/FRONT-01-实体定义与动态实体操作指南.md`。
- **组织接口提示**：在 UI 与文档中统一组织维度说明，方便在角色框架内复用 `OrganizationId` 字段。
- **领域化菜单**：新增 `/api/access/functions/me` 以及动态领域导航（领域切换 + 领域内二/三级菜单），菜单与 FunctionNodes 权限实时联动。
- **权限过滤**：`BAS.AUTH.ROLE.PERM`、`BAS.AUTH.USER.ROLE` 功能节点现同时驱动菜单与 API 过滤，角色/用户相关端点通过 `FunctionPermissionFilter` 自动校验对应函数码，保持“所见即所得”的授权体验。

### Fixed
- **组织能力联动**：在接入权限框架的同时，修复组织接口在实体编辑器中的提示与验证不一致问题，防止实体勾选后缺少字段。
- **角色管理多语言**：补全角色管理页面缺失的 i18n 资源键（`BTN_NEW_ROLE`、`LBL_ROLE_INFO`、`LBL_ROLE_PERMISSIONS`、`LBL_ROLE_SYSTEM`、`LBL_ROLE_DEFAULT_NAME`、`MSG_SAVE_SUCCESS`、`ROLE_SYSTEM_ADMIN_NAME`、`ROLE_SYSTEM_ADMIN_DESC`），系统管理员角色名称和描述现在支持中文、日文、英文三语显示。

## [0.5.10] - 2025-11-12

### Added
- **组织管理**：新增 `OrganizationNode` 模型、EF 配置、领域服务、API 以及 Blazor 管理页，支持树形组织的增删改、PathCode 计算与实时刷新。
- **组织接口 (IOrganizational)**：实体定义可勾选“组织维度”，自动注入 `OrganizationId` 字段并与权限层打通，后端只持久化 ID、其余信息由权限上下文注入。
- **业务领域 / 自动命名**：实体编辑器提供领域下拉，命名空间自动拼接成 `BobCrm.<Domain>.<Entity>`，新增字段默认生成为 `Field1/Field2...`，减少手工输入。
- **回归测试**：为实体编辑器的行内增删、接口联动与多语提示补充集成测试，锁定关键交互。

### Changed
- **实体定义编辑体验**：② 区域改为 Card + Collapse，可折叠的属性面板更节省空间；图标字段加入预览提示并说明可接受的 Icon 名称/图片 URL。
- **文档同步**：在 UI 设计说明中补充组织接口、领域命名与自动字段策略，保持产品规范一致。

### Fixed
- **多语言控件稳定性**：修复 Dropdown 拉出后无法收起、宽度不一致、ESC/点击空白不生效等问题，并补齐缺失的 i18n 字典。
- **AntDesign 兼容性**：改写 Popover / Dropdown API 使用方式，去除 `VisibleChange` 等不存在的参数，同时移除 `AllowClear` 带来的 affix misalignment。
- **子实体与建模缺陷**：解决新增子实体后列表不刷新、必填校验不触发、数据库迁移缺失及 117 个单元测试失败等问题。

## [0.5.9] - 2025-11-11

### Added
- **实体定义页重构**：实现“实体信息 / 子实体 / 接口”多标签改版，子实体改为表格行内编辑；同时为多语控件增加默认语言加粗、灰色背景和行标题。
- **多语言输入重写**：MultilingualInput 采用自定义 Overlay 与 Ant Design Dropdown 组合，支持宽度同步、焦点展开、失焦收起。
- **持久化与测试**：补充实体聚合管理的持久化逻辑，并新增覆盖常见行为的单元测试。

### Fixed
- **多语言控件体验**：逐步修复双重描边、错位、Overlay 不对齐、滚动条缺失、无法关闭、点击空白不收起等问题。
- **页面滚动与遮罩**：解除全局 overflow 限制，保证下拉面板不会阻止页面滚动；按需移除 `Suffix` / `AllowClear`，避免 affix-wrapper 造成宽度差异。

## [0.5.8] - 2025-11-07

### 新增 (Added)

**🎨 完整的多模板管理系统 - FormTemplate 表与前后端全栈实现**

#### 背景与需求
- **旧方案问题**：
  - 每个用户每个实体类型只能有一个布局（UserLayout 表）
  - 无法创建多个模板供不同场景使用
  - 无默认模板机制，新用户需要重新设计
  - 模板命名、描述、分组功能缺失

- **新需求**：
  - 每个实体可以有多个命名模板（如"客户详情-简版"、"客户详情-完整版"）
  - 支持用户默认模板（每个用户每个实体一个）
  - 支持系统默认模板（每个实体一个，对所有用户生效）
  - 模板选择优先级：用户默认 > 系统默认 > 第一个模板
  - 实体类型锁定：模板保存后不允许修改实体类型
  - 删除保护：系统默认、用户默认、正在使用的模板不可删除

#### 后端实现

1. **FormTemplate 域模型** (`src/BobCrm.Api/Base/Models/FormTemplate.cs`)：
   ```csharp
   public class FormTemplate
   {
       public int Id { get; set; }
       public string Name { get; set; }              // 模板名称
       public string? EntityType { get; set; }       // 实体类型（customer/product/order）
       public string UserId { get; set; }            // 所属用户
       public bool IsUserDefault { get; set; }       // 是否为用户默认
       public bool IsSystemDefault { get; set; }     // 是否为系统默认
       public string? LayoutJson { get; set; }       // 布局JSON
       public string? Description { get; set; }      // 模板描述
       public DateTime CreatedAt { get; set; }
       public DateTime UpdatedAt { get; set; }
       public bool IsInUse { get; set; }             // 是否正在使用
   }
   ```

2. **数据库迁移** (`20251107030000_AddFormTemplateTable.cs`)：
   - 创建 FormTemplates 表
   - 添加三个复合索引优化查询：
     - `(UserId, EntityType)`
     - `(UserId, EntityType, IsUserDefault)`
     - `(EntityType, IsSystemDefault)`

3. **完整的 CRUD API** (`src/BobCrm.Api/Endpoints/TemplateEndpoints.cs`)：
   - **GET /api/templates** - 获取用户的所有模板
     - 支持 `?entityType=customer` 过滤
     - 支持 `?groupBy=entity` 按实体类型分组
     - 返回排序：用户默认优先，然后按更新时间倒序
   - **GET /api/templates/{id}** - 获取单个模板详情
   - **POST /api/templates** - 创建新模板
     - 自动清除同实体类型下的其他用户默认模板
     - 系统默认模板仅管理员可设置
   - **PUT /api/templates/{id}** - 更新模板
     - EntityType 一旦设置后锁定，不允许修改
     - 设置为用户默认时，自动取消其他模板的默认标记
   - **DELETE /api/templates/{id}** - 删除模板
     - 业务规则：系统默认、用户默认、正在使用的模板不可删除
   - **GET /api/templates/effective/{entityType}** - 获取有效模板
     - 优先级：用户默认 > 系统默认 > 第一个创建的模板

4. **业务逻辑保护**：
   - EntityType 锁定：防止修改已设计模板的实体类型导致数据混乱
   - 默认模板唯一性：同一用户同一实体只能有一个用户默认模板
   - 删除保护：三类模板受保护（系统默认/用户默认/正在使用）
   - 权限控制：所有端点都需要认证，只能操作自己的模板

#### 前端实现

1. **Templates.razor 完全重写** (602行，`/templates` 路由)：
   - **分组视图**：
     - 平铺列表：所有模板按更新时间倒序显示
     - 按实体分组：将模板按实体类型（客户/产品/订单）分组显示
   - **模板卡片**：
     - 显示模板名称、描述、更新时间
     - 徽章：用户默认（蓝色）、系统默认（紫色）、实体类型（绿色）
     - 操作按钮：
       - **编辑** - 跳转到设计器
       - **设为默认** - 设置为用户默认模板（已是默认则不显示）
       - **删除** - 删除模板（受保护模板禁用按钮）
   - **友好提示**：
     - 空状态：显示"暂无模板"提示
     - 删除确认：JavaScript confirm 对话框
     - 成功/失败消息：右上角浮动提示，3秒后自动消失

2. **FormDesigner.razor 增强** (1115行，`/designer` 和 `/designer/{id}` 路由)：

   **路由处理**：
   - `/designer/new` - 创建新模板（空白画布）
   - `/designer/{id}` - 编辑现有模板（加载模板数据）

   **模板属性面板**（右侧，无选中组件时显示）：
   - **模板名称**：文本输入框（必填）
   - **模板描述**：多行文本框（3行，可选）
   - **实体类型**：
     - 新建模板：使用 EntitySelector 组件选择实体
     - 编辑模板：显示为禁用输入框 + 🔒 锁定提示
   - **组件数量**：显示当前画布上的组件数量（蓝色大数字）
   - **提示信息**：蓝色信息卡片，说明模板的作用

   **保存逻辑升级**：
   - **验证**：模板名称和实体类型为必填
   - **创建模式**：
     - POST /api/templates 创建新模板
     - 保存成功后获取新模板ID
     - 自动更新 URL 为 `/designer/{newId}`（避免重复创建）
     - 锁定实体类型
   - **更新模式**：
     - PUT /api/templates/{id} 更新现有模板
     - 保留实体类型锁定状态

   **加载逻辑重构**：
   - 使用 FormTemplate API（GET /api/templates/{id}）
   - 解析模板元数据：名称、描述、实体类型、布局JSON
   - 判断实体类型锁定状态（有EntityType则锁定）
   - 兼容处理：非数字ID视为旧格式，初始化空模板

3. **PageLoader.razor 集成** (`/{entityType}/{id}` 路由)：
   - 优先使用 GET /api/templates/effective/{entityType} 加载模板
   - 如果找不到 FormTemplate，回退到旧的 UserLayout API（向后兼容）
   - 错误处理：模板未找到、JSON 解析失败、字段绑定错误都保留控件（只是没有数据）

4. **前端 DTO 模型** (`src/BobCrm.App/Models/FormTemplate.cs`)：
   ```csharp
   public class FormTemplate { /* 对应后端模型 */ }
   public class TemplateGroupByEntity { /* 按实体分组 */ }
   public class TemplateGroupByUser { /* 按用户分组 */ }
   ```

#### 技术亮点

1. **OOP 设计原则**：
   - 单一职责：TemplateEndpoints 专注模板管理
   - 开闭原则：新增实体类型无需修改模板系统
   - 依赖倒置：通过 IRepository<FormTemplate> 访问数据

2. **数据完整性**：
   - 复合索引优化查询性能
   - 唯一性约束通过代码强制执行
   - 事务保证：自动清除旧默认模板 + 设置新默认模板

3. **用户体验**：
   - 渐进式增强：先显示模板列表，再按需加载详情
   - 友好提示：操作成功/失败都有清晰反馈
   - 实体类型锁定：防止误操作导致数据混乱
   - 删除保护：重要模板禁用删除按钮，防止误删

4. **向后兼容**：
   - PageLoader 保留 UserLayout API 回退逻辑
   - FormDesigner 可以加载旧格式模板
   - 数据库保留 UserLayouts 表（旧数据不丢失）

#### 工作流程示例

**创建新模板**：
1. 用户访问 `/templates`，点击"新建模板"
2. 跳转到 `/designer/new`（空白画布）
3. 输入模板名称："客户详情-简版"
4. 输入描述："仅显示基本信息和联系方式"
5. 选择实体类型："客户"（customer）
6. 拖拽组件设计表单布局
7. 点击"保存布局"
8. 系统创建模板，URL 更新为 `/designer/123`
9. 实体类型自动锁定，不可再修改

**设置默认模板**：
1. 用户访问 `/templates`
2. 在"客户详情-简版"卡片上点击"设为默认"
3. 系统自动取消"客户详情-完整版"的默认标记
4. "客户详情-简版"变为用户默认模板
5. 后续访问 `/customer/1` 自动使用简版模板

**删除模板**：
1. 用户在模板列表点击"删除"按钮
2. 系统检查模板状态：
   - 是系统默认？→ 禁用按钮
   - 是用户默认？→ 禁用按钮
   - 正在使用？→ 禁用按钮
   - 可以删除 → 弹出确认对话框
3. 用户确认后，模板被删除

#### 测试与验证
- ✅ 创建/编辑/删除模板功能正常
- ✅ 实体类型锁定机制生效
- ✅ 默认模板优先级正确（用户默认 > 系统默认 > 第一个）
- ✅ 删除保护规则正确执行
- ✅ 分组视图正常切换
- ✅ 所有操作都有友好提示

#### 相关提交
- `34b05dc` - fix: 修复布局保存和加载的数据格式不匹配问题（双重序列化）
- `5dae952` - feat: 实现完整的多模板管理系统（FormTemplate表+前后端集成）
- `9bd67bc` - feat: 增强FormDesigner支持FormTemplate API和实体锁定

#### 收益
- ✅ **多模板支持**：每个实体可以有无限个命名模板
- ✅ **默认模板机制**：新用户可以快速开始，无需从零设计
- ✅ **分组管理**：按实体类型组织模板，清晰易用
- ✅ **数据保护**：实体类型锁定 + 删除保护，防止误操作
- ✅ **扩展性**：支持未来的模板导入/导出、模板市场等功能

---

## [0.5.7] - 2025-11-07

### 新增 (Added)

**🏷️ OOP实现：组件代码/名称自动生成与唯一性校验**

#### 功能需求
- 每个组件被放到画布上时，自动生成人类可读的 Code/ID（如 textbox1, button2）
- Code 在属性窗口中可见、可编辑，具有排重校验
- 按组件类型分组自动编号（textbox1, textbox2, button1, button2...）
- 必须遵循 OOP 最佳实践，不能在渲染层使用 if-else/switch-case

#### 实现方案

1. **基类扩展 - DraggableWidget**：
   - 新增 `Code` 属性（string，人类可读标识）
   - 新增抽象方法 `GetDefaultCodePrefix()`，所有组件必须实现
   - Code 自动加入属性元数据列表，显示在属性面板「基本」分组

2. **所有组件类实现前缀方法**（18个组件）：
   - TextboxWidget → "textbox"
   - NumberWidget → "number"
   - TextareaWidget → "textarea"
   - CalendarWidget → "calendar"
   - SelectWidget → "select"
   - ListboxWidget → "listbox"
   - ButtonWidget → "button"
   - LabelWidget → "label"
   - CheckboxWidget → "checkbox"
   - RadioWidget → "radio"
   - GridWidget → "grid"
   - PanelWidget → "panel"
   - SectionWidget → "section"
   - FrameWidget → "frame"
   - TabContainerWidget → "tabcontainer"
   - TabWidget → "tab"
   - GroupBoxWidget → "groupbox"
   - GenericContainerWidget → "container"

3. **WidgetCodeGenerator 服务**（新增文件）：
   - `GenerateUniqueCode(widget, allWidgets)` - 生成唯一代码
     - 获取组件前缀：`widget.GetDefaultCodePrefix()`
     - 递归获取所有现有 Code（包括嵌套子组件）
     - 自动递增数字直到找到唯一 Code（prefix1, prefix2...）
   - `IsCodeUnique(code, widgetId, allWidgets)` - 校验唯一性
   - `ValidateAndSuggestCode(widget, allWidgets)` - 验证并建议新 Code
   - `GetAllCodes(widgets)` - 递归提取所有 Code（私有方法）

4. **FormDesigner 集成**：
   - 两个组件拖放点自动生成 Code：
     - 画布拖放：`newWidget.Code = WidgetCodeGenerator.GenerateUniqueCode(newWidget, GetAllWidgets())`
     - 容器拖放：同上逻辑，确保在容器内创建的组件也有唯一 Code
   - 新增辅助方法 `GetAllWidgets()` - 调用 WidgetNavigationHelper

5. **WidgetNavigationHelper 扩展**：
   - 新增 `GetAllWidgets(widgets)` - 递归获取所有组件（包括容器嵌套）
   - 使用 `yield return` 优化内存占用

6. **多语言资源**：
   - `PROP_CODE` - 代码/名称 / コード/名前 / Code/Name
   - `PROP_GROUP_BASIC` - 基本 / 基本 / Basic

#### 技术细节
- 使用 **抽象方法模式**：基类定义契约，每个组件实现自己的前缀
- 遵循 **开闭原则**：新增组件类型只需实现 `GetDefaultCodePrefix()`，无需修改生成器
- 遵循 **单一职责**：WidgetCodeGenerator 专注于 Code 生成与校验逻辑
- 遵循 **里氏替换**：所有组件通过基类引用统一调用 `GetDefaultCodePrefix()`
- 使用 **递归遍历**：处理容器嵌套场景，确保跨整个组件树的唯一性
- 使用 **不区分大小写比较**：`StringComparer.OrdinalIgnoreCase` 避免 Code1 与 code1 冲突

#### 收益
- ✅ **OOP 最佳实践** - 多态替代分支逻辑
- ✅ **开闭原则** - 新增组件无需修改代码生成器
- ✅ **可扩展性** - 支持容器嵌套的递归校验
- ✅ **用户体验** - 自动命名，减少手动输入
- ✅ **数据引用** - 为未来的组件引用（如数据绑定、脚本）做准备

### 测试 (Tested)
- ✅ 编译通过：0 错误，1 个无关警告
- ✅ 单元测试：101 个测试通过，3 个跳过
- ✅ 功能验证：拖放组件自动生成唯一 Code（textbox1, textbox2...）

---

## [0.5.6] - 2025-11-07

### 重构 (Refactored)

**🏗️ OOP重构：组件渲染多态化，消除集中式分支逻辑**

#### 问题分析
- **RuntimeWidgetRenderer.cs** (325行) 包含大量 switch-case 分支逻辑
- **DesignWidgetContentRenderer.cs** (215行) 同样存在重复的分支结构
- 违反开闭原则：每增加新组件类型都需修改这两个渲染器
- 违反单一职责原则：渲染器承担了所有组件的渲染逻辑
- 维护成本高：组件的渲染逻辑分散在多个地方

#### 重构方案
1. **在基类 DraggableWidget 中添加虚方法**：
   - `RenderRuntime(RuntimeRenderContext)` - 运行时渲染（Browse/Edit模式）
   - `RenderDesign(DesignRenderContext)` - 设计态渲染
   - 引入 `RuntimeRenderContext` 和 `DesignRenderContext` 封装渲染上下文
   - 提供辅助方法：`RenderFieldLabel`, `RenderReadOnlyValue`

2. **所有组件类重写渲染方法** (9个组件)：
   - TextboxWidget - 文本输入框
   - NumberWidget - 数字输入框
   - TextareaWidget - 文本域
   - CalendarWidget - 日期选择
   - SelectWidget - 下拉选择
   - ListboxWidget - 列表框
   - ButtonWidget - 按钮
   - LabelWidget - 标签
   - CheckboxWidget / RadioWidget - 复选框/单选框

3. **渲染器简化为多态调用**：
   - RuntimeWidgetRenderer: 300+ 行 → **50 行** (-83%)
   - DesignWidgetContentRenderer: 200+ 行 → **42 行** (-79%)
   - 移除所有私有渲染方法，仅调用 `widget.RenderRuntime(context)` 或 `widget.RenderDesign(context)`

#### 收益
- ✅ **单一职责原则**：每个组件类负责自己的渲染逻辑
- ✅ **开闭原则**：新增组件类型无需修改渲染器代码
- ✅ **里氏替换原则**：所有组件都可以通过基类引用调用
- ✅ **多态**：使用虚方法重写替代 if-else/switch-case
- ✅ **封装**：渲染逻辑封装在组件类内部
- ✅ **代码简化**：渲染器总行数减少 ~450 行
- ✅ **可维护性**：每个组件的所有逻辑（属性、渲染）都在同一个类中
- ✅ **可扩展性**：添加新组件只需创建新类，无需修改现有代码

#### 技术细节
- 创建 `RuntimeWidgetRenderMode` 枚举（Browse/Edit）
- 使用 `EventCallbackFactory` 处理组件事件
- 支持 Browse 模式（只读显示）和 Edit 模式（可编辑）
- 设计态渲染提供纯视觉预览（禁用交互）

### 测试 (Tested)
- ✅ 编译通过：0 错误，1 个无关警告
- ✅ 单元测试：101 个测试通过，3 个跳过
- ✅ 架构验证：所有组件渲染逻辑正常工作

---

## [0.5.5] - 2025-11-06

### 重构 (Refactored)

**🏗️ 重大架构重构：设计态渲染与属性编辑器完全组件化**

#### 1. **容器设计态渲染器组件化**
- **问题**：所有容器的设计态渲染逻辑集中在 `FormDesigner.razor` 中（160+ 行 `if-else`）
- **重构**：
  - 创建 `Components/Designer/ContainerRenderers/` 目录
  - 为每种容器创建独立的渲染器组件：
    - `GridDesignRenderer.razor`
    - `PanelDesignRenderer.razor`
    - `SectionDesignRenderer.razor`
    - `FrameDesignRenderer.razor`
    - `TabContainerDesignRenderer.razor`
    - `GenericContainerDesignRenderer.razor`
  - `FormDesigner.razor` 从 160+ 行 `if-else` 简化为 50 行 `switch` + 组件调用
- **收益**：
  - ✅ 职责分离：设计器逻辑 vs 渲染细节
  - ✅ 开闭原则：添加新容器只需创建新文件
  - ✅ 代码简化：FormDesigner 减少 ~150 行

#### 2. **属性面板元数据驱动重构**
- **问题**：每个容器都有独立的 PropertyPanel 组件（5个文件，~250 行重复代码）
- **重构**：
  - 删除所有重复的 PropertyPanel 组件（5个文件）
  - 创建 `PropertyMetadata.cs` 定义属性元数据结构
  - 创建通用 `PropertyEditor.razor` 组件，根据元数据动态渲染
  - 创建 `IWidgetPropertyProvider` 服务
- **收益**：
  - ✅ 消除重复：5个组件 → 1个通用组件
  - ✅ 统一体验：所有属性编辑器UI一致
  - ✅ 易于扩展：添加新控件只需定义元数据清单
  - ✅ 支持高级特性：条件显示、分组、嵌套属性

#### 3. **面向对象重构：属性元数据归属 Widget**
- **问题**：属性元数据集中在 `WidgetPropertyProvider` 中（违反封装原则）
- **重构**：
  - 在 `DraggableWidget` 基类添加虚方法 `GetPropertyMetadata()`
  - **所有 15 个 Widget 类**重写此方法，定义自己的属性元数据：
    - **容器组件** (5个): GridWidget, PanelWidget, SectionWidget, FrameWidget, TabContainerWidget
    - **普通组件** (10个): TextboxWidget, NumberWidget, SelectWidget, TextareaWidget, ButtonWidget, LabelWidget, CalendarWidget, CheckboxWidget, RadioWidget, ListboxWidget
  - `WidgetPropertyProvider` 简化为仅 1 行：`widget.GetPropertyMetadata()`
- **收益**：
  - ✅ 封装原则：每个 Widget 是自己属性的提供者
  - ✅ 单一职责：属性定义与类在一起
  - ✅ 开闭原则：添加新 Widget 无需修改 Provider
  - ✅ 代码简化：WidgetPropertyProvider 从 143 行 → 34 行（-76%）
  - ✅ 架构完整性：所有 Widget 类型都实现了属性元数据

#### 4. **多语言国际化完善**
- **问题**：代码中硬编码了大量中文（属性标签、选项值、分组名等）
- **修复**：
  - 所有属性标签改为多语言键（`PROP_*` 前缀）
  - `PropertyEditor.razor` 使用 `I18n.T()` 进行翻译
  - 添加 **62 个**新的多语言资源：
    - **容器属性** (28个): `PROP_COLUMNS`, `PROP_GAP`, `PROP_TITLE`, `PROP_COLLAPSIBLE` 等
    - **普通组件属性** (20个): `PROP_TEXT`, `PROP_BOLD`, `PROP_MIN_VALUE`, `PROP_STEP`, `PROP_ROWS`, `PROP_AUTO_SIZE` 等
    - **选项值** (14个): `PROP_DIRECTION_ROW`, `PROP_BORDER_SOLID`, `PROP_BUTTON_PRIMARY` 等
    - **分组**: `PROP_GROUP_LAYOUT`
    - **占位符**: `PROP_PANEL_TITLE_PLACEHOLDER`, `PROP_SECTION_TITLE_PLACEHOLDER`
- **收益**：
  - ✅ 代码中不再有硬编码的中文
  - ✅ 支持多语言切换（中文/日文/英文）
  - ✅ 符合国际化最佳实践
  - ✅ 覆盖所有 Widget 类型的属性

### 修复 (Fixed)
- **容器内外错位视觉问题（多次迭代修复）**：
  
  **修复1 - 容器内容区域填充**：
  - 问题：容器内容区域使用 `min-height`，无法填充整个容器空间
  - 修复：所有容器改用 `display:flex; flex-direction:column;` + `flex:1` 布局
  - 影响：Grid, Panel, Section, Frame, TabContainer, GenericContainer
  
  **修复2 - 双层边框消除**：
  - 问题：外层包裹 div 有 `border` + `background` + `padding`，内层容器又有自己的 `border`，造成双层边框
  - 修复：外层 div 改用 `outline`（不占布局空间），去掉中间 `padding:4px` 包裹层
  - 效果：不再有双层边框，容器真实样式完全可见
  
  **修复3 - 空状态padding优化**：
  - 问题：空容器的 placeholder 被固定 padding（12-16px）挤压，内容区域比容器小
  - 修复：空状态时 `padding=0`，有子组件时 `padding=原值`（条件表达式）
  - 效果：空容器的 placeholder 填满整个容器，内外等宽等高
  
  **修复4 - 双层resize handle消除**：
  - 问题：顶层包裹 div 有 resize handle，内部调用 `RenderDesignWidget` 又添加一个 resize handle，同一组件显示两个 handle
  - 修复：职责分离
    - 创建 `RenderWidgetVisual()` 方法：仅渲染视觉外观，无交互层
    - 顶层包裹 div 内部调用 `RenderWidgetVisual`（纯视觉）
    - `RenderDesignWidget` 保留完整交互层，用于容器内子组件递归渲染
  - 效果：每个组件只有一个 resize handle，不再重复

- **Width 最大值逻辑移至 Widget 类（面向对象修复）**：
  - 问题：所有 Widget 的 Width 属性 `Max=100`（硬编码），只适用于百分比，对像素单位不合理（超过100被截断）
  - 错误尝试1：在 PropertyEditor 中判断 WidthUnit（违反职责分离）
  - 错误尝试2：硬编码 `px → 2000`（magic number，不知道屏幕实际宽度）
  - 正确方案：
    - 在 `DraggableWidget` 基类添加 `GetMaxWidth()` 虚方法
    - 返回类型改为 `int?`（可空）
    - `%` 模式：返回 `100`（硬约束，百分比不能超过100）
    - `px` 模式：返回 `null`（不限制，由画布和浏览器自然约束）
    - PropertyEditor 检查 `prop.Max.HasValue`，只在有值时设置 `Max` 属性
  - 效果：
    - ✅ 百分比模式：0-100 的硬约束
    - ✅ 像素模式：自由输入，由实际渲染宽度约束
    - ✅ 不再有 magic number（2000）
    - ✅ 符合单位语义：百分比是相对单位（需约束），像素是绝对单位（灵活）

- **组件设计态视觉差异化渲染**：
  - **问题**：
    - 所有组件在设计器中都渲染为相同的 32px 矩形框
    - 无法通过视觉区分不同类型的组件（Textbox、Calendar、Checkbox 等）
    - 拖入组件后不知道是什么类型，用户体验差
  - **实现**（`RenderWidgetVisual` 方法）：
    - **Textarea**：80px 高的多行文本框，显示 "多行文本..." 占位符
    - **Calendar**：带日历图标 📅 的输入框，显示 "选择日期"
    - **Select/Listbox**：带下拉箭头 ⬇️ 的选择框，显示 "请选择"
    - **Checkbox**：显示两个复选框选项（选中 ☑️ + 未选中 ☐）
    - **Radio**：显示两个单选按钮选项（选中 🔘 + 未选中 ⭕）
    - **Button**：蓝色背景按钮，显示 Label 文本
    - **Label**：文本标签，支持粗体显示（根据 `Bold` 属性）
    - **Number**：数值输入框，带上下箭头 ⬆️⬇️
    - **Textbox**（默认）：单行输入框
  - **设计原则**：
    - 所有样式符合 AntDesign 规范（颜色、圆角、边框）
    - 使用 `IconType.Outline` 图标增强识别度
    - 灰色占位文本 `color:#ccc` 表示非交互状态
  - **效果**：
    - ✅ 每种组件都有独特的视觉外观
    - ✅ 设计器中一眼就能识别组件类型
    - ✅ 提升设计体验和易用性
    - ✅ 符合现代 UI 设计工具的标准（如 Figma、Sketch）

- **PropertyEditor Select 组件类型转换异常（严重BUG修复）**：
  - **问题1 - 类型转换错误**：
    - Select 配置：`TItem="PropertyOption"`, `TItemValue="string"`
    - `OnSelectedItemChanged` 接收 `PropertyOption` 类型
    - 导致运行时类型转换异常：`Invalid cast from String to PropertyOption`
  - **问题2 - 级联失效（Circuit 断开）**：
    - Blazor Server 特性：C# 未处理异常导致 SignalR 连接断开
    - 一个组件错误导致整个页面 Circuit 断开
    - 后续所有交互失效：`No interop methods are registered for renderer 1`
    - 用户体验：添加第二个容器时崩溃，页面完全失效
  - **修复**：
    - 简化 Select 为纯字符串类型：`TItem="string"`, `TItemValue="string"`
    - `OnSelectedItemChanged` 接收 `string` 参数
    - `SelectOption` 也使用 `string` 类型
  - **效果**：
    - ✅ 不再有类型转换异常
    - ✅ 可以添加多个容器和组件
    - ✅ 属性面板下拉选择稳定工作
    - ✅ Circuit 保持连接，页面不再崩溃

### 文档 (Documentation)
- 删除重复的模块级 README（`ContainerRenderers/README.md`）
- 更新主设计文档，新增「容器设计态渲染器」章节
- 遵循文档集中管理原则

### 技术债务清理
- 删除 5 个重复的 PropertyPanel 组件（~250 行）
- 删除 `WidgetPropertyProvider` 中的所有静态方法（~109 行）
- 总计清理约 359 行重复代码

---

## [0.5.4] - 2025-11-06

### 新增 (Added)
- **个人中心页面** (`/profile`)
  - 用户信息展示（用户名、邮箱、角色）
  - 渐变色圆形头像显示（默认图标）
  - 密码修改功能（前端验证 + 后端API）
  - 头像上传占位（提示功能即将上线）
- **右上角用户区域改进**：
  - 已登录状态：显示头像 + 用户名（可点击跳转个人中心）+ 退出按钮
  - 未登录状态：显示登录按钮
  - 响应式悬停效果和动画
- **后端API支持**：
  - `POST /api/auth/change-password` - 修改密码端点
  - `GET /api/auth/me` 返回格式优化（`userName`, `role` 字段）
- **表单设计器容器功能三步重构**：
  - **属性面板组件化**：为每种容器创建独立属性面板组件（Grid/Panel/Section/Frame/TabContainer）
  - **容器渲染差异化**：5种容器有完全不同的视觉外观和布局特性
  - **容器拖放支持**：所有容器支持接收拖放，支持容器嵌套
- **集成测试大幅扩充（+37个测试，从67个增至104个）**：
  - EntityMetadataTests（6个）：测试实体元数据端点
  - UserProfileTests（9个）：测试个人中心和密码修改功能
  - DatabaseInitializerTests（6个）：测试实体自动注册的所有逻辑分支
  - CustomersTests扩展（+9个）：客户CRUD的各种异常路径和权限检查
  - LayoutTests扩展（+8个）：布局Scope优先级、生成逻辑、权限检查

### 变更 (Changed)
- **系统设置页面**：移除冗余的"用户信息"部分（用户相关功能迁移至个人中心）
- **AppHeader 组件**：根据登录状态动态渲染用户区域UI
- **FormDesigner**：属性面板从200+行if-else块重构为组件化设计
- **EntityMetadataEndpoints**：`/api/entities/all` 补充返回 `entityName`, `entityRoute`, `isRootEntity` 字段

### 修复 (Fixed)
- **多语言资源重复键值**：删除 `LBL_EMAIL`, `LBL_USERNAME`, `LBL_LOADING`, `LBL_LOAD_FAILED` 的重复定义（曾导致57个测试失败）
- **测试密码错误**：修正测试中使用的密码为 `User@12345`（而非`Test@12345`）

### 样式 (Styling)
- 新增用户头像和用户区域相关CSS样式
- 个人中心页面卡片式布局和渐变色设计
- 暗黑模式适配

### 测试 (Testing)
- **测试覆盖率大幅提升**：从67个测试增加到104个测试（**+37个，+55%**）
- **测试通过率**：100%（104个测试，0个失败，101个成功，3个跳过）
- **核心业务逻辑覆盖率**：~90%（所有关键分支都有测试）
- **API端点覆盖率**：~95%（所有公开端点的正常和异常路径）
- **新增测试文件**：
  - `EntityMetadataTests.cs` - 实体元数据端点测试（6个）
  - `UserProfileTests.cs` - 用户个人资料端点测试（9个）
  - `DatabaseInitializerTests.cs` - 数据库初始化逻辑测试（6个）
  - `docs/TEST-02-测试覆盖率报告.md` - 详细的测试覆盖情况分析文档

### 文档 (Documentation)
- 更新 `CHANGELOG.md` - 添加 v0.5.4 更新内容
- 更新 `README.md` - 指向最新版本

---

## [0.5.3] - 2025-11-05

### 新增 (Added)
- **EntitySelector 通用实体选择器组件** (237行)
  - 输入框 + 放大镜图标的用户友好界面
  - Modal 弹出框卡片式选择界面
  - 泛型支持、懒加载、搜索过滤
  - 支持自定义渲染（图标/标题/描述/元数据）
- **ISelectableEntity 接口** - 规范可选择实体的必需属性
  - Value: 唯一标识
  - DisplayName: 显示名称（已翻译）
  - Description: 描述（已翻译，可选）
  - Icon: 图标（可选）

### 变更 (Changed)
- **EntityMetadata 结构规范化**：
  - EntityType（主键）：改为存储类全名（如 `BobCrm.Api.Base.Customer`）
  - EntityName（新增）：类短名（如 `Customer`）
  - EntityRoute（新增）：URL路由名（如 `customer`）
  - 用于精确反射查找和反向检查
- **FormDesigner 实体类型选择**：
  - 使用 EntitySelector 替代下拉框
  - 实现实体类型锁定机制（已有组件的模板不可修改实体类型）
  - 数据加载时自动翻译 DisplayNameKey 为 DisplayName
  - 输入框正确显示翻译后的实体名称（如"顧客"）

### 修复 (Fixed)
- **全局布局滚动条问题**：
  - html/body 设置 `height: 100%` 和 `overflow: hidden`
  - app-shell 改为固定高度 `height: 100vh`
  - 各区域（侧边栏/内容区/设计器面板）独立滚动
  - 符合专业设计器布局规范
- **AntDesign Select 动态选项不显示问题**：
  - 尝试了10余种方案均无法稳定工作
  - 最终通过 EntitySelector 彻底解决

### 文档 (Documentation)
- 更新 `docs/ARCH-02-实体元数据自动注册机制.md` - 反映 EntityMetadata 结构变更
- 更新 `docs/PROD-01-客户信息管理系统设计文档.md` - 添加 EntitySelector 组件说明
- 新增 `CHANGELOG.md` - 统一管理所有版本更新历史

---

## [0.5.2] - 2025-11-05

### 新增 (Added)
- **Checkbox 组件** - 复选框/复选框组
  - 支持单个复选框或复选框组
  - 支持按钮样式（ButtonStyle）
  - 支持横向/纵向布局（Direction）
  - 默认值支持（多选时逗号分隔）
- **Radio 组件** - 单选按钮组
  - 标准单选按钮组
  - 支持按钮样式（ButtonStyle）
  - 支持横向/纵向布局（Direction）
  - 默认值支持
- **模板实体类型动态选择**：
  - 从硬编码改为动态从 `/api/entities` 加载
  - FormDesigner 点击画布背景显示模板属性
  - 实体类型下拉框（后在 v0.5.3 改为 EntitySelector）

### 修复 (Fixed)
- **模板加载功能**：实现 `LoadTemplate()` 方法
- **多语言资源补充**：
  - Number, Select, Textarea, Button, Panel, Grid, Tab 组件
  - 模板、实体类型、新建模板等 FormDesigner UI 元素
- **WidgetRegistry 图标错误**：NumberOutlined → FieldNumber
- **Alert 组件属性**：添加 `ShowIcon="true"`

### 变更 (Changed)
- **实体元数据管理**：
  - 从硬编码改为数据库驱动（EntityMetadata 表）
  - 实现自动注册/反注册机制
  - Customer 实现 IEntityMetadataProvider 接口
- **脚本优化**：
  - `verify-setup.ps1` 接受 .NET 8 或更高版本
  - 移除未使用的变量警告

---

## [0.5.1] - 2025-11-05

### 新增 (Added)
- **字段动作（Field Actions）** 功能：
  - RDP 下载：根据字段值生成 .rdp 文件
  - 文件验证：验证文件路径是否存在
  - Mailto 链接：生成邮件链接
  - 后端 API：`/api/actions/rdp/download`, `/api/actions/file/validate`, `/api/actions/mailto/generate`
  - 前端服务：FieldActionService
  - 集成测试：12个测试用例
- **对齐线功能（Snap Guides）**：
  - 拖拽时显示蓝色对齐参考线
  - 支持左/右/中心/上/下/中间对齐
  - 吸附阈值 8px
  - 性能优化：使用 requestAnimationFrame

### 变更 (Changed)
- **CustomerDetail 重构**：
  - 提取工具类：FileNameHelper, WidgetStyleHelper, WidgetSerializationHelper
  - 提取管理类：EditValueManager, TabStateManager
  - 提取辅助类：WidgetNavigationHelper, WidgetLabelHelper
  - 从 2750 行减少到 2348 行

### 文档 (Documentation)
- 更新 README.md - 标记字段动作功能为已完成
- 更新 `docs/API-01-接口文档.md` - 添加字段动作 API 文档
- 更新 `docs/PROD-01-客户信息管理系统设计文档.md` - 添加对齐线验证用例

---

## [0.5.0] - 2025-11-05

### 架构重构 (Refactoring)

**重大变更**：从单体架构重构为单一职责架构

#### 问题
- ❌ CustomerDetail.razor 混合了设计器、浏览器、编辑器三种职责
- ❌ 2750+ 行代码，难以维护
- ❌ 所有功能都与 Customer 实体强耦合

#### 解决方案
1. **FormDesigner.razor** (452行) - 通用表单设计器
   - 纯粹的布局设计，不依赖任何实体
   - 从 FieldDefinitions API 加载字段
   - 路由：`/designer` 或 `/designer/{templateId}`

2. **PageLoader.razor** (430行) - 通用页面加载器
   - 根据模板动态渲染任何实体
   - 路由：`/{entityType}/{id}`
   - 支持 Browse 和 Edit 模式

3. **CustomerDetail.razor** (17行 → 删除)
   - 简化为路由别名，后完全移除
   - 功能完全由 PageLoader 承担

#### 成果
- ✅ 代码量：2750行 → 882行（三个组件总和）
- ✅ 职责分离：设计 | 渲染 | 数据加载
- ✅ 可复用性：支持任意实体类型
- ✅ 可维护性：大幅提升

### 新增 (Added)
- **布局组件化**：
  - MainLayout, SiderLayout, SimpleLayout
  - EntityListSiderBase 抽象基类
- **FormTemplate 模型** - 表单模板元数据
- **WidgetRegistry** - 中央化Widget注册表

---

## [0.4.0] - 2025-11-04

### 新增 (Added)
- **Widget 系统完善**：
  - Number, Select, Textarea, Button, Calendar, Listbox
  - Panel, Grid, TabContainer, Tab
  - 共17种Widget类型
- **拖拽设计器**：
  - 组件工具栏（基础组件/布局组件）
  - 属性面板（动态编辑Widget属性）
  - 拖拽添加、调整大小、删除组件
- **运行态渲染**：
  - Browse 模式（只读）
  - Edit 模式（可编辑）
  - 字段数据绑定

### 文档 (Documentation)
- 初始版本的系统设计文档
- API 接口文档
- 测试指南

---

## [0.3.0] - 2025-11-03

### 新增 (Added)
- **国际化（i18n）**：中文、日文、英文三语支持
- **用户偏好设置**：主题、主题色、语言持久化
- **主题系统**：浅色/深色主题切换，自定义主题色
- **PostgreSQL 支持**：主数据库切换到 PostgreSQL
- **Docker Compose**：一键启动开发环境

---

## [0.2.0] - 2025-11-02

### 新增 (Added)
- **认证系统**：ASP.NET Identity + JWT
  - 注册、登录、登出、刷新令牌
  - 会话重连（服务器重启不掉线）
- **动态字段系统**：
  - FieldDefinition, FieldValue
  - JSONB 存储
- **客户访问控制**：CustomerAccess 表

---

## [0.1.0] - 2025-11-01

### 新增 (Added)
- **项目初始化**：
  - Blazor Server 前端
  - ASP.NET Core Web API 后端
  - EF Core + SQLite
- **基础实体**：
  - Customer（客户）
  - User（用户）
- **基础 CRUD**：
  - 客户列表、详情、创建、编辑

---

## 约定说明

### 变更类型
- `Added` - 新增功能
- `Changed` - 既有功能的变更
- `Deprecated` - 即将移除的功能
- `Removed` - 已移除的功能
- `Fixed` - 问题修复
- `Security` - 安全相关修复
- `Documentation` - 文档变更
- `Refactoring` - 代码重构（不改变功能）

### 版本号规则
- **主版本号（Major）**：不兼容的 API 修改
- **次版本号（Minor）**：向下兼容的功能性新增
- **修订号（Patch）**：向下兼容的问题修正

---

**维护者**：BobCRM 开发团队
**最后更新**：2025-11-17
