# Task 1.3 - 实体列表API改造设计文档

**任务ID**: ARCH-30-Task-1.3  
**依赖**: Task 0.3（DTO 双模式字段）  
**预计工作量**: 0.5小时  
**状态**: ⏳ 待开始  
**优先级**: 🔥 高（路由初始化性能）

---

## 📋 任务概述

改造实体列表 API `/api/entities`，支持语言参数，优化路由初始化性能。

### 核心目标

1. **性能优化**: 响应体积从 ~20KB → ~7KB（**节省 13KB**）
2. **路由提速**: 路由初始化时间减少
3. **语言支持**: 返回用户语言的实体显示名
4. **向后兼容**: 保持现有 API 契约

### 业务影响

- **调用频率**: 每次应用启动/路由初始化
- **影响用户**: 100% 用户
- **优化收益**: 应用启动速度提升

---

## 🏗️ 架构设计

### 当前架构

```
浏览器
  │
  ├─ GET /api/entities
  │  (无 lang 参数)
  │
  ▼
EntityDefinitionEndpoints
  │
  ├─ 查询所有已启用实体
  │
  ├─ 返回完整多语字典
  │
  ▼
返回实体列表（三语）
  [
    {
      entityName: "Customer",
      displayName: {
        zh: "客户",
        ja: "顧客",
        en: "Customer"
      }
    }
  ]
```

### 目标架构

```
浏览器
  │
  ├─ GET /api/entities?lang=zh
  │
  ▼
EntityDefinitionEndpoints
  │
  ├─ 获取语言: LangHelper.GetLang(http, lang)
  │
  ├─ 查询实体 + 应用语言过滤
  │  entities.Select(e => e.ToSummaryDto(lang))
  │
  ▼
返回实体列表（单语）
  [
    {
      entityName: "Customer",
      displayName: "客户"  // ✅ string 类型
    }
  ]
```

---

## 📂 涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Endpoints/EntityDefinitionEndpoints.cs` | 修改 | 添加 lang 参数，应用过滤 |
| `tests/.../EntityDefinitionEndpointsTests.cs` | 修改 | 添加语言参数测试 |

**注意**: 此任务**不需要修改 Service 层**，因为逻辑简单，直接在 Endpoint 完成。

---

## 🔧 技术方案

### 方案1: 定位端点

**查找代码**:
```bash
# 定位 GET /api/entities 端点（不是 /api/entities/{type}）
grep -n 'MapGet.*"entities"' src/BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs
```

**典型代码结构**:
```csharp
entitiesGroup.MapGet("", async (AppDbContext db) =>
{
    var entities = await db.EntityDefinitions
        .Where(ed => ed.IsEnabled && ed.Status == "Published")
        .AsNoTracking()
        .ToListAsync();
    
    // 当前可能直接返回实体，或简单映射
    var dtos = entities.Select(e => new EntitySummaryDto
    {
        // 基础字段映射
        DisplayName = new MultilingualText(e.DisplayName)  // ❌ 返回完整字典
    }).ToList();
    
    return Results.Ok(new SuccessResponse<List<EntitySummaryDto>>(dtos));
});
```

---

### 方案2: 修改端点逻辑

**修改伪代码**:
```csharp
entitiesGroup.MapGet("", async (
    string? lang,  // ⭐ 新增参数
    HttpContext http,
    AppDbContext db
) =>
{
    // ⭐ 获取目标语言
    var targetLang = lang ?? LangHelper.GetLang(http);
    
    // 查询实体（不变）
    var entities = await db.EntityDefinitions
        .Where(ed => ed.IsEnabled && ed.Status == "Published")
        .AsNoTracking()
        .ToListAsync();
    
    // ⭐ 使用扩展方法转换（应用语言过滤）
    var dtos = entities
        .Select(e => e.ToSummaryDto(targetLang))  // ✅ 使用 Task 0.3 的扩展方法
        .ToList();
    
    return Results.Ok(new SuccessResponse<List<EntitySummaryDto>>(dtos));
})
.WithName("GetEntities")
.WithSummary("获取所有已启用实体（支持语言参数）")
.WithDescription("返回所有已发布的实体列表。支持 ?lang=zh/ja/en 参数");
```

