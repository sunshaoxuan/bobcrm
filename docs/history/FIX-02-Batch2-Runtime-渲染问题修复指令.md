# FIX-02: Batch2 Runtime 渲染问题修复指令

**版本**: 1.0
**引用**: [ARCH-33](file:///c:/workspace/bobcrm/docs/design/ARCH-33-可视化E2E诊断方案.md), [STD-06](file:///c:/workspace/bobcrm/docs/process/STD-06-集成测试规范.md)
**紧急程度**: 高 (Blocker)

## 1. 任务背景
当前 E2E 测试 `test_batch2_003` 无法观测到 Tabbox 渲染。你需要按照以下方案对前端运行时进行增强。

## 2. 开发任务清单

### 2.1 强化 LazyRender 韧性 (LazyRender.razor)
- **目标**: 消除 Headless 环境下的渲染不确定性。
- **改动**: 
    - 在 `OnAfterRenderAsync` 捕获异常的分支中，确保 `_isVisible = true`。
    - 增加一种机制（如探测 `e2e` URL 参数），强制跳过 Observer。

### 2.2 注入诊断看板 (PageLoader.razor)
- **目标**: 实现运行时状态的可观测性。
- **改动**: 
    - 在 `<div class="runtime-shell">` 内（或顶部）增加一个调试面板：
    ```razor
    <div id="e2e-debug-banner" style="display:none" class="e2e-only">
        TemplateId: @ViewModel.RuntimeContext?.Template?.Id
        WidgetCount: @ViewModel.LayoutWidgets.Count
    </div>
    ```
    - 确保 `display:none` 但保留在 DOM 中，供 E2E 探测 `text_content`。

### 2.3 修复 WidgetRegistry 潜在冲突 (WidgetRegistry.cs)
- **目标**: 确保 Tabbox 正确映射。
- **检查**:
    - 确认 `builtIn` 数组中 `tabbox` 的定义没有被 `DiscoverWidgets()` 的动态扫描覆盖。
    - 统一使用 `ToLowerInvariant()` 进行类型匹配。

## 3. 测试与验证要求

### 3.1 可视化存证 (Evidence Retrieval)
- **指令**: 修改 `conftest.py` 或测试运行参数，强制开启 `--headed` 和 `--video on`。
- **产出**: 每一个失败的 Job 必须附带：
    - `debug_page_content.html` (全 DOM dump)
    - `debug_page_screenshot.png` (视觉快照)

### 3.2 流程合规
- 必须严格执行 `STD-06` 的五步法（Clean -> Verify -> Start -> Test -> Shutdown）。
- 未通过本地可视化验证前，禁止提交代码。

## 4. 交付物
- 修复后的代码。
- 包含截图链接的 `AUDIT-02-Batch2-渲染问题排查报告.md`。

---
**架构评审人**: Antigravity
**日期**: 2026-01-09
