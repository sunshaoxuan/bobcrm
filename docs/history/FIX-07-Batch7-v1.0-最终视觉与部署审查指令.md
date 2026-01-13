# FIX-07: Batch 7 - v1.0 最终视觉与部署审查指令

**生效日期**: 2026-01-13
**状态**: 待执行

---

## 任务背景
在通过 Batch 6 的严苛质量审计后，BobCRM v1.0.0-RC1 在核心功能和稳定性上已达到发布标准。
现进入 **Batch 7: 最终视觉打磨与部署审查 (Final Polish & Launch Audit)** 阶段，旨在提升产品的“生产就绪感”。

## 目标任务 (Goals)

### 1. 视觉一致性打磨 (UI/UX Polish)
*   **审计项**: 
    *   **阴影与圆角**: 确保所有 Card 和 Modal 使用统一的 `box-shadow` 和 `border-radius (4px/8px)`。
    *   **表格间距**: `DataGrid` 的行高和单元格内边距在不同屏幕宽度下的对齐情况。
    *   **空状态**: 确保所有空列表/搜索无结果时，`Ant Design Empty` 组件的文案已中文化。
*   **任务**: 针对上述视觉瑕疵进行全局 CSS 或组件参数修正。

### 2. 生产环境配置校验 (Production Readiness)
*   **任务**: 
    *   验证 `appsettings.Production.json`（或环境变量）中是否存在硬编码的密钥。
    *   确保 `/health` 端点在数据库连接模拟失败时能正确返回 `503 Service Unavailable`。
    *   验证日志级别在 `Production` 模式下默认设定为 `Information` 而非 `Debug`。

### 3. 性能看板最终确认
*   **任务**: 记录并确认一次完整的 E2E 运行耗时基准，并将其作为 v1.0 的性能 Baseline。

## 验收标准 (Definition of Done)
1. **视觉分值**: 全站无明显的样式错位或未翻译的英文占位符。
2. **零硬编码**: 生产配置通过安全扫描（无明文 Credential）。
3. **冒烟回归**: 执行全量回归测试 (FIX-06)，确保 UI Polish 未引入回归。

---
这是 v1.0 发布前的最后一个 Batch，请以最高标准执行。
