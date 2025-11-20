# BobCRM 文档与代码差距审计报告

## 2025-11-16（晚班）- 自定义实体闭环复核

- **审计范围**：围绕“实体发布即 CRUD、模板绑定、菜单连通、角色模板授权”四大目标的两轮开发成果。
- **审计人**：Codex（DocOps）
- **检查目标**：确认代码已满足 ARCH-22 的关键交付，并识别仍影响交付验收的差距。

### 功能完成度对照

| 目标 | 设计要求摘录 | 代码落地现状 | 结论 |
| --- | --- | --- | --- |
| 发布即 CRUD | ARCH-22 §5~§6 要求实体发布后自动生成模板、绑定并挂到菜单。 | `EntityPublishingService` 通过 `_defaultTemplateService.EnsureTemplatesAsync`、`_templateBindingService.UpsertBindingAsync` 以及 `_accessService.EnsureEntityMenuAsync` 串起 DDL→模板→菜单的链路；`DynamicEntityData.razor` 提供创建/编辑弹窗与字段校验，前端也能直接操作 CRUD。 | ✅ 功能完成 |
| 模板可视化与绑定 | 设计稿要求管理员能切换实体/用途模板，并回收默认模板。 | `TemplateBindings.razor` 支持实体/用途筛选、系统/个人切换、权限校验；`TemplateBindingService`/`TemplateEndpoints` 暴露查询、保存接口与运行态上下文。 | ✅ 功能完成 |
| 菜单多语 & 模板联动 | README/ARCH-22 要求菜单支持多语标题、模板/路由二选一。 | `MenuManagement.razor` 现有多语输入、导航类型（路由/模板）切换与拖拽排序；Access API 统一通过 `FunctionTreeBuilder` 输出多语与模板选项，供管理与授权两侧共用。 | ✅ 功能完成 |
| 角色模板授权闭环 | ARCH-22 §6 要求角色分配能精确到模板绑定。 | `Roles.razor` 渲染 `FunctionMenuNode.TemplateBindings` 并提交 `FunctionPermissionSelectionDto`；`AccessEndpoints` 将模板绑定写入 `RoleFunctionPermission`，完成角色→菜单→模板的闭环。 | ✅ 功能完成 |

### 遗留问题与修复建议

1. **功能树版本接口缺失**：前端 `RoleService` 依赖 `/api/access/functions/version` 来决定是否刷新缓存，但 `AccessEndpoints` 尚未提供该路由，导致每次进入角色页都要完整加载功能树。→ 建议在 AccessEndpoints 中实现版本查询端点（例如返回 `FunctionNodes` 最新 `UpdatedAt` Hash）并与 `FunctionTreeBuilder` 保持一致。
2. **模板绑定默认标识未写回功能节点**：`FunctionTreeBuilder` 期待通过 `FunctionNode.TemplateBindingId` 判断哪一个绑定是当前默认模板，但 `AccessService.EnsureEntityMenuAsync` 以及菜单 CRUD 过程中都没有为节点赋值，导致前端无法高亮默认模板并可能误选。→ 建议在绑定成功或菜单保存时同步 `FunctionNode.TemplateBindingId` 字段。

### 进度偏差统计

| 维度 | 设计交付（ARCH-22） | 实际完成 | 偏差 |
| --- | --- | --- | --- |
| 四大目标（发布即 CRUD / 模板绑定 / 菜单多语 / 角色授权） | 4 项 | 4 项 | 0%（全部到位） |
| 技术债（功能树版本口 / 模板默认标识） | 0 项 | 2 项 | +2（需补齐） |

> 以上差距已回填至 backlog，待 Access API 与菜单绑定模型补完后再关闭本次审计。

## 2025-11-16 - 模板与权限闭环阶段审计

- **审计范围**：最近两轮围绕“实体发布即 CRUD、模板-菜单-权限闭环”交付的功能。
- **审计人**：Codex（DocOps）
- **检查目标**：确认相关设计文档、指南、接口参考与历史记录均已同步；识别仍未完成的实现项，避免文档领先代码。

### 关键结论

