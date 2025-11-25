# UI-05 DataGrid Component Redesign (DataGrid组件重构设计)

## 概述

当前表单设计器中的 `DataGridWidget`（列表控件）缺乏用户友好的列配置机制。目前依赖于原始的 JSON 字符串 (`ColumnsJson`)，且不支持直接将字段拖入表格中。本文档旨在重构该机制，提供更健壮、交互性更强的列配置体验。

### 核心需求

1.  **结构化列定义**：引入强类型模型，替代纯 JSON 字符串操作。
2.  **可视化配置**：提供专门的属性编辑器，支持列的排序、显隐、宽度和格式设置。
3.  **拖拽集成**：支持从左侧“实体结构”面板直接拖拽字段到 DataGrid 预览区以添加列。
4.  **多语言支持**：列标题支持多语言键值配置。

## 数据模型设计

### 1. DataGridColumn (列定义模型)

我们将引入一个强类型的列模型，以确保类型安全并简化操作。

```csharp
namespace BobCrm.App.Models.Widgets;

/// <summary>
/// DataGrid 列定义
/// </summary>
public class DataGridColumn
{
    /// <summary>字段名（对应实体属性）</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>列标题（支持多语言Key）</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>宽度（像素，null表示自适应）</summary>
    public int? Width { get; set; }

    /// <summary>是否可见</summary>
    public bool Visible { get; set; } = true;

    /// <summary>是否可排序</summary>
    public bool Sortable { get; set; } = true;

    /// <summary>格式化字符串（如 "yyyy-MM-dd", "C2"）</summary>
    public string? Format { get; set; }

    /// <summary>对齐方式 (left, center, right)</summary>
    public string Align { get; set; } = "left";
}
```

### 2. DataGridWidget 更新

`DataGridWidget` 将保留 `ColumnsJson` 用于持久化（以兼容现有的序列化逻辑），但我们将通过新的属性编辑器将其作为结构化列表进行交互。

> **注意**：虽然存储仍为 JSON 字符串，但在运行时和设计时应尽量通过辅助方法将其反序列化为 `List<DataGridColumn>` 进行处理。

## 前端组件设计

### 1. ColumnDefinitionEditor (列定义编辑器)

我们将引入一个新的属性编辑器类型 `PropertyEditorType.ColumnDefinition`。

**组件路径**：`src/BobCrm.App/Components/Designer/PropertyEditors/ColumnDefinitionEditor.razor`

**功能特性**：
*   **列表视图**：显示所有已定义的列，包含标题和显隐状态。
*   **拖拽排序**：支持拖拽或上下按钮调整列顺序。
*   **快捷操作**：一键切换可见性，删除列。
*   **详细编辑**：点击列项展开详细配置（标题、宽度、对齐、格式）。

**界面原型**：
```text
[ 列配置 ]
---------------------------
(≡) 客户名称      [👁] [🗑]
(≡) 状态          [👁] [🗑]
(≡) 创建时间      [🚫] [🗑]
---------------------------
[+ 添加自定义列]
```

### 2. DataGridPreview (预览组件增强)

`DataGridPreview` 组件将增强为支持 `EntityFieldDto` 的拖放目标。

**交互流程**：
1.  用户从“实体结构”面板拖拽字段（如“客户名称”）。
2.  用户将其放入 `DataGridPreview` 区域。
3.  `DataGridPreview` 触发 `OnDrop` 事件：
    *   反序列化 `ColumnsJson`。
    *   检查字段是否已存在。
    *   使用字段的 `PropertyName` 和 `DisplayName` 创建新的 `DataGridColumn`。
    *   序列化回 `ColumnsJson`。
    *   触发状态更新。

**预览区原型**：
```text
+--------------------------------------------------+
|  客户名称  |  状态  |  创建时间  |               |
|----------------------------------|               |
|  ...       |  ...   |  ...         |               |
|                                                  |
| [拖拽字段到此处以添加列]                         |
+--------------------------------------------------+
```

## 实施步骤

### Phase 1: 基础建设
1.  在 `BobCrm.App.Models.Widgets` 中创建 `DataGridColumn` 类。
2.  更新 `DataGridWidget` 中的 `GetPropertyMetadata`，将 `ColumnsJson` 的 `EditorType` 修改为 `PropertyEditorType.ColumnDefinition`。
3.  在 `PropertyEditorType` 枚举中添加 `ColumnDefinition`。

### Phase 2: 属性编辑器开发
1.  创建 `ColumnDefinitionEditor.razor` 组件。
2.  更新 `PropertyEditor.razor`，在 `switch` 语句中处理 `PropertyEditorType.ColumnDefinition`，渲染新的编辑器组件。

### Phase 3: 拖拽集成
1.  更新 `DataGridPreview.razor`：
    *   添加 `ondragover` 和 `ondrop` 处理程序。
    *   实现拖放逻辑：解析拖拽数据，更新列定义。

## 注意事项

1.  **兼容性**：确保现有的 JSON 数据能被正确解析，避免破坏已有表单。
2.  **多语言**：列标题默认使用字段的 DisplayName，但在编辑器中应允许覆盖为多语言 Key。
3.  **性能**：频繁的 JSON 序列化/反序列化可能影响性能，在编辑器内部应维护对象状态，仅在变更时提交 JSON。
