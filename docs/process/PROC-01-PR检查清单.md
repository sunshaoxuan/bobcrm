# Pull Request Requirements

为了支撑 Stage 5 · 验证与治理，所有前端/设计相关的 PR 需要满足以下项，便于跨主题/断点/语言验证：

1. **设计章节引用**：在 PR 描述里指出所触及的设计章节（如 `docs/UI-03-阶段2-布局验证记录.md`）或任务编号，便于后续追踪哪份文档被更新。
2. **Light/Dark 截图**：至少附带一个 light 主题 + 一个 dark 主题的核心页面截图（推荐 `docs/screenshots/theme-light.png` 风格），并注明截屏 URL/时间点。
3. **功能回归说明**：列出主要交互/组件（Collection、Record Workspace、Form Designer 等）在本 PR 里的状态变化，说明是否触发了跨模块回归。
4. **可达性/焦点覆盖**：手动确认 Tab 顺序、焦点环和 contrast ratio；如引入新的交互控件，提供示意（例如 `focus-visible` 样式截图或 `tabindex` 描述）。
5. **自动化验证**：
   - 运行 `pwsh scripts/check-style-tokens.ps1`（加 `-FailOnMatch` 可作为 CI gate）确保新样式遵循 Tokens。
   - 确认 `tests/` 下已有单元/集成测试执行无误（若无测试则说明原因）。

将以上内容加入 PR 描述或附带的 `docs/` 更新，便于团队在 Review 阶段快速判断是否满足治理要求。

