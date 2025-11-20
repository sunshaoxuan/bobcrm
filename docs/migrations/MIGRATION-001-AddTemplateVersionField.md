# Migration: Add Version Field to FormTemplate

## 概述

为 `FormTemplate` 表添加 `Version` 字段，用于跟踪模板的版本变更。

## 变更内容

### 1. 模型变更

**文件**: `src/BobCrm.Api/Base/Models/FormTemplate.cs`

添加属性：
```csharp
/// <summary>模板版本号（用于跟踪变更）</summary>
public int Version { get; set; } = 1;
```

### 2. 数据库变更

需要执行以下数据库迁移命令：

```bash
# 创建迁移
dotnet ef migrations add AddTemplateVersionField --project src/BobCrm.Api

# 应用迁移
dotnet ef database update --project src/BobCrm.Api
```

### 3. SQL 脚本（手动执行）

如果无法使用 EF 迁移，可以手动执行以下 SQL：

```sql
-- 添加 Version 列
ALTER TABLE "FormTemplates"
ADD COLUMN "Version" integer NOT NULL DEFAULT 1;

-- 更新现有记录的版本号
UPDATE "FormTemplates"
SET "Version" = 1
WHERE "Version" IS NULL;
```

## 验证

迁移完成后，可以通过以下 SQL 验证：

```sql
-- 检查列是否添加成功
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'FormTemplates'
AND column_name = 'Version';

-- 检查数据
SELECT "Id", "Name", "Version", "IsSystemDefault"
FROM "FormTemplates"
LIMIT 5;
```

## 影响范围

- **FormTemplate 模型**: 添加 Version 属性
- **数据库表**: FormTemplates 添加 Version 列
- **DefaultTemplateGenerator**: 新生成的模板默认 Version = 1
- **模板更新逻辑**: 后续可以在更新模板时递增版本号

## 注意事项

1. 该字段为 NOT NULL，默认值为 1
2. 现有模板会自动设置 Version = 1
3. 未来可以实现模板版本控制功能（回滚、历史记录等）

## 相关任务

- ✅ T5.1: 添加 FormTemplate.Version 字段
- 📋 T6: 实现模板列表管理系统（可能会使用 Version 字段）

---

**创建日期**: 2025-11-20
**创建人**: BobCRM AI Development Team
**状态**: 待执行
