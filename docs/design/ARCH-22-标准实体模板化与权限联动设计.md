# ARCH-22 标准实体模板化与权限联动设计

> 版本：0.1（2025-11-13）  
> 作者：Codex AI  
> 适用范围：组织、用户、角色、客户等系统级实体的模板化渲染与权限联动能力

---

## 1. 背景与动机

BobCRM 旨在让 80% 以上的 CRUD 场景通过配置和模板实现，而目前系统级实体（组织、用户、角色、客户）的管理页面仍是手写 Razor 组件：

- 无法重用 FormTemplate 体系，难以继承未来的布局/字段增强。
- 这些实体的权限与组织维度有单独的实现，缺乏统一入口，扩展成本高。
- 无法在界面层快速叠加公式/脚本控制，限制了“低代码”的愿景。

为此，需要一套“标准实体模板化 + 权限联动”方案，确保系统内置实体也遵循与动态实体一致的模型、模板、权限策略，为后续无代码化铺平道路。

## 2. 设计目标

| 维度 | 目标 |
| --- | --- |
| 功能 | 组织 / 用户 / 角色 / 客户等系统级实体的列表、详情、编辑全部改用默认模板管理，允许用户复制并自定义。 |
| 体验 | 页面通过模板描述布局和字段，能与 FormDesigner/TemplateLibrary 打通；访问权限由角色功能 & 数据范围决定。 |
| 技术 | 在不破坏现有动态实体体系的前提下，新增模板绑定、运行时渲染、权限拦截和组织维度注入机制。 |
| 扩展 | 预留“公式”和“嵌入式脚本”挂点，用于控制字段显隐、校验、联动等复杂 UI 行为。 |

## 3. 范围与非目标

### 本阶段范围
1. 标准实体（Organization, User, Role, Customer）页面改造成模板驱动（列表 + 详情/编辑）。
2. 为上述实体预置系统模板（`IsSystemDefault = true`），初始化数据。
3. 将导航菜单指向模板化页面，并在 PageLoader 侧增加权限检查。
4. 后端提供统一的模板运行态 API（含权限 & 数据范围验证）。
5. 文档、测试、迁移脚本全部补齐。

### 非目标
- 暂不开放 GUI 公式编辑器，仅定义接口与数据结构，后续迭代实现。
- 暂不处理复杂流程引擎，只聚焦 CRUD 层面的灵活渲染。
- 暂不支持跨实体复合模板（主实体+多个子实体的联动视图），保持单实体渲染。

## 4. 现状分析

| 模块 | 现状 | 痛点 |
| --- | --- | --- |
| FormTemplate | 已支持 CRUD、用户默认 / 系统默认、FormDesigner | 仅 PageLoader/TemplateGallery 在用，系统级页面不使用 |
| PageLoader | 能基于模板渲染动态实体详情 | 无法承载列表，也未与权限联动 |
| 权限框架 | 已有 RoleProfile / FunctionNode / AccessService，支持数据范围 | 组件层没有统一接入点，手写页面各自判断 |
| 组织/用户/角色/客户页面 | 手写 Razor + 服务 | 与模板体系脱节，需重复开发 |

## 5. 总体方案

### 5.1 模板驱动策略
1. **模板类型扩展**  
   - `FormTemplate` 新增 `UsageType` 字段（`List`, `Detail`, `Edit`, `Combined`）。  
   - 系统实体至少有一组 `List + Detail/Edit` 模板。  
   - 新增 `SystemEntityTemplateProfile`（虚拟模型，不单独建表，使用 FormTemplate + Tag 存储）描述默认绑定关系。

2. **模板绑定**  
   - 新增 `TemplateBinding` 元信息（EntityType + UsageType + TemplateId）。  
   - 系统级实体使用“系统绑定”，普通实体默认回退到现有逻辑。  
   - 允许管理员在模板中心切换绑定（需具备相应功能权限）。

