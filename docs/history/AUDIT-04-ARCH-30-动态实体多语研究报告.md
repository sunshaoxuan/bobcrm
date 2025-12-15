# AUDIT-04: ARCH-30 动态实体多语研究报告（Task 3.1）

最后更新：2025-12-12

## 0. 结论先行

- 动态实体“数据查询”链路当前不做 DTO 转换，API 直接返回运行时实体对象；因此**字段显示名（DisplayName/DisplayNameKey）不在动态实体查询结果中出现**。
- 字段显示名的多语解析（尤其是接口字段的 `DisplayNameKey`）最佳落点是**元数据 API（EntityDefinition/FieldMetadata DTO）**与**菜单/树等需要显示名的 DTO**，而不是动态实体数据查询本身。
- 若未来需要“查询结果携带列元数据（字段名/显示名）”，推荐**运行时预加载/缓存实体字段元数据 + 批量加载 i18n 资源**，在端点层拼装 `meta`，而不是把翻译字符串硬编码进动态生成代码中。

---

## 1. 动态实体代码生成机制（CSharpCodeGenerator）

参考：`src/BobCrm.Api/Services/CSharpCodeGenerator.cs`

### 1.1 生成流程概览

- 输入：`EntityDefinition`（包含 `Fields`、`Interfaces`）
- 输出：一段完整的 C# 源码字符串（包含 namespace/class/属性/注释/少量 DataAnnotations）
- 生成实体类入口方法：`GenerateEntityClass(EntityDefinition entity)`

### 1.2 字段属性如何生成

在 `GenerateEntityClass()` 中按 `entity.Fields.OrderBy(f => f.SortOrder)` 逐个调用 `GenerateProperty(sb, field, entity)`。

`GenerateProperty()` 生成：

- XML 注释（summary）
- `[Required]` / `[MaxLength]` / `[Column(TypeName=...)]` / `[ForeignKey]` 等属性
- 属性声明：`public <csType> <PropertyName> { get; set; }`

### 1.3 字段显示名是否注入到生成代码中？

当前“显示名”只用于**XML 注释**，并且来源是 `FieldMetadata.DisplayName`：

- 实体注释：`MultilingualTextHelper.Resolve(entity.DisplayName, entity.EntityName)`
- 字段注释：`MultilingualTextHelper.Resolve(field.DisplayName, field.PropertyName)`

关键点：

- 生成代码**不包含** `DisplayNameKey`（字段/实体）相关信息。
- 生成代码**不包含**任何能在运行时用于 API 响应的显示名元数据（例如自定义 Attribute、静态字典、接口等）。
- `MultilingualTextHelper.Resolve(...)` 是“生成时固定回退顺序”的解析（见 `src/BobCrm.Api/Services/MultilingualTextHelper.cs`），并非按请求 `lang` 解析。

结论：当前动态实体代码生成阶段**没有把字段显示名元数据注入到实体类型可访问的结构中**；只把“某个语言的字符串”写进 XML 注释。

---

## 2. 动态实体编译与加载机制（DynamicEntityService）

参考：`src/BobCrm.Api/Services/DynamicEntityService.cs`

### 2.1 编译入口

- `CompileEntityAsync(Guid entityDefinitionId)`
  - 从 DB 读取实体定义：`EntityDefinitions.Include(e => e.Fields).Include(e => e.Interfaces)`
  - 生成代码：`_codeGenerator.GenerateEntityClass(entity)`
  - 编译：`_compiler.Compile(code, assemblyName)`
  - 缓存程序集：`_loadedAssemblies[entity.FullTypeName] = result.Assembly`

- `CompileMultipleEntitiesAsync(List<Guid> entityDefinitionIds)`
  - 对每个已发布实体生成源码
  - 额外加入 `_Interfaces.cs`（`GenerateInterfaces()`）
  - `CompileMultiple(sources, assemblyName)`，然后把同一个 assembly 缓存到多个 `FullTypeName`

### 2.2 程序集缓存机制

- 缓存结构：`private static readonly Dictionary<string, Assembly> _loadedAssemblies`
- Key：`fullTypeName`（例如 `BobCrm.Base.Custom.Customer`）
- Value：Roslyn 编译得到的 `Assembly`
- 并发保护：`lock (_lock)`

### 2.3 运行时类型访问能力

运行时对动态实体类型的能力主要是反射层面：

- `GetEntityType(fullTypeName)`：从缓存 assembly 中 `GetType(fullTypeName)`
- `CreateEntityInstance(fullTypeName)`：`Activator.CreateInstance(type)`
- `GetEntityProperties(fullTypeName)`：`type.GetProperties(...)`
- `GetEntityTypeInfo(fullTypeName)`：返回属性列表（名称/类型/可空/读写），但不包含字段显示名元数据

