# 模板设计器与宿主补齐临时进度表

> 用途：跟踪“系统实体模板化闭环”在控件、设计器与宿主侧的逐步交付。完成的步骤会在 `CHANGELOG.md` 留痕，同时从本文件移除对应条目。

## 当前迭代目标

1. **Widget 覆盖**
   - 梳理组织/用户/角色/客户页面所需控件，并输出 Schema（属性、数据源、事件）。
   - 在 FormDesigner palette 中加入控件，占位 UI + 属性面板。
   - 扩展 `RuntimeWidgetRenderer` 支持新控件。

2. **宿主能力**
   - 实现 `ListTemplateHost`，负责列表 View 模板渲染（字段列、筛选、批量操作）。
   - 拆分 PageLoader，按 `UsageType` 分别加载 Detail/Edit 模板与数据。
   - 针对四个系统实体交付默认模板与宿主改造，保证能够仅靠模板运行。

3. **验证与落地**
   - FormDesigner 中复制/编辑系统默认模板 → 发布 → 菜单/角色授权 → 页面成功渲染。
   - 补充端到端用例或脚本，验证模板绑定 + 权限链路。

## 交付检查清单

| 步骤 | 负责人 | 输出 | 状态 |
| --- | --- | --- | --- |
| 设计器控件 Schema 定义 | Codex | Widget Schema (DataGridWidget, OrganizationTreeWidget, RolePermissionTreeWidget) | ☑ |
| 后端数据源模型定义 | Codex | DataSet/QueryDefinition/PermissionFilter 模型 + DTO + EF 配置 | ☑ |
| FormDesigner 控件注册 | Codex | WidgetRegistry 更新 + PropertyEditorType 扩展 | ☑ |
| 数据源/条件执行管道 | Codex | DataSet 服务实现、运行态 API、权限注入策略 | ☑ |
| 数据库迁移生成 | Codex | EF migration `AddDataSourceInfrastructure` | ☑ |
| ListTemplateHost 实现 | Codex | List 模板宿主组件 + WidgetJsonConverter | ☑ |
| Runtime Widget 渲染器扩展 | Codex | DataGridRuntime 组件 + Widget RenderRuntime 方法完善 | ☑ |
| FormDesigner 属性面板扩展 | TBD | 数据源选择器 UI 实现 | ☐ |
| PageLoader 扩展 | TBD | 支持 UsageType (Detail/Edit/List) | ☐ |
| 系统实体默认模板 | TBD | 组织/用户/角色/客户 List+Detail+Edit 模板 | ☐ |
| 单元测试与集成测试 | TBD | DataSet/DataSourceHandler 测试 | ☐ |
| 系统数据源类型种子数据 | TBD | entity/api/sql/view 类型初始化 | ☐ |
| E2E 验证与文档更新 | TBD | 测试记录 + 文档/Changelog 更新 | ☐ |

> 说明：每完成一项，将其状态改为 `☑`，同步 `CHANGELOG.md`，然后从本文件删除该行，以保持"仅包含剩余事项"的特性。

## Phase A 完整实现 (2025-11-17)

已交付的基础组件:

### 后端数据模型层
- ✅ 创建 `DataSet.cs`, `QueryDefinition.cs`, `PermissionFilter.cs`, `DataSourceTypeEntry.cs` 模型
- ✅ 创建对应的 DTO 类 (`DataSetDtos.cs`)
- ✅ 创建 EF Core 配置 (DataSetConfiguration, QueryDefinitionConfiguration, PermissionFilterConfiguration, DataSourceTypeEntryConfiguration)
- ✅ 更新 AppDbContext 添加 DbSet
- ✅ 生成数据库迁移 `AddDataSourceInfrastructure`,创建4个新表及索引

### 后端服务层
- ✅ 创建 `IDataSourceHandler` 接口定义数据源处理器契约
- ✅ 实现 `EntityDataSourceHandler` 作为示例数据源处理器
- ✅ 实现 `DataSetService` 提供完整的 CRUD 和 Execute 功能
- ✅ 在 `Program.cs` 注册服务和 Handler,实现依赖注入
- ✅ 创建 `DataSetEndpoints.cs`,实现完整的 REST API 端点

### 前端 Widget 模型层
- ✅ 创建 `DataGridWidget.cs` - 通用数据网格控件,支持分页、排序、筛选
- ✅ 创建 `OrganizationTreeWidget.cs` - 组织树控件,支持搜索、节点图标
- ✅ 创建 `RolePermissionTreeWidget.cs` - 角色权限树控件,支持级联选择、模板绑定显示
- ✅ 扩展 `PropertyEditorType` 枚举(Json, DataSetPicker, FieldPicker)
- ✅ 更新 `WidgetRegistry.cs`,添加 Data 类别和新控件注册

