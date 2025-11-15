# 动态实体系统 - 完整使用指南

本指南介绍如何使用BobCRM的动态实体系统，从定义实体到运行时数据操作的完整流程。

## 概述

动态实体系统允许您在运行时定义、发布和操作自定义实体，无需重启应用程序。完整流程包括：

1. **实体定义** - 通过API定义实体结构
2. **实体发布** - 生成数据库表（DDL）
3. **代码生成** - 生成C#实体类代码
4. **动态编译** - 编译并加载到内存
5. **数据操作** - 运行时CRUD操作

## 完整示例：创建Product实体

### 步骤1：定义实体

```bash
POST /api/entity-definitions
Content-Type: application/json

{
  "namespace": "BobCrm.Base.Custom",
  "entityName": "Product",
  "displayNameKey": "ENTITY_PRODUCT",
  "descriptionKey": "ENTITY_PRODUCT_DESC",
  "structureType": "Single",
  "interfaces": ["Base", "Archive", "Audit"],
  "fields": [
    {
      "propertyName": "Price",
      "displayNameKey": "FIELD_PRICE",
      "dataType": "Decimal",
      "precision": 10,
      "scale": 2,
      "isRequired": true,
      "sortOrder": 10
    },
    {
      "propertyName": "Stock",
      "displayNameKey": "FIELD_STOCK",
      "dataType": "Integer",
      "isRequired": true,
      "defaultValue": "0",
      "sortOrder": 11
    },
    {
      "propertyName": "Description",
      "displayNameKey": "FIELD_DESCRIPTION",
      "dataType": "Text",
      "isRequired": false,
      "sortOrder": 12
    }
  ]
}
```

**响应：**
```json
{
  "id": "guid-of-entity-definition",
  "namespace": "BobCrm.Base.Custom",
  "entityName": "Product",
  "status": "Draft",
  ...
}
```

### 步骤2：预览DDL

```bash
GET /api/entity-definitions/{id}/preview-ddl
```

**响应：**
```sql
-- 创建表：ENTITY_PRODUCT (Product)
CREATE TABLE IF NOT EXISTS "Products" (
    "Id" INTEGER NOT NULL,
    "Code" VARCHAR(64) NOT NULL,
    "Name" VARCHAR(256) NOT NULL,
    "Price" NUMERIC(10,2) NOT NULL,
    "Stock" INTEGER NOT NULL DEFAULT 0,
    "Description" TEXT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(100) NULL,
    ...
);
```

### 步骤3：发布实体（生成数据库表）

```bash
POST /api/entity-definitions/{id}/publish
```

**响应：**
```json
{
  "success": true,
  "scriptId": "guid-of-ddl-script",
  "ddlScript": "CREATE TABLE IF NOT EXISTS ...",
  "message": "实体发布成功"
}
```

### 步骤4：生成并查看C#代码

```bash
GET /api/entity-definitions/{id}/generate-code
```

**响应：**
```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BobCrm.Base.Custom
{
    /// <summary>
    /// ENTITY_PRODUCT
    /// ENTITY_PRODUCT_DESC
    /// 自动生成于: 2025-11-07 10:30:00 UTC
    /// </summary>
    [Table("Products")]
    public class Product : IEntity, IArchive, IAuditable
    {
        /// <summary>
        /// FIELD_ID
        /// </summary>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// FIELD_CODE
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// FIELD_PRICE
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        /// <summary>
        /// FIELD_STOCK
        /// </summary>
        [Required]
        public int Stock { get; set; } = 0;

        /// <summary>
        /// FIELD_DESCRIPTION
        /// </summary>
        [Column(TypeName = "text")]
        public string? Description { get; set; }

        ...
    }
}
```

### 步骤5：编译并加载实体

```bash
POST /api/entity-definitions/{id}/compile
```

**响应：**
```json
{
  "success": true,
  "assemblyName": "DynamicEntity_Product_abc123",
  "loadedTypes": [
    "BobCrm.Base.Custom.Product"
  ],
  "message": "实体编译成功"
}
```

### 步骤6：创建Product记录

```bash
POST /api/dynamic-entities/BobCrm.Base.Custom.Product
Content-Type: application/json

{
  "code": "P001",
  "name": "MacBook Pro",
  "price": 12999.00,
  "stock": 50,
  "description": "Apple MacBook Pro 16-inch"
}
```

