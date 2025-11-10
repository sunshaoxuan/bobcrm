# 元数据多语言机制（Metadata I18n）技术文档

## 概述

元数据多语言机制是BobCRM系统中用于管理实体定义和字段元数据的多语言支持功能。该机制允许用户在创建实体和字段时直接输入多语言文本，系统自动生成资源Key并保存到多语言资源表，实现了元数据的国际化。

## 设计原则

1. **用户友好**：用户输入实际的多语言文本，而不是抽象的Key
2. **系统自动化**：Key由系统自动生成，遵循统一的命名规范
3. **OOP设计**：遵循面向对象设计原则，服务层高内聚低耦合
4. **数据一致性**：多语言资源与元数据定义保持同步

## 架构设计

### 1. 持久化层

#### 1.1 数据库表结构

**复用现有的 LocalizationResources 表：**

```sql
CREATE TABLE LocalizationResources (
    Key VARCHAR(256) PRIMARY KEY,  -- 多语言资源Key
    ZH TEXT,                        -- 中文文本
    JA TEXT,                        -- 日语文本
    EN TEXT                         -- 英文文本
);
```

#### 1.2 Key命名规范

- **实体显示名**：`ENTITY_{EntityName}`
  - 示例：`ENTITY_PRODUCT`（产品实体）

- **实体描述**：`ENTITY_{EntityName}_DESC`
  - 示例：`ENTITY_PRODUCT_DESC`（产品实体描述）

- **字段显示名**：`FIELD_{EntityName}_{FieldName}`
  - 示例：`FIELD_PRODUCT_PRICE`（产品实体的价格字段）

#### 1.3 实体定义表

EntityDefinition 表保留 DisplayNameKey 和 DescriptionKey 字段，这些字段存储自动生成的Key：

```csharp
public class EntityDefinition
{
    // ... 其他字段
    public string DisplayNameKey { get; set; }  // 自动生成的Key
    public string? DescriptionKey { get; set; }  // 自动生成的Key
}
```

### 2. 领域模型

#### 2.1 多语言文本DTO

**前端DTO（BobCrm.App）：**

```csharp
public class MultilingualTextDto
{
    public string? ZH { get; set; }
    public string? JA { get; set; }
    public string? EN { get; set; }

    public bool HasValue() =>
        !string.IsNullOrWhiteSpace(ZH) ||
        !string.IsNullOrWhiteSpace(JA) ||
        !string.IsNullOrWhiteSpace(EN);

    public string? GetValue(string lang) => lang?.ToLowerInvariant() switch
    {
        "zh" => ZH,
        "ja" => JA,
        "en" => EN,
        _ => JA
    };
}
```

**后端Record（BobCrm.Api）：**

```csharp
public record MultilingualText
{
    public string? ZH { get; init; }
    public string? JA { get; init; }
    public string? EN { get; init; }
}
```

#### 2.2 创建实体定义请求

```csharp
public record CreateEntityDefinitionDto
{
    public string Namespace { get; init; } = "BobCrm.Domain.Custom";
    public string EntityName { get; init; } = string.Empty;
    public MultilingualText? DisplayName { get; init; }
    public MultilingualText? Description { get; init; }
    // ... 其他字段
}
```

### 3. 服务层

#### 3.1 MetadataI18nService

核心服务类，负责元数据多语言资源的管理：

```csharp
public class MetadataI18nService
{
    // 生成Key
    public string GenerateEntityDisplayNameKey(string entityName);
    public string GenerateEntityDescriptionKey(string entityName);
    public string GenerateFieldDisplayNameKey(string entityName, string fieldName);

    // 保存/更新资源
    public Task<bool> SaveOrUpdateMetadataI18nAsync(
        string key, string? zh, string? ja, string? en);
    public Task<bool> SaveOrUpdateBatchAsync(
        Dictionary<string, (string? zh, string? ja, string? en)> resources);

    // 删除资源
    public Task<bool> DeleteMetadataI18nAsync(string key);
    public Task<int> DeleteEntityRelatedI18nAsync(
        string entityName, IEnumerable<string>? fieldNames = null);

    // 查询资源
    public Task<(string? zh, string? ja, string? en)?> GetMetadataI18nAsync(string key);
    public Task<bool> ExistsAsync(string key);
}
```

**服务注册：**

```csharp
// Program.cs
builder.Services.AddScoped<BobCrm.Api.Services.MetadataI18nService>();
```

### 4. API端点

#### 4.1 创建实体定义

**端点：** `POST /api/entity-definitions`

**请求体：**

```json
{
  "namespace": "BobCrm.Domain.Custom",
  "entityName": "Product",
  "displayName": {
    "zh": "产品",
    "ja": "商品",
    "en": "Product"
  },
  "description": {
    "zh": "管理产品信息",
    "ja": "商品情報を管理します",
    "en": "Manage product information"
  },
  "structureType": "Single",
  "interfaces": ["Base"],
  "fields": [
    {
      "propertyName": "Price",
      "displayName": {
        "zh": "价格",
        "ja": "価格",
        "en": "Price"
      },
      "dataType": "Decimal",
      "precision": 18,
      "scale": 2,
      "isRequired": true
    }
  ]
}
```

**处理流程：**

1. 验证多语言显示名（至少提供一种语言）
2. 生成实体显示名Key：`ENTITY_PRODUCT`
3. 保存实体显示名多语言资源
4. 如果提供了描述，生成描述Key：`ENTITY_PRODUCT_DESC`
5. 保存描述多语言资源
6. 为每个字段生成Key：`FIELD_PRODUCT_PRICE`
7. 保存字段多语言资源
8. 创建实体定义记录
9. 清除ILocalization缓存

### 5. 前端组件

#### 5.1 MultilingualInput 组件

