# ARCH-24: 紧凑型顶部菜单导航 - 实施计划

> **创建日期**: 2025-11-13
> **相关文档**: [ARCH-24-紧凑型顶部菜单导航设计](./ARCH-24-紧凑型顶部菜单导航设计.md)

---

## 实施概览

本计划分为 **4 个阶段**，共 **20+ 个任务**，预计总工作量约 **3-5 天**。

---

## 阶段一：基础结构搭建（必需）

### 任务 1.1：扩展 LayoutState 服务
**目标**：添加菜单面板状态管理

**文件**: `src/BobCrm.App/Services/LayoutState.cs`

**新增属性**：
```csharp
public bool IsMenuPanelOpen { get; private set; }
public bool IsDomainSelectorOpen { get; private set; }
```

**新增方法**：
```csharp
public void ToggleMenuPanel()
public void OpenMenuPanel()
public void CloseMenuPanel()
public void ToggleDomainSelector()
public void CloseDomainSelector()
```

**工作量**: 30 分钟

---

### 任务 1.2：创建 DomainSelector 组件
**目标**：实现领域切换器按钮和领域列表 Popover

**文件**: `src/BobCrm.App/Components/Shared/DomainSelector.razor`

**功能要求**：
- 显示当前领域图标和名称
- 点击展开领域列表 Popover
- 领域列表支持点击切换
- 当前领域高亮显示
- 支持键盘导航（↑↓ 箭头，Enter 确认，ESC 关闭）

**Props**：
```csharp
[Parameter] public List<FunctionMenuNode> Domains { get; set; }
[Parameter] public FunctionMenuNode? ActiveDomain { get; set; }
[Parameter] public EventCallback<FunctionMenuNode> OnDomainChanged { get; set; }
```

**工作量**: 2 小时

---

### 任务 1.2.1：领域档案数据来源（新增）
**目标**：前端领域列表改为从后端 EntityDomain 档案表读取，支持多语名称与自定义排序。

**实现要点**：
- 新增 `EntityDomain` 实体、`/api/entity-domains` 端点以及 `EntityDomainService`（App/Api 双端）。
- 数据由数据库档案维护，默认提供 CRM/SCM/FA/HR/MFM/System/Custom，可随时在表中扩展。
- `EntityDefinitionEdit` 页面的领域下拉、`DomainSelector` 等组件统一走 API 数据源。

**文档更新**：本任务已落实在本计划文档以及变更日志，禁止再在前端写死领域列表。

---

### 任务 1.3：创建 MenuPanel 组件框架
**目标**：创建菜单面板的基本容器结构

**文件**: `src/BobCrm.App/Components/Shared/MenuPanel.razor`

**功能要求**：
- 遮罩层（点击关闭菜单）
- 面板容器（从顶部展开）
- 面板头部（领域名称、关闭按钮）
- 面板主体（占位，稍后填充内容）
- 展开/关闭动画

**Props**：
```csharp
[Parameter] public bool IsOpen { get; set; }
[Parameter] public FunctionMenuNode? Domain { get; set; }
[Parameter] public EventCallback OnClose { get; set; }
[Parameter] public EventCallback OnBackToDomains { get; set; }
```

**工作量**: 1.5 小时

---

### 任务 1.4：更新 AppHeader 集成新组件
**目标**：在 AppHeader 中集成 DomainSelector

**文件**: `src/BobCrm.App/Components/Shared/AppHeader.razor`

**改动**：
- 移除或注释旧的导航栏切换按钮
- 在 Logo 右侧添加 `<DomainSelector>` 组件
- 添加菜单图标按钮（可选，用于打开 MenuPanel）
- 传递领域数据和事件处理

**工作量**: 1 小时

---

### 任务 1.5：调整 MainLayout 移除左侧导航
**目标**：移除 Rail 和 Sider，让内容区域占满全宽

**文件**: `src/BobCrm.App/Components/Layout/MainLayout.razor`

**改动**：
- 注释或移除 `<nav class="chrome-rail">` 整个区块
- 注释或移除 `<section class="domain-menu-panel">` 整个区块
- 调整 `.chrome-app` 布局，移除 `sider-collapsed` 等类
- 确保 `.layout-main` 宽度为 100%

**工作量**: 1 小时

---

### 阶段一检查点
- [ ] LayoutState 新增方法可用
- [ ] DomainSelector 显示当前领域并可点击展开列表
- [ ] 切换领域后正确触发回调
- [ ] MenuPanel 可以打开和关闭（尽管内容为空）
- [ ] 左侧导航已移除，页面全宽显示

**预计总时间**: 6 小时

---

## 附录：I18n 端点多语化（新增说明）

