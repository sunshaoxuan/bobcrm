# PLAN-11: v0.7.0 模板运行链路修复实施计划

**版本**: 1.0  
**状态**: 进行中  
**关联文档**:  
- `docs/design/PROD-01-客户信息管理系统设计文档.md`（需求基线）  
- `docs/reviews/REVIEW-08-v0.7.0-项目审计.md`（偏差来源）  
- `docs/process/STD-02-文档编写规范.md`（写作规范）  
- `docs/process/STD-06-集成测试规范.md`（测试与验证流程）

---

## 1. 目标与背景
- 目标：恢复并闭环“设计器 → 模板存储 → 运行时渲染”链路，使 Detail/Edit/List 三类模板按需求文档可用、可配、可验证。
- 背景：`PageLoader.razor` 仍使用旧版 `item_*` 结构解析，无法渲染当前模板 JSON；设计器未接入统一宿主/布局模型，数据控件缺少配置能力，导致模板交付阻塞（参见 REVIEW-08 的 Critical Deviation）。

---

## 2. 范围与假设
- 范围：前端模板运行链路（设计器、模板 JSON、运行时渲染）、数据类控件配置、主题与样式最小对齐。
- 不在本计划内：后端模板生成算法大改、全站视觉重设计（仅做最小 token 化补位）。
- 假设：API 端点保持现状；现有 Widget 模型可扩展满足需求文档中列出的属性。

---

## 3. 分阶段实施

### 阶段 A：JSON 统一与运行时解码（优先级 P0）
- 将设计器保存格式、默认模板生成、`WidgetJsonConverter/LayoutMapper` 输入输出统一为数组式 Widget JSON（含 children / layout 属性）。
- 改造 `src/BobCrm.App/Components/Pages/PageLoader.razor`，按统一 JSON 解析并支持 children、NewLine、Width/HeightUnit、Visible 等属性。
- 为 List 模板沿用同一解析路径（`ListTemplateHost` → `RuntimeWidgetRenderer`）。

### 阶段 B：设计器能力补齐（优先级 P0）
- 引入统一宿主/DropZone/Resize 交互（遵循 PROD-01 的 WidgetHost 约束），支撑 Flow/Absolute 模式与 NewLine。
- 在 `PropertyEditor` 中补齐 DataSetPicker/FieldPicker/TextStyle/Background 等 EditorType，完成 DataGrid/SubForm/UserRole/PermTree 的配置闭环。
- 设计器保存流程改用统一 JSON 写出（含 WidthUnit/HeightUnit/children/ExtendedProperties），并提供加载回放校验。

### 阶段 C：视觉与可用性兜底（优先级 P1）
- 引入最小设计 Token（色板/间距/字体/阴影）并替换设计器和模板宿主的内联硬编码颜色。
- 为运行时/设计器提供空态、错误态、加载态的统一样式，避免空白屏。

### 阶段 D：验证与留痕（优先级 P1）
- 按 `STD-06` 执行：清理 → verify-setup → dev 启动 → E2E（登录 + 模板加载 + 详情编辑）→ 清理。
- 增补至少一条 Playwright 脚本覆盖“List 模板打开 → 点击行 → 详情页渲染成功 → 保存返回 200”。
- 在 `CHANGELOG.md` 和 `docs/reviews/REVIEW-08-v0.7.0-项目审计.md` 补充修复记录。

---

## 4. 行动清单（可勾选）
- [x] 统一模板 JSON：`WidgetJsonConverter/LayoutMapper` 支持 children/WidthUnit/HeightUnit/NewLine/Visible/ExtendedProperties。
- [x] `PageLoader.razor`：改用统一解析；支持 children 嵌套、NewLine、百分比尺寸；Detail/Edit 表头与校验保持。
- [x] `ListTemplateHost`：复用统一解析与渲染路径，消除专用解析分支。
- [x] 设计器宿主：引入 WidgetHost/DropZone/Resize，确保 Flow/Absolute 交互一致；防止 children 递归拖拽成环。
- [x] `PropertyEditor`：实现 DataSetPicker/FieldPicker/TextStyle/Background 选择器；为数据控件挂上对应元数据。
- [x] 主题兜底：增加基础 design token，并替换设计器与运行时的硬编码颜色/阴影；空态/错误态一致。
- [ ] 测试：执行 `scripts/verify-setup.ps1`，运行 Playwright 登录+模板链路脚本；补充至少 1 条模板链路 API/前端集成测试。
- [ ] 文档与变更记录：更新 `CHANGELOG.md` 与 REVIEW-08 备注修复项。

---

## 5. 验收标准
1. Detail/Edit/List 三类模板在 UI 端均能加载并渲染（无回退空布局），能提交保存且返回 2xx。
2. 设计器拖拽/嵌套/Resize/属性编辑可用，保存再加载后布局不丢失（children/WidthUnit/HeightUnit/ExtendedProperties 均可回放）。
3. DataGrid/SubForm/UserRole/PermTree 可配置数据源或必要字段且能在运行时使用。
4. 通过 `STD-06` 流程，E2E 脚本通过并产出截图/日志。

---

## 6. 立即执行的提示（Prompt）
> 请按照 `docs/process/STD-02-文档编写规范.md` 和本计划执行，先完成阶段 A/B 的最小闭环，再进入阶段 C/D。  
> 聚焦文件：`src/BobCrm.App/Components/Pages/PageLoader.razor`、`src/BobCrm.App/Components/Shared/ListTemplateHost.razor`、`src/BobCrm.App/Services/Widgets/WidgetJsonConverter.cs`（若无则新增）和 `src/BobCrm.App/Components/Designer/PropertyEditor.razor`。  
> 动手顺序：统一 JSON → 运行时解析 → 设计器保存/加载 → 属性编辑器扩展 → 视觉兜底 → 按 `STD-06` 跑通登录+模板链路 E2E。
