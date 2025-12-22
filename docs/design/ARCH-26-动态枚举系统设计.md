# Dynamic Enum System Design (动态枚举系统设计)

## 概述

**动态枚举系统**是低代码/无代码平台的核心能力之一。它允许用户通过界面定义和管理枚举类型，无需修改代码。

### 核心需求

1. **动态定义**：用户可通过UI创建/编辑枚举
2. **持久化存储**：枚举定义存储在数据库
3. **字段引用**：在实体字段定义时可引用动态枚举
4. **多语言支持**：枚举项支持多语言显示名
5. **前后端闭环**：UI组件能自动加载并渲染动态枚举

### 与硬编码枚举的区别

| 特性 | 硬编码枚举 | 动态枚举 |
|------|-----------|----------|
| 定义方式 | C# 代码 | 数据库 + UI |
| 修改方式 | 修改代码 + 编译 + 部署 | 通过管理界面 |
| 适用场景 | 系统级配置（LayoutMode等） | 业务级数据（客户类型、订单状态等） |
| 多语言 | 需hardcode或资源文件 | 内置多语言支持 |

**重要**：系统级枚举（如 `LayoutMode`、`DetailDisplayMode`）应保持硬编码，业务级枚举应使用动态枚举。

## 数据模型设计

### 1. EnumDefinitions (枚举定义表)
| 逻辑字段 | 逻辑类型 | 说明 |
| :--- | :--- | :--- |
| Id | GUID | 逻辑主键 |
| Code | String | 枚举唯一代码（作为外部引用键） |
| DisplayName | **Map<String, String>** | 多语言显示名 |
| Description | **Map<String, String>** | 多语言描述 |
| IsSystem | Boolean | 锁定标记（系统内置不可删除） |
| IsEnabled | Boolean | 状态标记 |
| CreatedAt | DateTime | 创建时间 |
| UpdatedAt | DateTime | 更新时间 |

### 2. EnumOptions (枚举选项表)
| 逻辑字段 | 逻辑类型 | 说明 |
| :--- | :--- | :--- |
| Id | GUID | 逻辑主键 |
| EnumDefinitionId | GUID | 所属枚举定义 ID |
| Value | String | 物理存储值 |
| DisplayName | **Map<String, String>** | 多语言显示名 |
| Description | **Map<String, String>** | 多语言描述 |
| SortOrder | Int | 排序权重 |
| IsEnabled | Boolean | 状态标记 |
| ColorTag | String | UI 颜色标记 (HEX 或 CSS 类名) |
| Icon | String | UI 图标标识 |

### 3. FieldMetadata 逻辑扩展
| 逻辑字段 | 逻辑类型 | 说明 |
| :--- | :--- | :--- |
| EnumDefinitionId | GUID | 引用的枚举 ID (仅在 DataType 为 Enum 时有效) |
| IsMultiSelect | Boolean | 枚举字段是否支持多选 |

### 4. FieldDataType 扩展

```csharp
public enum FieldDataType
{
    // ... 现有类型 ...
    
    /// <summary>
    /// 枚举类型 - 引用动态枚举定义
    /// </summary>
    Enum = 100
}
```

## API 设计

### EnumDefinitionEndpoints

```csharp
// GET /api/enums - 获取所有枚举定义
// GET /api/enums/{id} - 获取单个枚举定义
// GET /api/enums/{id}/options - 获取枚举的所有选项
// POST /api/enums - 创建枚举定义
// PUT /api/enums/{id} - 更新枚举定义
// DELETE /api/enums/{id} - 删除枚举定义（仅非系统枚举）
// PUT /api/enums/{id}/options - 批量更新枚举选项
```

### DTO 设计

```csharp
public class EnumDefinitionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public Dictionary<string, string?> DisplayName { get; set; }
    public Dictionary<string, string?> Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsEnabled { get; set; }
    public List<EnumOptionDto> Options { get; set; }
}

public class EnumOptionDto
{
    public Guid Id { get; set; }
    public string Value { get; set; }
    public Dictionary<string, string?> DisplayName { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
    public string? ColorTag { get; set; }
    public string? Icon { get; set; }
}

public class CreateEnumDefinitionRequest
{
    [Required]
    public string Code { get; set; }
    public Dictionary<string, string?> DisplayName { get; set; }
    public Dictionary<string, string?> Description { get; set; }
    public List<CreateEnumOptionRequest> Options { get; set; }
}

public class CreateEnumOptionRequest
{
    [Required]
    public string Value { get; set; }
    public Dictionary<string, string?> DisplayName { get; set; }
    public int SortOrder { get; set; }
    public string? ColorTag { get; set; }
}
```

## 前端组件设计

### 1. EnumSelector Widget

用于在表单中选择枚举值：

```razor
<EnumSelector EnumCode="customer_type"
              @bind-Value="@_selectedValue"
              MultiSelect="false"
              Placeholder="请选择客户类型" />
```

**职责：**
- 根据 `EnumCode` 动态加载枚举选项
- 支持单选/多选模式
- 支持搜索和过滤
- 显示多语言名称
- 支持颜色标签和图标

### 2. EnumManagement Page

枚举管理界面：

```razor
@page "/system/enums"

<!-- 左侧：枚举列表 -->
<aside>
    <Search />
    <Button OnClick="CreateEnum">新建枚举</Button>
    <EnumList />
</aside>

<!-- 右侧：枚举详情编辑器 -->
<section>
    <EnumBasicInfo />
    <EnumOptionsList />
    <DragDropReorder />
</section>
```

