# PLAN-13: 表单设计器恢复与实现计划（v0.7.0）

## 目标
恢复并完整实现 `src/BobCrm.App/Components/Pages/FormDesigner.razor`（当前为空），以闭环 v0.7.0 的“模板系统链路”（Template System Loop），提供可视化界面编辑 `FormTemplate` 布局。

## 需要评审确认
> [!IMPORTANT]
> 由于原始文件内容缺失/为空，本计划基于项目既有技术栈与模式（AntDesign Blazor、`ITemplateService`、Blazor 原生拖拽）提出**新的实现方案**，落地前建议快速评审确认。

## 方案设计

### 1) 布局结构
使用 `AntDesign.Layout`：
- **左侧 Sider（250px）**：工具箱（Toolbox），展示实体可用字段（`FieldMetadata`），作为拖拽源。
- **中间 Content（自适应）**：画布（Canvas），渲染当前布局 widgets，作为拖拽目标，支持排序。
- **右侧 Sider（300px）**：属性面板（Properties），展示/编辑选中 widget 的属性（Label、Width、Visible 等）。

### 2) 数据模型
- **状态**：`FormTemplate _template`（通过 `TemplateService` 加载）
- **Widgets**：`List<Dictionary<string, object>> _widgets`（从 `_template.LayoutJson` 解析）
- **可用字段**：`List<FieldMetadata> _fields`（通过 `EntityDefinition` 读取，或从实体定义解析）

### 3) 拖拽交互（Blazor 原生）
参考 `MenuManagement.razor` 的实现模式：
- **可拖拽项**：工具箱字段项、画布中的 widget 项
- **事件**：
  - `@ondragstart`：记录 `draggingId` 与来源类型（Toolbox / Canvas）
  - `@ondragover`：`event.PreventDefault()` 允许放置
  - `@ondrop`：处理落点
    - 新字段：在列表中新增 widget
    - 重排：在列表内移动 widget
  - 对可拖拽元素设置 `draggable=\"true\"`

### 4) 服务集成
- `[Inject] ITemplateService TemplateService`
- `[Inject] IEntityDefinitionService EntityService`（用于加载字段列表）
- **保存**：将 `_widgets` 序列化为 JSON，调用 `TemplateService.UpdateTemplateAsync(...)` 写回 `LayoutJson`

### 5) 可能需要的辅助组件/文件
- `FormDesignerUtils.cs`：widget JSON 序列化/反序列化帮助方法（可尽量复用 `DefaultTemplateGenerator` 规则）
- `PropertyEditor.razor`：可独立成组件或内联实现，根据 widget 类型渲染不同的编辑控件

## 代码改动清单（建议）

### BobCrm.App

#### 新增：`src/BobCrm.App/Components/Pages/FormDesigner.razor`
- 使用 AntDesign 组件实现页面布局与交互
- 关键流程：
  - `OnInitializedAsync`：按 `Id`（路由/查询参数）加载模板
  - `SaveAsync`：JSON 序列化并调用 Service 更新
  - `RenderWidget`：渲染 widget 预览（只读模式或占位符）

#### 修改：`src/BobCrm.App/Components/Pages/Templates.razor`
- 确认“编辑/设计”按钮能导航到 `/form-designer/{TemplateId}`

## 验证方案

### 静态验证
- `dotnet build`
- `pwsh scripts/check-i18n.ps1`

### 手工验证
1. 进入 Templates 页面 → 点击某模板的“Design/Edit” → 跳转到 Form Designer
2. 页面三栏正常渲染：左（Fields）、中（Preview/Canvas）、右（Properties）
3. 交互验证：
   - 从左侧拖字段到画布 → 新 widget 出现
   - 画布内拖拽 widget → 顺序变化
   - 点击 widget → 右侧属性面板更新
   - 修改属性（如 Label）→ 画布预览同步变化
4. 保存验证：点击 Save → 刷新页面 → 修改仍存在
