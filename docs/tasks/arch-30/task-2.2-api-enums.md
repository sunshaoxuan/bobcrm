# Task 2.2 - 枚举接口改造设计文档

**任务ID**: ARCH-30-Task-2.2  
**依赖**: Task 0.3（DTO 双模式字段）  
**预计工作量**: 0.5小时  
**状态**: ⏳ 待开始  
**优先级**: 🔸 中（管理界面）  
**复杂度**: ⭐ 低

---

## 📋 任务概述

改造枚举接口 `/api/enums`，支持语言参数，优化管理界面的枚举显示。

### 核心目标

1. **语言支持**: 接受 `lang` 参数，返回单语或多语枚举
2. **枚举值处理**: 枚举值（`EnumValue`）也需要语言过滤
3. **向后兼容**: 不传 lang 参数时保持现有行为
4. **性能优化**: 预期响应体积减少 40-50%

### 业务影响

- **调用频率**: 中（管理界面加载时）
- **影响用户**: 管理员用户
- **优化收益**: 管理界面加载速度提升

---

## 🏗️ 架构设计

### 当前架构

```
管理界面
  │
  ├─ GET /api/enums
  │  (无 lang 参数)
  │
  ▼
EnumDefinitionEndpoints
  │
  ├─ 查询所有枚举定义
  │
  ▼
返回完整枚举（三语）
  {
    name: "CustomerStatus",
    displayName: {
      zh: "客户状态",
      ja: "顧客ステータス",
      en: "Customer Status"
    },
    values: [
      {
        code: "Active",
        displayName: {
          zh: "活跃",
          ja: "アクティブ",
          en: "Active"
        }
      }
    ]
  }
```

### 目标架构

```
管理界面
  │
  ├─ GET /api/enums?lang=zh
  │
  ▼
EnumDefinitionEndpoints
  │
  ├─ LangHelper.GetLang(http, lang) → "zh"
  ├─ 查询所有枚举定义
  ├─ 应用语言过滤（枚举 + 枚举值）
  │
  ▼
返回单语枚举
  {
    name: "CustomerStatus",
    displayName: "客户状态",  // ✅ string
    values: [
      {
        code: "Active",
        displayName: "活跃"  // ✅ string
      }
    ]
  }
```

---

## 📂 涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Endpoints/EnumDefinitionEndpoints.cs` | 修改 | 添加 lang 参数 |
| `DTOs/EnumDefinitionDto.cs` | 检查/修改 | 确认双模式字段 |
| `DTOs/EnumValueDto.cs` | 检查/修改 | 确认双模式字段 |
| `tests/.../EnumEndpointsTests.cs` | 新增/修改 | 语言参数测试 |

---

## 🔧 技术方案

### 方案1: 定位端点

**查找枚举端点**:
```bash
grep -n "MapGet.*enums" src/BobCrm.Api/Endpoints/EnumDefinitionEndpoints.cs
```

**典型代码结构**（推测）:
```csharp
group.MapGet("", async (AppDbContext db) =>
{
    var enums = await db.EnumDefinitions
        .Include(e => e.Values)
        .ToListAsync();
    
    // 当前可能直接返回，或简单映射
    return Results.Ok(new SuccessResponse<List<EnumDefinitionDto>>(enums));
});
```

---

### 方案2: 修改端点逻辑

**修改伪代码**:
```csharp
group.MapGet("", async (
    string? lang,  // ⭐ 新增参数
    HttpContext http,
    AppDbContext db) =>
{
    // ⭐ 获取目标语言
    var targetLang = LangHelper.GetLang(http, lang);
    
    // 查询枚举定义（包含枚举值）
    var enums = await db.EnumDefinitions
        .Include(e => e.Values)
        .AsNoTracking()
        .ToListAsync();
    
    // ⭐ 应用语言过滤
    var dtos = enums.Select(e => ToEnumDto(e, targetLang)).ToList();
    
    return Results.Ok(new SuccessResponse<List<EnumDefinitionDto>>(dtos));
})
.WithName("GetEnums")
.WithSummary("获取所有枚举定义（支持语言参数）");
```

---

### 方案3: 枚举DTO转换

**选项A: 使用扩展方法**（推荐）

创建 `ToEnumDto` 扩展方法:

```csharp
// 文件：Extensions/EnumExtensions.cs
public static EnumDefinitionDto ToEnumDto(
    this EnumDefinition enumDef, 
    string? lang = null)
{
    return new EnumDefinitionDto
    {
        Id = enumDef.Id,
        Name = enumDef.Name,
        
        // ⭐ 单语/多语模式
        DisplayName = !string.IsNullOrWhiteSpace(lang)
            ? enumDef.DisplayName?.Resolve(lang) ?? enumDef.Name
            : null,
        DisplayNameTranslations = string.IsNullOrWhiteSpace(lang)
            ? enumDef.DisplayName
            : null,
        
        // ⭐ 枚举值应用语言过滤
        Values = enumDef.Values
            .Select(v => v.ToEnumValueDto(lang))
            .ToList()
    };
}

public static EnumValueDto ToEnumValueDto(
    this EnumValue value, 
    string? lang = null)
{
    return new EnumValueDto
    {
        Code = value.Code,
        Value = value.Value,
        
        // ⭐ 单语/多语模式
        DisplayName = !string.IsNullOrWhiteSpace(lang)
            ? value.DisplayName?.Resolve(lang) ?? value.Code
            : null,
        DisplayNameTranslations = string.IsNullOrWhiteSpace(lang)
            ? value.DisplayName
            : null
    };
}
```

