# ARCH-30 Task 3.3: 动态实体查询 API 改造

**任务编号**: Task 3.3
**阶段**: 阶段3 - 低频API改造（动态实体查询优化）
**状态**: 待开始
**预计工时**: 1 天
**依赖**: Task 3.2 完成

---

## 1. 任务目标

改造动态实体查询 API，在响应中附加 `meta.fields` 字段元数据，支持前端显示字段标签。

### 1.1 改造端点

| 端点 | 方法 | 改造内容 |
|------|------|----------|
| `/api/dynamic-entities/{fullTypeName}/query` | POST | 追加 `meta.fields` |
| `/api/dynamic-entities/{fullTypeName}/{id}` | GET | 新增 `includeMeta` 参数 |

### 1.2 设计参考

> **必读**: ARCH-30 主设计文档 第3.2.6节「端点修改方案」

---

## 2. 前置条件

- [ ] Task 3.2 缓存服务已实现
- [ ] `IFieldMetadataCache.GetFieldsAsync()` 可正常调用
- [ ] 现有 API 运行正常

---

## 3. 文件操作清单

### 3.1 新建文件

| 文件路径 | 用途 |
|----------|------|
| `src/BobCrm.Api/Contracts/DTOs/DynamicEntityQueryResultDto.cs` | 查询结果 DTO |
| `src/BobCrm.Api/Contracts/DTOs/DynamicEntityMetaDto.cs` | 元数据 DTO |

### 3.2 修改文件

| 文件路径 | 修改内容 |
|----------|----------|
| `src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs` | 改造 query 和 getById 端点 |
| `src/BobCrm.Api/Services/DynamicEntityService.cs` | 调整返回结构（如需） |
| `tests/BobCrm.Api.Tests/DynamicEntityEndpointsTests.cs` | 补充测试用例 |

---

## 4. 实现步骤

### 4.1 定义 DTO

**文件**: `src/BobCrm.Api/Contracts/DTOs/DynamicEntityMetaDto.cs`

```csharp
using System.Text.Json.Serialization;

namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 动态实体查询元数据
/// </summary>
public class DynamicEntityMetaDto
{
    /// <summary>
    /// 字段元数据列表
    /// </summary>
    [JsonPropertyName("fields")]
    public IReadOnlyList<FieldMetadataDto> Fields { get; set; } = Array.Empty<FieldMetadataDto>();
}
```

**文件**: `src/BobCrm.Api/Contracts/DTOs/DynamicEntityQueryResultDto.cs`

```csharp
using System.Text.Json.Serialization;

namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 动态实体查询结果（含元数据）
/// </summary>
public class DynamicEntityQueryResultDto
{
    /// <summary>
    /// 元数据（字段信息等）
    /// </summary>
    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DynamicEntityMetaDto? Meta { get; set; }

    /// <summary>
    /// 数据列表
    /// </summary>
    [JsonPropertyName("data")]
    public List<object> Data { get; set; } = new();

    /// <summary>
    /// 总记录数
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// 每页大小
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
}

/// <summary>
/// 动态实体单体查询结果（含元数据）
/// </summary>
public class DynamicEntityDetailResultDto
{
    /// <summary>
    /// 元数据（字段信息等）
    /// </summary>
    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DynamicEntityMetaDto? Meta { get; set; }

    /// <summary>
    /// 实体数据
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
```

### 4.2 改造 Query 端点

**文件**: `src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs`

找到 `/api/dynamic-entities/{fullTypeName}/query` 端点，改造如下：

```csharp
// 1. 注入 IFieldMetadataCache
// 2. 添加 lang 查询参数
// 3. 构建带 meta.fields 的响应

app.MapPost("/api/dynamic-entities/{fullTypeName}/query", async (
    string fullTypeName,
    [FromQuery] string? lang,
    [FromBody] DynamicQueryRequest request,
    IDynamicEntityService dynamicEntityService,
    IFieldMetadataCache fieldMetadataCache,
    HttpContext http,
    CancellationToken ct) =>
{
    // 解析语言参数
    var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.NormalizeLang(lang);

    // 执行查询
    var queryResult = await dynamicEntityService.QueryAsync(fullTypeName, request, ct);

    // 获取字段元数据
    var fields = await fieldMetadataCache.GetFieldsAsync(fullTypeName, targetLang, ct);

    // 构建响应
    var response = new DynamicEntityQueryResultDto
    {
        Meta = new DynamicEntityMetaDto { Fields = fields },
        Data = queryResult.Data,
        Total = queryResult.Total,
        Page = queryResult.Page,
        PageSize = queryResult.PageSize
    };

    return Results.Ok(ApiResponse.Success(response));
})
.WithTags("DynamicEntity")
.WithSummary("查询动态实体数据")
.WithDescription("支持 lang 参数指定字段显示名语言，返回 meta.fields 元数据");
```

