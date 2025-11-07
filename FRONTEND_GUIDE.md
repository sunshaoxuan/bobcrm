# BobCRM 动态实体系统 - 前端使用指南

## 概述

本文档介绍如何使用 BobCRM 的动态实体系统前端界面。该系统允许开发者和管理员通过可视化界面定义、配置、发布和管理动态实体。

## 架构说明

### 技术栈
- **Blazor Server**: 服务器端 Blazor 应用，使用 SignalR 实现实时交互
- **AntDesign Blazor**: UI 组件库，提供丰富的企业级组件
- **InteractiveServer 渲染模式**: Blazor 8 的交互式服务器渲染模式

### 核心组件

#### 1. 数据传输对象 (DTOs)
- `EntityDefinitionDto`: 实体定义的完整数据结构
- `FieldMetadataDto`: 字段元数据
- `EntityInterfaceDto`: 实体接口配置
- `DynamicEntityQueryRequest/Response`: 动态实体查询请求和响应

位置: `src/BobCrm.App/Models/`

#### 2. API 服务层
- `EntityDefinitionService`: 实体定义 CRUD 和发布操作
- `DynamicEntityService`: 动态实体数据操作

位置: `src/BobCrm.App/Services/`

#### 3. 页面组件
- `EntityDefinitions.razor`: 实体定义列表页
- `EntityDefinitionEdit.razor`: 实体定义创建/编辑页
- `EntityDefinitionPublish.razor`: 实体发布页
- `DynamicEntityData.razor`: 动态实体数据管理页
- `EntityDefinitionsTable.razor`: 共享的实体定义表格组件

位置: `src/BobCrm.App/Components/Pages/` 和 `src/BobCrm.App/Components/Shared/`

## 快速开始

### 访问实体定义管理

在浏览器中访问以下 URL:

```
https://localhost:5100/entity-definitions
```

### 基本工作流程

1. **创建实体定义** → 2. **编辑字段和配置** → 3. **发布实体** → 4. **管理数据**

## 功能详解

### 1. 实体定义列表页 (`/entity-definitions`)

#### 功能特性
- 分标签显示不同类型的实体（全部、系统、自定义、草稿）
- 支持刷新列表
- 快速创建新实体
- 查看实体状态、来源、字段数量

#### 操作按钮
- **新建**: 创建新的实体定义
- **编辑**: 修改现有实体定义
- **发布**: 发布草稿或修改后的实体
- **删除**: 删除自定义实体（系统实体无法删除）

#### 状态说明
- **草稿** (Draft): 新创建但未发布的实体
- **已发布** (Published): 已成功发布的实体
- **已修改** (Modified): 已发布但又被修改的实体

#### 来源类型
- **System**: 系统内置实体（如 Customer, Contact 等）
- **Custom**: 用户自定义实体

### 2. 创建/编辑实体定义 (`/entity-definitions/create` 或 `/entity-definitions/edit/{id}`)

#### 基本信息配置

##### 命名空间 (Namespace)
- 定义实体的 C# 命名空间
- 示例: `BobCrm.Domain.Custom`
- **注意**: 创建后不可修改

##### 实体名称 (EntityName)
- C# 类名
- 示例: `Product`, `Invoice`
- **注意**: 创建后不可修改

##### 显示名Key (DisplayNameKey)
- 多语言翻译键
- 示例: `ENTITY_PRODUCT`
- 用于 I18n 系统显示实体名称

##### 描述Key (DescriptionKey)
- 实体描述的多语言键
- 示例: `ENTITY_PRODUCT_DESC`

##### 结构类型 (StructureType)
- **单一实体** (Single): 独立的实体表
- **主从实体** (MasterDetail): 一对多关系
- **主从孙实体** (MasterDetailGrandchild): 三层级关系
- **注意**: 创建后不可修改

#### 接口配置

可选择实体实现的接口，每个接口会自动添加相应的字段：

##### Base (IEntity)
- **必选接口**
- 字段: `Id` (int, 主键)

##### Archive (IArchive)
- 字段: `Code` (string), `Name` (string)
- 用于需要编码和名称的档案类实体

##### Audit (IAuditable)
- 字段: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- 自动记录创建和修改信息

