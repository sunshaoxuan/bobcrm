# REVIEW-02: v0.7.0 重构与质量检查

**日期**: 2025-11-20
**评审人**: Antigravity
**范围**: 容器拖放、组件封装、I18n/UI 验证

## 1. 执行摘要

本次评审重点验证“容器拖放”问题的修复情况，并评估组件系统的面向对象设计（封装性）。同时交叉验证 REVIEW-01 中要求的 I18n 和 UI/UX 改进状态。

**状态概览**:
*   ✅ **容器拖放**: **已修复** (历史修复已正确实施)。
*   ✅ **组件封装**: **优秀** (运行时) / **可接受** (设计时)。
*   ⚠️ **I18n**: **部分修复** (`Templates.razor` 已改进，`FormDesigner.razor` 仍有硬编码字符串)。
*   ❌ **UI/UX**: **未修复** (仍在使用原生 `prompt`/`confirm`)。
*   ❌ **架构**: **未修复** (端点逻辑未重构)。

---

## 2. 详细发现

### 2.1 容器拖放 (重点领域)
**状态**: ✅ **验证已修复**

`FormDesigner.razor` 中的实现正确包含了历史调试会话 (`OPS-02`) 中确定的关键修复：
*   **事件处理**: `@ondragover:preventDefault` 和 `@ondrop:stopPropagation` 正确应用于容器包装器 (第 794-795 行)。
*   **竞态条件保护**: `isProcessingDrop` 保护模式在 `OnContainerDrop` (第 1002 行) 和 `OnDragEnd` (第 980 行) 中正确实现，以防止异步操作期间的状态损坏。
*   **初始化**: `bobcrm.initDragDrop` 在 `OnAfterRenderAsync` (第 341 行) 中正确调用。

### 2.2 组件封装与 OOP (重点领域)
**状态**: ✅ **良好**

用户特别要求避免渲染器中的“大量 if/else”块。

*   **运行时渲染 (`RuntimeWidgetRenderer.cs`)**: **优秀**。
    *   使用严格的多态性：`request.Widget.RenderRuntime(context)`。
    *   渲染器本身零 `if/else` 或 `switch` 语句。逻辑完全封装在每个 Widget 类中。

*   **设计时渲染 (`FormDesigner.razor`)**: **可接受**。
    *   对叶子组件使用 `DynamicComponent` (`RenderWidgetVisual`, 第 753 行)，这非常好。
    *   对容器组件使用 `switch` 语句 (`RenderContainerDesign`, 第 861-904 行)。
    *   **批评**: 虽然不算“大量”（约 40 行），但此 `switch` 违反了开闭原则。添加新容器类型需要修改 `FormDesigner.razor`。
    *   **建议**: 为 `ContainerWidget` 引入 `DesignRendererType` 属性（类似于 `PreviewComponentType`），并对容器也使用 `DynamicComponent`，完全消除 `switch`。

### 2.3 国际化 (I18n)
**状态**: ⚠️ **部分 / 需要工作**

*   **`Templates.razor`**: 大部分已修复。标签和消息使用 `I18n.T()`。
*   **`FormDesigner.razor`**: **仍有关键缺口**。
    *   第 618 行: `alert("请输入模板名称")` - 硬编码中文。
    *   第 624 行: `alert("请选择实体类型")` - 硬编码中文。
    *   第 664 行: `alert("模板更新成功")` - 硬编码中文。
    *   第 701 行: `alert("模板创建成功")` - 硬编码中文。
    *   错误消息 (第 499, 511, 565, 670, 711 行) 也是硬编码的。

### 2.4 UI/UX 现代化
**状态**: ❌ **未修复**

*   **`Templates.razor`**: 仍在使用原生浏览器对话框：
    *   第 398 行: `JS.InvokeAsync<string>("prompt", ...)`
    *   第 437 行: `JS.InvokeAsync<bool>("confirm", ...)`
    *   **要求**: 必须替换为 Ant Design `Modal` 服务交互，以获得专业的“高级”感。

### 2.5 架构
**状态**: ❌ **未修复**

*   **`TemplateEndpoints.cs`**: 逻辑仍大量嵌入在端点定义中 (第 28-139 行)。未引入 `TemplateService`。

---

## 3. 行动计划

1.  **重构 FormDesigner I18n**: 将 `FormDesigner.razor` 中的所有硬编码字符串提取到资源文件。
2.  **替换原生 Alert**: 在 `Templates.razor` 和 `FormDesigner.razor` 中，用 `ModalService` 或 `MessageService` 替换 `window.alert`、`window.confirm` 和 `window.prompt`。
3.  **重构容器渲染 (可选但推荐)**: 用动态组件方法替换 `RenderContainerDesign` 中的 `switch`，以实现 100% OCP 合规。

## 4. 结论

核心功能需求（容器拖放）稳固。运行时渲染的 OOP 结构优秀。然而，“高级”感因原生浏览器警报和不完整的 I18n 而大打折扣。发布前必须解决这些问题。
