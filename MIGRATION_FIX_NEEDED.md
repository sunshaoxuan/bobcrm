# 数据库迁移修复说明

## ✅ 已修复

AppDbContextModelSnapshot.cs 已经手动更新，包含了 FormTemplate 实体定义。

## 🔧 需要在本地执行的步骤

### 1. 拉取最新代码

```powershell
git pull origin claude/entity-matching-template-render-011CUskuQxzSrG45HgXUsnxC
```

### 2. 删除重复的迁移文件

**重要：** 您的本地环境中有一个重复的迁移文件导致编译错误。

删除这个文件：
```
src/BobCrm.Api/Infrastructure/Migrations/20251107043832_AddFormTemplateTable.cs
```

使用以下命令：
```powershell
Remove-Item src\BobCrm.Api\Infrastructure\Migrations\20251107043832_AddFormTemplateTable.cs
```

**保留原始文件：**
```
src/BobCrm.Api/Infrastructure/Migrations/20251107030000_AddFormTemplateTable.cs  ✅ 保留这个
```

### 3. 删除并重建数据库

由于迁移历史的问题，最简单的方法是重建数据库：

```powershell
# 确保 Docker 容器正在运行
docker compose up -d

# 删除数据库（强制）
dotnet ef database drop --project src/BobCrm.Api --force

# 应用所有迁移（包括 AddFormTemplateTable）
dotnet ef database update --project src/BobCrm.Api
```

### 4. 运行测试验证

```powershell
dotnet test
```

## 预期结果

修复成功后，应该看到：
- ✅ 编译成功（无 CS0111 错误）
- ✅ 数据库包含 FormTemplates 表
- ✅ 测试通过（预期：101 通过，3 跳过）

## 问题原因说明

1. **原始问题：** AppDbContextModelSnapshot.cs 缺少 FormTemplate 实体定义
2. **尝试修复时：** 运行 `dotnet ef migrations add AddFormTemplateTable` 创建了重复的迁移类
3. **编译错误：** 两个同名的迁移类（AddFormTemplateTable）导致 CS0111 错误
4. **解决方案：** 手动更新模型快照 + 删除重复迁移 + 重建数据库

## 已修复的文件

- ✅ `src/BobCrm.Api/Infrastructure/Migrations/AppDbContextModelSnapshot.cs` - 已添加 FormTemplate 实体
- ✅ Commit: `1f01ccd` - fix: 添加FormTemplate实体到EF Core模型快照

## 如果遇到问题

1. **编译错误仍然存在：** 确认已删除 `20251107043832_AddFormTemplateTable.cs` 文件
2. **数据库更新失败：** 尝试完全重启 Docker 容器
   ```powershell
   docker compose down
   docker compose up -d
   ```
3. **测试失败：** 检查数据库是否已成功创建 FormTemplates 表
   - 可以使用 pgAdmin 或其他 PostgreSQL 工具查看

## 技术细节

FormTemplate 实体在模型快照中的定义包含：

**属性：**
- Id (int, PK, Identity)
- Name (string, required)
- EntityType (string, nullable)
- UserId (string, required)
- IsUserDefault (bool)
- IsSystemDefault (bool)
- LayoutJson (string, nullable)
- Description (string, nullable)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)
- IsInUse (bool)

**索引：**
- IX_FormTemplates_UserId_EntityType
- IX_FormTemplates_UserId_EntityType_IsUserDefault
- IX_FormTemplates_EntityType_IsSystemDefault