| 能力 | 文档状态 | 问题/后续动作 |
| --- | --- | --- |
| 动态实体数据页 CRUD | `docs/guides/FRONT-01` 与 `docs/design/ARCH-22` 已补充 TemplateHost 行为；截图与步骤待在 UI 收敛后补充。 | 无阻断。 |
| 默认模板生成/绑定 | `docs/design/ARCH-22` 第 14 章新增进度表，描述 DefaultTemplateGenerator、TemplateBindingService 责任划分。 | 需在代码层完成 `DefaultTemplateService` 接口收敛。 |
| 菜单多语+模板关联 | `docs/design/ARCH-23` 和本档均记录 FunctionNode DTO 扩展，但 `/api/access/functions` helper 尚未落地，造成文档领先。 | 将在 `AccessEndpoints` 补齐 `LoadFunctionNameTranslationsAsync` 等 helper 前禁止在 README 勾选此项。 |
| 角色模板粒度授权 | `docs/design/ARCH-21`、`docs/reference/API-01` 更新了 `TemplateBindingId` 字段描述；`Roles.razor` 注入冲突需修复后才能宣称 GA。 | 已在“遗留问题”列表登记。 |

### 遗留问题登记（同步至 backlog）

1. `EntityPublishingService` 构造函数缺少 `DefaultTemplateService` 参数 → 阻断实体发布。
2. `AccessEndpoints` 内引用的 `LoadFunctionNameTranslationsAsync`/`LoadTemplateBindingsAsync` 未实现 → API 编译失败。
3. `FunctionMenuNode` DTO 重复声明 `DisplayName`，`Roles.razor` 注入冲突 → 需调整模型与前端调用。

> 本次审计结果已同步至 `docs/design/ARCH-22-标准实体模板化与权限联动设计.md` 第 14 章及 `CHANGELOG.md [未发布]`，确保后续评审可追溯。

---

**审计日期**: 2025-11-06
**审计人**: Claude
**审计范围**: 全部文档声称功能 vs 实际代码实现

---

## 执行摘要

本次审计共检查了 **15 个关键功能领域**，发现：

- ✅ **9 个功能完全实现且文档准确** (60%)
- ⚠️ **4 个功能部分实现或存在差距** (27%)
- ❌ **2 个功能文档有误或夸大** (13%)

### 🔴 严重问题（需立即修正）

1. **I18n客户端缓存机制** - 文档详细描述但完全未实现
2. **标签驱动布局UI** - 后端完整但前端UI缺失

### 🟡 中等问题（建议修正）

3. **Widget数量声称** - 声称19个但实际16个
4. **API字段命名不一致** - 文档说primaryColor，代码用udfColor

### 🟢 轻微问题（透明度改进）

5. **测试数量披露** - 104个测试包含3个跳过的测试

---

## 详细审计结果

### 1. ✅ Profile.razor 个人中心页面 - **完全实现**

**文档声称** (README.md:55-60):
- 用户信息展示
- 密码修改功能
- 头像显示
- 文件大小：318行

**实际实现**:
- ✅ 文件位置：`src/BobCrm.App/Components/Pages/Profile.razor`
- ✅ 用户信息：用户名、邮箱、角色（带彩色徽章）
- ✅ 密码修改：完整表单，带验证（最小6字符，确认匹配）
- ✅ 头像显示：96x96圆形头像，渐变背景，占位图标
- ✅ 文件大小：**精确318行**

**结论**: 文档与代码100%一致 ✅

---

### 2. ⚠️ UserPreferences 用户偏好 - **实现但有命名差异**

**文档声称** (docs/PROD-01-客户信息管理系统设计文档.md:385-389):
```json
Body: { "theme": "...", "primaryColor": "...", "language": "..." }
```

**实际实现**:
- ✅ UserPreferences 模型存在 (`src/BobCrm.Api/Base/Models/UserPreferences.cs`)
- ✅ GET /api/user/preferences 端点实现
- ✅ PUT /api/user/preferences 端点实现
- ⚠️ **API使用 `udfColor` 而非 `primaryColor`**

**实际API契约**:
```json
Body: { "theme": "...", "udfColor": "...", "language": "..." }
```

**问题分析**:
- 数据库列名：`PrimaryColor`
- DTO字段名：`udfColor` (user-defined color)
- 代码注释说明了这是有意的映射
- 但文档未同步更新

**影响**: 低 - 功能完整，只是命名不一致
**建议**: 更新文档使用 `udfColor` 或在代码中改为 `primaryColor` 保持一致

---

### 3. ✅ 数据多语支持 - **完全实现**

**文档声称** (docs/I18N-01-多语机制设计文档.md:64-260):
- CustomerLocalization 表
- ILocalizationData 接口
- [Localizable] 属性标记
- 查询中使用本地化名称

**实际实现**:
- ✅ `CustomerLocalization.cs` 实体（复合主键：CustomerId + Language）
- ✅ `ILocalizationData.cs` 接口
- ✅ `LocalizableAttribute.cs` 属性
- ✅ `CustomerQueries.cs` 中实现语言感知查询
- ✅ 数据库配置和迁移
- ✅ 完整文档说明扩展方法