##### Version (IVersioned)
- 字段: `Version` (int)
- 用于实现乐观并发控制

##### TimeVersion (ITimeVersioned)
- 字段: `TimeVersion` (DateTime)
- 基于时间戳的版本控制

#### 字段定义

点击"添加字段"按钮可添加自定义字段：

##### 字段属性
- **属性名** (PropertyName): C# 属性名，如 `Price`, `Description`
- **显示名Key** (DisplayNameKey): 多语言键，如 `FIELD_PRICE`
- **数据类型** (DataType):
  - `String`: 字符串（需指定长度）
  - `Integer`: 32位整数
  - `Long`: 64位整数
  - `Decimal`: 小数（需指定精度和小数位）
  - `Boolean`: 布尔值
  - `DateTime`: 日期时间
  - `Date`: 日期
  - `Text`: 长文本（无长度限制）
  - `Guid`: 全局唯一标识符

- **长度** (Length): 对于 String 类型，指定最大长度
- **精度** (Precision): 对于 Decimal 类型，总位数
- **小数位** (Scale): 对于 Decimal 类型，小数位数
- **必填** (IsRequired): 是否为必填字段
- **默认值** (DefaultValue): 字段默认值（如 `0`, `NOW`, `NEWID`）
- **排序** (SortOrder): 字段显示顺序

### 3. 实体发布页 (`/entity-definitions/publish/{id}`)

#### 实体信息概览
显示实体的基本信息：名称、命名空间、完整类型名、状态、来源、字段数、接口数等。

#### DDL 脚本标签

##### DDL 预览
- 显示将要执行的 SQL DDL 语句
- 支持刷新预览
- 包含 CREATE TABLE、ALTER TABLE 等操作

##### DDL 历史
- 查看历史 DDL 执行记录
- 显示执行状态（成功/失败）
- 查看脚本类型、创建时间、执行时间、创建人

#### 生成代码标签
- 点击"生成代码"按钮查看生成的 C# 实体类代码
- 包含所有字段、接口实现、特性标注
- 可用于验证代码生成是否正确

#### 编译状态标签
- 点击"编译"按钮编译生成的代码
- 显示编译结果：成功或失败
- 如果成功，显示程序集名称和加载的类型
- 如果失败，显示详细的编译错误（错误代码、行号、列号、错误消息）

#### 发布按钮
- 仅对状态为"草稿"或"已修改"的实体显示
- 执行完整的发布流程：
  1. 代码生成
  2. 代码编译
  3. DDL 脚本生成
  4. DDL 执行
  5. 更新实体状态为"已发布"

### 4. 动态实体数据管理页 (`/dynamic-entities/{fullTypeName}`)

#### 功能特性
- 查询和显示动态实体的数据
- 支持分页（每页 10 条记录）
- 显示前 6 列数据（避免表格过宽）
- 创建、编辑、删除实体数据

#### 当前限制
- 创建和编辑功能需要根据具体字段定义实现动态表单
- 示例代码中标记为 TODO，需要扩展开发

## 开发指南

### 扩展动态实体数据表单

`DynamicEntityData.razor` 页面提供了基础框架，但创建和编辑表单需要根据实体定义动态生成。

#### 实现步骤

1. **获取实体定义**

```csharp
// 在 OnInitializedAsync 中加载实体定义
private EntityDefinitionDto? _entityDefinition;

protected override async Task OnInitializedAsync()
{
    await LoadEntity();

    // 加载实体定义以获取字段信息
    var allEntities = await EntityDefService.GetAllAsync();
    _entityDefinition = allEntities.FirstOrDefault(e => e.FullTypeName == FullTypeName);
}
```

2. **动态生成表单字段**

