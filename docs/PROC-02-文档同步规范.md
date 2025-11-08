# 文档同步指南

本项目的 Stage 5 · 验证与治理要求「设计说明书/计划」与代码保持对齐。请遵循下面流程：

1. **明确版本与生效日期**：每次修改 `README.md` 中的阶段条目或 `docs/UI...` 系列文件时，在文件开头附上 `生效于 YYYY-MM-DD` 的注释，便于 QA/PM 追踪。
2. **同步 UI/UE 说明**：
   - 若修改组件视觉（buttons、cards、layouts 等），同时更新对应的 `docs/ui/*.md` 文件或新增 `docs/screenshots/...`（命名建议 `screenshots/<page>-light.png`）。
   - 在文档中注明 affected components 和对应的 `Theme/State` services（如 `ThemeState`、`LayoutState`、`InteractionState`）。
3. **记录 PR Checklist 结果**：在审核过程中，将 `docs/PROC-01-PR检查清单.md` 里的每项结果写入 PR body 的 `Validation` 段并引用（如 `Validation: ✅ Light/Dark screenshots added`）。
4. **文档版本控制**：大范围修改设计规范或交互流程时，在 `CHANGELOG.md` 或 `docs/UI-02-阶段1-实现计划.md` 追加条目，说明变更的“原因/影响/验证”。

如此一来，每次需要 Stage 5 检查的 PR 都会有明确的视觉验证、治理依据和可查档案。