**选项B: 端点内直接映射**（如果结构简单）

```csharp
var dtos = enums.Select(e => new EnumDefinitionDto
{
    Id = e.Id,
    Name = e.Name,
    DisplayName = !string.IsNullOrWhiteSpace(targetLang)
        ? e.DisplayName?.Resolve(targetLang) ?? e.Name
        : null,
    DisplayNameTranslations = string.IsNullOrWhiteSpace(targetLang)
        ? e.DisplayName
        : null,
    Values = e.Values.Select(v => new EnumValueDto
    {
        Code = v.Code,
        DisplayName = !string.IsNullOrWhiteSpace(targetLang)
            ? v.DisplayName?.Resolve(targetLang) ?? v.Code
            : null,
        DisplayNameTranslations = string.IsNullOrWhiteSpace(targetLang)
            ? v.DisplayName
            : null
    }).ToList()
}).ToList();
```

**推荐**: 选项A（扩展方法），代码更清晰，可复用

---

### 方案4: DTO 检查

**需要确认**:
1. `EnumDefinitionDto` 是否有双模式字段？
2. `EnumValueDto` 是否有双模式字段？

**检查方法**:
```bash
grep -A 20 "class EnumDefinitionDto" src/BobCrm.Api/Contracts/DTOs/
grep -A 20 "class EnumValueDto" src/BobCrm.Api/Contracts/DTOs/
```

**如果没有双模式字段**，需要先添加：

```csharp
public class EnumDefinitionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // ✅ 单语字段
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
    
    // ✅ 多语字段（向后兼容）
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DisplayNameTranslations { get; set; }
    
    public List<EnumValueDto> Values { get; set; } = new();
}

public class EnumValueDto
{
    public string Code { get; set; } = string.Empty;
    public int Value { get; set; }
    
    // ✅ 单语字段
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
    
    // ✅ 多语字段（向后兼容）
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DisplayNameTranslations { get; set; }
}
```

---

## 🧪 测试策略

### 测试用例设计

#### 测试1: 无 lang 参数（向后兼容）

```csharp
[Fact]
public async Task GetEnums_WithoutLang_ReturnsMultilingual()
{
    // Arrange
    var client = await CreateAuthenticatedClientAsync();
    
    // Act
    var response = await client.GetAsync("/api/enums");
    response.EnsureSuccessStatusCode();
    
    // Assert
    using var json = await ReadJsonAsync(response);
    var enums = json.RootElement.GetProperty("data");
    
    if (enums.GetArrayLength() == 0) return; // 空数据跳过
    
    var firstEnum = enums[0];
    Assert.False(firstEnum.TryGetProperty("displayName", out _));
    Assert.True(firstEnum.TryGetProperty("displayNameTranslations", out var translations));
    Assert.Equal(JsonValueKind.Object, translations.ValueKind);
    
    // ⭐ 验证枚举值也是多语
    if (firstEnum.TryGetProperty("values", out var values) && values.GetArrayLength() > 0)
    {
        var firstValue = values[0];
        Assert.False(firstValue.TryGetProperty("displayName", out _));
        Assert.True(firstValue.TryGetProperty("displayNameTranslations", out _));
    }
}
```

---

#### 测试2: 指定 lang 参数（单语模式）

```csharp
[Fact]
public async Task GetEnums_WithLang_ReturnsSingleLanguage()
{
    // Arrange
    var client = await CreateAuthenticatedClientAsync();
    
    // Act
    var response = await client.GetAsync("/api/enums?lang=ja");
    response.EnsureSuccessStatusCode();
    
    // Assert
    using var json = await ReadJsonAsync(response);
    var enums = json.RootElement.GetProperty("data");
    
    if (enums.GetArrayLength() == 0) return;
    
    var firstEnum = enums[0];
    Assert.True(firstEnum.TryGetProperty("displayName", out var displayName));
    Assert.Equal(JsonValueKind.String, displayName.ValueKind);
    Assert.False(firstEnum.TryGetProperty("displayNameTranslations", out _));
    
    // ⭐ 验证枚举值也是单语
    if (firstEnum.TryGetProperty("values", out var values) && values.GetArrayLength() > 0)
    {
        var firstValue = values[0];
        Assert.True(firstValue.TryGetProperty("displayName", out var valueName));
        Assert.Equal(JsonValueKind.String, valueName.ValueKind);
        Assert.False(firstValue.TryGetProperty("displayNameTranslations", out _));
    }
}
```

---

#### 测试3: 响应体积减少验证