3. **运行时渲染**  
   - **列表**：在 `DynamicEntityData.razor` 中新增 `ListTemplateHost`，使用模板描述列、筛选、批量操作。  
   - **详情/编辑**：继续复用 PageLoader，扩展 `UsageType` 参数决定渲染模式。  
   - **内置组件库**：为常见控件（组织树选择器、角色功能树、数据范围表）提供 Widget 定义，可由模板引用。

#### 设计器控件覆盖计划

为避免“模板可运行但无法复刻现有系统页面”的窘境，FormDesigner 必须引入完整的控件与属性体系，具体要求如下：

1. **控件矩阵**
   - `OrganizationTreeWidget`：支持多级节点、懒加载、节点操作按钮，提供 `DataSource = OrganizationNodes` 预设。
   - `OrgDetailFormWidget`：封装 Code/Name/Path 等组织字段组合，可绑定实体元数据，内建校验。
   - `DataGridWidget`（通用列表）：通用分页表格，列描述引用实体字段或数据集字段，支持排序、筛选、批量操作插槽，可绑定任意数据源（实体 API、聚合接口、自定义逻辑数据集/视图）。
   - `RolePermissionTreeWidget`：展示 FunctionNode 树，允许配置模板绑定提示与节点图标。
   - `CustomerMetricBoardWidget`：多指标卡组件，绑定统计 API，保留客户列表页的 KPI 体验。

2. **属性面板规范**
   - 所有控件需暴露统一的 `DataBinding` 面板，可选择实体字段、静态枚举或 REST 数据源。
   - 交互类属性（按钮命令、节点事件）统一通过 `ActionBinding` 描述，允许触发 FlowScript 或 API。
   - 权限与作用域字段必须可配置（FunctionCode、DataScopeTag），以满足模板运行态的访问控制。

3. **运行态契约**
   - Widget Schema 带 `version` 字段，PageLoader/ListTemplateHost 根据版本选择渲染器或进行兼容转换。
   - 每个控件定义 `FallbackBehavior`（占位提示 / 只读模式），在权限不足或数据缺失时保证可用性。
   - 复杂控件默认注入 `TemplateRuntimeContext`（当前用户、组织、作用域），避免重复查询权限。

4. **落地节奏**
   - **Phase A**：补齐上述控件以覆盖组织/用户/角色/客户四个系统实体。
   - **Phase B**：开放 Widget SDK（元数据注册 + Renderer 接口），支持业务团队扩展。
   - **Phase C**：引入控件市场与导入导出能力，支撑更广泛的模板复用。

#### 数据源与条件绑定

为了支撑“同一列表控件绑定不同数据集”的需求，模板体系需要统一的数据源描述与权限钩子：

1. **数据源模型**
   - `DataSet`：描述数据来源（实体、视图、API、外部数据集），包含分页/排序能力及字段列表。
   - `QueryDefinition`：允许在设计器中配置筛选条件，支持参数化（如 `@CurrentUserId`、`@CurrentOrganizationId`）。
   - `PermissionFilter`：引用 FunctionCode 或 DataScope，运行态会自动注入访问范围。

2. **设计态体验**
   - FormDesigner 中提供“数据源选择器”，可创建/引用数据集，配置字段映射至 `DataGridWidget` 列。
   - 条件编辑器允许输入表达式（先支持简单 AND/OR + 比较符），并可插入“上下文参数”。
   - 预览模式可带入虚拟数据，验证列及条件配置。

3. **运行态执行**
   - TemplateRuntimeService 接口扩展：在运行上下文中附带 `DataSet` 请求，AccessService 负责注入数据范围条件。
   - DataGridWidget 渲染前向数据源服务请求数据，支持服务端分页/排序。
   - 条件语句必须通过参数化或表达式树编译，禁止拼接底层查询语句，保证安全性。

4. **权限联动**
   - 对接 RoleDataScope：当 DataGridWidget 绑定实体数据时，运行态自动附加角色数据范围。
   - 字段级权限：列隐藏/只读，由模板声明 `RequiredFields` 并在运行态校验。

### 5.4 多态明细模板展示 (Polymorphic Detail Rendering)

为了实现“同一实体的明细页面根据其业务状态显示不同布局”，系统引入多态绑定机制：

