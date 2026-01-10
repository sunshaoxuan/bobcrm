# ARCH-34: 解决 E2E 环境下组件“可见性”失效方案

**生效日期**: 2026-01-10
**状态**: 评审中

---

## 1. 现状分析 (Gaps)

经过 Batch 2 的第一轮修复，**DOM 存在性 (Presence)** 问题已解决，但 **可见性 (Visibility)** 依然失效。
- **现象**: `expect(locator).to_be_visible()` 超时。
- **原因怀疑**: 
    1. **高度塌陷**: 虽然增加了 `min-height`，但如果父容器（如 `.runtime-layout`）使用了 `flex` 且没有正确拉伸，或者其祖先级元素高度为 0，Playwright 会判定子组件不可见。
    2. **Headless 视口约束**: Headless 浏览器默认视口较小（800x600），如果组件位于初始视口外，且 `IntersectionObserver` 被绕过但滚动逻辑未配合，可能导致可见性计算异常。
    3. **UI 覆盖物**: 某些加载遮罩或 Skeleton 尚未完全移除。

## 2. 架构设计：增强可视化与稳定性

### 2.1 强制布局拉伸 (Force-Flush Layout)
在 E2E 模式下，对以下关键容器注入高度修正：
- `html`, `body`: `height: 100vh; overflow: auto;`
- `.runtime-shell`: `min-height: 800px;` (强制撑开内容)

### 2.2 可见性检测容错机制
在测试侧，改为更稳健的检测序列：
1. `wait_for_selector(state="attached")`: 确认 DOM 已挂载。
2. `locator.scroll_into_view_if_needed()`: 确保进入视口。
3. `expect(locator).to_be_visible()`: 最终可见性检查。

### 2.3 视觉差异审计 (Visual Diff Audit)
- **目标**: 记录 Headless 渲染的实际快照。
- **机制**: 在测试失败时，除了 HTML Dump，必须执行 `evaluate` 脚本，输出该元素的 `getBoundingClientRect()` 到日志，以物理坐标确认其位置。

## 3. 验证标准

- [ ] `debug_page_screenshot.png` 中 `Tabbox` 必须处于屏幕内且无遮挡。
- [ ] 日志输出组件的高度（`clientHeight`）必须 > 0。
- [ ] `test_batch2_003` 顺利通过 `to_be_visible` 断言。

---
## 附录：JS 诊断脚本建议
```javascript
// 在测试过程中注入
const el = document.querySelector('.runtime-tab-container');
const rect = el.getBoundingClientRect();
console.log(`E2E-COORD: x=${rect.x}, y=${rect.y}, w=${rect.width}, h=${rect.height}`);
```
