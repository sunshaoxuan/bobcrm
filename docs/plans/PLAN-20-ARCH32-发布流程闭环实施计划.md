# PLAN-20: ARCH-32 发布流程闭环实施计划

**版本**: 1.0
**创建日期**: 2025-01-06
**状态**: 待实施
**设计文档**: [ARCH-32-发布流程闭环.md](../design/ARCH-32-发布流程闭环.md)

---

## 1. 项目概述

### 1.1 目标

解决实体发布流程的"最后一公里"问题，包括：
- AggVO 级联发布
- 全局权限拦截
- 发布撤回功能

### 1.2 核心能力

| 能力 | 描述 |
|------|------|
| 级联发布 | 主实体发布时自动发布依赖的子实体 |
| 权限拦截 | 前端路由级别的权限预检 |
| 撤回功能 | 支持将已发布实体撤回为草稿 |
| 引用保护 | 被引用的实体不可撤回 |

### 1.3 预计工时

| 阶段 | 工时 | 内容 |
|------|------|------|
| 阶段1 | 1.5 天 | AggVO 级联发布 |
| 阶段2 | 1 天 | 全局权限拦截器 |
| 阶段3 | 1 天 | 发布撤回功能 |
| 阶段4 | 0.5 天 | 测试与文档 |
| **总计** | **4 天** | |

---

## 2. 阶段1：AggVO 级联发布

### 2.1 任务目标

增强发布服务，支持自动识别并发布依赖实体。

### 2.2 文件操作清单

| 操作 | 文件 | 说明 |
|------|------|------|
| 修改 | `src/BobCrm.Api/Services/EntityPublishingService.cs` | 增强发布逻辑 |
| 新增 | `src/BobCrm.Api/Services/EntityDependencyAnalyzer.cs` | 依赖分析服务 |
| 新增 | `tests/BobCrm.Api.Tests/CascadePublishingTests.cs` | 级联发布测试 |

### 2.3 依赖分析服务

**文件**: `src/BobCrm.Api/Services/EntityDependencyAnalyzer.cs`

```csharp
namespace BobCrm.Api.Services;

/// <summary>
/// 实体依赖分析服务
/// </summary>
public interface IEntityDependencyAnalyzer
{
    /// <summary>
    /// 获取实体的所有依赖实体（Lookup 引用）
    /// </summary>
    Task<IReadOnlyList<EntityDefinition>> GetDependenciesAsync(
        Guid entityDefinitionId,
        CancellationToken ct = default);

    /// <summary>
    /// 获取引用当前实体的所有实体
    /// </summary>
    Task<IReadOnlyList<EntityDefinition>> GetReferencingEntitiesAsync(
        Guid entityDefinitionId,
        CancellationToken ct = default);
}

public class EntityDependencyAnalyzer : IEntityDependencyAnalyzer
{
    private readonly AppDbContext _db;
    private readonly ILogger<EntityDependencyAnalyzer> _logger;

    public EntityDependencyAnalyzer(AppDbContext db, ILogger<EntityDependencyAnalyzer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EntityDefinition>> GetDependenciesAsync(
        Guid entityDefinitionId,
        CancellationToken ct = default)
    {
        // 1. 获取实体的所有字段
        var entity = await _db.EntityDefinitions
            .AsNoTracking()
            .Include(e => e.Fields.Where(f => !f.IsDeleted))
            .FirstOrDefaultAsync(e => e.Id == entityDefinitionId, ct);

        if (entity == null)
            return Array.Empty<EntityDefinition>();

        // 2. 找出所有 Lookup 字段引用的实体
        var lookupEntityNames = entity.Fields
            .Where(f => f.DataType == FieldDataType.Lookup && !string.IsNullOrEmpty(f.LookupEntityName))
            .Select(f => f.LookupEntityName!)
            .Distinct()
            .ToList();

        if (lookupEntityNames.Count == 0)
            return Array.Empty<EntityDefinition>();

        // 3. 查询依赖实体
        var dependencies = await _db.EntityDefinitions
            .AsNoTracking()
            .Where(e => lookupEntityNames.Contains(e.EntityName) || lookupEntityNames.Contains(e.FullTypeName))
            .ToListAsync(ct);

        _logger.LogDebug("Entity {EntityId} has {Count} dependencies: {Names}",
            entityDefinitionId, dependencies.Count, string.Join(", ", dependencies.Select(d => d.EntityName)));

        return dependencies;
    }

    public async Task<IReadOnlyList<EntityDefinition>> GetReferencingEntitiesAsync(
        Guid entityDefinitionId,
        CancellationToken ct = default)
    {
        var entity = await _db.EntityDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entityDefinitionId, ct);

        if (entity == null)
            return Array.Empty<EntityDefinition>();

        // 查找所有引用此实体的字段
        var referencingEntityIds = await _db.FieldMetadata
            .AsNoTracking()
            .Where(f => !f.IsDeleted &&
                        f.DataType == FieldDataType.Lookup &&
                        (f.LookupEntityName == entity.EntityName || f.LookupEntityName == entity.FullTypeName))
            .Select(f => f.EntityDefinitionId)
            .Distinct()
            .ToListAsync(ct);

        var referencingEntities = await _db.EntityDefinitions
            .AsNoTracking()
            .Where(e => referencingEntityIds.Contains(e.Id) && e.Status == EntityStatus.Published)
            .ToListAsync(ct);

        return referencingEntities;
    }
}
```