**响应：**
```json
{
  "id": 1,
  "code": "P001",
  "name": "MacBook Pro",
  "price": 12999.00,
  "stock": 50,
  "description": "Apple MacBook Pro 16-inch",
  "createdAt": "2025-11-07T10:35:00Z",
  "createdBy": "user-guid",
  ...
}
```

### 步骤7：查询Product列表

```bash
POST /api/dynamic-entities/BobCrm.Base.Custom.Product/query
Content-Type: application/json

{
  "filters": [
    {
      "field": "Price",
      "operator": "equals",
      "value": 12999.00
    }
  ],
  "orderBy": "CreatedAt",
  "orderByDescending": true,
  "skip": 0,
  "take": 10
}
```

**响应：**
```json
{
  "data": [
    {
      "id": 1,
      "code": "P001",
      "name": "MacBook Pro",
      "price": 12999.00,
      ...
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 10
}
```

### 步骤8：更新Product

```bash
PUT /api/dynamic-entities/BobCrm.Base.Custom.Product/1
Content-Type: application/json

{
  "stock": 45,
  "description": "Updated description"
}
```

### 步骤9：删除Product

```bash
DELETE /api/dynamic-entities/BobCrm.Base.Custom.Product/1
```

## 完整API端点列表

### 实体定义管理

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/api/entity-definitions` | 获取实体定义列表 |
| GET | `/api/entity-definitions/{id}` | 获取实体定义详情 |
| POST | `/api/entity-definitions` | 创建实体定义 |
| PUT | `/api/entity-definitions/{id}` | 更新实体定义 |
| DELETE | `/api/entity-definitions/{id}` | 删除实体定义 |

### 实体发布

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/api/entity-definitions/{id}/preview-ddl` | 预览DDL脚本 |
| POST | `/api/entity-definitions/{id}/publish` | 发布新实体 |
| POST | `/api/entity-definitions/{id}/publish-changes` | 发布实体修改 |
| GET | `/api/entity-definitions/{id}/ddl-history` | 获取DDL历史 |

### 代码生成与编译

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/api/entity-definitions/{id}/generate-code` | 生成C#代码 |
| GET | `/api/entity-definitions/{id}/validate-code` | 验证代码语法 |
| POST | `/api/entity-definitions/{id}/compile` | 编译实体 |
| POST | `/api/entity-definitions/compile-batch` | 批量编译 |
| POST | `/api/entity-definitions/{id}/recompile` | 重新编译 |
| GET | `/api/entity-definitions/loaded-entities` | 获取已加载实体 |
| GET | `/api/entity-definitions/type-info/{fullTypeName}` | 获取类型信息 |
| DELETE | `/api/entity-definitions/loaded-entities/{fullTypeName}` | 卸载实体 |

### 动态实体数据操作

| 方法 | 端点 | 说明 |
|------|------|------|
| POST | `/api/dynamic-entities/{fullTypeName}/query` | 查询实体列表 |
| GET | `/api/dynamic-entities/{fullTypeName}/{id}` | 查询单个实体 |
| POST | `/api/dynamic-entities/raw/{tableName}/query` | 原始SQL查询 |
| POST | `/api/dynamic-entities/{fullTypeName}` | 创建实体 |
| PUT | `/api/dynamic-entities/{fullTypeName}/{id}` | 更新实体 |
| DELETE | `/api/dynamic-entities/{fullTypeName}/{id}` | 删除实体 |
| POST | `/api/dynamic-entities/{fullTypeName}/count` | 统计数量 |

## 高级功能

### 1. 批量编译多个实体

```bash
POST /api/entity-definitions/compile-batch
Content-Type: application/json

{
  "entityIds": [
    "guid-1",
    "guid-2",
    "guid-3"
  ]
}
```

将多个实体编译到同一个程序集，减少内存占用。

### 2. 原始SQL查询

如果实体未编译加载，可以直接使用表名查询：

```bash
POST /api/dynamic-entities/raw/Products/query
Content-Type: application/json

{
  "filters": [
    {"field": "Price", "value": 12999}
  ],
  "orderBy": "CreatedAt",
  "take": 10
}
```

### 3. 实体热更新

修改实体定义后：

```bash
# 1. 更新实体定义
PUT /api/entity-definitions/{id}

# 2. 发布修改
POST /api/entity-definitions/{id}/publish-changes

