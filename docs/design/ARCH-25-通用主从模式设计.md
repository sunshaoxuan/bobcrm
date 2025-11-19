# 通用主从模式设计（Universal Master-Detail Pattern）

## 设计原则

### 核心理念
**所有实体（无论简单档案还是复杂单据）都应遵循"列表-详情"（Master-Detail）模式。**

这不是特例，而是**统一的交互范式**：
1. 先展示列表（可筛选、排序、搜索）
2. 点击列表项进入详情（查看/编辑）
3. 保存或取消后返回列表

### 适用范围
- ✅ 简单档案：Customer、Product、Supplier
- ✅ 复杂单据：Order、Invoice、Contract
- ✅ 系统实体：User、Role、Organization
- ✅ 动态实体：所有用户自定义实体

## 模板体系扩展

### 实体模板分组
每个实体应有**至少一组模板**（可有多组，供不同角色/场景使用）：

```
EntityTemplateGroup
  ├── ListTemplate (列表模板)
  │   ├── DisplayFields (显示字段)
  │   ├── Filters (过滤器)
  │   ├── Actions (操作按钮)
  │   └── Layout (布局模式)
  │
  ├── DetailTemplate (详情模板 - View Only)
  │   └── FormLayout (表单布局)
  │
  └── EditTemplate (编辑模板 - Editable)
      └── FormLayout (表单布局)
```

### 列表模板（ListTemplate）

**必需属性：**
- `EntityType`: 实体类型标识
- `DisplayFields`: 列表中显示的字段及其配置
  - `FieldName`: 字段名
  - `DisplayName`: 显示标签
  - `Width`: 列宽
  - `Sortable`: 是否可排序
  - `Formatter`: 格式化器（日期、货币等）
- `DefaultSort`: 默认排序规则
- `PageSize`: 每页显示数量
- `Filters`: 可用的筛选器
  - `FilterType`: 筛选类型（文本搜索、日期范围、下拉选择等）
  - `TargetField`: 目标字段
- `Actions`: 列表级操作
  - `新建`: 创建新记录
  - `导出`: 导出列表数据
  - `批量操作`: 批量删除、批量更新等

**行级操作：**
- `查看`: 进入详情视图（只读）
- `编辑`: 进入详情编辑
- `删除`: 删除当前记录
- 自定义操作：打印、复制、发送等

### 详情模板（DetailTemplate / EditTemplate）

**表单布局：**
- 字段分组（Panels/Sections）
- 字段排列（Flow、Grid、Tabs）
- 验证规则
- 字段依赖关系

**导航：**
- `返回列表`: 返回上一级列表

## 布局模式（Layout Modes）

### 模式 1: 左列表右明细（Left-Right Split）
```
┌─────────────────────────────────────┐
│  Filter Bar                         │
├──────────┬──────────────────────────┤
│          │                          │
│  List    │    Detail Form          │
│  (Keys)  │    (Full Fields)        │
│          │                          │
│  - Code  │    ┌──────────────────┐ │
│  - Name  │    │ Code: C001       │ │
│          │    │ Name: ACME       │ │
│  [+New]  │    │ Email: ...       │ │
│          │    │ ...              │ │
│          │    │ [Save] [Cancel]  │ │
│          │    └──────────────────┘ │
└──────────┴──────────────────────────┘
```

**特点：**
- 左侧：简化列表（仅关键字段如 Code、Name）
- 右侧：完整详情表单
- 无行级操作（点击即选中并显示详情）
- 适用场景：数据量中等，需要快速浏览并编辑

**实现：**
- `LayoutMode="LeftRightSplit"`
- `ListDisplayMode="Simplified"` (仅显示关键字段)
- `DetailDisplayMode="Inline"` (右侧内嵌显示)

### 模式 2: 上列表下明细（Top-Bottom Split）
```
┌─────────────────────────────────────┐
│  Filter Bar   [+New] [Export]       │
├─────────────────────────────────────┤
│  List Table (Full Columns)          │
│  Code │ Name │ Email │ ... │ Action │
│  C001 │ ACME │ ...   │ ... │  Edit  │
│  C002 │ Nova │ ...   │ ... │  Edit  │
├─────────────────────────────────────┤
│  Detail Form (Selected Row)         │
│  ┌────────────────────────────────┐ │
│  │ Code: C001                     │ │
│  │ Name: ACME                     │ │
│  │ ...                            │ │
│  │ [Save] [Cancel]                │ │
│  └────────────────────────────────┘ │
└─────────────────────────────────────┘
```