### 2.4 增强发布服务

**文件**: `src/BobCrm.Api/Services/EntityPublishingService.cs`

```csharp
public async Task<PublishResult> PublishNewEntityAsync(
    Guid entityDefinitionId,
    string? publishedBy = null,
    CancellationToken ct = default)
{
    var entity = await _db.EntityDefinitions
        .Include(e => e.Fields.Where(f => !f.IsDeleted))
        .FirstOrDefaultAsync(e => e.Id == entityDefinitionId, ct);

    if (entity == null)
        return PublishResult.Failure("ENTITY_NOT_FOUND", "实体不存在");

    // ========== 级联发布依赖实体 ==========
    var cascadeResult = await CascadePublishDependenciesAsync(entity, publishedBy, ct);
    if (!cascadeResult.Success)
        return cascadeResult;

    // 原有发布逻辑...
    return await DoPublishAsync(entity, publishedBy, ct);
}

private async Task<PublishResult> CascadePublishDependenciesAsync(
    EntityDefinition entity,
    string? publishedBy,
    CancellationToken ct)
{
    var dependencies = await _dependencyAnalyzer.GetDependenciesAsync(entity.Id, ct);

    foreach (var dep in dependencies)
    {
        // 跳过已发布的实体
        if (dep.Status == EntityStatus.Published)
            continue;

        // 跳过系统实体
        if (dep.Source == EntitySource.System)
        {
            _logger.LogWarning("Dependency {DepName} is a system entity, skipping cascade publish",
                dep.EntityName);
            continue;
        }

        _logger.LogInformation("Cascade publishing dependency: {DepName} for {EntityName}",
            dep.EntityName, entity.EntityName);

        // 级联发布（标记为系统发布，跳过模板生成）
        var result = await PublishAsCascadeAsync(dep.Id, publishedBy, ct);
        if (!result.Success)
        {
            return PublishResult.Failure("CASCADE_FAILED",
                $"级联发布依赖实体 {dep.EntityName} 失败: {result.ErrorMessage}");
        }
    }

    return PublishResult.Successful();
}

private async Task<PublishResult> PublishAsCascadeAsync(
    Guid entityDefinitionId,
    string? publishedBy,
    CancellationToken ct)
{
    var entity = await _db.EntityDefinitions
        .Include(e => e.Fields.Where(f => !f.IsDeleted))
        .FirstOrDefaultAsync(e => e.Id == entityDefinitionId, ct);

    if (entity == null)
        return PublishResult.Failure("ENTITY_NOT_FOUND", "依赖实体不存在");

    // 标记为系统级联发布
    entity.Source = EntitySource.System;

    // 执行发布（跳过 EnsureTemplatesAsync）
    return await DoPublishAsync(entity, publishedBy, skipTemplates: true, ct);
}
```

