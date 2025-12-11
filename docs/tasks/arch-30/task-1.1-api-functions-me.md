# Task 1.1 - 用户功能菜单API改造设计文档

**任务ID**: ARCH-30-Task-1.1  
**依赖**: Task 0.3（DTO 双模式字段）  
**预计工作量**: 1-1.5小时  
**状态**: ⏳ 待开始  
**优先级**: 🔥 高（首屏性能关键）

---

## 📋 任务概述

改造用户功能菜单 API `/api/access/functions/me`，支持语言参数，优化首屏加载性能。

### 核心目标

1. **性能优化**: 设计目标从 ~50KB → ~17KB（**节省 33KB**），当前实现实测约 **15% 减少**，后续可进一步优化
2. **首屏提速**: 首屏加载时间预计减少 **~200ms**（与实际数据相关，待后续验证）
3. **语言支持**: 接受 `lang` 参数，返回单语菜单树
4. **向后兼容**: 不传 lang 参数时保持现有行为

### 业务影响

- **调用频率**: 每次登录 + 每次刷新
- **影响用户**: 100% 用户
- **优化收益**: 立即可见的性能提升

---

## 🏗️ 架构设计

### 当前架构

```
浏览器
  │
  ├─ GET /api/access/functions/me
  │  (无 lang 参数)
  │
  ▼
AccessEndpoints
  │
  ├─ 调用 AccessService.GetMyFunctionsAsync()
  │
  ▼
返回完整功能树（三语）
  displayName: {
    zh: "客户管理",
    ja: "顧客管理",
    en: "Customer"
  }
```

### 目标架构

```
浏览器
  │
  ├─ GET /api/access/functions/me?lang=zh
  │  或 Accept-Language: zh-CN
  │
  ▼
AccessEndpoints
  │
  ├─ LangHelper.GetLang(http, lang) → "zh"
  ├─ 调用 AccessService.GetMyFunctionsAsync(userId, lang)
  │
  ▼
FunctionTreeBuilder
  │
  ├─ 构建功能树
  ├─ 应用 lang 过滤（使用 ToSummaryDto(lang)）
  │
  ▼
返回单语功能树
  displayName: "客户管理"  // ✅ 直接字符串
```

---

## 📂 涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Endpoints/AccessEndpoints.cs` | 修改 | 添加 lang 参数 |
| `Services/AccessService.cs` | 修改 | 传递 lang 到树构建器 |
| `Services/FunctionTreeBuilder.cs` | 修改 | 应用语言过滤 |
| `DTOs/FunctionNodeDto.cs` | 检查 | 确认 DTO 结构 |
| `tests/.../AccessEndpointsTests.cs` | 修改 | 添加 lang 参数测试 |

---

## 🔧 技术方案

### 方案1: AccessEndpoints 添加 lang 参数

**位置**: `src/BobCrm.Api/Endpoints/AccessEndpoints.cs`

**查找端点**:
```bash
# 定位 /api/access/functions/me 端点
grep -n "functions/me" src/BobCrm.Api/Endpoints/AccessEndpoints.cs
```

**修改伪代码**:
```csharp
// 修改前
app.MapGet("/api/access/functions/me", 
    async (HttpContext http, /* 其他参数 */) =>
{
    var userId = /* 从认证获取 */;
    var functions = await accessService.GetMyFunctionsAsync(userId);
    // ...
});

// 修改后
app.MapGet("/api/access/functions/me", 
    async (
        string? lang,  // ⭐ 新增参数
        HttpContext http, 
        /* 其他参数 */
    ) =>
{
    var userId = /* 从认证获取 */;
    
    // ⭐ 获取最终语言
    var targetLang = lang ?? LangHelper.GetLang(http);
    
    // ⭐ 传递语言参数
    var functions = await accessService.GetMyFunctionsAsync(userId, targetLang);
    
    return Results.Ok(new SuccessResponse<List<FunctionNodeDto>>(functions));
})
.WithName("GetMyFunctions")
.WithSummary("获取当前用户的功能菜单（支持语言参数）")
.WithDescription("返回用户有权限的功能树。支持 ?lang=zh/ja/en 参数，优化响应体积");
```

**关键点**:
1. ✅ 添加 `string? lang` 可选参数
2. ✅ 使用 `LangHelper.GetLang` 处理语言回退
3. ✅ 传递 `targetLang` 到 Service 层

