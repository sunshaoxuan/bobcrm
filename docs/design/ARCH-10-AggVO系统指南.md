# BobCRM 聚合根（AggVO）系统指南

## 概述

本文档详细说明 BobCRM 的 **聚合根值对象（AggVO）** 系统，这是处理主子表和主子孙表结构的核心架构。

### 什么是 AggVO？

**AggVO (Aggregate Value Object)** = 主实体 VO + 多个子实体 VO 列表

- **聚合根模式**：DDD（领域驱动设计）中的核心概念，将相关实体组织成一个聚合
- **统一操作**：提供 Save、Load、Delete 等统一的级联操作
- **事务一致性**：确保主子实体的数据一致性
- **简化开发**：封装复杂的主子表操作逻辑

## 核心概念

### 1. 实体结构类型

BobCRM 支持三种实体结构：

```
Single（单实体）
└── 独立的实体表，无父子关系

MasterDetail（主子结构，两层）
└── Master（主表）
    ├── Detail 1（子表1）
    ├── Detail 2（子表2）
    └── Detail n（子表n）

MasterDetailGrandchild（主子孙结构，三层）
└── Master（主表）
    └── Detail（子表）
        └── Grandchild（孙表）
```

### 2. AggVO 类层次结构

```csharp
// 抽象基类
public abstract class AggBaseVO
{
    public abstract Type GetHeadEntityType();
    public abstract List<Type> GetSubEntityTypes();
    public abstract object GetHeadVO();
    public abstract void SetHeadVO(object headVO);

    public virtual List<object>? GetSubEntities(Type entityType);
    public virtual void SetSubEntities(Type entityType, List<object> entities);

    public abstract Task<int> SaveAsync();
    public abstract Task LoadAsync(int id);
    public abstract Task DeleteAsync();
}

// 具体实现（自动生成）
public class OrderAggVO : AggBaseVO
{
    public OrderVO HeadVO { get; set; }  // 主实体
    public List<OrderLineVO> OrderLineVOs { get; set; }  // 子实体1
    public List<OrderCommentVO> CommentVOs { get; set; }  // 子实体2

    // 实现抽象方法...
}
```

### 3. 主子孙结构（双层 AggVO）

对于三层结构，子实体本身也是 AggVO：

```csharp
// 子实体的 AggVO
public class OrderLineAggVO : AggBaseVO
{
    public OrderLineVO HeadVO { get; set; }
    public List<OrderLineAttributeVO> AttributeVOs { get; set; }
}

// 主实体的 AggVO
public class OrderAggVO : AggBaseVO
{
    public OrderVO HeadVO { get; set; }
    public List<OrderLineAggVO> OrderLineAggVOs { get; set; }  // 子实体是 AggVO
    public List<OrderCommentVO> CommentVOs { get; set; }
}
```

## 系统架构

### 组件图

```
┌──────────────────────────────────────────────────────────────┐
│                      API Layer                                │
│  EntityAdvancedFeaturesController                            │
│  - GetChildEntities()                                        │
│  - ConfigureMasterDetail()                                   │
│  - GenerateAggVO()                                           │
│  - EvaluateMigration()                                       │
└────────────────────┬─────────────────────────────────────────┘
                     │
┌────────────────────┴─────────────────────────────────────────┐
│                   Service Layer                              │
│  ┌─────────────────────────┐  ┌────────────────────────────┐│
│  │  AggVOCodeGenerator     │  │  DataMigrationEvaluator    ││
│  │  - GenerateAggVOClass() │  │  - EvaluateImpactAsync()   ││
│  └─────────────────────────┘  └────────────────────────────┘│
│  ┌────────────────────────────────────────────────────────┐ │
│  │  AggVOService                                          │ │
│  │  - SaveAggVOAsync()                                    │ │
│  │  - LoadAggVOAsync()                                    │ │
│  │  - DeleteAggVOAsync()                                  │ │
│  └────────────────────────────────────────────────────────┘ │
└────────────────────┬─────────────────────────────────────────┘
                     │
┌────────────────────┴─────────────────────────────────────────┐
│                   Domain Layer                               │
│  ┌─────────────────────────┐  ┌────────────────────────────┐│
│  │  AggBaseVO (Abstract)   │  │  EntityDefinition          ││
│  └─────────────────────────┘  │  - ParentEntityId          ││
│  ┌─────────────────────────┐  │  - ParentForeignKeyField   ││
│  │  OrderAggVO (Generated) │  │  - CascadeDeleteBehavior   ││
│  └─────────────────────────┘  └────────────────────────────┘│
└──────────────────────────────────────────────────────────────┘
```

