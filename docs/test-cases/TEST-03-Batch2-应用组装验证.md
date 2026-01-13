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
**目标**: 验证 `WidgetRegistry` 中定义的所有 18 种基础与数据控件的交互逻辑。

#### A. 基础控件 (Basic Inputs) - [UE-010~024]
| ID | 控件类型 (Registry) | C# Type | 验证操作 | 预期结果 |
|---|---|---|---|---|
| **UE-010** | `textbox` / `text` | `TextboxWidget` | 输入 "Hello" 与 `<script>` | Model 更新，XSS 被转义或拦截 |
| **UE-011** | `details_text` | `TextAreaWidget` | 输入多行文本 (含回车) | 高度自适应，详情页回显保留换行 |
| **UE-012** | `number` | `NumberWidget` | 输入 `123.456` (Scale=2) | 自动截断为 `123.46`，且 Model 类型正确 |
| **UE-013** | `switch` | `SwitchWidget` | 切换开关状态 (Bool) | 动画流畅，绑定 true/false |
| **UE-014** | `checkbox` | `CheckboxWidget` | 勾选 -> 取消 | bool 值在 true/false 间切换 |
| **UE-015** | `date`/`calendar` | `CalendarWidget` | 选日期 -> 选时间 | 依据 Format (`yyyy-MM-dd HH:mm`) 决定展示 |
| **UE-016** | `button` | `ButtonWidget` | 点击按钮 | 触发配置的 Action (如 Save/Cancel) |
| **UE-017** | `label` | `LabelWidget` | 配置静态文本 | 仅显示文本，无 Input 元素 |
| **UE-020** | `select` | `SelectWidget` | 下拉选择 "Option B" | 隐藏域值更新，Label 回显正确 |
| **UE-021** | `radio` | `RadioWidget` | 切换 Radio B | 互斥（A 自动取消），必填校验生效 |
| **UE-022** | `listbox` | `ListboxWidget` | 列表选择 (多选模式) | Model 绑定为 `List<string>` |
| **UE-023** | `enumselector` | `SelectWidget` | 绑定到 System Enum | 自动加载枚举定义，无需手动配置 Options |
| **UE-024** | `lookup` | `LookupWidget` | 点击放大镜 -> 弹窗搜索 | ID 回填，Name 显示正确，二次打开选中态 |

#### B. 数据控件 (Complex Data) - [UE-030~034]
| ID | 控件类型 (Registry) | C# Type | 验证操作 | 预期结果 |
|---|---|---|---|---|
| **UE-030** | `datagrid` | `DataGridWidget` | 1:N 子表添加行 | 弹出子表单模态框，保存后主表增加一行 |
| **UE-031** | `subform` | `SubFormWidget` | 1:1 级联编辑 | 直接在当前页面渲染子实体字段 (无弹窗) |
| **UE-032** | `orgtree` | `OrganizationTreeWidget` | 展开部门树 -> 勾选 | 选中部门 ID 列表回填 |
| **UE-033** | `permtree` | `RolePermissionTreeWidget` | 勾选功能节点 | 级联勾选（选子自动选父） |
| **UE-034** | `userrole` | `UserRoleAssignmentWidget` | 穿梭框 (Transfer) 分配 | 左侧选人 -> 移入右侧 -> Model 更新 |

#### C. 布局容器 (Layout Containers) - [UE-040~045]
| ID | 控件类型 (Registry) | C# Type | 验证操作 | 预期结果 |
|---|---|---|---|---|
| **UE-040** | `grid` | `GridWidget` | 配置 3 列 (Span 8) | 内部子组件横向排列，间距 (Gap) 正确渲染 |
| **UE-041** | `card` | `CardWidget` | 拖入组件至 Body | 卡片标题可见，子组件渲染在内容区内 |
| **UE-042** | `tabbox` | `TabContainerWidget` | 切换 Tab 2 | Tab 2 内容可见，Tab 1 内容隐藏 |
| **UE-043** | `section` | `SectionWidget` | 折叠/展开 | 标题栏点击可切换内容区的可见性 |
| **UE-044** | `panel` | `PanelWidget` | 设置背景色/边框 | CSS 样式正确应用，作为逻辑分组容器生效 |
| **UE-045** | `frame` | `FrameWidget` | 嵌入外部内容或边框 | 渲染为带边框的独立区块 (类似 GroupBox) |

#### D. 交互行为 (Interaction) - [UE-050]
| ID | 特性 | 验证操作 | 预期结果 |
|---|---|---|---|
| **UE-050** | **Real-time Validation** | 输入非法值 (如 Email 格式错误) | 焦点离开瞬间出现红字提示，Button 被禁用(若配置) |

## 3. 准出标准
*   模板可自定义并持久化。
*   运行时表单精确反映设计器状态。