多语言输入组件，提供标签页切换不同语言的输入：

```razor
<MultilingualInput @bind-Value="@_model.DisplayName"
                 PlaceholderJA="商品"
                 PlaceholderZH="产品"
                 PlaceholderEN="Product" />
```

**特点：**
- 标签页布局，支持日语、中文、英语
- 双向数据绑定
- 自定义占位符

#### 5.2 EntityDefinitionEdit 页面

实体定义编辑页面，集成多语言输入：

```razor
<FormItem Label="显示名">
    <MultilingualInput @bind-Value="@_model.DisplayName"
                     PlaceholderJA="商品"
                     PlaceholderZH="产品"
                     PlaceholderEN="Product" />
    <div class="ant-form-text">
        请提供实体的多语言显示名称（至少一种语言）
    </div>
</FormItem>
```

## 工作流程

### 创建实体定义的完整流程

```
┌─────────────────────────────────────────────────────────────┐
│ 1. 用户在前端输入多语言文本                                    │
│    - 实体名：Product                                          │
│    - 显示名：{zh: "产品", ja: "商品", en: "Product"}          │
│    - 字段名：Price                                            │
│    - 字段显示名：{zh: "价格", ja: "価格", en: "Price"}        │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. 前端发送请求到后端API                                      │
│    POST /api/entity-definitions                              │
│    { displayName: {...}, fields: [...] }                     │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. 后端验证多语言文本                                          │
│    - 至少提供一种语言的文本                                    │
│    - 实体名不为空                                             │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. MetadataI18nService 生成Key                               │
│    - displayNameKey = "ENTITY_PRODUCT"                       │
│    - fieldDisplayNameKey = "FIELD_PRODUCT_PRICE"             │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 5. 保存多语言资源到 LocalizationResources 表                  │
│    INSERT INTO LocalizationResources                         │
│    (Key, ZH, JA, EN) VALUES                                  │
│    ('ENTITY_PRODUCT', '产品', '商品', 'Product')              │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 6. 创建实体定义记录                                           │
│    INSERT INTO EntityDefinitions                             │
│    (EntityName, DisplayNameKey, ...)                         │
│    VALUES ('Product', 'ENTITY_PRODUCT', ...)                 │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 7. 清除 ILocalization 缓存                                    │
│    _localization.InvalidateCache()                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 8. 返回成功响应                                               │
│    201 Created                                               │
└─────────────────────────────────────────────────────────────┘
```

## 最佳实践

### 1. Key生成规范

- **一致性**：所有Key都使用大写字母和下划线
- **可读性**：Key名称应该清晰表达其含义
- **唯一性**：每个Key在系统中是唯一的

### 2. 多语言文本验证

```csharp
// 至少提供一种语言的文本
if (dto.DisplayName == null ||
    (string.IsNullOrWhiteSpace(dto.DisplayName.ZH) &&
     string.IsNullOrWhiteSpace(dto.DisplayName.JA) &&
     string.IsNullOrWhiteSpace(dto.DisplayName.EN)))
{
    return Results.BadRequest(new { error = "显示名至少需要提供一种语言的文本" });
}
```

### 3. 缓存失效

每次修改多语言资源后，必须清除ILocalization缓存：

```csharp
await _db.SaveChangesAsync();
_localization.InvalidateCache();
```

### 4. 事务处理

建议将多语言资源保存和实体定义创建放在同一个事务中：

```csharp
using var transaction = await _db.Database.BeginTransactionAsync();
try
{
    // 保存多语言资源
    await metadataI18nService.SaveOrUpdateMetadataI18nAsync(...);

    // 创建实体定义
    _db.EntityDefinitions.Add(definition);
    await _db.SaveChangesAsync();

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## 系统设置

### 默认语言

系统默认语言配置在 SystemSettings 表中：

```csharp
public class SystemSettings
{
    public string DefaultLanguage { get; set; } = "ja";  // 默认日语
}
```

前端I18nService也使用日语作为默认语言：

```csharp
public string CurrentLang { get; private set; } = "ja";
```

## 测试要点

### 1. 单元测试

- MetadataI18nService 的Key生成逻辑
- 多语言资源的CRUD操作
- 缓存失效机制

### 2. 集成测试

- 创建实体定义的完整流程
- 多语言资源与实体定义的同步
- 并发访问时的数据一致性

### 3. UI测试

- 多语言输入组件的交互
- 表单验证提示
- 数据提交和错误处理

## 注意事项

1. **向后兼容**：现有的DisplayNameKey字段保留，新的多语言机制与旧数据兼容

2. **权限控制**：元数据多语言资源的修改需要适当的权限验证

3. **数据迁移**：如果需要迁移现有数据，需要编写脚本将旧的Key转换为多语言资源

4. **性能优化**：使用ILocalization的缓存机制，避免频繁查询数据库

5. **错误处理**：保存多语言资源失败时，需要回滚整个操作

## 扩展性

### 添加新语言

如果需要支持更多语言，需要：

1. 在 LocalizationResources 表添加新列
2. 更新 MultilingualText 类添加新属性
3. 修改 MultilingualInput 组件添加新标签页
4. 更新 MetadataI18nService 的保存方法

示例：添加韩语支持

```csharp
// 数据库
ALTER TABLE LocalizationResources ADD COLUMN KO TEXT;

// DTO
public record MultilingualText
{
    public string? ZH { get; init; }
    public string? JA { get; init; }
    public string? EN { get; init; }
    public string? KO { get; init; }  // 新增
}
```

## 总结

元数据多语言机制通过系统自动生成Key和用户直接输入多语言文本的方式，实现了元数据的国际化管理。该机制遵循OOP原则，保持了良好的代码结构和可维护性，为BobCRM系统的多语言支持提供了坚实的基础。
