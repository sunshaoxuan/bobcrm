# Task 1.2 - 导航菜单API改造设计文档

**任务ID**: ARCH-30-Task-1.2  
**依赖**: Task 0.3（DTO 双模式字段）  
**预计工作量**: 0.5-1小时  
**状态**: ⏳ 待开始  
**优先级**: 🔥 高（语言一致性修复）

---

## 📋 任务概述

改造导航菜单API `/api/templates/menu-bindings`，修复语言不一致问题（使用用户语言替代系统语言）。

### 核心目标

1. **修复 Bug**: 日语用户看到中文菜单 → 看到日语菜单
2. **语言一致性**: 使用用户语言而非系统默认语言
3. **性能优化**: 支持单语模式，减少响应体积
4. **向后兼容**: 保持现有功能不受影响

### 问题现状

**当前行为**:
```
用户语言: ja (日语)
系统默认语言: zh (中文)
菜单显示: "客户管理" (❌ 中文，不符合用户预期)
```

**期望行为**:
```
用户语言: ja (日语)
菜单显示: "顧客管理" (✅ 日语，符合用户预期)
```

---

## 🏗️ 架构设计

### 当前架构（有问题）

```
浏览器 (用户语言: ja)
  │
  ├─ GET /api/templates/menu-bindings
  │  (无 lang 参数)
  │
  ▼
TemplateEndpoints
  │
  ├─ 查询系统设置获取默认语言
  │  systemLanguage = "zh"  ❌ 使用系统语言
  │
  ├─ 使用 systemLanguage 解析菜单名
  │
  ▼
返回中文菜单
  {
    entityDisplayName: "客户",  ❌ 中文
    menuName: "客户管理"
  }
```

### 目标架构

```
浏览器 (用户语言: ja)
  │
  ├─ GET /api/templates/menu-bindings?lang=ja
  │  或 Accept-Language: ja-JP
  │
  ▼
TemplateEndpoints
  │
  ├─ LangHelper.GetLang(http, lang) → "ja"
  │  ✅ 使用用户语言
  │
  ├─ 使用 targetLang 解析显示名
  │  entity.ToSummaryDto(targetLang)
  │
  ▼
返回日语菜单
  {
    entityDisplayName: "顧客",  ✅ 日语
    menuName: "顧客管理"
  }
```

---

## 📂 涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Endpoints/TemplateEndpoints.cs` | 修改 | 替换系统语言为用户语言 |
| `Services/TemplateService.cs` | 修改 | 传递 lang 参数（如有） |
| `DTOs/MenuBindingDto.cs` | 检查 | 确认 DTO 结构 |
| `tests/.../TemplateEndpointsTests.cs` | 新增 | 语言参数测试 |

---

## 🔧 技术方案

### 方案1: 定位问题代码

**查找端点**:
```bash
# 定位 /api/templates/menu-bindings 端点
grep -n "menu-bindings" src/BobCrm.Api/Endpoints/TemplateEndpoints.cs
```

**查找系统语言获取逻辑**:
```bash
# 查找可能的问题代码
grep -n "SystemSettings\|DefaultLanguage\|systemLanguage" src/BobCrm.Api/Endpoints/TemplateEndpoints.cs -B 5 -A 5
```

**典型问题代码模式**:
```csharp
// ❌ 可能的错误实现
var systemLanguage = await db.SystemSettings
    .Where(s => s.Key == "DefaultLanguage")
    .Select(s => s.Value)
    .FirstOrDefaultAsync() ?? "zh";

// 使用 systemLanguage 解析显示名
var displayName = entity.DisplayName[systemLanguage];
```

---

### 方案2: 修改端点使用用户语言

**修改伪代码**:
```csharp
// 修改前
app.MapGet("/api/templates/menu-bindings", async (
    HttpContext http,
    AppDbContext db,
    /* 其他参数 */
) => {
    // ❌ 使用系统默认语言
    var systemLanguage = await db.SystemSettings...;
    
    // 构建菜单绑定
    var bindings = await BuildMenuBindings(systemLanguage);
    // ...
});

// 修改后
app.MapGet("/api/templates/menu-bindings", async (
    string? lang,  // ⭐ 新增参数
    HttpContext http,
    AppDbContext db,
    /* 其他参数 */
) => {
    // ✅ 使用用户语言
    var targetLang = lang ?? LangHelper.GetLang(http);
    
    // 构建菜单绑定（使用用户语言）
    var bindings = await BuildMenuBindings(targetLang);
    
    return Results.Ok(new SuccessResponse(bindings));
})
.WithSummary("获取模板菜单绑定（支持用户语言）")
.WithDescription("返回模板与实体的菜单绑定。使用用户语言而非系统默认语言");
```

---

### 方案3: 应用语言过滤到菜单项

**菜单绑定结构**（推测）:
```csharp
class MenuBindingDto
{
    string MenuCode;
    string EntityType;
    
    // 实体显示名（需要应用语言过滤）
    string? EntityDisplayName;  // 或 MultilingualText
    
    // 菜单名称（需要应用语言过滤）
    string? MenuName;  // 或 MultilingualText
}
```

**语言过滤逻辑伪代码**:
```csharp
async Task<List<MenuBindingDto>> BuildMenuBindings(string lang)
{
    // 查询菜单绑定
    var bindings = await db.MenuBindings
        .Include(mb => mb.Entity)
        .Include(mb => mb.Template)
        .ToListAsync();
    
    // 转换为 DTO 并应用语言过滤
    var dtos = bindings.Select(binding => new MenuBindingDto
    {
        MenuCode = binding.MenuCode,
        EntityType = binding.Entity.EntityRoute,
        
        // ⭐ 使用 ToSummaryDto 应用语言过滤
        EntityDisplayName = binding.Entity.ToSummaryDto(lang).DisplayName,
        
        // ⭐ 如果菜单名也是多语的，同样处理
        MenuName = binding.MenuName?.Resolve(lang) ?? binding.MenuCode,
    }).ToList();
    
    return dtos;
}
```