```csharp
[Fact]
public async Task GetEnums_SingleLanguage_ReducesPayloadSize()
{
    // Arrange
    var client = await CreateAuthenticatedClientAsync();
    
    // Act
    var multiLangResp = await client.GetAsync("/api/enums");
    var singleLangResp = await client.GetAsync("/api/enums?lang=zh");
    
    multiLangResp.EnsureSuccessStatusCode();
    singleLangResp.EnsureSuccessStatusCode();
    
    var multiLangJson = await multiLangResp.Content.ReadAsStringAsync();
    var singleLangJson = await singleLangResp.Content.ReadAsStringAsync();
    
    // Assert
    if (multiLangJson.Length < 100) return; // 数据太少，跳过性能测试
    
    Assert.True(singleLangJson.Length < multiLangJson.Length);
    var reduction = 1.0 - ((double)singleLangJson.Length / multiLangJson.Length);
    Assert.True(reduction >= 0.3, 
        $"Expected >=30% reduction, got {reduction:P} (multi={multiLangJson.Length}, single={singleLangJson.Length})");
}
```

---

## 📋 实施步骤

### 步骤1: 检查 DTO 定义

```bash
# 1.1 检查 EnumDefinitionDto
cat src/BobCrm.Api/Contracts/DTOs/EnumDefinitionDto.cs

# 1.2 检查 EnumValueDto
cat src/BobCrm.Api/Contracts/DTOs/EnumValueDto.cs

# 1.3 如果缺少双模式字段，添加它们
```

---

### 步骤2: 创建扩展方法（可选）

```bash
# 2.1 创建文件（如果不存在）
code src/BobCrm.Api/Extensions/EnumExtensions.cs

# 2.2 实现 ToEnumDto 和 ToEnumValueDto
# 2.3 添加 XML 注释
```

---

### 步骤3: 修改端点

```bash
# 3.1 打开文件
code src/BobCrm.Api/Endpoints/EnumDefinitionEndpoints.cs

# 3.2 定位 GET /api/enums 端点
# 3.3 添加 lang 参数
# 3.4 使用 LangHelper.GetLang
# 3.5 应用语言过滤
```

---

### 步骤4: 编写测试

```bash
# 4.1 创建或打开测试文件
code tests/BobCrm.Api.Tests/Endpoints/EnumEndpointsTests.cs

# 4.2 添加 3 个测试用例
# 4.3 运行测试
dotnet test --filter "FullyQualifiedName~EnumEndpointsTests"
```

---

### 步骤5: 验证

```bash
# 5.1 编译
dotnet build BobCrm.sln -c Debug

# 5.2 运行测试
dotnet test --filter "FullyQualifiedName~EnumEndpoints"

# 5.3 手动测试（可选）
curl "https://localhost:5001/api/enums?lang=zh"
```

---

## 🎯 验收标准

### 功能验收

- [ ] `/api/enums` 接受 `lang` 参数
- [ ] 单语模式返回 `displayName: string`（枚举定义和枚举值）
- [ ] 多语模式返回 `displayNameTranslations: object`
- [ ] 向后兼容

### 性能验收

- [ ] 响应体积减少 ≥ 30%（目标 40-50%）

### 测试验收

- [ ] 至少 3 个测试用例全部通过
- [ ] 包含枚举值的语言模式测试

---

## 📝 Git 提交规范

```
feat(api): add lang parameter support to /api/enums

- Add optional lang query parameter to GetEnums endpoint
- Create ToEnumDto/ToEnumValueDto extension methods for language filtering
- Apply language filtering to both enum definitions and enum values
- Ensure dual-mode fields (DisplayName/DisplayNameTranslations)

Test coverage:
- WithoutLang: returns multilingual (backward compat)
- WithLang: returns single-language mode
- Validates enum values language consistency
- Performance test: verifies >=30% payload reduction

Performance impact:
- Expected reduction: 40-50% for typical enum responses

Ref: ARCH-30 Task 2.2
```

---

## ⚠️ 注意事项

### 注意1: 枚举值的回退逻辑

枚举值的 `DisplayName` 回退链：
```csharp
value.DisplayName?.Resolve(lang) ?? value.Code  // 回退到 Code
```

### 注意2: 空枚举处理

如果枚举定义没有枚举值（`Values` 为空），测试应优雅处理：
```csharp
if (enums.GetArrayLength() == 0) return; // 空数据跳过
```

### 注意3: 管理员认证

`/api/enums` 可能需要管理员权限：
```csharp
.RequireAuthorization()  // 或 .RequireRole("Admin")
```

---

## 🔗 相关文档

- [ARCH-30 设计文档](../../design/ARCH-30-实体字段显示名多语元数据驱动设计.md) - 第2135-2152行
- [Task 0.3 设计](task-0.3-dto-definitions.md) - DTO 双模式参考
- [Task 1.3 设计](task-1.3-api-entities.md) - 简单任务参考

---

**文档类型**: 技术设计文档  
**复杂度**: 低（类似 Task 1.3 + 枚举值处理）  
**目标读者**: 开发者  
**维护者**: ARCH-30 架构组  
**最后更新**: 2025-12-11

