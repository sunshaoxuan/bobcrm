# PLAN-19: ARCH-31 多态视图渲染实施计划

**版本**: 1.0
**创建日期**: 2025-01-06
**状态**: 待实施
**设计文档**: [ARCH-31-多态视图渲染.md](../design/ARCH-31-多态视图渲染.md)

---

## 1. 项目概述

### 1.1 目标

实现根据实体字段值（如 `Status`）动态选择不同显示模板的能力，支持如"草稿状态用模板A，审批状态用模板B"的业务场景。

### 1.2 核心能力

| 能力 | 描述 |
|------|------|
| 条件绑定 | 模板绑定支持字段匹配条件 |
| 优先级匹配 | 多规则按优先级排序匹配 |
| 优雅降级 | 无匹配时回退默认模板 |
| 动态切换 | 数据状态变更后自动切换模板 |

### 1.3 预计工时

| 阶段 | 工时 | 内容 |
|------|------|------|
| 阶段1 | 1 天 | 领域模型扩展 + 数据库迁移 |
| 阶段2 | 1 天 | 运行时匹配逻辑 |
| 阶段3 | 1 天 | 前端 PageLoader 适配 |
| 阶段4 | 0.5 天 | 测试与文档 |
| **总计** | **3.5 天** | |

---

## 2. 阶段1：领域模型扩展

### 2.1 任务目标

扩展 `TemplateStateBinding` 模型，支持条件匹配字段。

### 2.2 文件操作清单

| 操作 | 文件 | 说明 |
|------|------|------|
| 修改 | `src/BobCrm.Api/Base/Models/TemplateStateBinding.cs` | 新增3个字段 |
| 新增 | `src/BobCrm.Api/Migrations/xxx_AddPolymorphicBinding.cs` | EF Migration |
| 修改 | `src/BobCrm.Api/Infrastructure/Ef/AppDbContext.cs` | 更新索引配置 |

### 2.3 模型变更

**文件**: `src/BobCrm.Api/Base/Models/TemplateStateBinding.cs`

```csharp
public class TemplateStateBinding
{
    // 现有字段...
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string ViewState { get; set; } = string.Empty;  // "Detail", "Edit", "List"
    public Guid TemplateId { get; set; }
    public bool IsDefault { get; set; }

    // ========== 新增字段 ==========

    /// <summary>
    /// 匹配字段名（如 "Status"）。为空表示默认绑定。
    /// </summary>
    [MaxLength(100)]
    public string? MatchFieldName { get; set; }

    /// <summary>
    /// 匹配字段值（如 "Draft"）。为空表示默认绑定。
    /// </summary>
    [MaxLength(200)]
    public string? MatchFieldValue { get; set; }

    /// <summary>
    /// 匹配优先级。值越大优先级越高，默认绑定优先级为 0。
    /// </summary>
    public int Priority { get; set; } = 0;

    // 导航属性
    public FormTemplate? Template { get; set; }
}
```

### 2.4 索引配置

**文件**: `src/BobCrm.Api/Infrastructure/Ef/AppDbContext.cs`

在 `OnModelCreating` 中更新索引：

```csharp
modelBuilder.Entity<TemplateStateBinding>(entity =>
{
    // 移除旧的唯一索引（如有）
    // entity.HasIndex(e => new { e.EntityType, e.ViewState }).IsUnique();

    // 新索引：支持同一 EntityType+ViewState 下多条规则
    entity.HasIndex(e => new { e.EntityType, e.ViewState, e.MatchFieldName, e.MatchFieldValue })
        .IsUnique()
        .HasFilter("[MatchFieldName] IS NOT NULL");

    // 默认绑定索引（MatchFieldName 为空时）
    entity.HasIndex(e => new { e.EntityType, e.ViewState, e.IsDefault })
        .HasFilter("[MatchFieldName] IS NULL AND [IsDefault] = 1");
});
```

### 2.5 数据库迁移

```bash
cd src/BobCrm.Api
dotnet ef migrations add AddPolymorphicBinding
dotnet ef database update
```

### 2.6 验收标准

- [ ] 三个新字段已添加到模型
- [ ] Migration 创建成功
- [ ] 数据库更新成功
- [ ] 现有数据不受影响（新字段默认为 null/0）

---

## 3. 阶段2：运行时匹配逻辑

### 3.1 任务目标

实现模板匹配算法，支持按字段值和优先级选择模板。

### 3.2 文件操作清单

| 操作 | 文件 | 说明 |
|------|------|------|
| 修改 | `src/BobCrm.Api/Services/TemplateRuntimeService.cs` | 升级匹配逻辑 |
| 新增 | `tests/BobCrm.Api.Tests/TemplateMatchingTests.cs` | 匹配算法测试 |

### 3.3 接口设计

