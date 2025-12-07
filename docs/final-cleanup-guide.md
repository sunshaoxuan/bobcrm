# 最终清理指南 - OpenAPI 文档 (Stage 3)

## 🎯 目标

将后端 API 的 OpenAPI 文档描述（Swagger UI 显示的文本）从中文转换为英文。这是实现 0 违规的最后一步。

---

## 📋 待清理文件清单

所有位于 `src/BobCrm.Api/Endpoints/` 下的文件。

### 1. AdminEndpoints.cs
**修改示例**:
```csharp
// 之前
.WithTags("管理")
.WithSummary("获取系统信息")

// 之后
.WithTags("Admin")
.WithSummary("Get system information")
```

### 2. AuthEndpoints.cs
**修改示例**:
```csharp
// 之前
.WithTags("认证")
.WithSummary("用户注册")
.WithDescription("注册新用户...")

// 之后
.WithTags("Auth")
.WithSummary("User registration")
.WithDescription("Register a new user...")
```

### 3. EntityEndpoints.cs
**修改示例**:
```csharp
.WithTags("Entities")
.WithSummary("Get entity records")
```

### 4. EnumEndpoints.cs
**修改示例**:
```csharp
.WithTags("Enums")
.WithSummary("Get enum definitions")
```

### 5. TemplateEndpoints.cs
**修改示例**:
```csharp
.WithTags("Templates")
.WithSummary("Get templates")
```

---

## 🛠️ 翻译对照表

| 中文 | 英文 |
|---|---|
| 认证 | Auth |
| 管理 | Admin |
| 实体 | Entities |
| 枚举 | Enums |
| 模板 | Templates |
| 获取 | Get |
| 创建 | Create |
| 更新 | Update |
| 删除 | Delete |
| 列表 | list |
| 详情 | details |
| 注册 | Register |
| 登录 | Login |
| 刷新令牌 | Refresh token |
| 修改密码 | Change password |

---

## ✅ 验证步骤

1. **修改代码**
2. **验证构建**: `dotnet build src/BobCrm.Api/BobCrm.Api.csproj`
3. **验证扫描**: `pwsh ./scripts/check-i18n.ps1 --severity WARNING` (应该接近 0 违规)

---

**完成后通知我进行最终验证！**
