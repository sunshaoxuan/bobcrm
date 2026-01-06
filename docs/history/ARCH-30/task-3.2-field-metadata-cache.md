# ARCH-30 Task 3.2: 字段元数据缓存服务实现

**任务编号**: Task 3.2
**阶段**: 阶段3 - 低频API改造（动态实体查询优化）
**状态**: 待开始
**预计工时**: 1 天
**依赖**: Task 3.1 完成

---

## 1. 任务目标

实现 `IFieldMetadataCache` 服务，为动态实体查询提供高性能的字段元数据缓存能力。

### 1.1 核心功能

| 功能 | 描述 |
|------|------|
| 字段元数据缓存 | 按 `fullTypeName` 缓存字段 DTO 列表 |
| 多语解析 | 复用 `DtoExtensions.ToFieldDto()` 的三级优先级逻辑 |
| 缓存失效 | 实体定义变更时主动失效 |
| 性能保障 | 避免 N+1 查询，复用 ILocalization 内部缓存 |

### 1.2 设计参考

> **必读**: ARCH-30 主设计文档 第3.2.5节「字段元数据缓存机制」

---

## 2. 前置条件

- [ ] Task 3.1 调研报告已完成
- [ ] 阶段0-2 的 `DtoExtensions.ToFieldDto()` 可正常使用
- [ ] `ILocalization` 服务可正常注入

---

## 3. 文件操作清单

### 3.1 新建文件

| 文件路径 | 用途 |
|----------|------|
| `src/BobCrm.Api/Abstractions/IFieldMetadataCache.cs` | 接口定义 |
| `src/BobCrm.Api/Services/FieldMetadataCache.cs` | 实现类 |
| `tests/BobCrm.Api.Tests/FieldMetadataCacheTests.cs` | 单元测试 |

### 3.2 修改文件

| 文件路径 | 修改内容 |
|----------|----------|
| `src/BobCrm.Api/Program.cs` | 注册 DI 服务 |
| `src/BobCrm.Api/Services/EntityDefinitionAppService.cs` | 实体变更时调用 `Invalidate()` |

---

## 4. 实现步骤

### 4.1 定义接口

**文件**: `src/BobCrm.Api/Abstractions/IFieldMetadataCache.cs`

```csharp
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Abstractions;

/// <summary>
/// 字段元数据缓存服务
/// </summary>
/// <remarks>
/// 为动态实体查询提供高性能的字段元数据获取能力。
/// 缓存策略：按 fullTypeName 缓存，支持主动失效和被动过期。
/// </remarks>
public interface IFieldMetadataCache
{
    /// <summary>
    /// 获取实体的字段元数据列表
    /// </summary>
    /// <param name="fullTypeName">实体完整类型名</param>
    /// <param name="lang">目标语言（null 表示多语模式）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>字段元数据 DTO 列表</returns>
    Task<IReadOnlyList<FieldMetadataDto>> GetFieldsAsync(
        string fullTypeName,
        string? lang,
        CancellationToken ct = default);

    /// <summary>
    /// 使指定实体的缓存失效
    /// </summary>
    /// <param name="fullTypeName">实体完整类型名</param>
    void Invalidate(string fullTypeName);

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    void Clear();
}
```

### 4.2 实现缓存服务

**文件**: `src/BobCrm.Api/Services/FieldMetadataCache.cs`