```csharp
public interface ITemplateRuntimeService
{
    // 现有方法
    Task<FormTemplate?> GetTemplateAsync(string entityType, string viewState, CancellationToken ct = default);

    // 新增重载：支持数据匹配
    Task<FormTemplate?> GetTemplateAsync(
        string entityType,
        string viewState,
        IDictionary<string, object?>? entityData,
        CancellationToken ct = default);

    // 新增：根据实体 ID 获取（内部加载数据）
    Task<FormTemplate?> GetTemplateByEntityIdAsync(
        string entityType,
        string viewState,
        Guid entityId,
        CancellationToken ct = default);
}
```

### 3.4 匹配算法实现

```csharp
public async Task<FormTemplate?> GetTemplateAsync(
    string entityType,
    string viewState,
    IDictionary<string, object?>? entityData,
    CancellationToken ct = default)
{
    // 1. 获取所有匹配的绑定规则
    var bindings = await _db.TemplateStateBindings
        .AsNoTracking()
        .Include(b => b.Template)
        .Where(b => b.EntityType == entityType && b.ViewState == viewState)
        .OrderByDescending(b => b.Priority)  // 优先级降序
        .ToListAsync(ct);

    if (bindings.Count == 0)
        return null;

    // 2. 如果有数据，尝试条件匹配
    if (entityData != null)
    {
        foreach (var binding in bindings.Where(b => !string.IsNullOrEmpty(b.MatchFieldName)))
        {
            if (TryMatchCondition(binding, entityData))
            {
                _logger.LogDebug("Matched binding: {BindingId} for {EntityType}/{ViewState} with {Field}={Value}",
                    binding.Id, entityType, viewState, binding.MatchFieldName, binding.MatchFieldValue);
                return binding.Template;
            }
        }
    }

    // 3. 回退到默认绑定
    var defaultBinding = bindings.FirstOrDefault(b => b.IsDefault && string.IsNullOrEmpty(b.MatchFieldName));
    if (defaultBinding != null)
    {
        _logger.LogDebug("Using default binding: {BindingId} for {EntityType}/{ViewState}",
            defaultBinding.Id, entityType, viewState);
        return defaultBinding.Template;
    }

    // 4. 最后回退：返回第一个无条件绑定
    var fallback = bindings.FirstOrDefault(b => string.IsNullOrEmpty(b.MatchFieldName));
    return fallback?.Template;
}

private bool TryMatchCondition(TemplateStateBinding binding, IDictionary<string, object?> data)
{
    if (string.IsNullOrEmpty(binding.MatchFieldName))
        return false;

    if (!data.TryGetValue(binding.MatchFieldName, out var value))
        return false;

    var stringValue = value?.ToString();
    return string.Equals(stringValue, binding.MatchFieldValue, StringComparison.OrdinalIgnoreCase);
}
```

### 3.5 测试用例

```csharp
public class TemplateMatchingTests
{
    [Fact]
    public async Task GetTemplate_WithMatchingCondition_ShouldReturnConditionTemplate()
    {
        // Arrange: 创建两个绑定 - 一个条件绑定(Status=Draft)，一个默认绑定
        // Act: 传入 Status=Draft 的数据
        // Assert: 返回条件绑定的模板
    }

    [Fact]
    public async Task GetTemplate_WithNoMatchingCondition_ShouldReturnDefaultTemplate()
    {
        // Arrange: 创建条件绑定(Status=Draft) + 默认绑定
        // Act: 传入 Status=Published 的数据
        // Assert: 返回默认绑定的模板
    }

    [Fact]
    public async Task GetTemplate_WithMultipleConditions_ShouldRespectPriority()
    {
        // Arrange: 创建多个条件绑定，不同优先级
        // Act: 传入匹配多个条件的数据
        // Assert: 返回最高优先级的模板
    }

    [Fact]
    public async Task GetTemplate_WithMissingField_ShouldReturnDefaultTemplate()
    {
        // Arrange: 创建条件绑定(Status=Draft)
        // Act: 传入不包含 Status 字段的数据
        // Assert: 返回默认绑定
    }

    [Fact]
    public async Task GetTemplate_WithNullData_ShouldReturnDefaultTemplate()
    {
        // Arrange: 创建条件绑定 + 默认绑定
        // Act: 传入 null 数据
        // Assert: 返回默认绑定
    }
}
```

### 3.6 验收标准

- [ ] 匹配算法实现完成
- [ ] 支持条件匹配和优先级
- [ ] 优雅降级到默认模板
- [ ] 5个核心测试用例通过
- [ ] 测试覆盖率 ≥ 90%

---

## 4. 阶段3：前端 PageLoader 适配

### 4.1 任务目标

修改前端 PageLoader，支持根据实体数据动态请求模板。

### 4.2 文件操作清单

| 操作 | 文件 | 说明 |
|------|------|------|
| 修改 | `src/BobCrm.App/Components/Pages/PageLoader.razor` | 动态模板请求 |
| 修改 | `src/BobCrm.Api/Endpoints/TemplateEndpoints.cs` | 新增数据匹配端点 |