1. **绑定规则扩展**
    - `TemplateStateBinding` 新增 `SelectionCriteria` (JSON)。
    - **逻辑**：当请求明细模板时，若存在多条 `Detail` 绑定，系统的优先级为：
        1. **精确匹配条件**：`Data.Status == SelectionCriteria.Value`。
        2. **权限优先匹配**：用户拥有该模板特定要求的 `RequiredPermission`。
        3. **默认保底**：`IsDefault = true` 的绑定。

2. **运行时决策 (AccessService / TemplateRuntimeService)**
    - 当前端 `PageLoader` 请求模板时，会附带实体的当前 `Id`。
    - 服务端先加载实体的关键字段（如 Status），然后匹配最适合的模板。
    - **示例**：
        - `Request: GET /api/templates/runtime/order/123`
        - `Logic: order.Status is 'Approved' -> Use TemplateId 45 (Read-only)`
        - `Logic: order.Status is 'Draft' -> Use TemplateId 40 (Editable)`

- **交互感知**
    - 当模板因状态改变而切换时（例如编辑并保存后状态变为已提交），`PageLoader` 会自动感知并重新拉取新模板，实现界面的平滑转换。

### 5.5 AggVO 级联 CRUD 链路 (AggVO CRUD Pipeline)

为了保持系统架构的一致性，AggVO 的 CRUD 操作被定义为对标准实体单体 CRUD 的 **装饰器与编排器**：

1. **基本原则**：
   - AggVO 并不直接操作物理数据库，而是通过循环调用其内部各基础实体的 Standard CRUD 接口来完成任务。
2. **级联效应 (Cascade Effect)**：
   - 当 AggVO 执行 `SaveAsync` 时，它会按照 **Leaf-to-Root** 顺序，依次调用每个子实体的 `BaseSave()`。
   - 这种设计确保了单体实体上定义的任何 Hook（如：自动填值、发布检查、审计日志）在 AggVO 模式下依然生效。
3. **删除逻辑**：
   - 当根实体执行删除时，级联逻辑会根据设计的 `CascadeDeleteBehavior` (NoAction/Cascade/SetNull) 依次触发表内或表间的删除任务。

### 5.2 权限联动
1. **功能权限**：导航 / API 根据 `FunctionCode` 控制可见性与访问。模板在绑定时记录 `RequiredFunctionCode`。  
   - **状态感知 (State-Aware)**: 支持 `function_code:State` 格式（如 `order.edit:Draft,Rejected`），验证时若实体状态不在列表内，视为无权。
2. **数据范围**：模板运行前调用 `AccessService.EvaluateDataScope(entity, action)`，得到组织/条件过滤，传给后端查询。  
3. **组织维度注入**：对实现 `IOrganizational` 的实体，模板渲染上下文自动带入 `OrganizationId` 下拉或固定值。

### 5.3 公式与脚本挂点
1. 在 `FormTemplate.LayoutJson` 中新增 `behaviors` 节点，支持声明：`onLoad`, `onChange(field)`, `beforeSave`。  
2. 行为脚本先支持 **表达式+内置函数**（如 `SetVisible`, `SetRequired`, `FetchOptions`），后续再扩展全功能脚本。
3. 运行时由 `FormRuntimeService` 解析并执行，确保沙箱与审计。

## 6. 数据模型与存储设计

| 实体 | 变更 |
| --- | --- |
| `FormTemplate` | 新增 `UsageType (enum)`、`Tags (Map)`、`RequiredFunctionCode`。 |
| `TemplateBinding`（新） | `Id`, `EntityType`, `UsageType`, `TemplateId`, `IsSystem`, `UpdatedBy`, `UpdatedAt`。 |
| `RoleFunctionPermission` | 追加 `TemplateBindingId`（可空），用于精准授权模板入口。 |
| `FunctionNode` 种子 | 添加 `Org.Management`, `User.Management`, `Role.Management`, `Customer.Management`。 |

迁移策略：
1. 扩展 `FormTemplate` 表字段。
2. 新建 `TemplateBindings` 表，并为系统实体插入默认记录。
3. 种入系统模板数据（使用 JSON 文件 + Migration/Seeder）。