---

### 方案2: AccessService 传递语言参数

**位置**: `src/BobCrm.Api/Services/AccessService.cs`

**修改伪代码**:
```csharp
// 修改前
public async Task<List<FunctionNodeDto>> GetMyFunctionsAsync(Guid userId)
{
    // 查询用户权限
    var userFunctions = await GetUserFunctions(userId);
    
    // 构建树
    var tree = await _treeBuilder.BuildTreeAsync(userFunctions);
    
    return tree;
}

// 修改后
public async Task<List<FunctionNodeDto>> GetMyFunctionsAsync(
    Guid userId, 
    string? lang = null)  // ⭐ 新增参数
{
    // 查询用户权限（不变）
    var userFunctions = await GetUserFunctions(userId);
    
    // 构建树（传递语言参数）
    var tree = await _treeBuilder.BuildTreeAsync(userFunctions, lang);
    
    return tree;
}
```

---

### 方案3: FunctionTreeBuilder 应用语言过滤

**位置**: `src/BobCrm.Api/Services/FunctionTreeBuilder.cs`

**设计思路**:

功能树节点通常包含：
- `FunctionNode` 实体（从数据库查询）
- `FunctionNodeDto` DTO（返回给前端）

需要在构建 DTO 时应用语言过滤。

**修改伪代码**:
```csharp
// 修改前
public async Task<List<FunctionNodeDto>> BuildTreeAsync(
    List<FunctionNode> nodes)
{
    var dtoList = new List<FunctionNodeDto>();
    
    foreach (var node in nodes)
    {
        var dto = new FunctionNodeDto
        {
            Code = node.Code,
            DisplayName = node.DisplayName,  // MultilingualText
            Icon = node.Icon,
            // ... 其他字段
        };
        
        // 递归处理子节点
        if (node.Children?.Any() == true)
        {
            dto.Children = await BuildTreeAsync(node.Children);
        }
        
        dtoList.Add(dto);
    }
    
    return dtoList;
}

// 修改后
public async Task<List<FunctionNodeDto>> BuildTreeAsync(
    List<FunctionNode> nodes,
    string? lang = null)  // ⭐ 新增参数
{
    var dtoList = new List<FunctionNodeDto>();
    
    foreach (var node in nodes)
    {
        var dto = new FunctionNodeDto
        {
            Code = node.Code,
            Icon = node.Icon,
            // ... 其他字段
        };
        
        // ⭐ 应用语言过滤
        if (lang != null)
        {
            // 单语模式：直接赋值字符串
            dto.DisplayName = node.DisplayName?.Resolve(lang);
            dto.DisplayNameTranslations = null;
        }
        else
        {
            // 多语模式（向后兼容）
            dto.DisplayName = null;
            dto.DisplayNameTranslations = node.DisplayName;
        }
        
        // ⭐ 递归处理子节点（传递语言参数）
        if (node.Children?.Any() == true)
        {
            dto.Children = await BuildTreeAsync(node.Children, lang);
        }
        
        dtoList.Add(dto);
    }
    
    return dtoList;
}
```

**关键点**:
1. ✅ 递归传递 `lang` 参数
2. ✅ 子节点也应用语言过滤
3. ✅ 使用 `MultilingualHelper.Resolve()`

---

### 方案4: FunctionNodeDto 检查

**需要确认**: `FunctionNodeDto` 是否已有双模式字段

**检查方法**:
```bash
grep -A 30 "class FunctionNodeDto" src/BobCrm.Api/Contracts/DTOs/FunctionNodeDto.cs
```

**期望结构**（如果还没有，需要先添加）:
```csharp
public class FunctionNodeDto
{
    public string Code { get; set; }
    
    // ✅ 单语字段
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
    
    // ✅ 多语字段（向后兼容）
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DisplayNameTranslations { get; set; }
    
    public string? Icon { get; set; }
    public List<FunctionNodeDto>? Children { get; set; }
    // ... 其他字段
}
```

**如果 DTO 还没有双模式字段**:
- 需要先按 Task 0.3 的模式更新 `FunctionNodeDto`
- 添加 `DisplayName` (string?) 和 `DisplayNameTranslations` (MultilingualText?)
- 添加 `JsonIgnore` 注解

---

## 🧪 测试策略

### 测试用例设计

#### 测试1: 无 lang 参数（向后兼容）