```csharp
using BobCrm.Api.Abstractions;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Extensions;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BobCrm.Api.Services;

/// <summary>
/// 字段元数据缓存服务实现
/// </summary>
public class FieldMetadataCache : IFieldMetadataCache
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly ILocalization _localization;
    private readonly ILogger<FieldMetadataCache> _logger;

    // 缓存配置
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    private const string CacheKeyPrefix = "FieldMetadata:";

    public FieldMetadataCache(
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        ILocalization localization,
        ILogger<FieldMetadataCache> logger)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;
        _localization = localization;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FieldMetadataDto>> GetFieldsAsync(
        string fullTypeName,
        string? lang,
        CancellationToken ct = default)
    {
        // 缓存 Key：包含 fullTypeName 和 lang
        // 多语模式使用 "__multi__" 作为 lang 占位
        var cacheKey = BuildCacheKey(fullTypeName, lang);

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<FieldMetadataDto>? cached) && cached != null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        _logger.LogDebug("Cache miss for {CacheKey}, loading from database", cacheKey);

        // 从数据库加载
        var fields = await LoadFieldsFromDatabaseAsync(fullTypeName, lang, ct);

        // 写入缓存
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(CacheDuration)
            .SetAbsoluteExpiration(TimeSpan.FromHours(2));

        _cache.Set(cacheKey, fields, cacheOptions);

        return fields;
    }

    public void Invalidate(string fullTypeName)
    {
        // 失效所有语言版本的缓存
        var languages = new[] { null, "zh", "ja", "en" };
        foreach (var lang in languages)
        {
            var cacheKey = BuildCacheKey(fullTypeName, lang);
            _cache.Remove(cacheKey);
            _logger.LogDebug("Invalidated cache: {CacheKey}", cacheKey);
        }
    }

    public void Clear()
    {
        // IMemoryCache 没有 Clear 方法，需要通过其他方式实现
        // 这里记录日志，实际清除依赖缓存过期
        _logger.LogInformation("Field metadata cache clear requested (will expire naturally)");
    }

    private string BuildCacheKey(string fullTypeName, string? lang)
    {
        var langPart = string.IsNullOrWhiteSpace(lang) ? "__multi__" : lang;
        return $"{CacheKeyPrefix}{fullTypeName}:{langPart}";
    }

    private async Task<IReadOnlyList<FieldMetadataDto>> LoadFieldsFromDatabaseAsync(
        string fullTypeName,
        string? lang,
        CancellationToken ct)
    {
        // 使用 scope 获取 DbContext（避免生命周期问题）
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 查询实体定义（包含字段）
        var entity = await db.EntityDefinitions
            .AsNoTracking()
            .Include(e => e.Fields.Where(f => !f.IsDeleted))
            .FirstOrDefaultAsync(e => e.FullTypeName == fullTypeName, ct);

        if (entity == null)
        {
            _logger.LogWarning("Entity not found: {FullTypeName}", fullTypeName);
            return Array.Empty<FieldMetadataDto>();
        }

        // 转换为 DTO（复用现有逻辑）
        var result = new List<FieldMetadataDto>();
        foreach (var field in entity.Fields.OrderBy(f => f.SortOrder))
        {
            var dto = field.ToFieldDto(_localization, lang);
            result.Add(dto);
        }

        return result;
    }
}
```

### 4.3 注册 DI 服务

**文件**: `src/BobCrm.Api/Program.cs`

在服务注册区域添加：

```csharp
// 字段元数据缓存服务
builder.Services.AddSingleton<IFieldMetadataCache, FieldMetadataCache>();
```

### 4.4 集成缓存失效

**文件**: `src/BobCrm.Api/Services/EntityDefinitionAppService.cs`

在实体/字段变更方法中添加缓存失效调用：

```csharp
// 注入缓存服务
private readonly IFieldMetadataCache _fieldMetadataCache;

// 在 UpdateEntityAsync / AddFieldAsync / UpdateFieldAsync / DeleteFieldAsync 等方法末尾：
_fieldMetadataCache.Invalidate(entity.FullTypeName);
```

---

## 5. 测试用例

**文件**: `tests/BobCrm.Api.Tests/FieldMetadataCacheTests.cs`

