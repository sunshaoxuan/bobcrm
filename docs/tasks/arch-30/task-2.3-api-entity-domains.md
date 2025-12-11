# Task 2.3 - 实体域接口改造设计文档

**任务ID**: ARCH-30-Task-2.3  
**依赖**: Task 0.3（DTO 双模式字段）  
**预计工作量**: 0.5小时  
**状态**: ⏳ 待开始  
**优先级**: 🔸 中（管理界面）  
**复杂度**: ⭐ 低（最简单）

---

## 📋 任务概述

改造实体域接口 `/api/entity-domains`，支持语言参数。

### 核心目标

1. **语言支持**: 接受 `lang` 参数，返回单语或多语实体域
2. **向后兼容**: 不传 lang 参数时保持现有行为
3. **性能优化**: 预期响应体积减少 30-40%
4. **极简实现**: 类似 Task 1.3（实体列表）

### 业务影响

- **调用频率**: 低（仅在管理界面）
- **影响用户**: 管理员用户
- **优化收益**: 管理界面加载速度提升

---

## 🏗️ 架构设计

### 当前架构

```
管理界面
  │
  ├─ GET /api/entity-domains
  │  (无 lang 参数)
  │
  ▼
EntityDomainEndpoints
  │
  ├─ 查询所有实体域
  │
  ▼
返回完整实体域（三语）
  {
    name: "Sales",
    displayName: {
      zh: "销售域",
      ja: "販売ドメイン",
      en: "Sales Domain"
    }
  }
```

### 目标架构

```
管理界面
  │
  ├─ GET /api/entity-domains?lang=zh
  │
  ▼
EntityDomainEndpoints
  │
  ├─ LangHelper.GetLang(http, lang) → "zh"
  ├─ 查询所有实体域
  ├─ 应用语言过滤
  │
  ▼
返回单语实体域
  {
    name: "Sales",
    displayName: "销售域"  // ✅ string
  }
```

---

## 📂 涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Endpoints/EntityDomainEndpoints.cs` | 修改 | 添加 lang 参数 |
| `DTOs/EntityDomainDto.cs` | 检查/修改 | 确认双模式字段 |
| `tests/.../EntityDomainTests.cs` | 新增/修改 | 语言参数测试 |

---

## 🔧 技术方案

### 方案1: 定位端点

**查找端点**:
```bash
grep -n "MapGet.*entity-domains" src/BobCrm.Api/Endpoints/
```

**典型代码结构**（推测）:
```csharp
group.MapGet("", async (AppDbContext db) =>
{
    var domains = await db.EntityDomains
        .OrderBy(d => d.Name)
        .AsNoTracking()
        .ToListAsync();
    
    return Results.Ok(new SuccessResponse<List<EntityDomainDto>>(domains));
});
```

---

### 方案2: 修改端点逻辑（极简实现）

**修改伪代码**:
```csharp
group.MapGet("", async (
    string? lang,  // ⭐ 新增参数
    HttpContext http,
    AppDbContext db) =>
{
    var targetLang = LangHelper.GetLang(http, lang);  // ⭐ 语言获取
    
    var domains = await db.EntityDomains
        .OrderBy(d => d.Name)
        .AsNoTracking()
        .ToListAsync();
    
    // ⭐ 应用语言过滤（选择方案A或B）
    var dtos = domains.Select(d => ToDomainDto(d, targetLang)).ToList();
    
    return Results.Ok(new SuccessResponse<List<EntityDomainDto>>(dtos));
})
.WithName("GetEntityDomains")
.WithSummary("获取所有实体域（支持语言参数）");
```

**核心改造仅3行**（类似 Task 1.3）:
1. `var targetLang = LangHelper.GetLang(http, lang);`
2. `var domains = await db.EntityDomains...ToListAsync();`
3. `var dtos = domains.Select(d => ToDomainDto(d, targetLang)).ToList();`

---

### 方案3: DTO转换

**选项A: 扩展方法**（推荐，如果会复用）

```csharp
// 文件：Extensions/DtoExtensions.cs
public static EntityDomainDto ToDomainDto(
    this EntityDomain domain, 
    string? lang = null)
{
    return new EntityDomainDto
    {
        Id = domain.Id,
        Name = domain.Name,
        
        // ⭐ 单语/多语模式
        DisplayName = !string.IsNullOrWhiteSpace(lang)
            ? domain.DisplayName?.Resolve(lang) ?? domain.Name
            : null,
        DisplayNameTranslations = string.IsNullOrWhiteSpace(lang)
            ? domain.DisplayName
            : null
    };
}
```

**选项B: 端点内直接映射**（更简单，如果只用一次）

```csharp
var dtos = domains.Select(d => new EntityDomainDto
{
    Id = d.Id,
    Name = d.Name,
    DisplayName = !string.IsNullOrWhiteSpace(targetLang)
        ? d.DisplayName?.Resolve(targetLang) ?? d.Name
        : null,
    DisplayNameTranslations = string.IsNullOrWhiteSpace(targetLang)
        ? d.DisplayName
        : null
}).ToList();
```

**推荐**: 选项B（端点内映射），因为结构简单且只用一次

---

### 方案4: DTO 检查

**检查方法**:
```bash
grep -A 15 "class EntityDomainDto" src/BobCrm.Api/Contracts/DTOs/
```

**如果缺少双模式字段**，需要先添加：