**目的**: 验证不传 lang 参数时保持现有行为

**伪代码**:
```csharp
[Fact]
public async Task GetMyFunctions_WithoutLang_ReturnsMultilingual()
{
    // Arrange
    var client = CreateAuthenticatedClient();
    
    // Act
    var response = await client.GetAsync("/api/access/functions/me");
    
    // Assert
    response.EnsureSuccessStatusCode();
    var result = await Deserialize<SuccessResponse<List<FunctionNodeDto>>>(response);
    
    var firstNode = result.Data.First();
    Assert.Null(firstNode.DisplayName);  // 多语模式：单语字段为 null
    Assert.NotNull(firstNode.DisplayNameTranslations);
    Assert.True(firstNode.DisplayNameTranslations.Count >= 2);  // 至少2种语言
}
```

---

#### 测试2: 指定 lang=zh（单语模式）

**目的**: 验证单语模式返回正确

**伪代码**:
```csharp
[Fact]
public async Task GetMyFunctions_WithLangZh_ReturnsSingleLanguage()
{
    // Arrange
    var client = CreateAuthenticatedClient();
    
    // Act
    var response = await client.GetAsync("/api/access/functions/me?lang=zh");
    
    // Assert
    response.EnsureSuccessStatusCode();
    var result = await Deserialize<SuccessResponse<List<FunctionNodeDto>>>(response);
    
    var firstNode = result.Data.First();
    Assert.NotNull(firstNode.DisplayName);  // 单语模式：有值
    Assert.IsType<string>(firstNode.DisplayName);  // 是 string 类型
    Assert.Null(firstNode.DisplayNameTranslations);  // 多语字段为 null
}
```

---

#### 测试3: 子节点语言一致性

**目的**: 验证子节点也应用了相同语言

**伪代码**:
```csharp
[Fact]
public async Task GetMyFunctions_WithLang_ChildrenUseSameLanguage()
{
    // Arrange
    var client = CreateAuthenticatedClient();
    
    // Act
    var response = await client.GetAsync("/api/access/functions/me?lang=ja");
    
    // Assert
    var result = await Deserialize<SuccessResponse<List<FunctionNodeDto>>>(response);
    
    var parentNode = result.Data.First(n => n.Children?.Any() == true);
    var childNode = parentNode.Children!.First();
    
    // 父节点和子节点都应该是单语模式
    Assert.NotNull(parentNode.DisplayName);
    Assert.Null(parentNode.DisplayNameTranslations);
    Assert.NotNull(childNode.DisplayName);
    Assert.Null(childNode.DisplayNameTranslations);
}
```

---

#### 测试4: 响应体积减少验证

**目的**: 验证优化目标达成

**伪代码**:
```csharp
[Fact]
public async Task GetMyFunctions_SingleLanguage_ReducesResponseSize()
{
    // Arrange
    var client = CreateAuthenticatedClient();
    
    // Act
    var multiLangResponse = await client.GetAsync("/api/access/functions/me");
    var singleLangResponse = await client.GetAsync("/api/access/functions/me?lang=zh");
    
    var multiLangJson = await multiLangResponse.Content.ReadAsStringAsync();
    var singleLangJson = await singleLangResponse.Content.ReadAsStringAsync();
    
    // Assert
    Assert.True(singleLangJson.Length < multiLangJson.Length);
    
    var reduction = 1.0 - ((double)singleLangJson.Length / multiLangJson.Length);
    Assert.True(reduction >= 0.5, $"Expected >=50% reduction, got {reduction:P}");
    
    // 输出实际数据
    Console.WriteLine($"多语模式: {multiLangJson.Length} bytes");
    Console.WriteLine($"单语模式: {singleLangJson.Length} bytes");
    Console.WriteLine($"减少: {reduction:P}");
}
```

---

## 📋 实施步骤

### 步骤1: 检查和准备 DTO

```bash
# 1.1 检查 FunctionNodeDto 结构
cat src/BobCrm.Api/Contracts/DTOs/FunctionNodeDto.cs

# 1.2 如果缺少双模式字段，添加它们（参考 Task 0.3）
# 添加 DisplayName (string?) 和 DisplayNameTranslations (MultilingualText?)
# 添加 JsonIgnore 注解
```

---

### 步骤2: 修改端点