### 前端运行态组件
- ✅ 创建 `ListTemplateHost.razor` - 列表模板运行宿主,**使用 DI 注入 IRuntimeWidgetRenderer 和 AuthService**
- ✅ 创建 `WidgetJsonConverter.cs` - 自定义 JSON 转换器,支持根据 Type 属性反序列化为正确的 Widget 子类
- ✅ 创建 `DataGridRuntime.razor` - DataGrid 运行态渲染组件,**使用 AuthService 认证 API 调用**
- ✅ 实现 `DataGridWidget.RenderRuntime` - 使用 DataGridRuntime 组件进行渲染
- ✅ 实现 `OrganizationTreeWidget.RenderRuntime` - 完整的组织树 UI,包括搜索框和树结构
- ✅ 实现 `RolePermissionTreeWidget.RenderRuntime` - 完整的权限树 UI,包括工具栏、复选框、模板绑定显示

### OOP 架构设计 (遵循最佳实践)
- ✅ **避免硬编码枚举**: 创建 `DataSourceTypeEntry.cs` 元数据表,数据源类型可动态扩展
- ✅ **策略模式**: 创建 `IDataSourceHandler` 接口,每种数据源类型有独立实现类
- ✅ **依赖注入**: ListTemplateHost/DataGridRuntime 通过 DI 注入服务,避免直接 new 对象
- ✅ **认证集成**: 所有 API 调用使用 AuthService.CreateClientWithAuthAsync() 获取认证客户端
- ✅ **多语机制**: 所有模型使用 `Dictionary<string, string?>` 存储多语文本,避免硬编码
- ✅ **示例实现**: `EntityDataSourceHandler.cs` 展示如何实现数据源处理器
- ✅ **配置化设计**: 数据源配置通过 JSON 存储,支持不同类型的个性化配置

### 架构修复 (2025-11-17)
- ✅ 修复 `TemplateBindings.razor` CSS `@media` → `@@media` 转义问题
- ✅ 修复 `WidgetRegistry.cs` IconType.Outline.ApartmentOutline → Apartment
- ✅ 修复 `TemplateBindingService.cs` IReadOnlyList 类型转换
- ✅ 修复 `MenuManagement.razor` RadioGroup 组件语法
- ✅ 修复 `ListTemplateHost.razor` 使用 DI 而非直接实例化 RuntimeWidgetRenderer

### 编译错误全面修复 (2025-11-17)
- ✅ **MenuManagement.razor**:修复 Message 服务调用（11处 `await Message.*()` 错误,9处方法名缺失错误）
- ✅ **MenuManagement.razor**:修复 DragEventArgs.PreventDefault 调用（3处）
- ✅ **MenuManagement.razor**:修复 RadioGroup 导航类型切换逻辑（使用 `Value` + `ValueChanged` 模式,保留 OnNavigationTypeChanged 回调）
- ✅ **DynamicEntityData.razor**:修复 Form 类型推断错误（添加 Model 属性）
- ✅ **DynamicEntityData.razor**:修复 EventCallback 类型不匹配错误（13处 Switch/DatePicker/Input/InputNumber）
- ✅ **编译结果**:App 项目 0 错误 0 警告,Api 项目 0 错误 3 警告（可空引用和异步方法警告,非关键）

### 编译警告全面修复 (2025-11-17 第二轮)
- ✅ **EntityDataSourceHandler.cs**:修复 CS1998 异步方法警告（3处,移除 async 使用 Task.FromResult）
- ✅ **AppDbContext.cs**:修复 CS8620 可空引用类型警告（13处 jsonConverter! 空值宽容操作符）
- ✅ **DynamicEntityData.razor**:修复 CS8620 可空字典警告（2处 payload! 空值宽容操作符）
- ✅ **ListTemplateHost.razor**:修复 CS8602 空引用警告（1处 _widgets!.Count）
- ✅ **RolePermissionTreeWidget.cs**:修复 ASP0006 警告（20处,RenderTreeBuilder 使用整数字面量）
- ✅ **OrganizationTreeWidget.cs**:修复 ASP0006 警告（7处,RenderTreeBuilder 使用整数字面量）
- ✅ **最终编译结果**:Api 项目 0 错误 0 警告,App 项目 0 错误 0 警告 🎉

### 待后续迭代完成
- ⏸ 添加单元测试和集成测试(DataSet 服务、数据源处理器、Widget 运行态组件)
- ⏸ 初始化系统数据源类型种子数据(entity/api/sql/view 四种类型)
- ⏸ 实现 FormDesigner 属性面板中的数据源选择器 UI
- ⏸ PageLoader 扩展以支持 UsageType (Detail/Edit/List)
- ⏸ 创建系统实体的默认模板(组织/用户/角色/客户)
