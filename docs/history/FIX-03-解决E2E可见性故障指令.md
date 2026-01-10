# FIX-03: 解决 E2E 环境下组件可见性故障指令

**版本**: 1.0
**引用**: [ARCH-34](file:///c:/workspace/bobcrm/docs/design/ARCH-34-解决E2E环境下组件可见性失效方案.md), [FIX-02](file:///c:/workspace/bobcrm/docs/history/FIX-02-Batch2-Runtime-渲染问题修复指令.md)
**优先级**: 紧急 (Blocker)

## 1. 任务背景
目前已经解决了“渲染黑盒”（DOM 中已有组件），但 Playwright 仍判定组件不可见。这属于典型的“物理可见性故障”。

## 2. 开发任务清单

### 2.1 强制注入可见性样式 (RuntimeCanvas.razor / index.html)
- **目标**: 确保父级容器不会塌陷或隐藏内容。
- **改动**: 
    - 在 `RuntimeCanvas.razor` 的外层容器上注入 `min-height: 800px;`。
    - 检查 `.runtime-layout` 是否包含 `overflow: hidden`，若是，改为 `overflow: visible`。

### 2.2 测试侧可见性诊断增强 (test_batch2_runtime.py)
- **目标**: 捕获物理渲染坐标，定位为何“不可见”。
- **改动**: 
    - 在执行 `expect(page.locator(".runtime-tab-container")).to_be_visible()` 之前，插入以下调试代码：
    ```python
    # 物理坐标审计
    box = page.locator(".runtime-tab-container").bounding_box()
    print(f"E2E-AUDIT: Tabbox Bounding Box: {box}")
    # 强制滚动
    page.locator(".runtime-tab-container").scroll_into_view_if_needed()
    # 记录属性
    opacity = page.locator(".runtime-tab-container").evaluate("el => getComputedStyle(el).opacity")
    print(f"E2E-AUDIT: Tabbox Opacity: {opacity}")
    ```

### 2.3 状态同步延迟治理 (conftest.py / 脚本)
- **目标**: 排除 Blazor 渲染延迟干扰。
- **方案**: 
    - 增加对 `.runtime-tab-container[data-debug-tabs-count]` 属性的等待，确保逻辑完成。
    - 确保 `page.wait_for_load_state("networkidle")` 被调用。

## 3. 验收标准
- [ ] 运行脚本后，控制台输出 `E2E-AUDIT` 且 `height` > 0。
- [ ] `expect().to_be_visible()` 成功。
- [ ] `debug_page_screenshot.png` 必须能看到蓝色的 `Tab` 标题。

---
**架构评审人**: Antigravity
**日期**: 2026-01-10