**功能：**
- 创建/编辑/删除枚举定义
- 管理枚举选项（增删改、拖拽排序）
- 多语言编辑器
- 颜色选择器（用于标签）
- 图标选择器

## 使用流程

### 1. 定义枚举（管理员）

1. 进入"系统管理" → "枚举管理"
2. 点击"新建枚举"
3. 输入枚举代码：`customer_type`
4. 输入显示名（多语言）：
   - zh: "客户类型"
   - en: "Customer Type"
   - ja: "顧客タイプ"
5. 添加选项：
   - 值: `ENTERPRISE`, 显示名: "企业客户"
   - 值: `INDIVIDUAL`, 显示名: "个人客户"
   - 值: `PARTNER`, 显示名: "合作伙伴"
6. 保存

### 2. 引用枚举（实体定义）

1. 在实体定义编辑器中添加字段
2. 字段类型选择"枚举"
3. 从下拉列表选择 `customer_type`
4. 配置是否多选
5. 保存实体定义

### 3. 表单渲染（运行时）

1. `FormTemplateHost` 加载实体字段元数据
2. 发现字段类型为 `Enum`，`EnumDefinitionId` 指向 `customer_type`
3. 渲染 `EnumSelector` 组件
4. `EnumSelector` 动态加载 `customer_type` 的所有选项
5. 用户选择后，保存选项的 `Value`（如 "ENTERPRISE"）

### 4. 数据显示（列表）

1. `DataGrid` 显示客户列表
2. 客户类型列显示原始值 "ENTERPRISE"
3. 通过枚举服务解析为显示名"企业客户"
4. 应用颜色标签（如蓝色背景）

## 系统内置枚举

以下枚举建议作为系统内置枚举预先定义：

```json
[
  {
    "code": "boolean_yes_no",
    "options": [
      {"value": "true", "displayName": {"zh": "是", "en": "Yes"}},
      {"value": "false", "displayName": {"zh": "否", "en": "No"}}
    ]
  },
  {
    "code": "gender",
    "options": [
      {"value": "MALE", "displayName": {"zh": "男", "en": "Male"}},
      {"value": "FEMALE", "displayName": {"zh": "女", "en": "Female"}},
      {"value": "OTHER", "displayName": {"zh": "其他", "en": "Other"}}
    ]
  },
  {
    "code": "priority",
    "options": [
      {"value": "LOW", "displayName": {"zh": "低", "en": "Low"}, "colorTag": "green"},
      {"value": "MEDIUM", "displayName": {"zh": "中", "en": "Medium"}, "colorTag": "blue"},
      {"value": "HIGH", "displayName": {"zh": "高", "en": "High"}, "colorTag": "orange"},
      {"value": "URGENT", "displayName": {"zh": "紧急", "en": "Urgent"}, "colorTag": "red"}
    ]
  }
]
```

## 菜单结构

为了方便管理，"枚举管理" (Enum Management) 应置于 "实体管理" (Entity Management) 之前。

- 枚举管理 (SortOrder: 131)
- 实体管理 (SortOrder: 132)

## 实施步骤

### Phase 0: 数据模型 & 迁移
- [ ] 创建 `EnumDefinition` 模型
- [ ] 创建 `EnumOption` 模型
- [ ] 扩展 `FieldMetadata` 添加 `EnumDefinitionId`
- [ ] 扩展 `FieldDataType` 枚举添加 `Enum` 类型
- [ ] 生成数据库迁移
- [ ] 创建系统内置枚举种子数据

### Phase 1: Backend API
- [ ] 创建 `EnumDefinitionService`
- [ ] 实现 CRUD endpoints (`EnumDefinitionEndpoints`)
- [ ] 添加枚举验证逻辑（防止删除被引用的枚举）
- [ ] 实现枚举选项排序和启用/禁用

### Phase 2: Frontend Components
- [ ] 创建 `EnumSelector` 组件（表单输入）
- [ ] 创建 `EnumDisplay` 组件（只读显示）
- [ ] 创建 `EnumManagement` 页面
- [ ] 创建 `EnumOptionEditor` 子组件
- [ ] 集成拖拽排序功能

### Phase 3: 集成到实体系统
- [ ] 更新 `FieldMetadataEditor` 支持选择枚举
- [ ] 更新 `DefaultTemplateGenerator` 为枚举字段生成 `EnumSelector`
- [ ] 更新 `DataGridRuntime` 显示枚举解析值
- [ ] 更新实体发布流程验证枚举引用

### Phase 4: 测试 & 文档
- [ ] API集成测试
- [ ] UI端到端测试
- [ ] 更新用户手册
- [ ] 创建枚举使用示例

## 优势

1. **灵活性**：业务人员可自行维护枚举，无需开发介入
2. **一致性**：所有枚举统一管理，避免hardcode导致的不一致
3. **多语言**：内置多语言支持，无需额外配置
4. **可追溯**：枚举变更有审计记录
5. **性能**：枚举数据可缓存，查询效率高

## 注意事项

1. **系统枚举保护**：系统级枚举（`IsSystem=true`）不可删除
2. **引用检查**：删除枚举前需检查是否被字段引用
3. **值不可变**：枚举选项的 `Value` 一旦创建不应修改（影响历史数据）
4. **缓存策略**：枚举数据应缓存在前端，减少API调用
5. **迁移路径**：现有hardcode枚举需逐步迁移到动态枚举
