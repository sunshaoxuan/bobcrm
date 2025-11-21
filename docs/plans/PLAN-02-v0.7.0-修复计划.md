# PLAN-02: v0.7.0 修复计划

**目标**: 修复 REVIEW-03 中识别出的剩余质量问题（国际化、UI/UX、架构），以满足“高级”设计标准。

## 1. UI/UX 修复（高优先级）

### FIX-01: 替换 FormDesigner 中的原生 Alert
**目标**: `src/BobCrm.App/Components/Pages/FormDesigner.razor`
**任务**:
1.  注入 `AntDesign.MessageService` 和 `AntDesign.ModalService`。
2.  将所有 `await JS.InvokeVoidAsync("alert", ...)` 调用替换为 `_message.Error(...)` 或 `_message.Success(...)`。
3.  除非必要，否则删除用于调试的 `console.log` 调用。

### FIX-02: 替换 Templates 中的原生 Prompt
**目标**: `src/BobCrm.App/Components/Pages/Templates.razor`
**任务**:
1.  移除 `await JS.InvokeAsync<string>("prompt", ...)`。
2.  实现一个带有 `Input` 字段的 Ant Design `Modal`，用于重命名/复制模板。
3.  确保模态框正确支持“确定”和“取消”操作。

### FIX-03: 替换 Templates 中的原生 Confirm
**目标**: `src/BobCrm.App/Components/Pages/Templates.razor`
**任务**:
1.  移除 `await JS.InvokeAsync<bool>("confirm", ...)`。
2.  使用 `_modalService.ConfirmAsync(...)` 进行删除和应用操作。
3.  确保确认对话框文本的正确本地化。

## 2. 架构修复（中优先级）

### FIX-04: 提取 TemplateService
**目标**: `src/BobCrm.Api/Endpoints/TemplateEndpoints.cs` -> `src/BobCrm.Api/Services/TemplateService.cs`
**任务**:
1.  创建 `TemplateService` 类。
2.  将复杂的过滤、分组和复制逻辑从 `TemplateEndpoints.cs` 移动到 `TemplateService`。
3.  重构 `TemplateEndpoints.cs`，使其成为调用 `TemplateService` 的薄层。

## 3. 执行顺序
1.  FIX-01 (FormDesigner UI)
2.  FIX-02 & FIX-03 (Templates UI)
3.  FIX-04 (架构)