**特点：**
- 上方：完整列表表格（所有配置的显示字段）
- 下方：详情表单（选中行时显示）
- 有行级操作（编辑、删除等）
- 适用场景：数据量较大，需要完整列表信息

**实现：**
- `LayoutMode="TopBottomSplit"`
- `ListDisplayMode="Full"` (完整字段)
- `DetailDisplayMode="Inline"` (下方内嵌显示)

### 模式 3: 列表+模态详情（List with Modal Detail）
```
┌─────────────────────────────────────┐
│  Filter Bar   [+New] [Export]       │
├─────────────────────────────────────┤
│  List Table (Full Page)             │
│  Code │ Name │ Email │ ... │ Action │
│  C001 │ ACME │ ...   │ ... │  Edit  │
│  C002 │ Nova │ ...   │ ... │  Edit  │
│  C003 │ ...  │ ...   │ ... │  Edit  │
│  ...                                │
└─────────────────────────────────────┘

    (点击 Edit 弹出模态框)
    ┌─────────────────────────┐
    │  Edit Customer   [X]    │
    ├─────────────────────────┤
    │  Code: C001             │
    │  Name: ACME             │
    │  ...                    │
    │  [Save] [Cancel]        │
    └─────────────────────────┘
```

**特点：**
- 全页列表（无分栏）
- 点击编辑弹出模态框
- 保存/取消后自动关闭模态框并刷新列表
- 适用场景：数据量大，详情表单字段较少

**实现：**
- `LayoutMode="ListOnly"`
- `DetailDisplayMode="Modal"` (模态窗口)
- `ModalSize="Medium"` or `"Large"`

### 模式 4: 列表+页面详情（List with Page Detail）
```
列表页面 (/customers)
┌─────────────────────────────────────┐
│  Filter Bar   [+New] [Export]       │
├─────────────────────────────────────┤
│  List Table                         │
│  Code │ Name │ Email │ ... │ Action │
│  C001 │ ACME │ ...   │ ... │  View  │
│  C002 │ Nova │ ...   │ ... │  View  │
└─────────────────────────────────────┘

    (点击 View 跳转到)
    
详情页面 (/customer/{id})
┌─────────────────────────────────────┐
│  ← Back to List                     │
├─────────────────────────────────────┤
│  Customer Detail                    │
│                                     │
│  Code: C001                         │
│  Name: ACME Holdings                │
│  Email: contact@acme.com            │
│  ...                                │
│                                     │
│  [Save] [Cancel]                    │
└─────────────────────────────────────┘
```

**特点：**
- 全页列表（独立路由，如 `/customers`）
- 点击进入详情页面（独立路由，如 `/customer/123`）
- 典型的 SPA 路由模式
- 适用场景：详情表单复杂，需要独立页面展示

**实现：**
- `LayoutMode="ListOnly"`
- `DetailDisplayMode="Page"` (独立页面)
- `DetailRoute="/customer/{id}"`

## 实现规范

### 模板定义扩展

#### FormTemplate 扩展
```csharp
public class FormTemplate
{
    // 现有属性...
    public string EntityType { get; set; }
    public FormTemplateUsageType UsageType { get; set; } // List, Detail, Edit
    public string LayoutJson { get; set; }
    
    // 新增属性
    public LayoutMode LayoutMode { get; set; } // 布局模式
    public string? DetailRoute { get; set; } // 详情页面路由（用于 Page 模式）
    public ModalSize? ModalSize { get; set; } // 模态框大小（用于 Modal 模式）
}

public enum LayoutMode
{
    LeftRightSplit,    // 左列表右明细
    TopBottomSplit,    // 上列表下明细
    ListOnly,          // 仅列表（配合 Modal 或 Page）
}

public enum DetailDisplayMode
{
    Inline,   // 内嵌显示（用于 Split 模式）
    Modal,    // 模态框
    Page      // 独立页面
}
```

