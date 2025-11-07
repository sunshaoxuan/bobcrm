# 数据库迁移修复说明

## 问题描述

添加了 `FormTemplate` 实体和数据库迁移后，测试失败（94个测试失败）。

原因：EF Core 的模型快照文件（`AppDbContextModelSnapshot.cs`）没有包含新的 `FormTemplate` 实体。

## 修复步骤

在本地开发环境中运行以下命令：

```bash
# 1. 确保数据库正在运行
docker compose up -d

# 2. 删除现有的 AddFormTemplateTable 迁移（如果存在）
dotnet ef migrations remove --project src/BobCrm.Api

# 3. 重新创建迁移（这会自动更新模型快照）
dotnet ef migrations add AddFormTemplateTable --project src/BobCrm.Api

# 4. 应用迁移到数据库
dotnet ef database update --project src/BobCrm.Api

# 5. 运行测试验证
dotnet test
```

## 验证

修复成功后，应该看到：
- `src/BobCrm.Api/Infrastructure/Migrations/AppDbContextModelSnapshot.cs` 包含 `FormTemplate` 实体
- 所有测试通过

## 相关文件

- ✅ `src/BobCrm.Api/Domain/Models/FormTemplate.cs` - 域模型已创建
- ✅ `src/BobCrm.Api/Infrastructure/AppDbContext.cs` - DbSet 已注册
- ✅ `src/BobCrm.Api/Endpoints/TemplateEndpoints.cs` - API 端点已实现
- ⚠️ `src/BobCrm.Api/Infrastructure/Migrations/AppDbContextModelSnapshot.cs` - **需要更新**

## 临时解决方案

如果无法立即修复，可以暂时：
1. 注释掉 `AppDbContext.cs` 中的 `FormTemplates` DbSet
2. 运行测试（会跳过 FormTemplate 相关的功能）
3. 修复模型快照后，取消注释

但**不推荐**这个方案，因为会导致功能不完整。