### 数据库模型

#### EntityDefinitions 表扩展字段

```sql
-- 主子表结构配置
ParentEntityId UUID NULL,              -- 父实体ID
ParentEntityName VARCHAR(100) NULL,    -- 父实体名称
ParentForeignKeyField VARCHAR(100) NULL,  -- 外键字段名
ParentCollectionProperty VARCHAR(100) NULL,  -- 集合属性名
CascadeDeleteBehavior VARCHAR(20) DEFAULT 'NoAction',
AutoCascadeSave BOOLEAN DEFAULT TRUE
```

## 使用指南

### 1. 配置主子表关系

#### 步骤 1：创建主实体和子实体

```http
POST /api/entity-definitions
{
  "namespace": "BobCrm.Base.Sales",
  "entityName": "Order",
  "structureType": "MasterDetail",
  "fields": [...]
}

POST /api/entity-definitions
{
  "namespace": "BobCrm.Base.Sales",
  "entityName": "OrderLine",
  "structureType": "Single",
  "fields": [...]
}
```

#### 步骤 2：配置主子关系

```http
POST /api/entity-advanced/{orderId}/configure-master-detail
{
  "structureType": "MasterDetail",
  "children": [
    {
      "childEntityId": "...",
      "foreignKeyField": "OrderId",
      "collectionProperty": "OrderLines",
      "cascadeDeleteBehavior": "Cascade",
      "autoCascadeSave": true
    }
  ]
}
```

### 2. 生成 AggVO 代码

```http
POST /api/entity-advanced/{orderId}/generate-aggvo

Response:
{
  "entity": "Order",
  "aggVOClassName": "OrderAggVO",
  "aggVOCode": "public class OrderAggVO : AggBaseVO { ... }",
  "voCode": "public class OrderVO { ... }",
  "childVOCodes": {
    "OrderLine": "public class OrderLineVO { ... }"
  }
}
```

### 3. 使用 AggVO 进行开发

#### 创建和保存聚合

```csharp
// 创建主实体
var orderVO = new OrderVO
{
    CustomerName = "张三",
    OrderDate = DateTime.Now,
    TotalAmount = 1500.00m
};

// 创建子实体
var orderLines = new List<OrderLineVO>
{
    new OrderLineVO { ProductName = "产品A", Quantity = 2, UnitPrice = 500m },
    new OrderLineVO { ProductName = "产品B", Quantity = 1, UnitPrice = 500m }
};

// 组装 AggVO
var orderAgg = new OrderAggVO
{
    HeadVO = orderVO,
    OrderLineVOs = orderLines
};

// 级联保存（主 + 子）
var aggVOService = serviceProvider.GetRequiredService<AggVOService>();
int orderId = await aggVOService.SaveAggVOAsync(orderAgg);
```

#### 加载聚合

```csharp
// 创建 AggVO 实例
var orderAgg = new OrderAggVO();

// 加载整个聚合（主 + 子）
await aggVOService.LoadAggVOAsync(orderAgg, orderId);

// 访问数据
Console.WriteLine($"订单编号: {orderAgg.HeadVO.Id}");
Console.WriteLine($"订单金额: {orderAgg.HeadVO.TotalAmount}");
Console.WriteLine($"明细数量: {orderAgg.OrderLineVOs.Count}");

foreach (var line in orderAgg.OrderLineVOs)
{
    Console.WriteLine($"  {line.ProductName}: {line.Quantity} × {line.UnitPrice}");
}
```

#### 更新和删除

```csharp
// 更新主实体
orderAgg.HeadVO.TotalAmount = 2000.00m;

// 修改子实体
orderAgg.OrderLineVOs[0].Quantity = 3;

// 添加新子实体
orderAgg.OrderLineVOs.Add(new OrderLineVO
{
    ProductName = "产品C",
    Quantity = 1,
    UnitPrice = 500m
});

// 保存更新
await aggVOService.SaveAggVOAsync(orderAgg);

// 删除整个聚合（根据级联删除行为处理子实体）
await aggVOService.DeleteAggVOAsync(orderAgg);
```

