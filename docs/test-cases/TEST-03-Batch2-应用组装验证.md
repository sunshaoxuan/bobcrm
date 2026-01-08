# TEST-03: Batch 2 应用组装验证

> **关联计划**: [PLAN-24](file:///c:/workspace/bobcrm/docs/plans/PLAN-24-v1.0-全功能测试执行方案.md)
> **测试范围**: TPL-001~006
> **前置依赖**: Batch 1 Passed

## 1. 环境准备
*   **Fixture**: `standard_product_entity` (预发布实体)

## 2. 测试用例

### Case 2.1: 默认模板生成 (Default Template Generation)
**目标**: 验证自动生成能力。
*   **操作**: 发布 `Product` 实体。
*   **验证**:
    *   API `/api/templates?entity=Product` 返回 2 项 (`ProductDefaultList`, `ProductDefaultDetail`)。
    *   模板 JSON 包含实体定义的所有字段。
    *   `IsUserDefault` 为 True。

### Case 2.2: 设计器交互 (Designer Interaction)
**目标**: 验证所见即所得能力。
*   **操作**:
    1.  打开 `ProductDefaultDetail` 设计器。
    2.  **拖拽**: 将 'Price' 字段拖入新的 'TabContainer' -> 'Pricing Tab'。
    3.  **属性编辑**: 将 'Price' 标签改为 "List Price"。
    4.  保存。
*   **验证**: 重新加载设计器。结构已持久化 (Price 在 Tab 内)。

### Case 2.3: 运行时渲染 (Runtime Rendering)
**目标**: 验证 JSON Layout -> HTML Render 的转换。
*   **操作**: 访问 `/app/Product/new`。
*   **验证**:
    *   Tab 组件可见。
    *   'List Price'标签显示 (而非 'Price')。
    *   **控件类型**:
        *   `Price` 渲染为 `<InputNumber />`。
        *   `Name` 渲染为 `<Input />`。
        *   `IsActive` 渲染为 `<Switch />`。

### Case 2.4: 表单校验反馈 (Form Validation UI)
**目标**: 验证视觉反馈。
*   **操作**: 在空表单点击 "Save"。
*   **验证**:
    *   必填字段标红。
    *   输入框下方显示 "Field is required"。
    *   Save 未触发 API 调用。

## 3. 准出标准
*   模板可自定义并持久化。
*   运行时表单精确反映设计器状态。