结论：除非在“生成代码阶段”把字段元数据编码进类型，否则运行时类型本身无法提供 `DisplayName/DisplayNameKey` 等信息。

---

## 3. 查询结果转换流程（ReflectionPersistenceService）

参考：`src/BobCrm.Api/Services/ReflectionPersistenceService.cs`

### 3.1 QueryAsync：列表查询

- `QueryAsync(string fullTypeName, QueryOptions? options = null)`
- 通过 `DynamicEntityService.GetEntityType(fullTypeName)` 获取运行时类型
- 通过反射调用 `AppDbContext.Set<T>()` 获得 `DbSet<T>`，再以 `IQueryable<object>` 执行：
  - 软删过滤：若实体有 `IsDeleted` 属性则 `EF.Property<bool>(e, "IsDeleted") == false`
  - Filters / OrderBy / Skip / Take
  - `ToListAsync()`

返回值：`List<object>`（即实体实例列表）

### 3.2 GetByIdAsync：单体查询

- `GetByIdAsync(string fullTypeName, int id)`
- 同样获取 `DbSet<T>`，用 `EF.Property<int>(e, "Id") == id` 做过滤

返回值：`object?`（实体实例）

### 3.3 DTO/JSON 转换点

服务层没有 DTO 映射逻辑；序列化发生在端点层返回 `Results.Ok(entity)` / `Results.Ok(new { data = results, ... })` 时由 ASP.NET Core 的 JSON formatter 完成。

结论：动态实体“数据查询”链路目前没有字段显示名解析点；如果要加，需要在端点层（或新增专门的 mapper/serializer）引入元数据。

---

## 4. 动态实体端点研究（DynamicEntityEndpoints）

参考：`src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs`

### 4.1 关键端点与返回格式

- `POST /api/dynamic-entities/{fullTypeName}/query`
  - 返回：
    - `data`: `results`（`List<object>`）
    - `total`: `CountAsync(...)`
    - `page` / `pageSize`

- `GET /api/dynamic-entities/{fullTypeName}/{id}`
  - 返回：`entity`（单个对象）

- `POST /api/dynamic-entities/{fullTypeName}`
- `PUT /api/dynamic-entities/{fullTypeName}/{id}`
- `DELETE /api/dynamic-entities/{fullTypeName}/{id}`
- `POST /api/dynamic-entities/{fullTypeName}/count`

### 4.2 是否支持 lang 参数？

当前动态实体端点未显式声明 `string? lang` 参数；错误消息语言通过：

- `var lang = LangHelper.GetLang(http);`

这意味着它会从 Query/X-Lang/Accept-Language 推断语言（见 `src/BobCrm.Api/Infrastructure/Localization.cs`），但仅用于错误文本，不影响实体数据返回结构。

结论：动态实体端点当前**没有“单语/多语双模式响应”**的 DTO 结构；只在错误消息上体现语言。

---

## 5. 字段元数据存储与访问方式（EntityDefinition / FieldMetadata）

参考：

- `src/BobCrm.Api/Base/Models/EntityDefinition.cs`
- `src/BobCrm.Api/Base/Models/FieldMetadata.cs`
- `src/BobCrm.Api/Infrastructure/AppDbContext.cs`

### 5.1 存储位置

- `EntityDefinition.DisplayName` / `EntityDefinition.Description`：`jsonb`（多语字典）
- `FieldMetadata.DisplayName`：`jsonb`（多语字典）
- `FieldMetadata.DisplayNameKey`：`varchar(100)`（i18n 资源 key，用于接口字段）

`EntityDefinition.Fields` 通过 EF 导航属性持有字段集合。

### 5.2 访问方式

动态实体编译阶段与元数据 API 阶段均从 DB 加载 `EntityDefinition.Include(e => e.Fields)`，因此字段元数据天然可用。

DB 映射：

- `AppDbContext.OnModelCreating` 为多语字典配置了 `jsonb` + 值转换器（包括 `EntityDefinition.DisplayName/Description`、`FieldMetadata.DisplayName`）。

---

## 6. 字段 DisplayName 解析时机分析（编译时 vs 运行时）

### 6.1 编译时注入（将“翻译字符串”写入生成代码）

可行性：

- 可以在 `CSharpCodeGenerator` 里把某个语言的字符串写入 Attribute 或静态字段。
- 但要支持按请求 `lang` 返回，需要写入“所有语言”的翻译字典（或支持动态语言集合）。