### 列表模板 LayoutJson 格式
```json
{
  "displayFields": [
    {
      "fieldName": "Code",
      "displayName": "客户代码",
      "width": 120,
      "sortable": true
    },
    {
      "fieldName": "Name",
      "displayName": "客户名称",
      "width": 200,
      "sortable": true
    }
  ],
  "defaultSort": {
    "field": "Code",
    "direction": "asc"
  },
  "pageSize": 20,
  "filters": [
    {
      "type": "TextSearch",
      "targetFields": ["Code", "Name", "Email"]
    },
    {
      "type": "Dropdown",
      "targetField": "Status",
      "options": ["Active", "Inactive"]
    }
  ],
  "actions": {
    "list": ["New", "Export", "BatchDelete"],
    "row": ["Edit", "Delete", "Print"]
  },
  "layoutMode": "LeftRightSplit",
  "detailDisplayMode": "Inline"
}
```

### 组件架构

#### EntityWorkspace 组件（新）
```razor
@* 统一的实体工作区组件 *@
<EntityWorkspace EntityType="@EntityType"
                 LayoutMode="@LayoutMode"
                 DetailDisplayMode="@DetailDisplayMode">
    @* 内部根据 LayoutMode 渲染不同布局 *@
</EntityWorkspace>
```

**职责：**
- 根据 `LayoutMode` 选择布局容器
- 渲染列表（使用 `ListTemplateHost`）
- 渲染详情（使用 `FormTemplateHost`）
- 协调列表与详情的交互

#### 布局容器组件
- `LeftRightSplitLayout.razor`
- `TopBottomSplitLayout.razor`
- `ListOnlyLayout.razor`

### 页面重构示例

#### 重构前：Customers.razor（简单列表）
```razor
@page "/customers"

<CollectionHeader>...</CollectionHeader>
<ListTemplateHost EntityType="customer" />
```

#### 重构后：Customers.razor（完整主从）
```razor
@page "/customers"

<EntityWorkspace EntityType="customer"
                 LayoutMode="LayoutMode.TopBottomSplit"
                 DetailDisplayMode="DetailDisplayMode.Inline"
                 ListTemplate="customer-list-default"
                 EditTemplate="customer-edit-default" />
```

或使用独立页面模式：
```razor
@page "/customers"
<EntityWorkspace EntityType="customer"
                 LayoutMode="LayoutMode.ListOnly"
                 DetailDisplayMode="DetailDisplayMode.Page"
                 DetailRoute="/customer/{id}" />

@page "/customer/{id}"
<FormTemplateHost EntityType="customer"
                  UsageType="FormTemplateUsageType.Edit"
                  RecordId="@Id"
                  BackRoute="/customers" />
```

## 默认模板生成策略

### DefaultTemplateGenerator 扩展
为每个实体生成**3种模板**：
1. **List Template** - 默认列表模板
2. **Detail Template** - 只读详情模板
3. **Edit Template** - 编辑模板

**默认行为：**
- List: 包含所有字段的列（可后续隐藏）
- Detail/Edit: 包含所有字段的表单
- LayoutMode: 根据字段数量智能选择
  - 字段 ≤ 5: `LeftRightSplit`
  - 字段 5-15: `TopBottomSplit`
  - 字段 > 15: `ListOnly` + `Modal`

## 实施优先级

### Phase 3A: 基础架构（当前）
- [x] Customer 列表模板（已完成）
- [ ] 扩展 FormTemplate 模型
- [ ] 创建 EntityWorkspace 组件
- [ ] 创建布局容器组件

### Phase 3B: 实体补全
- [ ] Customer: 补充详情页面连接
- [ ] 其他简单实体：应用相同模式

### Phase 3C: 系统实体（可选）
- [ ] Users/Roles: 评估是否迁移到统一模式
  - 权限树可以作为特殊 Widget 处理

## 设计优势

1. **统一交互范式**: 所有实体遵循相同的列表-详情模式
2. **灵活布局**: 4种模式适应不同场景和数据复杂度
3. **可配置**: 通过模板配置，无需硬编码
4. **可扩展**: 新实体自动继承此模式
5. **用户一致性**: 降低学习成本，提升体验

## 与现有 Users/Roles 的关系

**Users 和 Roles 页面实际上已经是 LeftRightSplit 模式的手动实现：**
- 左侧：简化列表（用户名/角色名）
- 右侧：详情面板（用户信息/角色权限）

**未来可以：**
1. 保持现有实现（已经符合模式）
2. 或迁移到 EntityWorkspace（使用自定义 Widget 处理权限树）

二者并不冲突，只是实现方式不同。