**结论**: 架构设计优秀，实现完整 ✅

---

### 4. ❌ Widget 数量声称 - **文档错误**

**文档声称** (README.md:298):
> Widget模型定义（**19种控件**，含Checkbox和Radio）

**实际实现**:
- WidgetRegistry.cs 注册了 **16个widget**
- 所有16个都有对应的C#类文件
- 设计文档也只列出16个（10个基础 + 5个布局 + 1个内部）

**Widget清单**:
```
基础组件 (10): TextBox, Number, Select, Checkbox, Radio,
               Textarea, Calendar, Listbox, Button, Label
布局组件 (5):  Section, Panel, Grid, Frame, TabContainer
内部组件 (1):  Tab
总计: 16个
```

**问题**: README中的"19种控件"是**错误的**，应该是**16种**

**影响**: 中等 - 误导用户对系统能力的理解
**建议**: 将README第298行改为"Widget模型定义（**16种控件**，含Checkbox和Radio）"

---

### 5. ✅ Single-Flight 刷新机制 - **完全实现**

**文档声称** (docs/PROD-01-客户信息管理系统设计文档.md:686-691):
- 使用 SemaphoreSlim 单飞刷新
- 共享任务机制
- 竞态重试逻辑
- OnUnauthorized 全局事件
- 顶层订阅统一跳转

**实际实现** (src/BobCrm.App/Services/AuthService.cs):
- ✅ `SemaphoreSlim _refreshGate = new(1, 1)` (Line 13)
- ✅ `Task<bool>? _refreshTask` 共享任务 (Line 14)
- ✅ 竞态恢复：200ms延迟 + 重读token (Lines 117-129)
- ✅ `OnUnauthorized` 事件定义 (Lines 17-20)
- ✅ MainLayout.razor 全局订阅 (Lines 32-46)
- ✅ 双重检查模式防止重复刷新 (Lines 150-162)

**结论**: 认证机制实现精良，文档准确 ✅

---

### 6. ✅ EntitySelector 通用组件 - **完全实现**

**文档声称** (docs/PROD-01-客户信息管理系统设计文档.md:236-241, v0.5.3新增):
- 输入框 + 放大镜图标
- Modal选择界面
- 泛型支持
- 懒加载
- 搜索过滤
- 自定义渲染

**实际实现**:
- ✅ `EntitySelector.razor` (237行)
- ✅ `ISelectableEntity.cs` 接口
- ✅ 放大镜图标 (IconType.Outline.Search)
- ✅ Modal组件
- ✅ 泛型约束 `where TEntity : ISelectableEntity`
- ✅ `OpenSelector()` 懒加载
- ✅ `FilterData()` 搜索方法
- ✅ 自定义RenderFragment支持

**结论**: 完全按设计实现 ✅

---

### 7. 🔴 I18n ETag/缓存机制 - **服务端完整，客户端缺失**

**文档声称** (docs/I18N-01-多语机制设计文档.md:33-51):

**服务端要求**:
- ✅ GET /api/i18n/version 返回版本
- ✅ GET /api/i18n/{lang} 设置ETag头
- ✅ 支持If-None-Match检查返回304
- ✅ ILocalization.InvalidateCache()
- ✅ ILocalization.GetCacheVersion()

**客户端要求**:
- ❌ localStorage缓存 `{ lang, version, dict }`
- ❌ 携带If-None-Match请求头
- ❌ 版本检查跳过拉取
- ❌ 304响应处理

**实际实现差距**:

**服务端 (I18nEndpoints.cs)** - ✅ 完全实现:
```csharp
// Line 60-90: ETag generation and 304 handling
var version = loc.GetCacheVersion().ToString();
var etag = $"\"{version}_{lang}\"";
if (http.Request.Headers.TryGetValue("If-None-Match", out var clientEtag) && clientEtag == etag)
{
    return Results.StatusCode(304); // Not Modified
}
http.Response.Headers["ETag"] = etag;
http.Response.Headers["Cache-Control"] = "public, max-age=1800";
```

**客户端 (I18nService.cs)** - ❌ 完全缺失:
```csharp
// Lines 26-41: 简单的GET请求，无缓存检查
public async Task LoadAsync(string lang, CancellationToken ct = default)
{
    var http = await _auth.CreateClientWithLangAsync();
    var resp = await http.GetAsync($"/api/i18n/{lang}", ct);  // 无If-None-Match!
    // ... 无localStorage缓存逻辑
    // ... 无版本检查
    // ... 无304处理
}
```

