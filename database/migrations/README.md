# 数据库迁移脚本说明

本目录包含独立的SQL迁移脚本，可以直接在PostgreSQL数据库上执行。

## 执行方式

### 方式1：使用 psql 命令行

```bash
psql -h localhost -p 5432 -U postgres -d bobcrm -f 20251107090000_add_master_detail_fields.sql
```

### 方式2：使用 pgAdmin 或其他GUI工具

1. 连接到 bobcrm 数据库
2. 打开SQL查询窗口
3. 复制并执行SQL脚本内容

### 方式3：使用 EF Core 迁移（推荐）

```bash
cd src/BobCrm.Api
dotnet ef database update
```

EF Core 会自动应用 `Infrastructure/Migrations` 目录下的所有未执行迁移。

## 迁移记录

| 版本号 | 时间戳 | 说明 | 状态 |
|--------|---------|------|------|
| 20251107090000 | 2025-11-07 09:00 | 添加主子表配置字段和锁定机制 | ✅ 已创建 |

## 迁移内容详情

### 20251107090000_add_master_detail_fields.sql

**目的**：为 EntityDefinition 表添加主子表结构支持和实体锁定机制

**添加的字段**：
- `ParentEntityId` (uuid): 父实体ID（自引用外键）
- `ParentEntityName` (varchar(100)): 父实体名称（冗余字段）
- `ParentForeignKeyField` (varchar(100)): 外键字段名
- `ParentCollectionProperty` (varchar(100)): 集合属性名
- `CascadeDeleteBehavior` (varchar(20)): 级联删除行为，默认 'NoAction'
- `AutoCascadeSave` (boolean): 是否自动级联保存，默认 true
- `IsLocked` (boolean): 是否锁定，默认 false

**索引**：
- `IX_EntityDefinitions_ParentEntityId`: 父实体ID索引

**外键约束**：
- `FK_EntityDefinitions_EntityDefinitions_ParentEntityId`: 自引用外键，ON DELETE RESTRICT

**影响范围**：
- 仅修改 `EntityDefinitions` 表结构
- 不影响现有数据
- 向后兼容（所有新字段均可为NULL或有默认值）

## 回滚说明

每个迁移脚本底部都包含注释的回滚SQL。如需回滚，请：

1. 取消注释回滚部分
2. 单独执行回滚SQL
3. 或者使用 EF Core: `dotnet ef database update <previous-migration-name>`

## 注意事项

1. **执行前备份**：在生产环境执行前务必备份数据库
2. **测试验证**：先在开发/测试环境验证
3. **停机时间**：评估是否需要停机维护
4. **监控性能**：关注迁移执行时间和锁定情况
5. **数据一致性**：执行后验证数据完整性

## 验证迁移

执行以下SQL验证迁移是否成功：

```sql
-- 检查列是否存在
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'EntityDefinitions'
  AND column_name IN (
    'ParentEntityId',
    'ParentEntityName',
    'ParentForeignKeyField',
    'ParentCollectionProperty',
    'CascadeDeleteBehavior',
    'AutoCascadeSave',
    'IsLocked'
  )
ORDER BY column_name;

-- 检查索引是否存在
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'EntityDefinitions'
  AND indexname = 'IX_EntityDefinitions_ParentEntityId';

-- 检查外键约束是否存在
SELECT conname, contype, confupdtype, confdeltype
FROM pg_constraint
WHERE conname = 'FK_EntityDefinitions_EntityDefinitions_ParentEntityId';
```

## 相关文档

- [AggVO系统指南](/AGGVO_SYSTEM_GUIDE.md)
- [EntityDefinition模型](/src/BobCrm.Api/Domain/Models/EntityDefinition.cs)
- [主子表配置API](/src/BobCrm.Api/Controllers/EntityAdvancedFeaturesController.cs)