### 2.5 测试用例

```csharp
[Fact]
public async Task PublishEntity_WithDraftDependency_ShouldCascadePublish()
{
    // Arrange: 创建主实体A（引用实体B），A和B都是Draft
    // Act: 发布实体A
    // Assert: A和B都变为Published
}

[Fact]
public async Task PublishEntity_WithPublishedDependency_ShouldNotRepublish()
{
    // Arrange: 创建主实体A（引用实体B），B已是Published
    // Act: 发布实体A
    // Assert: A变为Published，B状态不变
}

[Fact]
public async Task PublishEntity_WithSystemDependency_ShouldSkipCascade()
{
    // Arrange: 创建主实体A（引用系统实体Customer）
    // Act: 发布实体A
    // Assert: A发布成功，Customer不受影响
}
```

### 2.6 验收标准

- [ ] 依赖分析服务实现完成
- [ ] 级联发布逻辑实现完成
- [ ] 系统实体保护（不级联）
- [ ] 测试覆盖率 ≥ 90%

---

## 3. 阶段2：全局权限拦截器

### 3.1 任务目标

实现前端路由级别的权限预检，防止通过 URL 直接访问无权限页面。

### 3.2 文件操作清单

| 操作 | 文件 | 说明 |
|------|------|------|
| 修改 | `src/BobCrm.App/Components/Layout/MainLayout.razor` | 添加权限检查 |
| 新增 | `src/BobCrm.App/Services/RoutePermissionService.cs` | 路由权限服务 |
| 新增 | `src/BobCrm.App/Components/Pages/Forbidden.razor` | 403 页面 |

### 3.3 路由权限服务

**文件**: `src/BobCrm.App/Services/RoutePermissionService.cs`

```csharp
namespace BobCrm.App.Services;

/// <summary>
/// 路由权限检查服务
/// </summary>
public interface IRoutePermissionService
{
    /// <summary>
    /// 检查当前用户是否有访问指定路由的权限
    /// </summary>
    Task<bool> CanAccessAsync(string route, CancellationToken ct = default);

    /// <summary>
    /// 获取路由所需的权限码
    /// </summary>
    Task<string?> GetRequiredFunctionCodeAsync(string route, CancellationToken ct = default);
}

public class RoutePermissionService : IRoutePermissionService
{
    private readonly HttpClient _httpClient;
    private readonly IAccessService _accessService;
    private readonly ILogger<RoutePermissionService> _logger;

    // 缓存：路由 -> 权限码
    private Dictionary<string, string?>? _routePermissions;

    public RoutePermissionService(
        HttpClient httpClient,
        IAccessService accessService,
        ILogger<RoutePermissionService> logger)
    {
        _httpClient = httpClient;
        _accessService = accessService;
        _logger = logger;
    }

    public async Task<bool> CanAccessAsync(string route, CancellationToken ct = default)
    {
        var requiredCode = await GetRequiredFunctionCodeAsync(route, ct);

        // 无权限要求，允许访问
        if (string.IsNullOrEmpty(requiredCode))
            return true;

        // 检查用户是否拥有该权限
        return await _accessService.HasPermissionAsync(requiredCode, ct);
    }

    public async Task<string?> GetRequiredFunctionCodeAsync(string route, CancellationToken ct = default)
    {
        // 懒加载路由权限映射
        if (_routePermissions == null)
        {
            await LoadRoutePermissionsAsync(ct);
        }

        // 匹配路由（支持通配符）
        foreach (var (pattern, code) in _routePermissions!)
        {
            if (RouteMatches(route, pattern))
                return code;
        }

        return null;
    }

    private async Task LoadRoutePermissionsAsync(CancellationToken ct)
    {
        // 从后端加载路由权限映射
        // 包括：菜单绑定、模板绑定的 RequiredFunctionCode
        var response = await _httpClient.GetAsync("/api/access/route-permissions", ct);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<Dictionary<string, string?>>(ct);
            _routePermissions = data ?? new();
        }
        else
        {
            _routePermissions = new();
        }
    }

    private bool RouteMatches(string route, string pattern)
    {
        // 简单匹配：精确匹配或前缀匹配
        if (pattern.EndsWith("/*"))
        {
            var prefix = pattern[..^2];
            return route.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
        return string.Equals(route, pattern, StringComparison.OrdinalIgnoreCase);
    }
}
```