---

## 🧪 测试策略

### 测试用例设计

#### 测试1: 默认使用用户语言（ja）

**目的**: 验证默认行为使用日语

```csharp
[Fact]
public async Task GetMenuBindings_WithoutLang_UsesAcceptLanguageHeader()
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, "/api/templates/menu-bindings");
    request.Headers.Add("Accept-Language", "ja-JP");
    
    // Act
    var response = await client.SendAsync(request);
    
    // Assert
    var json = await response.Content.ReadAsStringAsync();
    
    // 验证返回的是日语（而非中文）
    Assert.Contains("顧客", json);  // 日语的"客户"
    Assert.DoesNotContain("客户", json);  // 不应有中文
}
```

---

#### 测试2: 指定 lang=zh 返回中文

```csharp
[Fact]
public async Task GetMenuBindings_WithLangZh_ReturnsChineseNames()
{
    // Arrange & Act
    var response = await client.GetAsync("/api/templates/menu-bindings?lang=zh");
    var json = await response.Content.ReadAsStringAsync();
    
    // Assert
    Assert.Contains("客户", json);
    Assert.DoesNotContain("Customer", json);
    Assert.DoesNotContain("顧客", json);
}
```

---

#### 测试3: 指定 lang=en 返回英文

```csharp
[Fact]
public async Task GetMenuBindings_WithLangEn_ReturnsEnglishNames()
{
    // Arrange & Act
    var response = await client.GetAsync("/api/templates/menu-bindings?lang=en");
    var json = await response.Content.ReadAsStringAsync();
    
    // Assert
    Assert.Contains("Customer", json);
    Assert.DoesNotContain("客户", json);
}
```

---

#### 测试4: 单语模式优化响应

```csharp
[Fact]
public async Task GetMenuBindings_SingleLanguage_ReducesPayload()
{
    // Arrange & Act
    var multiLangResp = await client.GetAsync("/api/templates/menu-bindings");
    var singleLangResp = await client.GetAsync("/api/templates/menu-bindings?lang=zh");
    
    var multiLangJson = await multiLangResp.Content.ReadAsStringAsync();
    var singleLangJson = await singleLangResp.Content.ReadAsStringAsync();
    
    // Assert
    Assert.True(singleLangJson.Length < multiLangJson.Length);
    var reduction = 1.0 - ((double)singleLangJson.Length / multiLangJson.Length);
    Assert.True(reduction >= 0.3, $"Expected >=30% reduction, got {reduction:P}");
}
```

---

## 📋 实施检查清单

### 代码实现

- [ ] 定位 `/api/templates/menu-bindings` 端点
- [ ] 添加 `string? lang` 参数
- [ ] 移除系统语言查询逻辑
- [ ] 使用 `LangHelper.GetLang(http, lang)`
- [ ] 应用语言过滤到实体显示名
- [ ] 应用语言过滤到菜单名称（如适用）

### 测试实现

- [ ] 添加 4 个测试用例
- [ ] 测试默认行为（Accept-Language）
- [ ] 测试三种语言（zh, ja, en）
- [ ] 测试响应体积减少

### 验证

- [ ] 编译成功
- [ ] 测试全部通过
- [ ] 手动测试：日语用户看到日语菜单
- [ ] 性能测试：响应减少 ≥ 30%

---

## 📝 Git 提交规范

```
fix(api): use user language instead of system default in menu bindings

Critical bug fix:
- Replace system default language with user's lang parameter
- Use LangHelper.GetLang() for language detection
- Apply language filtering to entity display names
- Apply language filtering to menu names

Bug fixed:
- Japanese users no longer see Chinese menus
- Users always see menus in their preferred language

Test coverage:
- 4 test cases for language consistency
- Verify Accept-Language header behavior
- Verify explicit lang parameter
- Verify response size reduction

Ref: ARCH-30 Task 1.2
```

---

## ⚠️ 注意事项

### 注意1: 系统语言的其他用途

删除系统语言查询前，确认系统语言是否还有其他用途：

```bash
# 搜索系统语言的其他使用
grep -r "SystemSettings.*Language\|DefaultLanguage" src/BobCrm.Api/ --include="*.cs"
```

如果有其他用途，只移除菜单绑定相关的使用。

---

### 注意2: 菜单名称的数据结构

需要确认 `MenuBinding` 或相关实体中菜单名称的存储结构：
- 如果是 `Dictionary<string, string?>` - 使用 `Resolve(lang)`
- 如果是 `string` - 可能是资源 Key，需要通过 `ILocalization.T()` 翻译

---

### 注意3: 缓存策略

如果端点有缓存，需要按语言区分：

```csharp
.CacheOutput(policy =>
{
    policy.Expire(TimeSpan.FromMinutes(5));
    policy.SetVaryByQuery("lang");  // ⭐ 按语言区分缓存
});
```

---

## 🔗 相关文档

- [ARCH-30 设计文档](../../design/ARCH-30-实体字段显示名多语元数据驱动设计.md) - 第 56-61 行（问题描述）
- [Task 0.3 设计](task-0.3-dto-definitions.md) - DTO 双模式参考
- [LangHelper 文档](../../guides/I18N-01-多语机制设计文档.md)

---

**文档类型**: 技术设计文档  
**目标读者**: 开发者  
**维护者**: ARCH-30 架构组  
**最后更新**: 2025-12-11

