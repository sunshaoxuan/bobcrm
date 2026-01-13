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
### Case 2.5: 控件全集验证 (Component Gallery) - [UE-Gallery]
**目标**: 验证系统涉及的所有 9 种基础与高阶控件的交互逻辑。

| ID | 控件类型 | 测试操作 (Input) | 验证点 (Assertion) |
|---|---|---|---|
| **UE-007** | **Lookup** | 点击放大镜 -> 弹窗搜索 "Hardware" -> 选中 -> 回填 | 1. 隐藏域存储 GUID <br> 2. 显示文本无闪烁 <br> 3. 再次打开回显选中态 |
| **UE-008** | **DatePicker** | 选值 `2025-12-31` -> 清空 -> 再选 | 1. 格式严格符合 `yyyy-MM-dd` <br> 2. 清空后 Model 为 null |
| **UE-009** | **Switch** | 快速切换 On/Off (防抖测试) | 1. 动画无卡顿 <br> 2. 最终值与后端 Payload 一致 |
| **UE-010** | **Input** | 输入特殊字符 `<script>` | 1. 显示正常 <br> 2. 提交后后端未被转义(或按策略处理) |
| **UE-011** | **TextArea** | 输入多行文本 (包含换行符 `\n`) | 1. 高度自适应或滚动条出现 <br> 2. 详情页回显保留换行格式 |
| **UE-012** | **InputNumber** | 输入非数字字符 -> 输入超大数值 -> 精度测试 | 1. 非法字符被阻断 <br> 2. 精度 (`Scale=2`) 自动截断或四舍五入 |
| **UE-013** | **Select** | 点击下拉 -> 键盘上下键选择 | 1. 下拉面板不被弹窗遮挡 (Z-Index) <br> 2. 选中值正确高亮 |
| **UE-014** | **RadioGroup** | 切换选项 A -> B | 1. 互斥性验证 (A 自动取消) <br> 2. 必填校验时未选标红 |
| **UE-015** | **Checkbox** | 点击勾选 -> 再次点击取消 | 1. `bool` 值正确反转 <br> 2. Disabled 态下无法点击 |

**执行要求**:
*   必须在 **Edit** (表单态) 和 **View** (只读态) 下分别运行以上所有测试。
*   View 态下所有控件应渲染为纯文本或 Disabled 样式，不可交互。

## 3. 准出标准
*   模板可自定义并持久化。
*   运行时表单精确反映设计器状态。