- `I18nEndpoints` 的 Swagger Summary/Description 现已改为读取 `DOC_I18N_*` 多语资源，文本不再写死在代码中。
- 请在 `i18n-resources.json` 中维护以下键值：`DOC_I18N_VERSION_*`、`DOC_I18N_RESOURCES_*`、`DOC_I18N_LANGUAGE_*`、`DOC_I18N_LANGUAGES_*`。
- 若新增或调整 I18n 相关端点，请同步新增资源键并更新数据库。

---

## 阶段二：菜单面板内容实现（必需）

### 任务 2.1：创建 ModuleGroup 组件
**目标**：渲染模块分组（二级节点）

**文件**: `src/BobCrm.App/Components/Shared/ModuleGroup.razor`

**功能要求**：
- 显示模块图标和名称作为分组标题
- 渲染模块下的功能卡片网格
- 支持折叠/展开（可选）

**Props**：
```csharp
[Parameter] public FunctionMenuNode Module { get; set; }
[Parameter] public List<FunctionMenuNode> Functions { get; set; }
[Parameter] public string? CurrentRoute { get; set; }
[Parameter] public EventCallback<string> OnNavigate { get; set; }
```

**工作量**: 1.5 小时

---

### 任务 2.2：创建 FunctionCard 组件
**目标**：渲染单个功能卡片

**文件**: `src/BobCrm.App/Components/Shared/FunctionCard.razor`

**功能要求**：
- 显示功能图标和名称
- 高亮当前页面对应的卡片
- 点击触发导航事件
- 禁用无路由的功能

**Props**：
```csharp
[Parameter] public FunctionMenuNode Function { get; set; }
[Parameter] public bool IsActive { get; set; }
[Parameter] public EventCallback OnClick { get; set; }
```

**工作量**: 1 小时

---

### 任务 2.3：在 MenuPanel 中渲染模块和功能
**目标**：填充 MenuPanel 的内容区域

**文件**: `src/BobCrm.App/Components/Shared/MenuPanel.razor`

**改动**：
- 遍历当前领域的模块（二级节点）
- 为每个模块渲染 `<ModuleGroup>` 组件
- 提取模块下的叶子功能节点
- 传递当前路由和导航回调

**工作量**: 2 小时

---

### 任务 2.4：实现点击导航和关闭逻辑
**目标**：点击功能卡片后导航到目标页面并关闭菜单

**文件**: `src/BobCrm.App/Components/Shared/MenuPanel.razor`

**逻辑**：
```csharp
private async Task HandleFunctionClick(string route)
{
    if (string.IsNullOrWhiteSpace(route)) return;
    await OnClose.InvokeAsync();
    Nav.NavigateTo(route);
}
```

**工作量**: 30 分钟

---

### 任务 2.5：添加键盘导航支持
**目标**：支持 ESC 键关闭菜单

**文件**: `src/BobCrm.App/Components/Shared/MenuPanel.razor`

**实现**：
- 监听 `@onkeydown` 事件
- 检测 ESC 键（KeyCode 27 或 Key = "Escape"）
- 调用 `OnClose` 回调

**工作量**: 30 分钟

---

### 阶段二检查点
- [ ] MenuPanel 显示所有模块分组
- [ ] 每个模块下显示功能卡片网格
- [ ] 点击功能卡片可正确导航
- [ ] 导航后菜单自动关闭
- [ ] 当前页面对应的功能卡片高亮
- [ ] 按 ESC 键可关闭菜单

**预计总时间**: 5.5 小时

---

## 阶段三：样式与交互优化（必需）

### 任务 3.1：实现 DomainSelector 样式
**目标**：美化领域切换器和 Popover

**文件**: `src/BobCrm.App/wwwroot/css/components/domain-selector.css`（新建）

**样式要点**：
- `.domain-selector`: 按钮容器
- `.domain-button`: 按钮样式，带图标、文字、下箭头
- `.domain-popover`: Popover 容器，阴影、圆角
- `.domain-item`: 列表项，悬停高亮
- `.domain-item.active`: 当前选中高亮

**参考**：Ant Design Dropdown 样式

**工作量**: 1.5 小时

---

### 任务 3.2：实现 MenuPanel 样式
**目标**：美化菜单面板和遮罩

**文件**: `src/BobCrm.App/wwwroot/css/components/menu-panel.css`（新建）

**样式要点**：
- `.menu-panel-overlay`: 遮罩层，半透明黑色
- `.menu-panel`: 面板容器，白色背景，顶部展开动画
- `.menu-panel-header`: 头部样式，Flex 布局
- `.menu-panel-body`: 主体内容，滚动区域

**动画**：
```css
@keyframes slideDown {
  from { transform: translateY(-100%); opacity: 0; }
  to { transform: translateY(0); opacity: 1; }
}
```

**工作量**: 2 小时

---

### 任务 3.3：实现 ModuleGroup 和 FunctionCard 样式
**目标**：美化模块分组和功能卡片