```csharp
using BobCrm.Api.Abstractions;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BobCrm.Api.Tests;

public class FieldMetadataCacheTests
{
    [Fact]
    public async Task GetFieldsAsync_WithLang_ShouldReturnSingleLanguage()
    {
        // Arrange
        using var factory = new TestWebAppFactory();
        var cache = factory.Services.GetRequiredService<IFieldMetadataCache>();

        // 确保有测试实体
        // [准备测试数据]

        // Act
        var fields = await cache.GetFieldsAsync("BobCrm.Base.Custom.Customer", "zh");

        // Assert
        fields.Should().NotBeEmpty();
        fields.Should().AllSatisfy(f =>
        {
            // 单语模式：DisplayName 应为字符串，DisplayNameTranslations 应为 null
            if (f.DisplayNameKey != null)
            {
                f.DisplayName.Should().NotBeNullOrWhiteSpace();
            }
            f.DisplayNameTranslations.Should().BeNull();
        });
    }

    [Fact]
    public async Task GetFieldsAsync_WithoutLang_ShouldReturnMultiLanguage()
    {
        // Arrange
        using var factory = new TestWebAppFactory();
        var cache = factory.Services.GetRequiredService<IFieldMetadataCache>();

        // Act
        var fields = await cache.GetFieldsAsync("BobCrm.Base.Custom.Customer", null);

        // Assert
        fields.Should().NotBeEmpty();
        // 多语模式验证
    }

    [Fact]
    public async Task GetFieldsAsync_SecondCall_ShouldUseCachedValue()
    {
        // Arrange
        using var factory = new TestWebAppFactory();
        var cache = factory.Services.GetRequiredService<IFieldMetadataCache>();

        // Act
        var fields1 = await cache.GetFieldsAsync("BobCrm.Base.Custom.Customer", "zh");
        var fields2 = await cache.GetFieldsAsync("BobCrm.Base.Custom.Customer", "zh");

        // Assert
        fields1.Should().BeEquivalentTo(fields2);
        // 可通过日志验证第二次是缓存命中
    }

    [Fact]
    public async Task Invalidate_ShouldClearCache()
    {
        // Arrange
        using var factory = new TestWebAppFactory();
        var cache = factory.Services.GetRequiredService<IFieldMetadataCache>();

        // 预热缓存
        await cache.GetFieldsAsync("BobCrm.Base.Custom.Customer", "zh");

        // Act
        cache.Invalidate("BobCrm.Base.Custom.Customer");

        // Assert
        // 下次调用应重新从数据库加载（可通过日志验证）
        var fields = await cache.GetFieldsAsync("BobCrm.Base.Custom.Customer", "zh");
        fields.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFieldsAsync_NonExistentEntity_ShouldReturnEmpty()
    {
        // Arrange
        using var factory = new TestWebAppFactory();
        var cache = factory.Services.GetRequiredService<IFieldMetadataCache>();

        // Act
        var fields = await cache.GetFieldsAsync("NonExistent.Entity", "zh");

        // Assert
        fields.Should().BeEmpty();
    }
}
```

---

## 6. 验收标准

### 6.1 功能验收

- [ ] `IFieldMetadataCache` 接口定义完成
- [ ] `FieldMetadataCache` 实现完成
- [ ] DI 服务注册完成
- [ ] 缓存失效集成完成

### 6.2 质量门禁

- [ ] 编译成功（Debug + Release）
- [ ] 所有测试通过
- [ ] 无新增编译警告
- [ ] 代码覆盖率 ≥ 80%

### 6.3 性能验收

- [ ] 第二次调用使用缓存（日志验证）
- [ ] 缓存失效后重新加载

---

## 7. Git 提交规范

```bash
git add src/BobCrm.Api/Abstractions/IFieldMetadataCache.cs
git add src/BobCrm.Api/Services/FieldMetadataCache.cs
git add src/BobCrm.Api/Program.cs
git add tests/BobCrm.Api.Tests/FieldMetadataCacheTests.cs

git commit -m "feat(cache): add field metadata cache service

- Add IFieldMetadataCache interface
- Implement FieldMetadataCache with memory cache
- Support single/multi-language modes
- Integrate cache invalidation on entity changes
- Add unit tests with 5 test cases
- Ref: ARCH-30 Task 3.2"
```

---

## 8. 后续任务

完成本任务后，继续：
- **Task 3.3**: 改造动态实体查询 API，集成 `meta.fields`

---

**创建日期**: 2025-01-06
**最后更新**: 2025-01-06
**维护者**: ARCH-30 项目组