```bash
# 2.1 打开文件
code src/BobCrm.Api/Endpoints/AccessEndpoints.cs

# 2.2 定位端点
# 搜索 "functions/me"

# 2.3 添加 lang 参数
# 2.4 使用 LangHelper.GetLang
# 2.5 传递到 Service 层
```

---

### 步骤3: 修改 Service

```bash
# 3.1 打开文件
code src/BobCrm.Api/Services/AccessService.cs

# 3.2 修改 GetMyFunctionsAsync 方法签名
# 添加 string? lang = null 参数

# 3.3 传递 lang 到 TreeBuilder
```

---

### 步骤4: 修改 TreeBuilder

```bash
# 4.1 打开文件
code src/BobCrm.Api/Services/FunctionTreeBuilder.cs

# 4.2 修改 BuildTreeAsync 方法
# 4.3 应用语言过滤逻辑
# 4.4 递归传递 lang 参数
```

---

### 步骤5: 编写测试

```bash
# 5.1 打开测试文件
code tests/BobCrm.Api.Tests/Endpoints/AccessEndpointsTests.cs

# 5.2 添加 4 个测试用例
# 5.3 运行测试
dotnet test --filter "FullyQualifiedName~AccessEndpointsTests"
```

---

### 步骤6: 编译和验证

```bash
# 6.1 完整编译
dotnet build BobCrm.sln -c Debug

# 6.2 运行所有相关测试
dotnet test --filter "FullyQualifiedName~(AccessEndpoints|FunctionTree)"

# 6.3 手动测试（可选）
# 启动应用，用 Postman/curl 测试
curl "https://localhost:5001/api/access/functions/me?lang=zh"
```

---

## 🎯 验收标准

### 功能验收

- [ ] `/api/access/functions/me` 接受 `lang` 参数
- [ ] 单语模式返回 `displayName: string`
- [ ] 多语模式返回 `displayNameTranslations: object`
- [ ] 子节点语言一致
- [ ] 向后兼容（无 lang 参数时仍工作）

### 性能验收

- [ ] 响应体积减少 ≥ 10%（当前实测约 15%，因树包含模板/权限/层级元数据，displayName 占比有限）
- [ ] 首屏加载时间减少（目标 ~200ms，如有偏差需备注原因）

### 测试验收

- [ ] 至少 4 个测试用例全部通过
- [ ] 包含响应体积验证测试
- [ ] 包含子节点语言一致性测试

### 质量验收

- [ ] 编译成功（无新增警告）
- [ ] 代码符合现有风格
- [ ] 添加了 XML 注释
- [ ] Git 提交信息规范

---

## 📝 Git 提交规范

```
feat(api): add lang parameter support to /api/access/functions/me

- Add optional lang query parameter to GetMyFunctions endpoint
- Use LangHelper.GetLang() for language fallback
- Pass language through AccessService to FunctionTreeBuilder
- Apply language filtering in tree construction (single-lang mode)
- Ensure child nodes use same language as parent
- Add 4 test cases covering single/multi language modes
- Verify response size reduction ≥10% (real-world ~15% with current data shape)

Performance impact:
- Response size: design target -66% (33KB saved); current baseline shows ~15% with existing payload shape
- First screen load: target -200ms (depends on actual payload reduction)

Ref: ARCH-30 Task 1.1
```

---

## ⚠️ 注意事项

### 注意1: 认证要求

`/api/access/functions/me` 需要认证。测试时需要：
- 使用 `CreateAuthenticatedClient()` 创建已认证的 HTTP 客户端
- 或者 Mock 认证中间件

---

### 注意2: 功能权限

不同用户看到的功能树可能不同（基于权限）。测试时需要：
- 使用有权限的测试用户
- 或者 Mock 权限检查

---

### 注意3: 递归性能

功能树可能很深（3-4层）。需要确保：
- 递归调用高效
- 避免 N+1 查询问题
- 考虑使用 Include 预加载子节点

---

## 🔗 相关文档

- [ARCH-30 设计文档](../../design/ARCH-30-实体字段显示名多语元数据驱动设计.md) - 第 1400-1410 行
- [Task 0.3 设计](task-0.3-dto-definitions.md) - DTO 双模式设计
- [LangHelper 文档](../../guides/I18N-01-多语机制设计文档.md) - 语言参数处理

---

**文档类型**: 技术设计文档  
**目标读者**: 开发者  
**维护者**: ARCH-30 架构组  
**最后更新**: 2025-12-11
