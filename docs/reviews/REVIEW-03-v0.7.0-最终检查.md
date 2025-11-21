# REVIEW-03: v0.7.0 最终质量检查

**日期**: 2025-11-20
**评审人**: Antigravity
**范围**: 验证 I18n、UI/UX 和架构修复。

## 1. 执行摘要

用户在 `FormDesigner.razor` 的国际化 (I18n) 方面取得了进展，成功将硬编码字符串提取到资源键。然而，用专业 UI 组件（Ant Design Modals）替换原生浏览器对话框（`alert`, `confirm`, `prompt`）的 **UI/UX 需求** **未实施**。`TemplateEndpoints.cs` 的架构重构也被跳过。

**状态概览**:
*   ⚠️ **I18n**: **部分修复**。字符串已提取，但显示机制仍为原生 alert。
*   ❌ **UI/UX**: **失败**。`Templates.razor` 和 `FormDesigner.razor` 中仍大量使用原生 `window.alert`、`window.confirm` 和 `window.prompt`。
*   ❌ **架构**: **失败**。业务逻辑仍与 API 端点耦合。

---

## 2. 详细发现与修复项目

必须逐一修复以下项目以满足项目的质量标准。

### 2.1 UI/UX: 替换 FormDesigner 中的原生 Alert
**位置**: `src/BobCrm.App/Components/Pages/FormDesigner.razor`
**当前状态**:
```csharp
await JS.InvokeVoidAsync("alert", I18n.T("MSG_TEMPLATE_NAME_REQUIRED"));
```
**要求**: 使用 `AntDesign.MessageService` 或 `ModalService`。
**修复任务**: `FIX-01: 用 MessageService 替换 FormDesigner 中的原生 alert`

### 2.2 UI/UX: 替换 Templates 中的原生 Prompt
**位置**: `src/BobCrm.App/Components/Pages/Templates.razor`
**当前状态**:
```csharp
var newName = await JS.InvokeAsync<string>("prompt", ...);
```
**要求**: 使用带有输入字段的 `AntDesign.Modal.ConfirmAsync`，或用于重命名的自定义 Modal 组件。
**修复任务**: `FIX-02: 用 Ant Design Modal 替换 Templates.razor 中的原生 prompt`

### 2.3 UI/UX: 替换 Templates 中的原生 Confirm
**位置**: `src/BobCrm.App/Components/Pages/Templates.razor`
**当前状态**:
```csharp
var confirm = await JS.InvokeAsync<bool>("confirm", ...);
```
**要求**: 使用 `AntDesign.Modal.ConfirmAsync`。
**修复任务**: `FIX-03: 用 Ant Design Modal 替换 Templates.razor 中的原生 confirm`

### 2.4 架构: 重构 Template Endpoints
**位置**: `src/BobCrm.Api/Endpoints/TemplateEndpoints.cs`
**当前状态**: 过滤、分组和复制的逻辑嵌入在端点委托中。
**要求**: 将逻辑移动到 `TemplateService`。
**修复任务**: `FIX-04: 从 TemplateEndpoints 提取 TemplateService`

---

## 3. 结论

代码功能正常，但由于使用了原生浏览器对话框，缺乏设计指南要求的“高级”润色。我已创建修复计划 (`docs/plans/PLAN-02-v0.7.0-修复计划.md`) 来解决这些具体项目。