### 4. 级联删除行为

```csharp
public static class CascadeDeleteBehavior
{
    NoAction,   // 不执行任何操作
    Cascade,    // 级联删除所有子实体
    SetNull,    // 将子实体的外键设置为 NULL
    Restrict    // 如果存在子实体，阻止删除主实体
}
```

**示例：**

```json
{
  "cascadeDeleteBehavior": "Cascade"
}
```

删除订单时，自动删除所有订单明细。

### 5. 主子孙结构（三层）

#### 配置三层结构

```http
POST /api/entity-advanced/{orderId}/configure-master-detail
{
  "structureType": "MasterDetailGrandchild",
  "children": [
    {
      "childEntityId": "{orderLineId}",
      "foreignKeyField": "OrderId",
      "collectionProperty": "OrderLines",
      "cascadeDeleteBehavior": "Cascade"
    }
  ]
}

POST /api/entity-advanced/{orderLineId}/configure-master-detail
{
  "structureType": "MasterDetail",
  "children": [
    {
      "childEntityId": "{lineAttributeId}",
      "foreignKeyField": "OrderLineId",
      "collectionProperty": "Attributes",
      "cascadeDeleteBehavior": "Cascade"
    }
  ]
}
```

#### 使用三层 AggVO

```csharp
// 创建三层结构
var orderAgg = new OrderAggVO
{
    HeadVO = new OrderVO { CustomerName = "李四" },
    OrderLineAggVOs = new List<OrderLineAggVO>
    {
        new OrderLineAggVO
        {
            HeadVO = new OrderLineVO { ProductName = "产品A" },
            AttributeVOs = new List<OrderLineAttributeVO>
            {
                new OrderLineAttributeVO { AttributeName = "颜色", AttributeValue = "红色" },
                new OrderLineAttributeVO { AttributeName = "尺寸", AttributeValue = "L" }
            }
        }
    }
};

// 级联保存（主 → 子 → 孙）
await aggVOService.SaveAggVOAsync(orderAgg);
```

## 数据迁移评估

### 在发布前评估影响

```http
POST /api/entity-advanced/{entityId}/evaluate-migration
Content-Type: application/json

[
  {
    "propertyName": "Status",
    "dataType": "String",
    "length": 50,
    "isRequired": true
  }
]

Response:
{
  "entityName": "Order",
  "tableName": "Orders",
  "affectedRows": 1250,
  "riskLevel": "Medium",
  "isSafe": true,
  "operations": [
    {
      "operationType": "AddColumn",
      "fieldName": "Status",
      "newDataType": "String",
      "mayLoseData": false,
      "requiresConversion": false,
      "description": "Add column 'Status' of type 'String'",
      "sqlPreview": "ALTER TABLE \"Orders\" ADD COLUMN \"Status\" VARCHAR(50) NOT NULL DEFAULT 'Draft';"
    }
  ],
  "warnings": [
    "Adding required column 'Status' with default value 'Draft'"
  ],
  "errors": []
}
```

### 风险等级

- **Low**: 仅添加可空字段
- **Medium**: 修改字段类型但可以安全转换
- **High**: 可能导致数据丢失
- **Critical**: 必定导致数据丢失或系统异常（阻塞发布）

## 最佳实践

### 1. 实体命名规范

```
主实体: Order, Invoice, Project
子实体: OrderLine, InvoiceLine, ProjectTask
AggVO: OrderAggVO, InvoiceAggVO, ProjectAggVO
VO: OrderVO, OrderLineVO
```

### 2. 外键命名

```
主表: Order (Id)
子表: OrderLine (Id, OrderId)
孙表: OrderLineAttribute (Id, OrderLineId)
```

### 3. 集合属性命名

```csharp
public class OrderAggVO : AggBaseVO
{
    public OrderVO HeadVO { get; set; }
    public List<OrderLineVO> OrderLineVOs { get; set; }  // 复数形式 + VOs
    public List<OrderCommentVO> CommentVOs { get; set; }
}
```