### 4.3 改造 GetById 端点

**文件**: `src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs`

找到 `/api/dynamic-entities/{fullTypeName}/{id}` 端点，改造如下：

```csharp
app.MapGet("/api/dynamic-entities/{fullTypeName}/{id:guid}", async (
    string fullTypeName,
    Guid id,
    [FromQuery] string? lang,
    [FromQuery] bool includeMeta,  // 新增参数，默认 false
    IDynamicEntityService dynamicEntityService,
    IFieldMetadataCache fieldMetadataCache,
    HttpContext http,
    CancellationToken ct) =>
{
    // 查询实体
    var entity = await dynamicEntityService.GetByIdAsync(fullTypeName, id, ct);
    if (entity == null)
    {
        return Results.NotFound(ApiResponse.Fail("NOT_FOUND", "实体不存在"));
    }

    // 不需要元数据时，保持向后兼容
    if (!includeMeta)
    {
        return Results.Ok(ApiResponse.Success(entity));
    }

    // 需要元数据时，返回包裹结构
    var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.NormalizeLang(lang);
    var fields = await fieldMetadataCache.GetFieldsAsync(fullTypeName, targetLang, ct);

    var response = new DynamicEntityDetailResultDto
    {
        Meta = new DynamicEntityMetaDto { Fields = fields },
        Data = entity
    };

    return Results.Ok(ApiResponse.Success(response));
})
.WithTags("DynamicEntity")
.WithSummary("获取动态实体详情")
.WithDescription("includeMeta=true 时返回 meta.fields 字段元数据");
```

---

## 5. 响应示例

### 5.1 Query 响应（lang=zh）

```json
{
  "code": "SUCCESS",
  "message": null,
  "data": {
    "meta": {
      "fields": [
        {
          "propertyName": "Code",
          "displayNameKey": "LBL_FIELD_CODE",
          "displayName": "编码",
          "dataType": "String"
        },
        {
          "propertyName": "Name",
          "displayNameKey": "LBL_FIELD_NAME",
          "displayName": "名称",
          "dataType": "String"
        },
        {
          "propertyName": "CustomField",
          "displayName": "自定义字段",
          "dataType": "String"
        }
      ]
    },
    "data": [
      { "id": "...", "code": "C001", "name": "客户A" }
    ],
    "total": 100,
    "page": 1,
    "pageSize": 20
  }
}
```

### 5.2 GetById 响应（includeMeta=true&lang=zh）

```json
{
  "code": "SUCCESS",
  "data": {
    "meta": {
      "fields": [...]
    },
    "data": {
      "id": "...",
      "code": "C001",
      "name": "客户A"
    }
  }
}
```

### 5.3 GetById 响应（includeMeta=false，向后兼容）

```json
{
  "code": "SUCCESS",
  "data": {
    "id": "...",
    "code": "C001",
    "name": "客户A"
  }
}
```

---

## 6. 测试用例

**文件**: `tests/BobCrm.Api.Tests/DynamicEntityEndpointsPhase3Tests.cs`

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace BobCrm.Api.Tests;

