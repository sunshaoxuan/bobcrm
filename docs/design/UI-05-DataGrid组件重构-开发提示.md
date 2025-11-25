# DataGrid组件重构 - 开发提示 (Development Prompts)

请按顺序使用以下提示来实现 DataGrid 组件的重构。

## Prompt 1: 基础建设 - 数据模型
```text
我需要开始 DataGrid 的重构工作，首先建立数据模型。

1. 创建新文件 `src/BobCrm.App/Models/Widgets/DataGridColumn.cs`。
   - 包含属性：`Field` (string), `Label` (string), `Width` (int?), `Visible` (bool, 默认 true), `Sortable` (bool, 默认 true), `Format` (string?), `Align` (string, 默认 "left")。
   - 请添加中文注释。

2. 更新 `src/BobCrm.App/Models/Widgets/DataGridWidget.cs`。
   - 在 `GetPropertyMetadata()` 方法中，找到 `ColumnsJson` 的元数据配置。
   - 将其 `EditorType` 从 `PropertyEditorType.Json` 修改为新的枚举值 `PropertyEditorType.ColumnDefinition`（你需要先在 `PropertyEditorType` 枚举中添加此值）。
   - 确保 `ColumnsJson` 属性保持不变，仍作为存储机制。

请实现这些模型变更。
```

## Prompt 2: 属性编辑器 - 列定义编辑器
```text
现在需要实现用于编辑列的 UI 组件。

1. 创建新组件 `src/BobCrm.App/Components/Designer/PropertyEditors/ColumnDefinitionEditor.razor`。
   - 接收参数 `Value` (string, JSON格式) 和 `ValueChanged` (EventCallback<string>)。
   - 将 JSON 反序列化为 `List<DataGridColumn>`。
   - 渲染列列表，每行包含：
     - 拖拽手柄（图标）。
     - 列标题（文本）。
     - 可见性切换开关。
     - 删除按钮。
   - 底部添加“添加列”按钮。
   - 点击列项时，显示详细编辑表单（内联或模态框），可编辑 Label, Width, Align, Format。
   - 任何变更发生时，将列表序列化回 JSON 并调用 `ValueChanged`。

2. 更新 `src/BobCrm.App/Models/Designer/WidgetPropertyMetadata.cs`（或定义 `PropertyEditorType` 的位置），确保包含 `ColumnDefinition`。

3. 更新 `src/BobCrm.App/Components/Designer/PropertyEditor.razor`。
   - 在 `switch` 语句中添加 `PropertyEditorType.ColumnDefinition` 的 case。
   - 渲染新的 `ColumnDefinitionEditor` 组件，并绑定到控件属性。

请实现编辑器组件并进行集成。
```

## Prompt 3: 拖拽集成
```text
最后，我们需要支持从工具箱拖拽字段到 DataGrid 以添加列。

1. 更新 `src/BobCrm.App/Components/Designer/WidgetPreviews/DataGridPreview.razor`。
   - 在 `div.dg-preview` 上添加 `ondragover` 事件处理（允许放置，`e.PreventDefault()`）。
   - 添加 `ondrop` 事件处理。
   - 在 `OnDrop` 中，检查拖拽数据是否包含 `EntityFieldDto`（注意：Blazor 的 DragEventArgs 可能无法直接跨组件传递自定义对象，你可能需要利用 `FormDesigner` 的状态服务或静态/Scoped 服务来获取“当前拖拽项”）。
   - 如果检测到字段被放置：
     - 反序列化 `Grid.ColumnsJson`。
     - 使用字段的 `PropertyName` 和 `DisplayName` 创建新的 `DataGridColumn`。
     - 添加到列表。
     - 序列化回 `Grid.ColumnsJson`。
     - 强制重新渲染。

请实现 DataGrid 预览区的拖放逻辑。
```