```csharp
private void ShowCreateModal()
{
    _editingEntity = new Dictionary<string, object>();

    // 根据字段定义初始化默认值
    if (_entityDefinition != null)
    {
        foreach (var field in _entityDefinition.Fields)
        {
            if (!string.IsNullOrEmpty(field.DefaultValue))
            {
                _editingEntity[field.PropertyName] = ParseDefaultValue(field);
            }
        }
    }

    _modalVisible = true;
}

private object ParseDefaultValue(FieldMetadataDto field)
{
    // 根据字段类型解析默认值
    return field.DataType switch
    {
        FieldDataType.Integer => int.TryParse(field.DefaultValue, out var i) ? i : 0,
        FieldDataType.Decimal => decimal.TryParse(field.DefaultValue, out var d) ? d : 0m,
        FieldDataType.Boolean => bool.TryParse(field.DefaultValue, out var b) ? b : false,
        FieldDataType.DateTime => field.DefaultValue?.ToUpper() == "NOW" ? DateTime.Now : DateTime.MinValue,
        _ => field.DefaultValue ?? string.Empty
    };
}
```

3. **在模态框中渲染表单**

```razor
<Modal Title="@(_isNew ? "新建" : "编辑")" @bind-Visible="@_modalVisible" OnOk="SaveEntity" OnCancel="CancelEdit">
    <Form Model="@_editingEntity">
        @if (_entityDefinition != null)
        {
            @foreach (var field in _entityDefinition.Fields.OrderBy(f => f.SortOrder))
            {
                <FormItem Label="@field.DisplayNameKey">
                    @switch (field.DataType)
                    {
                        case FieldDataType.String:
                        case FieldDataType.Text:
                            <Input @bind-Value="@GetStringValue(field.PropertyName)" />
                            break;
                        case FieldDataType.Integer:
                        case FieldDataType.Long:
                            <InputNumber @bind-Value="@GetIntValue(field.PropertyName)" />
                            break;
                        case FieldDataType.Decimal:
                            <InputNumber @bind-Value="@GetDecimalValue(field.PropertyName)" />
                            break;
                        case FieldDataType.Boolean:
                            <Switch @bind-Value="@GetBoolValue(field.PropertyName)" />
                            break;
                        case FieldDataType.DateTime:
                        case FieldDataType.Date:
                            <DatePicker @bind-Value="@GetDateTimeValue(field.PropertyName)" />
                            break;
                    }
                </FormItem>
            }
        }
    </Form>
</Modal>

@code {
    private string GetStringValue(string propertyName)
    {
        return _editingEntity.TryGetValue(propertyName, out var value) ? value?.ToString() ?? "" : "";
    }

    private int GetIntValue(string propertyName)
    {
        return _editingEntity.TryGetValue(propertyName, out var value) ? Convert.ToInt32(value) : 0;
    }

    // 其他类型的 getter/setter 方法...
}
```

### 添加自定义验证

在保存实体时添加验证逻辑：

```csharp
private async Task SaveEntity()
{
    // 验证必填字段
    if (_entityDefinition != null)
    {
        foreach (var field in _entityDefinition.Fields.Where(f => f.IsRequired))
        {
            if (!_editingEntity.ContainsKey(field.PropertyName) ||
                string.IsNullOrEmpty(_editingEntity[field.PropertyName]?.ToString()))
            {
                await MessageService.Error($"字段 {field.DisplayNameKey} 为必填项");
                return;
            }
        }
    }

    try
    {
        if (_isNew)
        {
            await DynamicEntityService.CreateAsync(FullTypeName, _editingEntity);
        }
        else
        {
            var id = Convert.ToInt32(_editingEntity["Id"]);
            await DynamicEntityService.UpdateAsync(FullTypeName, id, _editingEntity);
        }

        await MessageService.Success("保存成功");
        _modalVisible = false;
        await LoadEntity();
    }
    catch (Exception ex)
    {
        await MessageService.Error($"保存失败: {ex.Message}");
    }
}
```

### 创建自定义实体页面

如果需要为特定实体创建定制的管理页面：

1. 创建新的 Razor 组件 `CustomEntityPage.razor`