**文件**: `src/BobCrm.App/wwwroot/css/components/menu-panel.css`（继续）

**样式要点**：
- `.module-group`: 分组容器，间距
- `.module-group-title`: 标题样式，灰色小字
- `.function-cards`: Grid 布局，自动换行
- `.function-card`: 卡片样式，圆角、阴影、悬停效果
- `.function-card.active`: 当前页面高亮（蓝色边框）
- `.function-card-icon`: 图标容器
- `.function-card-label`: 文字居中

**响应式断点**：
```css
@media (min-width: 1024px) { .function-cards { grid-template-columns: repeat(auto-fill, minmax(120px, 1fr)); } }
@media (max-width: 768px) { .function-cards { grid-template-columns: repeat(auto-fill, minmax(90px, 1fr)); } }
```

**工作量**: 2 小时

---

### 任务 3.4：实现点击遮罩关闭菜单
**目标**：点击面板外部区域关闭菜单

**文件**: `src/BobCrm.App/Components/Shared/MenuPanel.razor`

**实现**：
```razor
<div class="menu-panel-overlay" @onclick="HandleOverlayClick">
    <div class="menu-panel" @onclick:stopPropagation>
        <!-- 面板内容 -->
    </div>
</div>

@code {
    private async Task HandleOverlayClick()
    {
        await OnClose.InvokeAsync();
    }
}
```

**工作量**: 30 分钟

---

### 任务 3.5：调整 Layout CSS 移除旧样式
**目标**：清理不再使用的左侧导航样式

**文件**: `src/BobCrm.App/wwwroot/css/components/layout.css`

**改动**：
- 注释或移除 `.chrome-rail` 相关样式
- 注释或移除 `.domain-menu-panel` 相关样式
- 调整 `.chrome-app` 宽度为 100%
- 移除 `sider-collapsed`、`nav-mode-*` 等类

**工作量**: 1 小时

---

### 任务 3.6：主题适配（Light/Dark）
**目标**：确保新组件在两种主题下都正常显示

**文件**: `src/BobCrm.App/wwwroot/css/theme.css`

**改动**：
- 添加 `.calm-light` 下的颜色变量
- 添加 `.calm-dark` 下的颜色变量
- 使用 CSS 变量替代硬编码颜色

**工作量**: 1 小时

---

### 阶段三检查点
- [ ] DomainSelector 样式美观，与整体风格一致
- [ ] MenuPanel 展开/关闭动画流畅
- [ ] 功能卡片网格布局响应式
- [ ] 点击遮罩可关闭菜单
- [ ] Light/Dark 主题下样式正确
- [ ] 无多余的旧样式残留

**预计总时间**: 8 小时

---

## 阶段四：优化与测试（可选但推荐）

### 任务 4.1：添加"返回领域列表"功能
**目标**：在 MenuPanel 头部添加按钮，点击返回领域列表

**文件**: `src/BobCrm.App/Components/Shared/MenuPanel.razor`

**实现**：
```razor
<div class="menu-panel-header">
    <Button Size="@ButtonSize.Small" OnClick="OnBackToDomains">
        <Icon Type="@IconType.Outline.ArrowLeft" /> @I18n.T("BTN_BACK_TO_DOMAINS")
    </Button>
    <h3>@Domain?.Name</h3>
    <Button Type="@ButtonType.Text" Icon="@IconType.Outline.Close" OnClick="OnClose" />
</div>
```

**工作量**: 30 分钟

---

### 任务 4.2：优化菜单数据加载
**目标**：缓存菜单数据，避免重复请求

**文件**: `src/BobCrm.App/Components/Layout/MainLayout.razor`

**改动**：
- 将菜单数据提升到 MainLayout 级别
- 传递给 AppHeader 和 MenuPanel
- 添加刷新按钮（可选）

**工作量**: 1 小时

---

### 任务 4.3：添加多语言资源
**目标**：补全新组件的 i18n 键

**文件**: `src/BobCrm.Api/Resources/i18n-resources.json`

**新增键**：
```json
{
  "BTN_MENU": { "zh": "菜单", "ja": "メニュー", "en": "Menu" },
  "BTN_BACK_TO_DOMAINS": { "zh": "返回领域列表", "ja": "ドメインリストに戻る", "en": "Back to Domains" },
  "LBL_SELECT_DOMAIN": { "zh": "选择领域", "ja": "ドメインを選択", "en": "Select Domain" },
  "LBL_NO_FUNCTIONS": { "zh": "暂无可用功能", "ja": "利用可能な機能がありません", "en": "No functions available" }
}
```

**工作量**: 15 分钟

---

### 任务 4.4：移动端适配测试
**目标**：在不同屏幕尺寸下测试布局

**测试设备**：
- 桌面端（>= 1920px）
- 笔记本（1366px - 1920px）
- 平板（768px - 1024px）
- 手机（< 768px）