# 3. 重新编译
POST /api/entity-definitions/{id}/recompile
```

### 4. 查询已加载的实体

```bash
GET /api/entity-definitions/loaded-entities
```

响应：
```json
{
  "count": 3,
  "entities": [
    "BobCrm.Base.Custom.Product",
    "BobCrm.Base.Custom.Order",
    "BobCrm.Base.Custom.OrderItem"
  ]
}
```

### 5. 获取实体类型元数据

```bash
GET /api/entity-definitions/type-info/BobCrm.Base.Custom.Product
```

响应：
```json
{
  "fullName": "BobCrm.Base.Custom.Product",
  "name": "Product",
  "namespace": "BobCrm.Base.Custom",
  "isLoaded": true,
  "properties": [
    {
      "name": "Id",
      "typeName": "Int32",
      "isNullable": false,
      "canRead": true,
      "canWrite": true
    },
    {
      "name": "Code",
      "typeName": "String",
      "isNullable": false,
      "canRead": true,
      "canWrite": true
    },
    ...
  ],
  "interfaces": ["IEntity", "IArchive", "IAuditable"]
}
```

## 接口模板

系统提供5种接口模板，自动生成相应字段：

### Base (IEntity)
- `Id` (int) - 主键

### Archive (IArchive)
- `Code` (string) - 编码
- `Name` (string) - 名称

### Audit (IAuditable)
- `CreatedAt` (DateTime) - 创建时间
- `CreatedBy` (string) - 创建人
- `UpdatedAt` (DateTime) - 修改时间
- `UpdatedBy` (string) - 修改人
- `Version` (int) - 版本号

### Version (IVersioned)
- `Version` (int) - 版本号

### TimeVersion (ITimeVersioned)
- `ValidFrom` (DateTime) - 生效开始时间
- `ValidTo` (DateTime?) - 生效结束时间
- `VersionNo` (int) - 版本编号

## 数据类型支持

| 字段类型 | PostgreSQL类型 | C#类型 | 示例 |
|----------|----------------|---------|------|
| String | VARCHAR(n) | string | "Hello" |
| Integer | INTEGER | int | 100 |
| Long | BIGINT | long | 1000000 |
| Decimal | NUMERIC(p,s) | decimal | 99.99 |
| Boolean | BOOLEAN | bool | true |
| DateTime | TIMESTAMP | DateTime | "2025-11-07T10:00:00Z" |
| Date | DATE | DateOnly | "2025-11-07" |
| Text | TEXT | string | "Long text..." |
| Guid | UUID | Guid | "guid-string" |

## 注意事项

1. **编译顺序** - 必须先发布（publish）实体后才能编译
2. **内存管理** - 已加载的实体会占用内存，不使用时应及时卸载
3. **权限控制** - 所有端点都需要授权（RequireAuthorization）
4. **并发安全** - 程序集缓存使用线程安全的锁机制
5. **审计字段** - 创建和更新操作会自动设置CreatedBy/UpdatedBy（如果实体有这些字段）

## 故障排除

### 问题1：编译失败
**解决方案：**
1. 使用 `GET /api/entity-definitions/{id}/validate-code` 验证语法
2. 检查生成的代码是否正确
3. 确认Roslyn包已安装

### 问题2：查询失败 - Entity type not loaded
**解决方案：**
1. 检查实体是否已编译加载
2. 使用 `GET /api/entity-definitions/loaded-entities` 查看已加载实体
3. 如未加载，执行 `POST /api/entity-definitions/{id}/compile`

### 问题3：表不存在
**解决方案：**
1. 确认实体已发布（Status = Published）
2. 检查 `GET /api/entity-definitions/{id}/ddl-history` 查看DDL执行状态
3. 如需要，重新发布实体

## 性能优化建议

1. **批量编译** - 将相关实体批量编译到同一程序集
2. **分页查询** - 始终使用Skip/Take进行分页，避免一次查询大量数据
3. **索引优化** - 为常用查询字段添加索引
4. **缓存策略** - 对不常变化的实体数据进行缓存
5. **定期清理** - 卸载不再使用的动态实体

## 下一步

- 了解更多：查看 `docs/DEV-02-Roslyn环境配置.md` 了解Roslyn安装
- 实体设计：查看 `docs/ARCH-01-实体自定义与发布系统设计文档.md`
- 前端集成：使用Swagger UI测试所有端点