```razor
@page "/custom-entities/product"
@rendermode RenderMode.InteractiveServer
@inject BobCrm.App.Services.DynamicEntityService DynamicEntityService
@inject BobCrm.App.Services.EntityDefinitionService EntityDefService

<PageHeader Title="产品管理">
    <PageHeaderExtra>
        <Button Type="@ButtonType.Primary" OnClick="ShowCreateModal">
            <Icon Type="plus" /> 新建产品
        </Button>
    </PageHeaderExtra>
</PageHeader>

<Card>
    <Table TItem="ProductDto" DataSource="@_products" Loading="@_loading">
        <PropertyColumn Property="p => p.Code" Title="产品编码" />
        <PropertyColumn Property="p => p.Name" Title="产品名称" />
        <PropertyColumn Property="p => p.Price" Title="价格" />
        <PropertyColumn Property="p => p.Category" Title="分类" />
        <ActionColumn Title="操作">
            <Space>
                <SpaceItem>
                    <Button Size="@ButtonSize.Small" OnClick="() => EditProduct(context)">
                        编辑
                    </Button>
                </SpaceItem>
                <SpaceItem>
                    <Popconfirm Title="确定删除？" OnConfirm="() => DeleteProduct(context.Id)">
                        <Button Danger Size="@ButtonSize.Small">删除</Button>
                    </Popconfirm>
                </SpaceItem>
            </Space>
        </ActionColumn>
    </Table>
</Card>

@code {
    private List<ProductDto> _products = new();
    private bool _loading = false;

    private class ProductDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    // 实现加载、创建、编辑、删除逻辑...
}
```

### 集成 I18n 多语言

为新实体添加多语言支持：

1. 在后端添加翻译资源（`src/BobCrm.Api/Resources/i18n.*.json`）

```json
{
  "ENTITY_PRODUCT": "产品",
  "ENTITY_PRODUCT_DESC": "产品信息管理",
  "FIELD_PRODUCT_CODE": "产品编码",
  "FIELD_PRODUCT_NAME": "产品名称",
  "FIELD_PRODUCT_PRICE": "价格"
}
```

2. 在前端页面中使用

```razor
@inject BobCrm.App.Services.I18nService I18n

<PageHeader Title="@I18n.T("ENTITY_PRODUCT")">
    ...
</PageHeader>
```

## 服务注册

所有服务已在 `Program.cs` 中注册：

```csharp
// 动态实体系统服务
builder.Services.AddScoped<BobCrm.App.Services.EntityDefinitionService>();
builder.Services.AddScoped<BobCrm.App.Services.DynamicEntityService>();
```

## 安全性说明

- 所有 API 调用通过 `AuthService` 进行，自动附加认证令牌
- 使用 `AuthChecker` 组件确保页面需要登录才能访问
- 系统实体的关键属性（命名空间、实体名称、结构类型）在创建后不可修改
- 系统实体无法被删除，仅自定义实体可删除

## 故障排查

### 问题: 实体定义列表为空
**解决方案**:
1. 检查后端 API 是否正常运行
2. 查看浏览器控制台是否有错误
3. 确认用户已登录并有权限访问

### 问题: 发布实体失败
**解决方案**:
1. 查看编译状态标签，检查是否有编译错误
2. 检查字段定义是否合法（如 String 类型必须指定长度）
3. 确认数据库连接正常

### 问题: 动态实体数据无法加载
**解决方案**:
1. 确认实体已成功发布（状态为"已发布"）
2. 检查实体的 FullTypeName 是否正确
3. 查看后端日志，确认实体类型已加载

## 最佳实践

1. **命名规范**
   - 实体名称使用 PascalCase（如 `Product`, `SalesOrder`）
   - 字段名称使用 PascalCase（如 `ProductCode`, `UnitPrice`）
   - 多语言键使用 UPPER_SNAKE_CASE（如 `ENTITY_PRODUCT`, `FIELD_PRICE`）

2. **接口选择**
   - 档案类实体（如产品、客户）应实现 Archive 接口
   - 需要审计跟踪的实体应实现 Audit 接口
   - 有并发更新需求的实体应实现 Version 或 TimeVersion 接口

3. **字段设计**
   - 合理设置字段排序，影响表单显示顺序
   - 为重要字段设置默认值，提升用户体验
   - String 字段长度设置需考虑实际业务需求

4. **发布流程**
   - 先使用"生成代码"预览代码
   - 再使用"编译"确认无错误
   - 最后使用"发布"执行 DDL

## 相关文档

- [后端实现文档](./IMPLEMENTATION.md)
- [架构设计文档](./ARCHITECTURE.md)
- [测试文档](./TESTING.md)
- [API 文档](./API.md)

## 支持

如有问题或建议，请联系开发团队或提交 Issue。