```csharp
public class EntityDomainDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // ✅ 单语字段
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
    
    // ✅ 多语字段（向后兼容）
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DisplayNameTranslations { get; set; }
    
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## 🧪 测试策略

### 测试用例设计

#### 测试1: 无 lang 参数（向后兼容）

```csharp
[Fact]
public async Task GetEntityDomains_WithoutLang_ReturnsMultilingual()
{
    // Arrange
    var client = await CreateAuthenticatedClientAsync();
    
    // Act
    var response = await client.GetAsync("/api/entity-domains");
    response.EnsureSuccessStatusCode();
    
    // Assert
    using var json = await ReadJsonAsync(response);
    var domains = json.RootElement.GetProperty("data");
    
    if (domains.GetArrayLength() == 0) return; // 空数据跳过
    
    var firstDomain = domains[0];
    Assert.False(firstDomain.TryGetProperty("displayName", out _));
    Assert.True(firstDomain.TryGetProperty("displayNameTranslations", out var translations));
    Assert.Equal(JsonValueKind.Object, translations.ValueKind);
}
```

---

#### 测试2: 指定 lang 参数（单语模式）

```csharp
[Fact]
public async Task GetEntityDomains_WithLang_ReturnsSingleLanguage()
{
    // Arrange
    var client = await CreateAuthenticatedClientAsync();
    
    // Act
    var response = await client.GetAsync("/api/entity-domains?lang=ja");
    response.EnsureSuccessStatusCode();
    
    // Assert
    using var json = await ReadJsonAsync(response);
    var domains = json.RootElement.GetProperty("data");
    
    if (domains.GetArrayLength() == 0) return;
    
    var firstDomain = domains[0];
    Assert.True(firstDomain.TryGetProperty("displayName", out var displayName));
    Assert.Equal(JsonValueKind.String, displayName.ValueKind);
    Assert.False(firstDomain.TryGetProperty("displayNameTranslations", out _));
}
```

---

#### 测试3: 响应体积减少验证

```csharp
[Fact]
public async Task GetEntityDomains_SingleLanguage_ReducesPayloadSize()
{
    // Arrange
    var client = await CreateAuthenticatedClientAsync();
    
    // Act
    var multiLangResp = await client.GetAsync("/api/entity-domains");
    var singleLangResp = await client.GetAsync("/api/entity-domains?lang=zh");
    
    var multiLangJson = await multiLangResp.Content.ReadAsStringAsync();
    var singleLangJson = await singleLangResp.Content.ReadAsStringAsync();
    
    // Assert
    if (multiLangJson.Length < 50) return; // 数据太少，跳过
    
    Assert.True(singleLangJson.Length < multiLangJson.Length);
    var reduction = 1.0 - ((double)singleLangJson.Length / multiLangJson.Length);
    Assert.True(reduction >= 0.2, 
        $"Expected >=20% reduction, got {reduction:P}");
}
```

---

## 📋 实施步骤

### 步骤1: 检查 DTO

```bash
# 1.1 检查 EntityDomainDto
cat src/BobCrm.Api/Contracts/DTOs/EntityDomainDto.cs

# 1.2 如果缺少双模式字段，添加它们
```

---

### 步骤2: 修改端点

```bash
# 2.1 打开文件
code src/BobCrm.Api/Endpoints/EntityDomainEndpoints.cs

# 2.2 定位 GET /api/entity-domains 端点
# 2.3 添加 lang 参数
# 2.4 使用 LangHelper.GetLang
# 2.5 端点内直接映射（选项B）
```

---

### 步骤3: 编写测试

```bash
# 3.1 创建或打开测试文件
code tests/BobCrm.Api.Tests/EntityDomainTests.cs

# 3.2 添加 3 个测试用例
# 3.3 运行测试
dotnet test --filter "FullyQualifiedName~EntityDomainTests"
```

---

### 步骤4: 验证

```bash
# 编译和测试
dotnet build BobCrm.sln -c Debug
dotnet test --filter "FullyQualifiedName~EntityDomainTests"
```

---

## 🎯 验收标准

### 功能验收

- [ ] `/api/entity-domains` 接受 `lang` 参数
- [ ] 单语模式返回 `displayName: string`
- [ ] 多语模式返回 `displayNameTranslations: object`
- [ ] 向后兼容

### 性能验收

- [ ] 响应体积减少 ≥ 20%（目标 30-40%）

### 测试验收

- [ ] 至少 3 个测试用例全部通过

---

## 📝 Git 提交规范

```
feat(api): add lang parameter support to /api/entity-domains

- Add optional lang query parameter to GetEntityDomains endpoint
- Apply language filtering using inline mapping (simple structure)
- Dual-mode fields: DisplayName (string) / DisplayNameTranslations (dict)

Test coverage:
- WithoutLang: multilingual mode (backward compat)
- WithLang: single-language mode  
- Performance: verifies >=20% reduction

Ref: ARCH-30 Task 2.3
```

---

## ⚠️ 注意事项

### 注意1: 极简实现

Task 2.3 是阶段2**最简单的任务**，应保持代码极简：
- 核心逻辑仅3行
- 端点内直接映射（无需扩展方法）
- 测试简洁

### 注意2: 认证要求

`/api/entity-domains` 可能需要管理员权限：
```csharp
.RequireAuthorization()
```

---

## 🔗 相关文档

- [ARCH-30 设计文档](../../design/ARCH-30-实体字段显示名多语元数据驱动设计.md) - 第2154-2156行
- [Task 1.3 设计](task-1.3-api-entities.md) - 极简任务参考

---

**文档类型**: 技术设计文档  
**复杂度**: 极低（3行核心逻辑）  
**目标读者**: 开发者  
**维护者**: ARCH-30 架构组  
**最后更新**: 2025-12-11

