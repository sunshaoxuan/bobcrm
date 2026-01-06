# ARCH-33 动态实体查询端点与字段元数据缓存调研报告

## 1. 背景与目标

动态实体（Dynamic Entity）提供了在运行时按元数据定义进行数据 CRUD/Query 的能力。随着前端对“字段元数据（字段显示名、多语、字段类型/约束）”依赖增强，动态实体查询端点在返回数据的同时也需要返回字段元数据；但字段元数据来自 `EntityDefinitions` 表，读取/转换成本较高，且频繁请求会带来不必要的 DB 压力。

本调研目标：

- 梳理动态实体查询端点的当前行为与瓶颈。
- 梳理字段元数据多语输出模式（单语/多语字典模式）与缓存现状。
- 明确缓存失效（invalidation）触发点与改造方案。
- 给出 API 端点改造建议（兼容与可演进）。

## 2. 现状梳理

### 2.1 端点入口

- `POST /api/dynamic-entities/{fullTypeName}/query`
  - 读取 `QueryRequest`（filters/order/skip/take），调用持久化服务进行查询与计数。
  - 当前实现默认返回 `meta.fields`（字段元数据）以及分页信息。
- `GET /api/dynamic-entities/{fullTypeName}/{id}`
  - 默认仅返回数据对象；当 `includeMeta=true` 时返回 `meta.fields`。

代码位置：

- `src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs`
- `src/BobCrm.Api/Services/IReflectionPersistenceService.cs` / `src/BobCrm.Api/Services/ReflectionPersistenceService.cs`

### 2.2 字段元数据生成与多语模式

字段元数据由 `EntityDefinitions.Include(ed => ed.Fields)` 加载，然后按字段排序并投影为 `FieldMetadataDto`：

- `lang == null`：返回“多语字典模式”（`displayNameTranslations` / `descriptionTranslations` 等），用于前端在 UI 侧选择语言渲染。
- `lang != null`：返回“单语模式”（`displayName` 等），用于后端直接输出目标语言文本，减少 payload。

相关代码位置：

- `src/BobCrm.Api/Extensions/FieldMetadataExtensions.cs`（`ToFieldDto(...)`）
- `src/BobCrm.Api/Contracts/Responses/Entity/FieldMetadataDto.cs`

### 2.3 字段元数据缓存现状

当前已有字段元数据缓存服务：

- 接口：`src/BobCrm.Api/Services/IFieldMetadataCache.cs`
- 实现：`src/BobCrm.Api/Services/FieldMetadataCache.cs`
  - 基于 `IMemoryCache`：滑动过期 30 分钟 + 绝对过期 2 小时
  - key 维度：`fullTypeName + lang`；`lang != null` 时叠加 `loc.GetCacheVersion()`，用于本地化资源更新后的自动版本隔离
  - 维护 key-set：`FieldMetadata:Keys:{fullTypeName}`，用于一次失效清除该实体所有缓存变体（单语/多语/不同语言/不同版本）

当前使用情况：

- `DynamicEntityEndpoints` 已注入并使用 `IFieldMetadataCache`，查询与按 ID 获取均会走缓存服务。
- `Program.cs` 已注册：`AddScoped<IFieldMetadataCache, FieldMetadataCache>()`。

## 3. 风险与问题

### 3.1 失效触发不足导致“元数据陈旧”

如果实体字段定义发生变更（新增字段、修改显示名/Key、删除字段等），`IMemoryCache` 仍可能在过期窗口内返回旧数据，造成前端渲染与后端规则不一致。

需要补齐的关键触发点：

- `EntityDefinition`/`FieldMetadata` 发生写入变更（Create/Update）。
- 发布/撤回流程导致字段集合或元数据关键字段变化（如发布时生成/同步字段配置）。

### 3.2 查询端点默认返回 meta 的性能权衡

`POST /query` 默认返回 `meta.fields` 对 UI 友好，但对于只需要数据列表的调用会引入额外的元数据构建与 payload 传输成本。

理想状态：

- 保持默认行为兼容（不破坏现有调用方）。
- 提供显式开关让调用方按需关闭元数据（例如后台任务、批处理、仅数据导出场景）。

## 4. 改造方案与落地建议

### 4.1 缓存失效策略（Task 3.2）

建议：在“实体定义写入”成功后主动调用 `IFieldMetadataCache.Invalidate(fullTypeName)`。

最小可用落地点：

- `EntityDefinitionAppService.CreateEntityDefinitionAsync(...)`：保存成功后失效一次（按 namespace+entityName 计算 fullTypeName 兜底）。
- `EntityDefinitionAppService.UpdateEntityDefinitionAsync(...)`：保存成功后失效一次（必要时对变更前后 fullTypeName 双失效）。

### 4.2 动态实体查询端点改造（Task 3.3）

建议：为 `POST /api/dynamic-entities/{fullTypeName}/query` 增加可选查询参数：

- `includeMeta`：
  - 未传：保持现有行为（返回 `meta.fields`）
  - `includeMeta=false`：不返回 `meta`，并跳过字段元数据读取

该方式兼容既有调用方，同时为性能敏感场景提供优化入口。

## 5. 验证与测试建议

- 单元/集成测试覆盖：
  - `includeMeta=false` 时响应体不包含 `meta`。
  - 实体定义更新后触发字段元数据缓存失效（可用 mock 验证 `Invalidate(...)` 被调用）。
- 质量门禁：
  - `pwsh scripts/check-warning-baseline.ps1`（0/0）
  - `dotnet test -c Release`

## 6. 相关代码索引

- `src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs`
- `src/BobCrm.Api/Services/IFieldMetadataCache.cs`
- `src/BobCrm.Api/Services/FieldMetadataCache.cs`
- `src/BobCrm.Api/Services/EntityDefinitionAppService.cs`