**影响**: 高 - 每次都全量拉取翻译词典，浪费带宽
**问题严重性**: 🔴 严重 - 文档详细描述了完整机制但客户端完全未实现
**建议**:
1. 实现客户端localStorage缓存
2. 添加If-None-Match请求头
3. 处理304响应
4. 或者从文档中删除客户端缓存描述

---

### 8. ⚠️ 标签驱动快速布局 - **后端完整，前端UI缺失**

**文档声称** (docs/PROD-01-客户信息管理系统设计文档.md:426-441):

**后端**:
- ✅ GET /api/fields/tags - 标签列表统计
- ✅ POST /api/layout/generate - 生成布局
- ✅ flow模式支持
- ✅ free模式支持
- ✅ 完整测试覆盖

**前端交互**:
> "设计态提供'标签候选区'，支持拖入画布或点击应用"

**实际实现**:

**后端** - ✅ 完全实现:
- LayoutEndpoints.cs (Lines 38-69): GET /api/fields/tags
- LayoutEndpoints.cs (Lines 439-543): POST /api/layout/generate
- 支持flow和free两种模式
- LayoutTests.cs 有完整测试

**前端** - ❌ UI缺失:
- ✅ FieldService.cs 有 `GetTagsAsync()` 和 `GenerateLayoutAsync()` 方法
- ❌ FormDesigner.razor (452行) 中**没有标签选择UI**
- ❌ 没有"标签候选区"
- ❌ 没有组件调用这些service方法

**影响**: 中等 - 功能已开发但无法使用
**问题**: 🟡 后端完成，前端TODO
**建议**:
1. 在FormDesigner中添加标签选择面板
2. 或从文档中删除"前端交互（现状）"描述，改为"前端计划"

---

### 9. ✅ 数据库配置管理 - **文档诚实标注未实现**

**文档声称** (docs/API-01-接口文档.md:141-143):
```markdown
- 数据库配置（仅管理员，后续实现）  👈 明确标注
  - GET `/api/admin/dbconfig`
  - POST `/api/admin/dbconfig`
```

**实际实现**:
- ❌ 端点不存在
- ❌ UI页面不存在

**结论**: 文档**诚实地**标注为"后续实现"，这是正确的做法 ✅

---

### 10. ⚠️ 测试覆盖率声称 - **技术准确但有误导**

**文档声称** (README.md:10):
> 测试覆盖率大幅提升 - 从67个增至104个测试（**+55%**），核心业务逻辑100%覆盖

**实际实现**:
- ✅ 确实有104个test方法定义
- ⚠️ 其中3个测试被Skip（跳过）
  - Database_Recreate_Works - 会破坏其他测试
  - Admin_Password_Change - 会破坏其他测试
  - DatabaseInitializer_RecreateAsync_Works - 重复测试

**实际运行**: 101个活跃测试 + 3个跳过测试 = 104个总测试

**问题分析**:
- 数学上准确：104个测试定义存在
- 但未披露3个是跳过的
- 行业惯例：skip测试通常不计入覆盖率

**影响**: 轻微 - 透明度问题而非虚假
**建议**: 更透明的表述："101个活跃测试 + 3个跳过测试" 或简单说"101个测试"

---

## 其他已验证功能（完全实现）

### 11. ✅ AppHeader用户区域改进
- 头像 + 用户名（可点击）
- 动态登录/退出按钮
- 响应式悬停效果

### 12. ✅ FormDesigner通用表单设计器
- 独立页面，不依赖实体
- 从FieldDefinitions加载字段
- 路由：/designer 或 /designer/{templateId}
- 452行代码

### 13. ✅ PageLoader通用页面加载器
- 动态加载任何实体类型
- 路由：/{entityType}/{id}
- Browse/Edit模式支持
- 430行代码

### 14. ✅ 架构重构成果
- CustomerDetail.razor: 2524行 → 删除（-100%）
- 总代码量：4118行 → 1827行（-55.6%）
- 所有64个测试通过

### 15. ✅ 多语言机制
- LocalizationResource表
- LocalizationLanguage表
- 服务端ETag支持
- 完整的回退机制

---

## 问题汇总表

| # | 功能 | 文档 | 代码 | 严重性 | 状态 |
|---|------|------|------|--------|------|
| 1 | I18n客户端缓存 | 详细描述 | 未实现 | 🔴 高 | 需修正 |
| 2 | 标签布局UI | 声称有UI | 无UI | 🟡 中 | 需实现 |
| 3 | Widget数量 | 19个 | 16个 | 🟡 中 | 需更正 |
| 4 | API字段名 | primaryColor | udfColor | 🟢 低 | 建议统一 |
| 5 | 测试披露 | 104个 | 101+3skip | 🟢 低 | 建议澄清 |