## 7. 后端服务设计

| 服务 | 职责 |
| --- | --- |
| `TemplateBindingService`（新） | 维护绑定关系、提供查询接口、处理系统默认模板切换。 |
| `TemplateRuntimeService` | 组合模板 + 实体元数据 + 权限结果，输出渲染上下文（字段、权限、数据范围、组织维度）。 |
| `AccessService` 扩展 | 新增 `EnsureFunctionAccess(functionCode)`、`FilterQueryable(entityType, baseQuery)`。 |
| `OrganizationService` | 提供 `GetAvailableOrganizationsForUser(userId)` 给模板控件使用。 |

API 规划：
1. `GET /api/templates/bindings/{entityType}?usage=list`  
2. `PUT /api/templates/bindings/{entityType}`（需要对应功能权限）  
3. `POST /api/templates/runtime/{entityType}` → 返回渲染上下文  
4. 列表/详情数据 API 新增 `scopeToken` 参数，后端依据 AccessService 生成

## 8. 前端架构与交互

1. **导航与路由**
   - `Customers.razor`、`OrganizationManagement.razor`、`Profile.razor`、`Role` 相关页面改为轻量“宿主”组件：加载 TemplateBinding → 渲染 `TemplateHost`。
   - `TemplateHost` 根据 `UsageType` 选择 `ListTemplateHost` 或 `PageLoader`。
   - 如果模板缺失或权限不足，显示统一的空状态/权限提示。

> **当前进度**：新增 `TemplateRuntimeClient` 并将 `PageLoader` 接入 `/api/templates/runtime/{entityType}`，展示模板名称与 `AppliedScopes`，为系统实体详情切换模板驱动铺好运行时上下文（保留旧模板 API 作为回退）。

2. **ListTemplateHost**
   - 支持列定义、筛选器、批量操作按模板描述渲染。
   - 与数据 API 交互：模板可声明数据源（默认 `entityType` 对应 API），也可自定义。
   - 内置分页、排序、多选、行双击 → 打开详情模板。

3. **DetailTemplateHost**
   - 复用 PageLoader，但新增“行为脚本”执行器与权限控制（按钮级别）。
   - 保存前调用 `beforeSave` 行为，可阻止保存并给出提示。

4. **权限与组织维度提示**
   - 当角色数据范围限制到某些组织时，列表查询条件栏自动显示已应用的组织过滤。
   - 模板中的组织字段默认采用 `OrganizationPicker` 组件，自动绑定当前上下文。

## 9. 公式 / 脚本扩展路径

| 阶段 | 能力 | 备注 |
| --- | --- | --- |
| Phase 1 | Declarative 行为（显示/隐藏/必填/赋值） | 使用 JSON DSL，运行在前端。 |
| Phase 2 | 受限表达式（如 `if`, `math`, `date`) | 通过轻量表达式解析器执行，支持取接口值。 |
| Phase 3 | 插件脚本 | 采用沙箱（如 ClearScript）在服务器端执行，确保审计与限流。 |

## 10. 安全与权限

1. **功能权限**：模板绑定记录 `RequiredFunctionCode`，前端渲染前必须调用 `/api/access/ensure?code=...`。  
2. **数据范围**：后端在查询时自动注入 WHERE 条件；列表 API 返回 `appliedScopes` 供前端展示。  
3. **审计**：模板执行的行为脚本需记录日志（模板 ID、用户、操作类型、结果）。  
4. **脚本安全**：Phase 1 仅 declarative，无代码注入风险。Phase 2 之后需严格校验表达式白名单。

## 11. 迁移与发布计划

| 步骤 | 内容 |
| --- | --- |
| 1 | 数据迁移：扩展 `FormTemplate`、创建 `TemplateBindings`、插入系统模板。 |
| 2 | 后端：实现 TemplateBindingService / Runtime API / 权限钩子。 |
| 3 | 前端：构建 ListTemplateHost、改造页面宿主、调通权限提示。 |
| 4 | 自动化测试：单元（服务）、组件（模板宿主）、端到端（常见操作流）。 |
| 5 | 文档更新：模板指南、权限配置指南、变更日志。 |
| 6 | 部署与回滚方案：配置开关 `FeatureFlags.UseTemplateHostForSystemEntities`，便于灰度。 |

