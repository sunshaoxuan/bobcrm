# ARCH-33: 可视化 E2E 诊断与 Tabbox 渲染优化方案

**生效日期**: 2026-01-09
**状态**: 评审中

---

## 1. 背景与问题定义

在 Batch 2 的 E2E 测试（`test_batch2_003`）中，前端运行时页面无法正确渲染 `Tabbox` 组件，导致测试超时失败。尽管后端 `TemplateId` 选择正确，但前端 DOM 中缺失 `.runtime-tab-container`。

### 1.1 核心挑战
- **渲染黑盒**: 在 headless 环境下，难以判断是组件未加载、JS 报错还是 CSS 隐藏。
- **环境差异**: 开发者本地测试与 E2E 容器环境在浏览器行为（如 Interaction Observer）上存在差异。

## 2. 架构设计：可视化增强

为打破黑盒，引入“可视化优先”的诊断架构。

### 2.1 运行时调试横幅 (Debug Banner)
在 `PageLoader.razor` 中集成一个仅在特定条件下显示的调试仪表盘，输出关键状态：
- `EntityType` / `Id`
- `TemplateId` (Backend Selection)
- `LayoutWidgets.Count`
- `IsEditMode`

### 2.2 环境感知组件 (Environment-Aware Component)
修改 `LazyRender.razor`，引入“E2E 强制渲染”模式。
- **逻辑**: 当检测到环境变量或 URL 参数时，跳过 `IntersectionObserver` 检查，强制设置 `_isVisible = true`。
- **预期**: 解决 headless 环境下滚动探测失灵导致的组件不渲染问题。

## 3. 逻辑模型：Tabbox 渲染规则

- **注册标识**: `type: "tabbox"` 必须映射到 `RuntimeTabContainer.cs`。
- **递归渲染**: `RuntimeTabContainer` 必须调用 `RuntimeWidgetRenderer` 来处理 `TabWidget` 的子控件。
- **样式原子化**: 使用 `WidgetStyleHelper` 生成 CSS 变量，确保跨模式样式一致。

## 4. 验证标准

- [ ] Chrome 开发者工具 Console 无红字报错。
- [ ] 页面 HTML 源码包含 `class="runtime-tab-container"`。
- [ ] 自动生成的测试证据（视频/截图）必须清晰可见渲染后的组件。

---

## 附录：逻辑 DTO 结构 (Logical Schema)

| 字段 | 逻辑类型 | 说明 |
| :--- | :--- | :--- |
| `Type` | String | 必须为 `tabbox` |
| `ActiveTabId` | String | 初始激活的 Tab 标识 |
| `Children` | Array | 包含 `type: "tab"` 的子项 |