### 3.4 MainLayout 集成

**文件**: `src/BobCrm.App/Components/Layout/MainLayout.razor`

```razor
@inject IRoutePermissionService PermissionService
@inject NavigationManager Navigation

@code {
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // 权限预检
        var currentRoute = Navigation.ToBaseRelativePath(Navigation.Uri);
        if (!await PermissionService.CanAccessAsync(currentRoute))
        {
            Navigation.NavigateTo("/forbidden", replace: true);
        }
    }
}
```

### 3.5 403 页面

**文件**: `src/BobCrm.App/Components/Pages/Forbidden.razor`

```razor
@page "/forbidden"
@layout EmptyLayout

<div class="forbidden-container">
    <h1>403</h1>
    <h2>@I18n.T("ERR_FORBIDDEN_TITLE")</h2>
    <p>@I18n.T("ERR_FORBIDDEN_MESSAGE")</p>
    <a href="/" class="back-link">@I18n.T("BTN_BACK_HOME")</a>
</div>
```

### 3.6 验收标准

- [ ] 路由权限服务实现完成
- [ ] MainLayout 集成权限检查
- [ ] 403 页面实现
- [ ] 未授权用户通过 URL 访问被拦截

---

## 4. 阶段3：发布撤回功能

### 4.1 任务目标

实现实体撤回功能，支持将已发布实体退回草稿状态。

### 4.2 文件操作清单

| 操作 | 文件 | 说明 |
|------|------|------|
| 修改 | `src/BobCrm.Api/Services/EntityPublishingService.cs` | 实现撤回逻辑 |
| 修改 | `src/BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs` | 添加撤回端点 |
| 新增 | `tests/BobCrm.Api.Tests/EntityWithdrawTests.cs` | 撤回功能测试 |

### 4.3 撤回逻辑实现

```csharp
public async Task<WithdrawResult> WithdrawAsync(
    Guid entityDefinitionId,
    string? withdrawnBy = null,
    CancellationToken ct = default)
{
    var entity = await _db.EntityDefinitions
        .FirstOrDefaultAsync(e => e.Id == entityDefinitionId, ct);

    if (entity == null)
        return WithdrawResult.Failure("ENTITY_NOT_FOUND", "实体不存在");

    if (entity.Status != EntityStatus.Published)
        return WithdrawResult.Failure("NOT_PUBLISHED", "实体未发布，无法撤回");

    // ========== 引用检查 ==========
    var referencingEntities = await _dependencyAnalyzer.GetReferencingEntitiesAsync(entity.Id, ct);
    if (referencingEntities.Any())
    {
        var names = string.Join(", ", referencingEntities.Select(e => e.EntityName));
        return WithdrawResult.Failure("HAS_REFERENCES",
            $"实体被以下已发布实体引用，无法撤回: {names}");
    }

    // ========== 执行撤回 ==========
    entity.Status = EntityStatus.Withdrawn;
    entity.UpdatedAt = DateTime.UtcNow;
    entity.UpdatedBy = withdrawnBy;

    // 可选：记录撤回历史
    _db.EntityStatusHistory.Add(new EntityStatusHistory
    {
        EntityDefinitionId = entity.Id,
        FromStatus = EntityStatus.Published,
        ToStatus = EntityStatus.Withdrawn,
        ChangedBy = withdrawnBy,
        ChangedAt = DateTime.UtcNow,
        Reason = "Manual withdraw"
    });

    await _db.SaveChangesAsync(ct);

    _logger.LogInformation("Entity {EntityName} withdrawn by {User}",
        entity.EntityName, withdrawnBy ?? "system");

    return WithdrawResult.Successful();
}
```