**关键改进**:
1. ✅ 添加 `lang` 参数
2. ✅ 使用 `LangHelper.GetLang` 处理回退
3. ✅ 使用 `ToSummaryDto(targetLang)` 应用语言过滤
4. ✅ 代码简洁（无需 Service 层）

---

## 🧪 测试策略

### 测试用例设计

#### 测试1: 无 lang 参数（向后兼容）

```csharp
[Fact]
public async Task GetEntities_WithoutLang_ReturnsMultilingual()
{
    // Act
    var response = await client.GetAsync("/api/entities");
    
    // Assert
    var result = await Deserialize<SuccessResponse<List<EntitySummaryDto>>>(response);
    var firstEntity = result.Data.First();
    
    Assert.Null(firstEntity.DisplayName);
    Assert.NotNull(firstEntity.DisplayNameTranslations);
    Assert.True(firstEntity.DisplayNameTranslations.Count >= 2);
}
```

---

#### 测试2: 指定语言返回单语

```csharp
[Fact]
public async Task GetEntities_WithLang_ReturnsSingleLanguage()
{
    // Act
    var response = await client.GetAsync("/api/entities?lang=zh");
    
    // Assert
    var result = await Deserialize<SuccessResponse<List<EntitySummaryDto>>>(response);
    
    foreach (var entity in result.Data)
    {
        Assert.NotNull(entity.DisplayName);  // 单语字段有值
        Assert.IsType<string>(entity.DisplayName);  // 是 string 类型
        Assert.Null(entity.DisplayNameTranslations);  // 多语字段为 null
    }
}
```

---

#### 测试3: 响应体积减少验证

```csharp
[Fact]
public async Task GetEntities_SingleLanguage_ReducesResponseSize()
{
    // Act
    var multiLangResp = await client.GetAsync("/api/entities");
    var singleLangResp = await client.GetAsync("/api/entities?lang=zh");
    
    var multiLangJson = await multiLangResp.Content.ReadAsStringAsync();
    var singleLangJson = await singleLangResp.Content.ReadAsStringAsync();
    
    // Assert - 预期减少约 65%
    var reduction = 1.0 - ((double)singleLangJson.Length / multiLangJson.Length);
    Assert.True(reduction >= 0.5, $"Expected >=50%, got {reduction:P}");
}
```

---

## 📋 实施步骤

### 步骤1: 修改端点

```bash
# 1. 打开文件
code src/BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs

# 2. 定位 GET /api/entities 端点（注意不是 /api/entities/{type}）
# 搜索 MapGet("") 或 MapGet("/")

# 3. 添加 lang 参数
# 4. 使用 LangHelper.GetLang
# 5. 使用 ToSummaryDto(lang) 转换
```

---

### 步骤2: 编写测试

```bash
# 1. 打开或创建测试文件
code tests/BobCrm.Api.Tests/Endpoints/EntityDefinitionEndpointsTests.cs

# 2. 添加 3 个测试用例
# 3. 运行测试
dotnet test --filter "FullyQualifiedName~EntityDefinitionEndpointsTests.GetEntities"
```

---

### 步骤3: 验证

```bash
# 编译
dotnet build BobCrm.sln -c Debug

# 测试
dotnet test --filter "FullyQualifiedName~EntityDefinitionEndpointsTests"

# 手动测试（可选）
curl "https://localhost:5001/api/entities?lang=zh"
```

---

## ✅ 验收标准

### 功能验收

- [ ] `/api/entities` 接受 `lang` 参数
- [ ] 单语模式返回 `displayName: string`
- [ ] 多语模式返回 `displayNameTranslations: object`
- [ ] 向后兼容

### 性能验收

- [ ] 响应体积减少 ≥ 50%（约从 20KB → 7KB）
- [ ] 应用启动时间有改善

### 测试验收

- [ ] 至少 3 个测试用例全部通过
- [ ] 包含响应体积验证

---

## 🎯 预期收益

| 指标 | 改造前 | 改造后 | 改善 |
|------|--------|--------|------|
| 响应体积 | ~20KB | ~7KB | **-65%** |
| 网络传输时间 | ~100ms | ~35ms | -65ms |
| JSON 解析时间 | ~5ms | ~2ms | -3ms |

---

**文档类型**: 技术设计文档  
**复杂度**: 低（最简单的任务）  
**目标读者**: 开发者  
**维护者**: ARCH-30 架构组  
**最后更新**: 2025-12-11

