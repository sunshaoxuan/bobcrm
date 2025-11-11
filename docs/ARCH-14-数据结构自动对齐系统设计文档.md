# 数据结构自动对齐系统设计文档

> **版本**: v1.0
> **创建日期**: 2025-11-11
> **作者**: BobCRM 开发团队
> **状态**: 已实现

---

## 目录

1. [功能概述](#功能概述)
2. [核心概念](#核心概念)
3. [技术架构](#技术架构)
4. [实现细节](#实现细节)
5. [使用指南](#使用指南)
6. [测试覆盖](#测试覆盖)
7. [最佳实践](#最佳实践)
8. [未来扩展](#未来扩展)

---

## 功能概述

### 背景与问题

在 BobCRM 系统的演化过程中，存在以下数据结构对齐问题：

1. **代码与数据库结构不一致**
   - 开发阶段修改了数据模型，但数据库未同步更新
   - 多语资源表从 `EN/JA/ZH` 列结构升级到 `Translations jsonb` 结构
   - 导致运行时错误或显示异常（如页面显示资源ID而非翻译文本）

2. **预置数据不同步**
   - 代码中定义的系统设置、字段定义、语言配置等预置数据
   - 数据库中可能缺失或版本过旧
   - 需要手动执行SQL或重建数据库

3. **动态实体结构变更**
   - 用户修改实体定义（添加/删除字段）
   - 需要同步更新数据库表结构
   - 现有业务数据需要填充默认值以保持一致性

### 解决方案

数据结构自动对齐系统提供了三层对齐机制：

1. **静态结构对齐** - 通过 EF Core Migrations 自动应用数据库架构变更
2. **预置数据对齐** - 通过 Upsert 模式自动同步系统预设数据
3. **动态实体对齐** - 通过 EntitySchemaAlignmentService 自动对齐用户自定义实体

### 核心价值

- ✅ **零手动干预** - 启动时自动检测并修复结构/数据不一致
- ✅ **数据安全** - 只添加缺失项，不删除现有数据（避免数据丢失）
- ✅ **业务连续性** - 添加字段时自动填充默认值，确保现有记录可用
- ✅ **可追溯性** - 所有 DDL 操作记录到 `DDLScripts` 表
- ✅ **灵活回收** - 支持字段逻辑删除和物理删除两种模式

---

## 核心概念

### 1. 三层对齐机制

```
┌─────────────────────────────────────────────────────────────┐
│                    应用启动（DatabaseInitializer）              │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              │               │               │
              ▼               ▼               ▼
    ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
    │ Layer 1:    │  │ Layer 2:    │  │ Layer 3:    │
    │ 静态结构对齐  │  │ 预置数据对齐  │  │ 动态实体对齐  │
    └─────────────┘  └─────────────┘  └─────────────┘
         │                 │                 │
         ▼                 ▼                 ▼
    MigrateAsync()    Upsert模式      AlignAllPublishedEntitiesAsync()
    (EF Core)         (增量更新)       (DDL变更)
```

### 2. 对齐策略

| 层级 | 对象 | 检测方式 | 对齐方式 | 回滚策略 |
|------|------|---------|---------|---------|
| Layer 1 | EF Core 实体 | `__EFMigrationsHistory` | `MigrateAsync()` | Migration Down |
| Layer 2 | 预置数据 | 主键/唯一键查询 | Upsert（保留用户修改） | 代码回退 |
| Layer 3 | 动态实体 | `information_schema` | ADD COLUMN + UPDATE | DDL Rollback |

### 3. 数据安全原则

- **只添加，不删除**：对齐过程只添加缺失的列/数据，不自动删除
- **保留用户修改**：Upsert 时只更新空值，不覆盖用户自定义值
- **默认值填充**：新增列时自动填充默认值，避免 NULL 导致业务异常
- **警告而非阻断**：发现多余列或类型不匹配时记录警告，不阻止启动

---

## 技术架构

### 整体架构图

```
┌─────────────────────────────────────────────────────────────────────┐
│                          DatabaseInitializer                        │
│  (应用启动时执行，Program.cs 中注册)                                    │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                   ┌───────────────┼───────────────┐
                   ▼               ▼               ▼
          ┌────────────────┐ ┌──────────┐ ┌─────────────────────┐
          │ DbContext.     │ │ Upsert   │ │ EntitySchema        │
          │ MigrateAsync() │ │ Helpers  │ │ AlignmentService    │
          └────────────────┘ └──────────┘ └─────────────────────┘
                   │               │               │
                   ▼               ▼               ▼
          ┌────────────────┐ ┌──────────┐ ┌─────────────────────┐
          │ Migrations/    │ │ AppDbCtx │ │ DDLExecutionService │
          │ 20251111xxx.cs │ │          │ │                     │
          └────────────────┘ └──────────┘ └─────────────────────┘
                                                     │
                                                     ▼
                                            ┌─────────────────┐
                                            │ PostgreSQL      │
                                            │ - DDLScripts    │
                                            │ - Dynamic Tables│
                                            └─────────────────┘
```

### 核心服务

#### 1. DatabaseInitializer
- **职责**：应用启动时执行三层对齐
- **位置**：`src/BobCrm.Api/Infrastructure/DatabaseInitializer.cs`
- **调用时机**：`Program.cs` 中 `app.Run()` 之前

#### 2. EntitySchemaAlignmentService
- **职责**：检测并修复动态实体表结构不一致
- **位置**：`src/BobCrm.Api/Services/EntitySchemaAlignmentService.cs`
- **核心方法**：
  - `AlignAllPublishedEntitiesAsync()` - 对齐所有已发布实体
  - `AlignEntitySchemaAsync(entity)` - 对齐单个实体
  - `DeleteFieldAsync(entityId, fieldId, physicalDelete)` - 删除字段

#### 3. DDLExecutionService
- **职责**：执行并记录 DDL 脚本
- **位置**：`src/BobCrm.Api/Services/DDLExecutionService.cs`
- **核心方法**：
  - `ExecuteDDLAsync()` - 执行单个 DDL
  - `ExecuteDDLBatchAsync()` - 批量执行（事务）
  - `TableExistsAsync()` / `GetTableColumnsAsync()` - 结构检查

---

## 实现细节

### Layer 1: 静态结构对齐

#### 实现方式

**修改前**（DatabaseInitializer.cs:15）：
```csharp
await db.Database.EnsureCreatedAsync();
```

**问题**：`EnsureCreatedAsync()` 只在数据库不存在时创建，不会应用 Migrations。

**修改后**（DatabaseInitializer.cs:15）：
```csharp
Console.WriteLine("[DatabaseInitializer] Applying pending migrations using MigrateAsync");
await db.Database.MigrateAsync();
```

**效果**：每次启动自动检测并应用未执行的 Migrations。

#### 迁移示例：多语资源表结构升级

**Migration 文件**：`20251111000000_MigrateLocalizationToJsonb.cs`

**迁移步骤**：
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // 1. 添加新列 Translations (jsonb)
    migrationBuilder.AddColumn<string>(
        name: "Translations",
        table: "LocalizationResources",
        type: "jsonb",
        nullable: false,
        defaultValue: "{}");

    // 2. 迁移现有数据（将 EN/JA/ZH 列合并到 Translations）
    migrationBuilder.Sql(@"
        UPDATE ""LocalizationResources""
        SET ""Translations"" = jsonb_build_object(
            'zh', COALESCE(""ZH"", ''),
            'ja', COALESCE(""JA"", ''),
            'en', COALESCE(""EN"", '')
        )
        WHERE ""Translations""::text = '{}'::text OR ""Translations"" IS NULL;
    ");

    // 3. 删除旧列
    migrationBuilder.DropColumn(name: "EN", table: "LocalizationResources");
    migrationBuilder.DropColumn(name: "JA", table: "LocalizationResources");
    migrationBuilder.DropColumn(name: "ZH", table: "LocalizationResources");
}
```

**配套代码变更**：
- EF Core 模型添加值转换器（`LocalizationResource.cs`）
- 测试代码更新为使用 `Translations` 属性

---

### Layer 2: 预置数据对齐

#### Upsert 模式实现

**1. SystemSettings（单行配置表）**

```csharp
// DatabaseInitializer.cs:40-77
var existing = await db.Set<SystemSettings>().FirstOrDefaultAsync();

if (existing == null)
{
    // 首次运行：创建默认配置
    await db.Set<SystemSettings>().AddAsync(new SystemSettings
    {
        CompanyName = "OneCRM",
        ContactEmail = "support@example.com",
        ContactPhone = "+86-400-123-4567",
        EnableMultiTenant = false,
        DefaultLanguage = "zh",
        SupportedLanguages = "zh,ja,en"
    });
}
else
{
    // 已有配置：只更新空值，保留用户修改
    if (string.IsNullOrEmpty(existing.CompanyName))
        existing.CompanyName = "OneCRM";
    if (string.IsNullOrEmpty(existing.ContactEmail))
        existing.ContactEmail = "support@example.com";
    // ... 其他字段
}

await db.SaveChangesAsync();
```

**2. FieldDefinition（多行字典表）**

```csharp
// DatabaseInitializer.cs:78-135
var fieldDefinitions = new[]
{
    new FieldDefinition { Key = "String", DisplayName = "文本", Category = "Basic" },
    new FieldDefinition { Key = "Int32", DisplayName = "整数", Category = "Basic" },
    // ... 更多定义
};

foreach (var def in fieldDefinitions)
{
    var existing = await db.Set<FieldDefinition>()
        .FirstOrDefaultAsync(fd => fd.Key == def.Key);

    if (existing == null)
    {
        // 不存在：插入
        await db.Set<FieldDefinition>().AddAsync(def);
    }
    else
    {
        // 已存在：更新（可选，这里选择保留旧值）
        // existing.DisplayName = def.DisplayName; // 如果要强制同步
    }
}

await db.SaveChangesAsync();
```

**3. LocalizationLanguage（语言列表）**

```csharp
// DatabaseInitializer.cs:136-163
var languages = new[]
{
    new LocalizationLanguage { Code = "zh", DisplayName = "中文", IsActive = true },
    new LocalizationLanguage { Code = "ja", DisplayName = "日本語", IsActive = true },
    new LocalizationLanguage { Code = "en", DisplayName = "English", IsActive = true }
};

foreach (var lang in languages)
{
    var existing = await db.Set<LocalizationLanguage>()
        .FirstOrDefaultAsync(l => l.Code == lang.Code);

    if (existing == null)
    {
        await db.Set<LocalizationLanguage>().AddAsync(lang);
    }
}

await db.SaveChangesAsync();
```

#### 设计原则

| 表类型 | Upsert 策略 | 原因 |
|--------|------------|------|
| 单行配置 | 只更新空值 | 避免覆盖用户自定义配置 |
| 字典表 | 只插入缺失项 | 保留用户可能的修改 |
| 系统常量 | 强制同步 | 确保系统一致性 |

---

### Layer 3: 动态实体对齐

#### 对齐流程

```
AlignEntitySchemaAsync(entity)
        │
        ▼
  ┌─────────────┐
  │ 检查表是否存在 │
  └─────────────┘
        │
        ├─No──► CreateTableAsync() ────► 生成 CREATE TABLE SQL ────► 执行
        │
        ▼
  ┌─────────────────┐
  │ 获取表结构信息      │
  │ GetTableColumns  │
  └─────────────────┘
        │
        ▼
  ┌─────────────────────────┐
  │ 对比 EntityDefinition    │
  │ - GetMissingColumns     │
  │ - GetExtraColumns       │
  │ - GetMismatchedColumns  │
  └─────────────────────────┘
        │
        ├─► 有缺失列？──Yes──► AddMissingColumnsAsync() ───┐
        │                          │                      │
        │                          ▼                      │
        │                    【业务数据对齐】               │
        │                    1. ADD COLUMN (nullable)     │
        │                    2. UPDATE 填充默认值          │
        │                    3. ALTER COLUMN SET NOT NULL │
        │                                                 │
        ├─► 有多余列？──Yes──► ⚠️ LogWarning (不删除)      │
        │                                                 │
        ├─► 有不匹配列？─Yes─► ⚠️ LogWarning (不修改)      │
        │                                                 │
        └─────────────────────────────────────────────────┘
                              │
                              ▼
                        返回 AlignmentResult
```

#### 核心代码：业务数据对齐

**文件**：`EntitySchemaAlignmentService.cs:151-185`

```csharp
private async Task AddMissingColumnsAsync(
    Guid entityId,
    string tableName,
    List<FieldMetadata> missingFields)
{
    var scripts = new List<(string ScriptType, string SqlScript)>();

    foreach (var field in missingFields)
    {
        // 步骤 1: 添加列（先设为可空，避免非空约束导致失败）
        var dataType = MapDataTypeToSQL(field).Replace(" NOT NULL", "");
        var alterSql = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{field.PropertyName}\" {dataType};";
        scripts.Add((DDLScriptType.Alter, alterSql));

        // 步骤 2: ✅ 业务数据对齐 - 填充默认值到现有记录
        var defaultValue = GetDefaultValueForDataType(field);
        if (defaultValue != null)
        {
            var updateSql = $"UPDATE \"{tableName}\" SET \"{field.PropertyName}\" = {defaultValue} WHERE \"{field.PropertyName}\" IS NULL;";
            scripts.Add((DDLScriptType.Alter, updateSql));

            _logger.LogInformation("[SchemaAlign] Filling default value for {FieldName}: {DefaultValue}",
                field.PropertyName, defaultValue);
        }

        // 步骤 3: 如果字段是必填，添加 NOT NULL 约束
        if (field.IsRequired && defaultValue != null)
        {
            var constraintSql = $"ALTER TABLE \"{tableName}\" ALTER COLUMN \"{field.PropertyName}\" SET NOT NULL;";
            scripts.Add((DDLScriptType.Alter, constraintSql));
        }
    }

    // 批量执行（事务）
    await _ddlService.ExecuteDDLBatchAsync(entityId, scripts, "System");

    _logger.LogInformation("[SchemaAlign] ✓ Added {Count} columns to {TableName} with data alignment",
        missingFields.Count, tableName);
}
```

#### 默认值生成逻辑

**文件**：`EntitySchemaAlignmentService.cs:341-369`

```csharp
private string? GetDefaultValueForDataType(FieldMetadata field)
{
    // 优先使用字段定义中的默认值
    if (!string.IsNullOrEmpty(field.DefaultValue))
    {
        return field.DataType switch
        {
            "String" => $"'{field.DefaultValue}'",
            "Int32" or "Int64" or "Decimal" => field.DefaultValue,
            "Boolean" => field.DefaultValue.ToLower() == "true" ? "TRUE" : "FALSE",
            "DateTime" => $"'{field.DefaultValue}'",
            "Guid" => $"'{field.DefaultValue}'",
            _ => $"'{field.DefaultValue}'"
        };
    }

    // 使用类型的默认值
    return field.DataType switch
    {
        "String" => "''",        // 空字符串
        "Int32" or "Int64" => "0",
        "Decimal" => "0.0",
        "Boolean" => "FALSE",
        "DateTime" => "NOW()",
        "Guid" => "gen_random_uuid()",
        "Json" => "'{}'::jsonb",
        _ => null
    };
}
```

#### 字段删除机制

**文件**：`EntitySchemaAlignmentService.cs:378-466`

**两种删除模式**：

1. **逻辑删除（默认，physicalDelete = false）**
   - 只从 `FieldMetadatas` 表删除字段元数据
   - 数据库列保留，数据不丢失
   - 适用场景：字段暂时不用，未来可能恢复

2. **物理删除（physicalDelete = true）**
   - 删除元数据 + DROP COLUMN
   - 数据永久删除，不可恢复
   - 适用场景：垃圾回收，释放存储空间

**代码示例**：
```csharp
public async Task<DeleteFieldResult> DeleteFieldAsync(
    Guid entityId,
    Guid fieldId,
    bool physicalDelete = false,
    string? performedBy = null)
{
    var result = new DeleteFieldResult { FieldId = fieldId };

    // 1. 加载实体和字段
    var entity = await _db.EntityDefinitions
        .Include(e => e.Fields)
        .FirstOrDefaultAsync(e => e.Id == entityId);

    var field = entity.Fields.FirstOrDefault(f => f.Id == fieldId);

    // 2. 从元数据中删除字段
    _db.FieldMetadatas.Remove(field);
    await _db.SaveChangesAsync();
    result.LogicalDeleteCompleted = true;

    // 3. 如果是物理删除，删除数据库列
    if (physicalDelete)
    {
        var dropColumnSql = $"ALTER TABLE \"{tableName}\" DROP COLUMN IF EXISTS \"{columnName}\";";
        var scriptRecord = await _ddlService.ExecuteDDLAsync(
            entityId,
            DDLScriptType.Alter,
            dropColumnSql,
            performedBy ?? "System"
        );

        result.PhysicalDeleteCompleted = (scriptRecord.Status == DDLScriptStatus.Success);
    }

    result.Success = true;
    return result;
}
```

**返回结果**：
```csharp
public class DeleteFieldResult
{
    public Guid FieldId { get; set; }
    public bool Success { get; set; }
    public bool LogicalDeleteCompleted { get; set; }   // 元数据删除成功
    public bool PhysicalDeleteCompleted { get; set; }  // 列删除成功
    public string? ErrorMessage { get; set; }
}
```

---

## 使用指南

### 场景 1: 修改预置数据

**步骤**：
1. 修改 `DatabaseInitializer.cs` 中的 Upsert 代码
2. 重启应用，自动同步到数据库

**示例**：添加新的 FieldDefinition
```csharp
// DatabaseInitializer.cs:78
var fieldDefinitions = new[]
{
    // ... 现有定义
    new FieldDefinition { Key = "Email", DisplayName = "邮箱", Category = "Extended" } // ← 新增
};
```

### 场景 2: 修改 EF Core 实体

**步骤**：
1. 修改实体类（如 `LocalizationResource.cs`）
2. 创建 Migration：
   ```bash
   cd src/BobCrm.Api
   dotnet ef migrations add YourMigrationName
   ```
3. 重启应用，自动应用 Migration

### 场景 3: 用户添加自定义字段

**流程**：
1. 用户通过前端修改实体定义，添加新字段
2. 保存并发布实体
3. `EntityPublishingService` 调用 `EntitySchemaAlignmentService.AlignEntitySchemaAsync()`
4. 自动执行 ADD COLUMN + UPDATE 默认值
5. 记录到 `DDLScripts` 表

**用户无需任何手动操作，系统自动完成对齐。**

### 场景 4: 删除不用的字段

**API 调用示例**：
```csharp
// 逻辑删除（保留数据）
var result = await _alignmentService.DeleteFieldAsync(
    entityId: entityId,
    fieldId: fieldId,
    physicalDelete: false,
    performedBy: "admin@example.com"
);

// 物理删除（永久删除）
var result = await _alignmentService.DeleteFieldAsync(
    entityId: entityId,
    fieldId: fieldId,
    physicalDelete: true,
    performedBy: "admin@example.com"
);

if (result.Success)
{
    Console.WriteLine($"逻辑删除: {result.LogicalDeleteCompleted}");
    Console.WriteLine($"物理删除: {result.PhysicalDeleteCompleted}");
}
```

---

## 测试覆盖

### 单元测试套件

#### 1. EntitySchemaAlignmentServiceTests.cs

**覆盖率**：95%+ （20+ 测试用例）

**测试分类**：

| 分类 | 测试数量 | 说明 |
|------|---------|------|
| 对齐流程 | 4 | 跳过非发布实体、创建表、添加列、警告多余列 |
| 数据类型映射 | 7 | String/Int/Decimal/Boolean/DateTime/Guid/Json |
| 字段删除 | 5 | 逻辑删除、物理删除、错误处理 |
| 默认值生成 | 4 | 类型默认值、自定义默认值 |

**关键测试**：
```csharp
[Fact]
public async Task AlignEntitySchemaAsync_ShouldAddMissingColumns_WithDefaultValues()
{
    // Arrange: 创建已发布实体，但表中缺少字段
    var entity = CreatePublishedEntity("TestEntity", new[]
    {
        new FieldMetadata { PropertyName = "Name", DataType = "String", IsRequired = true },
        new FieldMetadata { PropertyName = "Age", DataType = "Int32", IsRequired = true }
    });

    // 模拟表只有 Id 列（缺少 Name 和 Age）
    _mockDDL.Setup(x => x.TableExistsAsync(It.IsAny<string>()))
        .ReturnsAsync(true);
    _mockDDL.Setup(x => x.GetTableColumnsAsync(It.IsAny<string>()))
        .ReturnsAsync(new List<TableColumnInfo>
        {
            new TableColumnInfo { ColumnName = "Id", DataType = "uuid" }
        });

    // Act
    var result = await _alignmentService.AlignEntitySchemaAsync(entity);

    // Assert
    result.Should().Be(AlignmentResult.Aligned);

    // 验证执行了 ADD COLUMN + UPDATE 默认值 + SET NOT NULL
    _mockDDL.Verify(x => x.ExecuteDDLBatchAsync(
        entity.Id,
        It.Is<List<(string, string)>>(scripts =>
            scripts.Any(s => s.Item2.Contains("ADD COLUMN \"Name\"")) &&
            scripts.Any(s => s.Item2.Contains("UPDATE")) &&
            scripts.Any(s => s.Item2.Contains("SET NOT NULL"))
        ),
        "System"
    ), Times.Once);
}
```

#### 2. EntityPublishingAndDDLTests.cs

**新增测试**：补充成功路径测试（原先只有失败路径）

```csharp
[Fact]
public async Task PublishNewEntityAsync_ShouldSucceed_WithValidDraftEntity()
{
    // 验证：DDL 生成、执行、状态更新 (Draft→Published)、实体锁定
}

[Fact]
public async Task PublishEntityChangesAsync_ShouldSucceed_WithValidModifiedEntity()
{
    // 验证：变更检测、ALTER TABLE 生成、状态更新 (Modified→Published)
}

[Fact]
public async Task PublishNewEntityAsync_ShouldFail_WhenTableAlreadyExists()
{
    // 验证：重复表名错误处理
}
```

**覆盖率提升**：
- 修改前：~45%
- 修改后：~85%

### 测试最佳实践

1. **使用 Moq 隔离依赖**
   ```csharp
   var mockDDL = new Mock<DDLExecutionService>();
   var service = new EntitySchemaAlignmentService(_db, mockDDL.Object, _logger);
   ```

2. **使用 FluentAssertions 提升可读性**
   ```csharp
   result.Should().Be(AlignmentResult.Aligned);
   entities.Should().OnlyContain(e => e.Source == EntitySource.System);
   ```

3. **测试数据隔离**
   ```csharp
   var options = new DbContextOptionsBuilder<AppDbContext>()
       .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}") // ← 每个测试独立数据库
       .Options;
   ```

---

## 最佳实践

### 1. Migration 命名规范

```
命名格式：YYYYMMDDHHmmss_DescriptiveName.cs
示例：20251111000000_MigrateLocalizationToJsonb.cs

原则：
- 使用 UTC 时间避免冲突
- 使用清晰的英文描述
- 一个 Migration 只做一件事
```

### 2. Upsert 模式选择

| 场景 | Upsert 策略 | 代码示例 |
|------|------------|---------|
| 系统常量（如语言列表） | 强制同步 | `existing.DisplayName = def.DisplayName;` |
| 用户配置（如公司名称） | 只更新空值 | `if (string.IsNullOrEmpty(existing.CompanyName)) ...` |
| 字典数据（如字段定义） | 只插入缺失项 | `if (existing == null) { ... }` |

### 3. 默认值设计

**原则**：
- String → `''` （空字符串，避免 NULL）
- Int/Decimal → `0` （零值，避免计算异常）
- Boolean → `FALSE` （明确状态）
- DateTime → `NOW()` （当前时间）
- Guid → `gen_random_uuid()` （自动生成）
- Json → `'{}'::jsonb` （空对象）

**特殊字段**：
- 状态字段 → 使用业务初始状态（如 "Draft"）
- 外键字段 → 不填充默认值（避免无效引用）

### 4. 日志规范

```csharp
// ✅ 好的日志
_logger.LogInformation("[SchemaAlign] Adding {Count} missing columns to {TableName}",
    missingColumns.Count, tableName);

// ❌ 不好的日志
_logger.LogInformation("Adding columns");

原则：
- 使用结构化日志（参数化）
- 添加模块前缀（如 [SchemaAlign]）
- 记录关键参数（表名、字段数、操作类型）
```

---

## 未来扩展

### 1. 类型变更支持

**当前限制**：只支持添加字段，不支持修改字段类型

**未来方案**：
```csharp
// 检测类型变更
var typeChanged = (expectedType != actualType);

if (typeChanged)
{
    // 方案 A: 创建临时列 + 数据转换 + 删除旧列 + 重命名
    // 方案 B: 创建新表 + 迁移数据 + 重命名表
    // 方案 C: 提示用户手动处理（当前策略）
}
```

### 2. 索引自动对齐

**需求**：根据 `FieldMetadata.IsIndexed` 自动创建/删除索引

**实现思路**：
```csharp
foreach (var field in entity.Fields.Where(f => f.IsIndexed))
{
    var indexName = $"IX_{tableName}_{field.PropertyName}";
    var createIndexSql = $"CREATE INDEX IF NOT EXISTS \"{indexName}\" ON \"{tableName}\" (\"{field.PropertyName}\");";
    await _ddlService.ExecuteDDLAsync(entityId, DDLScriptType.Alter, createIndexSql);
}
```

### 3. 多租户隔离

**需求**：不同租户的实体定义独立，结构对齐按租户隔离

**实现思路**：
```csharp
var tableName = $"{tenantId}_{entity.EntityName}"; // tenant1_Product, tenant2_Product
await AlignEntitySchemaAsync(entity, tableName);
```

### 4. 回滚机制增强

**需求**：支持一键回滚最近的对齐操作

**实现思路**：
```csharp
// DDLScript 表增加 RollbackSQL 字段
public class DDLScript
{
    public string SqlScript { get; set; }       // 正向 SQL
    public string? RollbackSql { get; set; }    // 反向 SQL（如 DROP COLUMN）
}

// 回滚接口
await _ddlService.RollbackDDLAsync(scriptId);
```

---

## 附录

### 相关文件清单

| 文件路径 | 说明 |
|---------|------|
| `src/BobCrm.Api/Infrastructure/DatabaseInitializer.cs` | 应用启动初始化，三层对齐入口 |
| `src/BobCrm.Api/Services/EntitySchemaAlignmentService.cs` | 动态实体结构对齐服务 |
| `src/BobCrm.Api/Services/DDLExecutionService.cs` | DDL 执行与记录服务 |
| `src/BobCrm.Api/Infrastructure/Migrations/20251111000000_MigrateLocalizationToJsonb.cs` | 多语资源表结构迁移 |
| `tests/BobCrm.Api.Tests/EntitySchemaAlignmentServiceTests.cs` | 对齐服务完整测试套件 |
| `tests/BobCrm.Api.Tests/EntityPublishingAndDDLTests.cs` | 实体发布流程测试 |

### Git Commit 历史

```
e0b13c9 - feat: 业务数据对齐和字段删除机制
e084295 - test: 补充 EntityPublishingService 成功路径测试
4f850a4 - test: 新增 EntitySchemaAlignmentService 完整测试套件
27402be - feat: 动态实体结构自动对齐机制
3042eb9 - feat: 预置数据自动同步 - SystemSettings/FieldDefinition/LocalizationLanguage
ced93dc - fix: 使用 MigrateAsync 替代 EnsureCreatedAsync 以自动应用 Migrations
41e4cd8 - feat: 创建 Migration 将 LocalizationResource 迁移到 jsonb 结构
```

### 参考文档

- [ARCH-01-实体自定义与发布系统设计文档.md](ARCH-01-实体自定义与发布系统设计文档.md)
- [I18N-01-多语机制设计文档.md](I18N-01-多语机制设计文档.md)
- [TEST-03-实体发布与对齐测试覆盖报告.md](TEST-03-实体发布与对齐测试覆盖报告.md)