public class DynamicEntityEndpointsPhase3Tests
{
    [Fact]
    public async Task Query_WithLang_ShouldReturnMetaFields()
    {
        // Arrange
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();
        // [准备测试实体]

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/dynamic-entities/BobCrm.Base.Custom.Customer/query?lang=zh",
            new { page = 1, pageSize = 10 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        json.GetProperty("data").GetProperty("meta").GetProperty("fields")
            .EnumerateArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task Query_WithoutLang_ShouldReturnMultiLanguageFields()
    {
        // Arrange
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/dynamic-entities/BobCrm.Base.Custom.Customer/query",
            new { page = 1, pageSize = 10 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // 验证多语模式：接口字段有 displayNameKey
        var fields = json.GetProperty("data").GetProperty("meta").GetProperty("fields");
        fields.EnumerateArray().Should().Contain(f =>
            f.TryGetProperty("displayNameKey", out _));
    }

    [Fact]
    public async Task GetById_WithIncludeMeta_ShouldReturnWrappedResponse()
    {
        // Arrange
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();
        // [创建测试实体并获取ID]
        var testId = Guid.NewGuid(); // 替换为实际ID

        // Act
        var response = await client.GetAsync(
            $"/api/dynamic-entities/BobCrm.Base.Custom.Customer/{testId}?includeMeta=true&lang=zh");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        json.GetProperty("data").TryGetProperty("meta", out var meta).Should().BeTrue();
        json.GetProperty("data").TryGetProperty("data", out var data).Should().BeTrue();
    }

    [Fact]
    public async Task GetById_WithoutIncludeMeta_ShouldReturnDirectEntity()
    {
        // Arrange
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();
        var testId = Guid.NewGuid(); // 替换为实际ID

        // Act
        var response = await client.GetAsync(
            $"/api/dynamic-entities/BobCrm.Base.Custom.Customer/{testId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // 向后兼容：直接返回实体，无 meta 包裹
        json.GetProperty("data").TryGetProperty("meta", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Query_FieldDisplayName_ShouldMatchLanguage()
    {
        // Arrange
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        // Act - 中文
        var zhResponse = await client.PostAsJsonAsync(
            "/api/dynamic-entities/BobCrm.Base.Custom.Customer/query?lang=zh",
            new { page = 1, pageSize = 10 });
        var zhJson = await zhResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Act - 英文
        var enResponse = await client.PostAsJsonAsync(
            "/api/dynamic-entities/BobCrm.Base.Custom.Customer/query?lang=en",
            new { page = 1, pageSize = 10 });
        var enJson = await enResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        var zhCodeField = zhJson.GetProperty("data").GetProperty("meta").GetProperty("fields")
            .EnumerateArray().FirstOrDefault(f => f.GetProperty("propertyName").GetString() == "Code");
        var enCodeField = enJson.GetProperty("data").GetProperty("meta").GetProperty("fields")
            .EnumerateArray().FirstOrDefault(f => f.GetProperty("propertyName").GetString() == "Code");

        zhCodeField.GetProperty("displayName").GetString().Should().Be("编码");
        enCodeField.GetProperty("displayName").GetString().Should().Be("Code");
    }
}
```

---

## 7. 验收标准

### 7.1 功能验收

- [ ] Query 端点支持 `lang` 参数
- [ ] Query 响应包含 `meta.fields`
- [ ] GetById 支持 `includeMeta` 参数
- [ ] `includeMeta=false` 时保持向后兼容

### 7.2 质量门禁

- [ ] 编译成功（Debug + Release）
- [ ] 所有测试通过（包括回归测试）
- [ ] 无新增编译警告
- [ ] 代码覆盖率 ≥ 80%

### 7.3 向后兼容验收

- [ ] 旧版客户端（不传 lang/includeMeta）仍可正常工作
- [ ] API 响应结构变更为增量式（新增字段，不删除）

---

## 8. Git 提交规范

```bash
git add src/BobCrm.Api/Contracts/DTOs/DynamicEntityMetaDto.cs
git add src/BobCrm.Api/Contracts/DTOs/DynamicEntityQueryResultDto.cs
git add src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs
git add tests/BobCrm.Api.Tests/DynamicEntityEndpointsPhase3Tests.cs

git commit -m "feat(api): add meta.fields to dynamic entity query endpoints

- Add lang parameter to /query endpoint
- Add includeMeta parameter to /{id} endpoint
- Return field metadata with displayName/displayNameKey
- Maintain backward compatibility when params not provided
- Add 5 test cases for new functionality
- Ref: ARCH-30 Task 3.3"
```

---

## 9. 后续任务

完成本任务后：
- **阶段3 完成**: 动态实体查询多语优化全部完成
- **继续阶段4**: 文档同步（Task 4.1-4.3）

---

**创建日期**: 2025-01-06
**最后更新**: 2025-01-06
**维护者**: ARCH-30 项目组