**检查项**：
- DomainSelector 是否正常显示
- MenuPanel 是否占满屏幕
- 功能卡片是否正确换行
- 遮罩是否覆盖全屏

**工作量**: 1 小时

---

### 任务 4.5：性能测试与优化
**目标**：确保大量功能时性能正常

**测试场景**：
- 模拟 50+ 功能的领域
- 反复打开/关闭菜单 10 次
- 检查内存泄漏（Chrome DevTools Memory）

**优化方向**：
- 使用 `@key` 指令优化列表渲染
- 虚拟滚动（如果功能 > 100）

**工作量**: 1.5 小时

---

### 任务 4.6：可访问性改进
**目标**：添加 ARIA 标签和焦点管理

**文件**: `MenuPanel.razor`, `DomainSelector.razor`

**改进**：
- 添加 `role="menu"`, `role="menuitem"`
- 添加 `aria-expanded`, `aria-current`
- 打开菜单时自动聚焦第一个可交互元素
- 关闭菜单时返回焦点到触发按钮

**工作量**: 1 小时

---

### 任务 4.7：编写单元测试
**目标**：为关键组件编写 bUnit 测试

**文件**: `tests/BobCrm.App.Tests/Components/`（新建）

**测试用例**：
- DomainSelector 渲染正确
- 点击领域触发回调
- MenuPanel 打开/关闭逻辑
- 功能卡片点击导航

**工作量**: 2 小时（可选）

---

### 阶段四检查点
- [ ] 返回领域列表按钮正常工作
- [ ] 菜单数据加载优化
- [ ] 所有文本已多语言化
- [ ] 移动端布局正常
- [ ] 性能测试通过（无卡顿、无内存泄漏）
- [ ] 可访问性改进完成

**预计总时间**: 7 小时（不含单元测试）

---

## 总时间估算

| 阶段 | 预计时间 | 必需程度 |
|------|---------|---------|
| 阶段一：基础结构 | 6 小时 | ✅ 必需 |
| 阶段二：菜单内容 | 5.5 小时 | ✅ 必需 |
| 阶段三：样式交互 | 8 小时 | ✅ 必需 |
| 阶段四：优化测试 | 7 小时 | ⚠️ 推荐 |
| **总计** | **26.5 小时** | - |

**工作日预估**：
- 如果每天投入 6-8 小时 → 约 **3-4 天**
- 如果包含测试和优化 → 约 **4-5 天**

---

## 风险与注意事项

### 风险 1：现有功能破坏
**问题**：移除左侧导航可能影响其他页面布局
**缓解**：
- 先在独立分支开发
- 逐页测试关键页面（客户、实体编辑器、设置等）
- 提供回退方案（保留旧代码，通过 Feature Flag 切换）

### 风险 2：用户习惯变化
**问题**：用户可能不习惯新布局
**缓解**：
- 首次使用时显示引导提示
- 在设置中提供"布局偏好"选项（未来）
- 收集用户反馈并迭代

### 风险 3：权限数据不一致
**问题**：菜单数据可能与后端权限不同步
**缓解**：
- 继续使用现有的 `/api/access/functions/me` 接口
- 前端只做渲染，不自行过滤权限

### 风险 4：移动端体验
**问题**：移动端菜单可能难以操作
**缓解**：
- 功能卡片尺寸适配触摸屏（最小 48x48px）
- 测试真机体验
- 考虑移动端专用布局（如抽屉式菜单）

---

## 验收标准

### 功能验收
- [x] 任务 1.1-1.5 完成（基础结构）
- [x] 任务 2.1-2.5 完成（菜单内容）
- [x] 任务 3.1-3.6 完成（样式交互）
- [ ] 任务 4.1-4.3 完成（优化）
- [ ] 任务 4.4-4.6 完成（测试）

### 质量验收
- [ ] 无明显 Bug
- [ ] 样式与设计稿一致（主题、间距、颜色）
- [ ] 所有交互流畅（< 300ms 响应）
- [ ] 通过主流浏览器测试（Chrome、Edge、Safari、Firefox）
- [ ] 移动端布局正常

### 性能验收
- [ ] 菜单展开时间 < 300ms
- [ ] 50+ 功能时无卡顿
- [ ] 内存无泄漏（多次打开关闭后稳定）

---

## 下一步行动

1. **评审设计文档**：与团队确认设计方案
2. **创建开发分支**：`feature/compact-top-menu`
3. **按阶段实施**：从阶段一开始，逐步推进
4. **定期演示**：每个阶段完成后演示给团队
5. **收集反馈**：根据反馈调整细节
6. **合并主分支**：全部完成并测试通过后合并

---

**执行人**: _待定_
**开始日期**: _待定_
**目标完成日期**: _待定_