### 4.3 API 端点设计

**新增端点**: `POST /api/templates/resolve`

```csharp
app.MapPost("/api/templates/resolve", async (
    [FromBody] ResolveTemplateRequest request,
    ITemplateRuntimeService templateService,
    CancellationToken ct) =>
{
    var template = await templateService.GetTemplateAsync(
        request.EntityType,
        request.ViewState,
        request.EntityData,
        ct);

    if (template == null)
        return Results.NotFound(ApiResponse.Fail("TEMPLATE_NOT_FOUND", "未找到匹配的模板"));

    return Results.Ok(ApiResponse.Success(new { templateId = template.Id, templateName = template.Name }));
});

public record ResolveTemplateRequest(
    string EntityType,
    string ViewState,
    Dictionary<string, object?>? EntityData);
```

### 4.4 前端适配

**文件**: `src/BobCrm.App/Components/Pages/PageLoader.razor`

```csharp
@code {
    // 现有代码...

    private async Task LoadData()
    {
        try
        {
            loading = true;

            // 1. 加载实体数据
            var entityData = await LoadEntityData();

            // 2. 根据数据动态解析模板
            var template = await ResolveTemplateAsync(entityData);

            // 3. 使用解析的模板渲染
            await RenderWithTemplate(template);

            loading = false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            loading = false;
        }
    }

    private async Task<FormTemplate?> ResolveTemplateAsync(Dictionary<string, object?> entityData)
    {
        // 调用新的 resolve 端点
        var response = await HttpClient.PostAsJsonAsync("/api/templates/resolve", new
        {
            EntityType = EntityType,
            ViewState = ViewState,
            EntityData = entityData
        });

        if (!response.IsSuccessStatusCode)
        {
            // 降级到原有逻辑
            return await GetDefaultTemplateAsync();
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ResolveResult>>();
        return await LoadTemplateById(result?.Data?.TemplateId);
    }
}
```

### 4.5 验收标准

- [ ] 新增 `/api/templates/resolve` 端点
- [ ] PageLoader 支持动态模板解析
- [ ] 状态变更后刷新页面能加载正确模板
- [ ] 向后兼容（无条件绑定时行为不变）

---

## 5. 阶段4：测试与文档

### 5.1 集成测试

**文件**: `tests/BobCrm.Api.Tests/TemplatePolymorphicTests.cs`

```csharp
[Fact]
public async Task EndToEnd_StatusChange_ShouldSwitchTemplate()
{
    // 1. 创建实体定义和两个模板（Draft模板、Default模板）
    // 2. 创建条件绑定（Status=Draft -> 模板1）和默认绑定（-> 模板2）
    // 3. 创建实体数据 Status=Draft
    // 4. 调用 resolve 端点，验证返回模板1
    // 5. 更新实体 Status=Published
    // 6. 再次调用 resolve 端点，验证返回模板2
}
```

### 5.2 文档更新

| 文档 | 更新内容 |
|------|----------|
| `CHANGELOG.md` | 添加多态视图渲染功能条目 |
| `API-01-接口文档.md` | 添加 `/api/templates/resolve` 端点文档 |
| `CLAUDE.md` | 更新模板系统章节 |

### 5.3 验收标准

- [ ] 集成测试通过
- [ ] 单元测试覆盖率 ≥ 90%
- [ ] 文档同步更新
- [ ] 回归测试无异常

---

## 6. 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 性能下降 | 每次页面加载多一次查询 | 使用内存缓存绑定规则 |
| 向后兼容 | 旧数据无新字段 | 新字段默认 null，匹配逻辑兼容 |
| 前端复杂度 | PageLoader 逻辑变复杂 | 抽取 TemplateResolver 服务 |

---

## 7. Git 提交规范

```bash
# 阶段1
git commit -m "feat(model): add polymorphic binding fields to TemplateStateBinding

- Add MatchFieldName, MatchFieldValue, Priority fields
- Update index configuration for multi-rule support
- Add EF migration
- Ref: ARCH-31 Phase 1"

# 阶段2
git commit -m "feat(service): implement polymorphic template matching

- Add GetTemplateAsync overload with entity data
- Implement priority-based matching algorithm
- Add graceful fallback to default template
- Add 5 unit tests with 90%+ coverage
- Ref: ARCH-31 Phase 2"

# 阶段3
git commit -m "feat(frontend): add dynamic template resolution to PageLoader

- Add POST /api/templates/resolve endpoint
- Update PageLoader to use data-driven template selection
- Support automatic template switch on status change
- Ref: ARCH-31 Phase 3"

# 阶段4
git commit -m "docs: update documentation for polymorphic view rendering

- Add CHANGELOG entry
- Update API reference
- Add integration test
- Ref: ARCH-31 Phase 4"
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