---

## 修正建议

### 立即修正（P0）

**1. I18n客户端缓存 - 二选一**:

**选项A**: 实现客户端缓存（推荐）
```csharp
// I18nService.cs 中添加
private async Task<bool> CheckCacheAsync(string lang)
{
    var cachedVersion = await _js.InvokeAsync<string?>("localStorage.getItem", $"i18n_{lang}_version");
    if (cachedVersion == null) return false;

    var serverVersion = await GetServerVersionAsync();
    return cachedVersion == serverVersion;
}
```

**选项B**: 删除文档中的客户端缓存描述
- 从 `docs/I18N-01-多语机制设计文档.md` 删除客户端缓存章节
- 仅保留服务端ETag实现说明

**2. 标签布局UI - 二选一**:

**选项A**: 实现UI（推荐）
- 在FormDesigner.razor添加标签选择面板
- 调用现有的FieldService.GetTagsAsync()
- 调用现有的FieldService.GenerateLayoutAsync()

**选项B**: 更新文档措辞
```markdown
- 前端交互（计划中）：设计态将提供"标签候选区"...  👈 改为计划中
```

### 短期修正（P1）

**3. 更新README Widget数量**
```markdown
- [x] Widget模型定义（16种控件，含Checkbox和Radio）  👈 19→16
```

**4. 统一API字段命名 - 二选一**:

**选项A**: 更新文档
```markdown
Body: { theme?, udfColor?, language? }  👈 primaryColor→udfColor
```

**选项B**: 更新代码
```csharp
public record UserPreferencesDto(string? theme, string? language, string? primaryColor);
```

### 透明度改进（P2）

**5. 测试数量披露**
```markdown
- ✅ 测试覆盖率大幅提升 - 101个活跃测试 + 3个跳过测试，核心业务逻辑100%覆盖
```

---

## 积极发现

### 文档质量亮点 ✨

1. **数据库配置** - 诚实标注"后续实现"，而非虚假声称
2. **架构重构文档** - 详细记录问题、方案、成果，非常专业
3. **多语机制文档** - 架构清晰，扩展步骤明确
4. **接口文档** - 契约定义规范，错误响应统一

### 代码质量亮点 ✨

1. **Single-Flight认证** - 实现精良，考虑周全（竞态重试、版本控制）
2. **数据多语架构** - 接口驱动，易扩展
3. **Widget系统** - 虽然数量有误，但实现的16个都很完整
4. **测试覆盖** - 101个活跃测试，覆盖面广

---

## 总体评价

### 评分卡

| 维度 | 评分 | 说明 |
|------|------|------|
| **功能完整度** | 8.5/10 | 大部分声称功能已实现 |
| **文档准确性** | 7.0/10 | 多数准确，少数夸大或滞后 |
| **代码质量** | 9.0/10 | 架构优秀，实现规范 |
| **测试覆盖** | 8.5/10 | 101个测试，覆盖核心逻辑 |
| **诚实度** | 8.0/10 | 大部分诚实，少数夸大 |

### 🎯 核心结论

**这是一个高质量的项目**，文档与代码的差距主要集中在：

1. **少数功能过度承诺** - I18n客户端缓存、标签UI
2. **个别数字夸大** - Widget数量
3. **命名不一致** - API字段名

**但整体上**:
- ✅ 核心架构扎实（重构减少55%代码）
- ✅ 测试覆盖充分（101个活跃测试）
- ✅ 大部分文档准确（60%完全一致）
- ✅ 关键功能实现完整（认证、多语、Widget系统）

**不是吹牛项目**，是有诚意的实现，只是部分细节需要对齐。

---

## 行动计划

### 本周内（必须）
- [ ] 修正README Widget数量（19→16）
- [ ] I18n缓存：实现客户端或删除文档
- [ ] 标签UI：实现或改文档为"计划中"

### 本月内（建议）
- [ ] 统一API字段命名（primaryColor vs udfColor）
- [ ] 更新测试数量披露（101活跃+3跳过）
- [ ] 审查其他文档与代码一致性

### 持续改进（可选）
- [ ] 建立CI检查文档与代码一致性
- [ ] 定期审计新功能的文档准确性
- [ ] 代码注释中标注对应文档章节

---

**审计完成时间**: 2025-11-06
**下次建议审计**: 每个大版本发布前