问题：

- i18n 资源是数据库驱动、语言集合可动态扩展；把所有翻译写进代码会膨胀且不易扩展。
- 资源更新（尤其是 i18n 资源）会要求重新编译动态实体程序集才能生效。
- 动态实体数据查询链路并不需要字段显示名；把显示名绑进实体类型本身收益有限。

结论：不推荐把“翻译字符串”作为动态实体生成代码的一部分来服务 API 响应。

### 6.2 编译时注入（仅注入 DisplayNameKey/字段元数据映射）

可行性：

- 生成代码时为每个属性生成一个“元数据表”，例如：
  - 自定义 Attribute：`[FieldMetadata(DisplayNameKey="LBL_...")]`
  - 或静态字典：`static readonly Dictionary<string,string> DisplayNameKeyMap`
- 运行时仍需要 `ILocalization` 才能把 key 翻译成目标语言。

优点：

- 实体类型在运行时可自描述（至少能拿到 key）。
- 不依赖额外 DB 查询 FieldMetadata（但仍需 i18n 资源的读取/缓存）。

缺点：

- 字段元数据修改（字段新增/删除/重命名）仍需要重新编译实体类型才能对齐。
- 与现有架构的“元数据 DB 即单一真实来源”会出现双写/一致性挑战。

结论：仅在确实需要“纯运行时类型自描述”时考虑；否则不作为首选。

### 6.3 运行时查询（在返回时拉取元数据并解析）

落点：

- 元数据 API（EntityDefinition/FieldMetadata）返回 DTO 时解析 `DisplayNameKey` → `DisplayName`
- 若动态实体查询需要列信息，则在查询端点返回 `meta.fields`，并在这里解析

优点：

- 字段元数据/i18n 资源更新无需重新编译动态实体类型。
- 可复用现有的 `ILocalization` 缓存与 `MultilingualFieldService.LoadResourcesAsync(...)` 批量加载能力。

成本：

- 若每次查询都访问 DB 拉元数据，会带来额外开销；需要缓存/预加载策略。

结论：推荐作为主方案，并配合缓存降低额外开销。

### 6.4 预加载缓存（推荐）

建议设计：

- 维度1：按 `EntityDefinitionId` 或 `FullTypeName` 缓存字段元数据（包含 `PropertyName`、`DisplayNameKey`、`DisplayName`）。
- 维度2：按 `ILocalization.GetCacheVersion()`（或资源更新时间戳）+ `EntityDefinition.UpdatedAt` 作为缓存失效条件。
- 解析策略：
  - 单语模式（显式 `?lang=xx`）：输出 `DisplayName`（string），必要时用 `DisplayNameKey` → `ILocalization.T(...)`。
  - 多语模式（无 `lang`）：接口字段输出 `DisplayNameKey`（不展开 translations），自定义字段输出 `DisplayNameTranslations`（字典）。

可复用能力：

- `DtoExtensions.ResolveFieldDisplayName(...)`（`src/BobCrm.Api/Extensions/DtoExtensions.cs`）已实现“DisplayNameKey → DisplayName → PropertyName”的优先级。
- `MultilingualFieldService.LoadResourcesAsync(...)` 可批量加载 key，减少 N+1。

---

## 7. 结论与建议（面向后续 Task 3.x）

### 7.1 最佳解析时机（推荐）

- **字段显示名的解析（DisplayNameKey → DisplayName）应发生在“元数据返回层”**（EntityDefinition/FieldMetadata/FunctionTree DTO 等），而不是动态实体数据查询返回层。
- 动态实体查询结果保持“纯数据对象”更符合职责分离：数据值 vs 元数据标签。

### 7.2 如果必须在动态查询响应中携带列信息

建议返回结构：

```json
{
  "meta": {
    "fields": [
      { "propertyName": "Code", "displayNameKey": "LBL_FIELD_CODE", "displayName": "编码" }
    ]
  },
  "data": [ ... ],
  "total": 123
}
```

并遵循 ARCH-30 统一规则：

- 显式 `?lang=xx` 才输出 `displayName`（单语）
- 无 `lang` 输出 `displayNameKey`（接口字段）与 `displayNameTranslations`（自定义字段）

### 7.3 性能与一致性建议

- 以 `ILocalization` 现有的缓存（语言字典 + cache version）为基础，新增“实体字段元数据缓存”即可。
- 缓存失效触发：
  - 实体字段变更（EntityDefinition/FieldMetadata 更新）
  - i18n 资源变更（`ILocalization.GetCacheVersion()` 变化）