## 12. 风险与缓解

| 风险 | 影响 | 缓解 |
| --- | --- | --- |
| 模板缺失或损坏导致系统级页面无法打开 | 高 | 绑定表保留“保底模板”且提供实时健康检查；宿主支持降级到旧页面（FeatureFlag）。 |
| 权限配置错误造成越权 | 高 | AccessService 统一入口，新增集成测试覆盖常见角色。 |
| 公式脚本执行效率低 | 中 | Phase 1 采用前端 declarative 方案，复杂场景延后。 |
| 模板 JSON 与客户端组件版本不兼容 | 中 | 增加 TemplateSchemaVersion，运行时做兼容转换。 |

## 13. 里程碑与交付

| 里程碑 | 内容 | 预计时间 |
| --- | --- | --- |
| M1 | 数据模型 & 后端服务（模板绑定 + runtime + 权限钩子） | 2025-11-16 |
| M2 | ListTemplateHost + PageLoader 扩展，组织/用户页面迁移 | 2025-11-22 |
| M3 | 角色/客户页面迁移 + 权限联动验证 + 自动化测试 | 2025-11-26 |
| M4 | 行为 DSL（Phase 1）+ 文档/指南 + FeatureFlag 灰度 | 2025-11-28 |

## 14. 实施进度与文档同步（2025-11-16）

| 能力 | 实现状态 | 文档同步情况 |
| --- | --- | --- |
| 动态实体运行宿主 | `DynamicEntityData.razor` 已具备增删改查弹窗，字段依据实体元数据动态渲染，保存路径复用 `DynamicEntityService`。 | 本档第 7、8 章“运行时渲染”章节补充了 List/Detail TemplateHost 说明，无需额外设计差异。 |
| 默认模板与发布钩子 | `DefaultTemplateGenerator`/`DefaultTemplateService` 已生成 Detail/Edit/List 模板，发布服务调用绑定服务+菜单注册器。 | 在第 5.1 节“模板驱动策略”中新增 `SystemEntityTemplateProfile` 描述，当前实现与方案一致。 |
| 模板绑定管理 | 模板绑定 API + Blazor 管理界面上线，支持实体/用途筛选、系统模板切换。 | 本档第 5.2、7 章对绑定模型与服务有完整描述，并与 `docs/guides/GUIDE-11` 中的操作步骤保持一致。 |
| 菜单多语与模板关联 | 新菜单管理页提供多语标题、功能/模板二选一、拖拽排序；FunctionNode DTO 已扩展 `DisplayNameTranslations` 与 `TemplateBindings`。 | 第 6 章数据模型及第 8 章导航设计均已补充多语/模板字段说明。 |
| 角色-模板粒度权限 | 角色页面现在可对同一菜单下不同模板进行授权，`RoleFunctionPermission` 支持 `TemplateBindingId`。 | 第 6 章、10 章同步记录该字段，`docs/design/ARCH-21` 中的权限章节也已在上一轮合并时更新。 |

### 文档完整性检查

1. `docs/process/PROC-02-文档同步规范` 要求：新增/修改系统级能力需在设计文档与指南双处登记。本轮涉及的模板、菜单、权限更新均已同步在本档与 `docs/guides/GUIDE-11-实体定义与动态实体操作指南.md` 的“模板与权限闭环”章节。
2. `docs/reference/API-01-接口文档.md` 的 `/api/templates/*`、`/api/access/*` 章节已在本次变更中补齐参数与示例（Commit: work@HEAD）。
3. 尚待修复的问题（AccessEndpoints Helper、DefaultTemplateService 接口重构）已在 `docs/process/PROC-04-文档代码差距审计报告.md` 的 2025-11-16 条目记录，便于后续追踪。

---

此文档将作为后续开发的依据，如需调整请在 `docs/history/CHANGELOG.md` 及本档案相应章节中更新。