### 4.4 API 端点

```csharp
app.MapPost("/api/entity-definitions/{id:guid}/withdraw", async (
    Guid id,
    IEntityPublishingService publishingService,
    HttpContext http,
    CancellationToken ct) =>
{
    var user = http.User.Identity?.Name;
    var result = await publishingService.WithdrawAsync(id, user, ct);

    if (!result.Success)
        return Results.BadRequest(ApiResponse.Fail(result.ErrorCode!, result.ErrorMessage!));

    return Results.Ok(ApiResponse.Success(new { message = "撤回成功" }));
})
.RequireAuthorization("AdminPolicy")
.WithTags("EntityDefinition")
.WithSummary("撤回已发布的实体定义");
```

### 4.5 测试用例

```csharp
[Fact]
public async Task Withdraw_PublishedEntity_ShouldSucceed()
{
    // Arrange: 创建已发布实体
    // Act: 调用撤回
    // Assert: 状态变为 Withdrawn
}

[Fact]
public async Task Withdraw_ReferencedEntity_ShouldFail()
{
    // Arrange: 创建实体A(已发布)，被实体B(已发布)引用
    // Act: 撤回实体A
    // Assert: 返回 HAS_REFERENCES 错误
}

[Fact]
public async Task Withdraw_DraftEntity_ShouldFail()
{
    // Arrange: 创建草稿实体
    // Act: 调用撤回
    // Assert: 返回 NOT_PUBLISHED 错误
}
```

### 4.6 验收标准

- [ ] 撤回逻辑实现完成
- [ ] 引用保护（被引用实体不可撤回）
- [ ] 撤回端点实现
- [ ] 测试覆盖率 ≥ 90%

---

## 5. 阶段4：测试与文档

### 5.1 集成测试

```csharp
[Fact]
public async Task EndToEnd_PublishWithdrawCycle()
{
    // 1. 创建实体A（引用实体B）
    // 2. 发布A（验证B被级联发布）
    // 3. 尝试撤回B（验证失败，被A引用）
    // 4. 撤回A
    // 5. 撤回B（验证成功）
}
```

### 5.2 文档更新

| 文档 | 更新内容 |
|------|----------|
| `CHANGELOG.md` | 添加发布流程闭环功能条目 |
| `API-01-接口文档.md` | 添加 withdraw 端点文档 |
| `CLAUDE.md` | 更新实体发布章节 |

---

## 6. 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 循环依赖 | 级联发布死循环 | 记录已处理实体，检测循环 |
| 性能 | 大量依赖时发布慢 | 并行发布依赖实体 |
| 数据一致性 | 部分发布失败 | 使用事务，全部成功或回滚 |

---

## 7. Git 提交规范

```bash
# 阶段1
git commit -m "feat(publish): add cascade publishing for entity dependencies

- Add EntityDependencyAnalyzer service
- Implement cascade publish for Lookup dependencies
- Skip system entities in cascade
- Add unit tests
- Ref: ARCH-32 Phase 1"

# 阶段2
git commit -m "feat(auth): add route-level permission interceptor

- Add RoutePermissionService for frontend
- Integrate permission check in MainLayout
- Add 403 Forbidden page
- Ref: ARCH-32 Phase 2"

# 阶段3
git commit -m "feat(publish): add entity withdraw functionality

- Implement WithdrawAsync in EntityPublishingService
- Add reference protection (prevent withdraw if referenced)
- Add POST /api/entity-definitions/{id}/withdraw endpoint
- Add unit tests
- Ref: ARCH-32 Phase 3"
```

---

## 8. 进度跟踪

| 阶段 | 状态 | 开始日期 | 完成日期 | 负责人 |
|------|------|----------|----------|--------|
| 阶段1 | ⏳ 待开始 | - | - | - |
| 阶段2 | ⏳ 待开始 | - | - | - |
| 阶段3 | ⏳ 待开始 | - | - | - |
| 阶段4 | ⏳ 待开始 | - | - | - |

---

**创建日期**: 2025-01-06
**最后更新**: 2025-01-06
**维护者**: 架构组