### 4. 事务管理

AggVOService 自动管理事务：

```csharp
// ✅ 正确：一次保存完成所有操作
await aggVOService.SaveAggVOAsync(orderAgg);

// ❌ 错误：手动分别保存主子实体（破坏事务一致性）
await orderService.SaveAsync(order);
await orderLineService.SaveAsync(orderLine);
```

### 5. 验证数据

```csharp
// 保存前验证
var errors = orderAgg.Validate();
if (errors.Any())
{
    throw new ValidationException(string.Join(", ", errors));
}

await aggVOService.SaveAggVOAsync(orderAgg);
```

## 故障排查

### 问题 1: 生成 AggVO 失败

**错误**: "Entity is not a master-detail structure"

**解决方案**:
1. 确认实体的 `StructureType` 为 `MasterDetail` 或 `MasterDetailGrandchild`
2. 确认已配置至少一个子实体

### 问题 2: 级联保存失败

**错误**: "Foreign key constraint violation"

**解决方案**:
1. 检查子实体的 `ParentForeignKeyField` 配置是否正确
2. 确认主实体已先保存并获得 ID
3. 检查子实体表中外键字段是否存在

### 问题 3: 数据迁移评估显示 Critical 风险

**错误**: "Dropping column will cause data loss"

**解决方案**:
1. 如果确实需要删除字段，先备份数据
2. 考虑将字段标记为可空而不是删除
3. 创建数据迁移脚本转移数据到新字段

### 问题 4: 级联删除未生效

**问题**: 删除主实体后子实体仍然存在

**解决方案**:
1. 检查 `CascadeDeleteBehavior` 配置
2. 确认子实体的 `ParentEntityId` 正确指向主实体
3. 查看日志确认是否有错误

## API 参考

### 获取子实体列表

```
GET /api/entity-advanced/{entityId}/children
```

### 配置主子表关系

```
POST /api/entity-advanced/{entityId}/configure-master-detail
```

### 生成 AggVO 代码

```
POST /api/entity-advanced/{entityId}/generate-aggvo
```

### 评估数据迁移影响

```
POST /api/entity-advanced/{entityId}/evaluate-migration
```

### 获取主实体候选列表

```
GET /api/entity-advanced/master-candidates
```

### 获取子实体候选列表

```
GET /api/entity-advanced/detail-candidates
```

## 示例场景

### 场景 1: 订单管理系统

```
Order (主实体)
├── OrderLines (子实体)
├── OrderComments (子实体)
└── OrderAttachments (子实体)
```

### 场景 2: 项目管理系统

```
Project (主实体)
└── ProjectTask (子实体)
    └── TaskComment (孙实体)
```

### 场景 3: 发票系统

```
Invoice (主实体)
├── InvoiceLines (子实体)
└── PaymentRecords (子实体)
```

## 性能优化

### 1. 批量加载

```csharp
// ❌ 低效：循环加载
foreach (var id in orderIds)
{
    var agg = new OrderAggVO();
    await aggVOService.LoadAggVOAsync(agg, id);
    orders.Add(agg);
}

// ✅ 高效：批量查询后组装
var allOrders = await orderRepository.GetByIdsAsync(orderIds);
var allLines = await orderLineRepository.GetByOrderIdsAsync(orderIds);
// 组装 AggVO...
```

### 2. 延迟加载子实体

```csharp
// 仅加载主实体
var order = await orderRepository.GetByIdAsync(orderId);

// 按需加载子实体
if (needDetails)
{
    var agg = new OrderAggVO();
    await aggVOService.LoadAggVOAsync(agg, orderId);
}
```

### 3. 选择性保存

```csharp
// 仅保存已修改的子实体
if (orderAgg.HeadVO.IsModified)
{
    // 只保存主实体
}

foreach (var line in orderAgg.OrderLineVOs.Where(l => l.IsModified))
{
    // 只保存修改过的子实体
}
```

## 相关文档

- [实体定义系统](./IMPLEMENTATION.md)
- [代码生成与编译](./ARCHITECTURE.md)
- [反射持久化](./API.md)
- [前端集成指南](./docs/DEV-01-前端开发指南.md)

## 技术支持

如有问题或建议，请联系开发团队或提交 Issue。

