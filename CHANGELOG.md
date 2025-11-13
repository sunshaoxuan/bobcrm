# Changelog

本文档记录 BobCRM 项目的所有重要变更。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

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

1. **FormTemplate 域模型** (`src/BobCrm.Api/Domain/Models/FormTemplate.cs`)：
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
  - EntityType（主键）：改为存储类全名（如 `BobCrm.Api.Domain.Customer`）
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
**最后更新**：2025-11-13

