# TEST-03: Batch 2 应用组装验证

> **关联计划**: [TEST-01](file:///c:/workspace/bobcrm/docs/test-cases/TEST-01-v1.0-全功能综合测试矩阵.md)
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
    *   'List Price'标签显示 (而非 'Price')。

### Case 2.4: 表单校验反馈 (Form Validation UI)
**目标**: 验证视觉反馈。
*   **操作**: 在空表单点击 "Save"。
*   **验证**:
    *   必填字段标红。
    *   输入框下方显示 "Field is required"。
    *   Save 未触发 API 调用。

### Case 2.5: 高阶控件专项 (High-Order Controls) - [UE-007/008/009]
**目标**: 验证复杂控件的交互逻辑。

#### 2.5.1 Lookup (UE-007)
*   **操作**: 点击 `CategoryId` 放大镜图标。
*   **验证**:
    1.  弹出模态框 (Modal)。
    2.  列表加载 Category 数据。
    3.  点击一行 -> 模态框关闭 -> 输入框显示 "Hardware" (Name) -> 隐藏域存储 GUID。

#### 2.5.2 DatePicker (UE-008)
*   **操作**: 点击 `ValidFrom` 日期控件。
*   **验证**:
    1.  弹出日历面板。
    2.  选值 `2025-01-01` -> 输入框显示格式正确 (如 `yyyy-MM-dd`)。
    3.  (可选) 验证无法选择小于 `MinDate` 的日期（若配置）。

#### 2.5.3 Switch (UE-009)
*   **操作**: 切换 `IsActive` 开关 3 次 (On -> Off -> On)。
*   **验证**:
    1.  UI 动画流畅切换。
    2.  Model 值实时变更为 `true/false` (通过 Console 或 Vue DevTools 观测)。
    3.  提交时 Payload 正确。

## 3. 准出标准
*   模板可自定义并持久化。
*   运行时表单精确反映设计器状态。
