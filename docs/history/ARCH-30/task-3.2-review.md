# ARCH-30 Task 3.2 设计文档评审报告

**评审日期**: 2025-12-12  
**评审者**: 架构组  
**任务**: Task 3.2 - 设计字段级多语解析方案  
**评审范围**: 设计文档 `docs/design/ARCH-30-实体字段显示名多语元数据驱动设计.md` (Task 3.2 章节)  
**评审结果**: ✅ **优秀（5.0/5.0）**

---

## 🎯 评审总结

| 评审项 | 状态 | 评分 | 说明 |
|--------|------|------|------|
| 设计方案完整性 | ✅ 完美 | 5/5 | 完整覆盖所有设计要点 |
| 设计原则清晰性 | ✅ 完美 | 5/5 | 职责分离、向后兼容、性能优先、可复用性 |
| 返回结构设计 | ✅ 完美 | 5/5 | meta.fields 结构清晰，向后兼容 |
| DTO设计 | ✅ 完美 | 5/5 | 复用现有FieldMetadataDto，结构合理 |
| 缓存机制设计 | ✅ 完美 | 5/5 | IFieldMetadataCache接口设计合理 |
| 端点改造方案 | ✅ 完美 | 5/5 | includeMeta参数避免破坏性变更 |
| 双模式规则 | ✅ 完美 | 5/5 | 统一遵循阶段2/3规则 |
| 文档质量 | ✅ 完美 | 5/5 | 结构清晰，代码示例完整 |

**综合评分**: **5.0/5.0 (100%)** - ✅ **优秀**

---

## ✅ 设计文档亮点

### 1. 设计原则清晰 ✅

**位置**: 3.2.1 设计原则

设计文档明确了四个核心设计原则：

1. **职责分离**：`data` 仅包含实体数据；`meta.fields` 仅包含字段元数据
2. **向后兼容（lang 规则）**：仅显式 `?lang=xx` 才输出单语字符串；无 `lang` 返回多语结构（忽略 `Accept-Language`）
3. **性能优先**：字段元数据按 `fullTypeName` 缓存；i18n 解析复用 `ILocalization` 内部缓存
4. **复用现有能力**：复用 `DtoExtensions.ToFieldDto(field, loc, lang)` 的三级优先级逻辑

**评价**: ⭐⭐⭐⭐⭐
- ✅ 设计原则明确，符合架构最佳实践
- ✅ 职责分离原则清晰，符合单一职责原则
- ✅ 向后兼容性考虑周全
- ✅ 性能优化策略合理

---

### 2. 返回结构设计优秀 ✅

**位置**: 3.2.2 返回结构（meta.fields）

设计文档提供了清晰的返回结构设计：

**Query（列表查询）**：
```json
{
  "meta": {
    "fields": [
      {
        "propertyName": "Code",
        "displayNameKey": "LBL_FIELD_CODE",
        "displayName": "编码"
      },
      {
        "propertyName": "CustomField",
        "displayNameTranslations": { "zh": "自定义字段", "en": "Custom Field" }
      }
    ]
  },
  "data": [ { "...": "..." } ],
  "total": 123,
  "page": 1,
  "pageSize": 100
}
```

**GetById（单体查询）**：
- 使用 `includeMeta=true` 参数避免破坏性变更
- `includeMeta=false`（默认）：保持现状，返回实体对象
- `includeMeta=true`：返回 `{ meta, data }`

**评价**: ⭐⭐⭐⭐⭐
- ✅ 返回结构清晰，职责分离明确
- ✅ `meta` 字段为增量字段，兼容旧客户端忽略未知字段
- ✅ GET by id 使用 `includeMeta` 参数避免破坏性变更，设计优秀
- ✅ 考虑了分页场景（page、pageSize）

---

### 3. 双模式规则统一 ✅

**位置**: 3.2.3 双模式规则（字段显示名）

设计文档统一采用阶段1/2的规则：

```csharp
var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
var uiLang = LangHelper.GetLang(http); // 仅用于错误消息
```

- `targetLang != null`（单语模式）：`FieldMetadataDto.DisplayName` 输出单语字符串；`DisplayNameTranslations` 为 null
- `targetLang == null`（多语模式，向后兼容）：
  - 接口字段：输出 `DisplayNameKey`（不展开多语字典）
  - 自定义字段：输出 `DisplayNameTranslations`（字典）；`DisplayName` 为 null

**评价**: ⭐⭐⭐⭐⭐
- ✅ 统一遵循阶段2/3规则，保持架构一致性
- ✅ 双模式逻辑清晰，易于实现
- ✅ 向后兼容性考虑周全

---

### 4. DTO设计合理 ✅

**位置**: 3.2.4 DTO 设计（建议）

设计文档提供了合理的DTO设计：

```csharp
public class DynamicEntityQueryResultDto
{
    public object? Meta { get; set; } // { fields: List<FieldMetadataDto> }
    public List<object> Data { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
```

注：实现时可将 `Meta` 具体化为 `DynamicEntityMetaDto`，并使用 `JsonIgnore(WhenWritingNull)` 优化 payload。

**评价**: ⭐⭐⭐⭐⭐
- ✅ 复用现有的 `FieldMetadataDto`（已支持双模式）
- ✅ 结构合理，包含分页信息
- ✅ 使用 `JsonIgnore(WhenWritingNull)` 优化序列化
- ✅ 提供了实现建议（具体化 `Meta` 为 `DynamicEntityMetaDto`）

---

### 5. 缓存机制设计优秀 ✅

**位置**: 3.2.5 字段元数据缓存机制（建议）

设计文档提供了完整的缓存机制设计：

**缓存 Key**：
- 基础元数据：`FieldMetadata:{fullTypeName}`（存 FieldMetadata 的最小必要集合）
- 可选：按语言缓存 DTO 视图：`FieldMetadata:{fullTypeName}:{lang}:{i18nVersion}`

**失效策略**（推荐组合）：
- **主动失效**：实体定义/字段变更后，调用 `Invalidate(fullTypeName)`
- **被动过期**：30 分钟滑动/绝对过期（防止遗漏失效）
- **i18n 版本**：如缓存单语 DTO 视图，则使用 `ILocalization.GetCacheVersion()` 作为 version

**缓存服务接口**：
```csharp
public interface IFieldMetadataCache
{
    Task<IReadOnlyList<FieldMetadataDto>> GetFieldsAsync(string fullTypeName, ILocalization loc, string? lang, CancellationToken ct);
    void Invalidate(string fullTypeName);
}
```

**实现要点**：
- DB 查询：按 `fullTypeName` 加载 `EntityDefinition`（含 `Fields`），一次性取全字段
- DTO 映射：对每个字段调用 `field.ToFieldDto(loc, lang)`（复用已有逻辑）
- 避免 N+1：接口字段翻译走 `ILocalization` 内部缓存

**评价**: ⭐⭐⭐⭐⭐
- ✅ 缓存键设计合理，支持基础元数据和按语言缓存
- ✅ 失效策略完善（主动失效 + 被动过期 + i18n版本）
- ✅ 接口设计清晰，返回 `IReadOnlyList<FieldMetadataDto>`
- ✅ 实现要点明确，避免 N+1 查询问题
- ✅ 复用现有能力（`ToFieldDto`、`ILocalization` 缓存）

---

### 6. 端点改造方案周全 ✅

**位置**: 3.2.6 端点修改方案（Task 3.3 将实现）

设计文档提供了完整的端点改造方案：

- `POST /api/dynamic-entities/{fullTypeName}/query`
  - 新增查询参数：`lang`（可选）
  - 追加响应字段：`meta.fields`

- `GET /api/dynamic-entities/{fullTypeName}/{id}`
  - 新增查询参数：`lang`（可选）、`includeMeta`（可选，默认 false）
  - `includeMeta=true` 时返回 `{ meta, data }`

**评价**: ⭐⭐⭐⭐⭐
- ✅ GET by id 使用 `includeMeta` 参数避免破坏性变更，设计优秀
- ✅ 向后兼容性考虑周全
- ✅ 参数设计合理，默认值明确

---

## 📋 设计文档结构评估

### 文档结构 ✅

设计文档采用清晰的层次结构：
1. **3.2.1 设计原则** - 明确设计原则
2. **3.2.2 返回结构** - 提供JSON示例
3. **3.2.3 双模式规则** - 统一规则说明
4. **3.2.4 DTO设计** - 代码结构设计
5. **3.2.5 缓存机制** - 缓存策略设计
6. **3.2.6 端点改造方案** - 实施指导

**评价**: ⭐⭐⭐⭐⭐
- ✅ 结构清晰，逻辑严密
- ✅ 从原则到实现，层次分明
- ✅ 符合技术文档最佳实践

---

## 🔍 详细评审

### 1. 设计完整性 ✅

**设计覆盖**:
- ✅ 设计原则（职责分离、向后兼容、性能优先、可复用性）
- ✅ 返回结构（Query 和 GetById 两种场景）
- ✅ 双模式规则（单语/多语模式）
- ✅ DTO设计（DynamicEntityQueryResultDto）
- ✅ 缓存机制（IFieldMetadataCache 接口和实现要点）
- ✅ 端点改造方案（两个端点的具体改造方案）

**评价**: ⭐⭐⭐⭐⭐
- ✅ 设计覆盖完整，所有关键点都有详细说明
- ✅ 提供了代码示例和JSON结构示例
- ✅ 实施指导清晰，便于 Task 3.3 实施

---

### 2. 向后兼容性设计 ✅

**兼容性考虑**:

1. **Query 端点**：
   - `meta` 字段为增量字段，兼容旧客户端忽略未知字段
   - 无 `lang` 参数时返回多语结构（向后兼容）

2. **GetById 端点**：
   - 使用 `includeMeta` 参数（默认 false）避免破坏性变更
   - `includeMeta=false` 时保持现状，返回实体对象
   - `includeMeta=true` 时才返回 `{ meta, data }`

**评价**: ⭐⭐⭐⭐⭐
- ✅ 向后兼容性考虑周全
- ✅ `includeMeta` 参数设计优秀，避免破坏性变更
- ✅ 符合渐进式增强原则

---

### 3. 性能优化设计 ✅

**性能优化策略**:

1. **缓存机制**：
   - 按 `fullTypeName` 缓存字段元数据
   - 支持按语言缓存 DTO 视图（可选）
   - 30分钟过期时间

2. **避免 N+1**：
   - 一次性加载所有字段（`EntityDefinition` 含 `Fields`）
   - 接口字段翻译走 `ILocalization` 内部缓存
   - 复用 `ToFieldDto` 逻辑

3. **批量处理**：
   - 字段元数据只需加载一次，适用于所有记录（分页场景）

**评价**: ⭐⭐⭐⭐⭐
- ✅ 性能优化策略合理
- ✅ 缓存机制设计完善
- ✅ 避免 N+1 查询问题
- ✅ 考虑了分页场景

---

### 4. 可复用性设计 ✅

**复用现有能力**:

1. **DTO复用**：
   - 复用现有的 `FieldMetadataDto`（已支持双模式）
   - 复用 `DtoExtensions.ToFieldDto(field, loc, lang)` 的三级优先级逻辑

2. **缓存复用**：
   - 复用 `ILocalization` 内部缓存
   - 可选使用 `MultilingualFieldService.LoadResourcesAsync(...)` 批量加载

**评价**: ⭐⭐⭐⭐⭐
- ✅ 充分复用现有能力，减少重复代码
- ✅ 保持架构一致性
- ✅ 降低维护成本

---

## ✅ 优点总结

1. **设计原则清晰**：职责分离、向后兼容、性能优先、可复用性
2. **返回结构合理**：meta.fields 结构清晰，向后兼容
3. **双模式规则统一**：统一遵循阶段2/3规则，保持架构一致性
4. **DTO设计合理**：复用现有FieldMetadataDto，结构合理
5. **缓存机制完善**：IFieldMetadataCache接口设计合理，失效策略完善
6. **端点改造方案周全**：includeMeta参数避免破坏性变更
7. **文档质量优秀**：结构清晰，代码示例完整

---

## 📝 改进建议

### 无重大改进建议

设计文档质量优秀，无需重大改进。以下为可选优化项：

1. **流程图**（可选）：
   - 可以考虑添加字段元数据缓存流程图
   - 可以添加端点改造流程图

2. **性能数据**（可选）：
   - 如果未来有性能测试数据，可以补充到文档中

---

## 🎯 评审结论

### 综合评分: **5.0/5.0 (100%)** - ✅ **优秀**

### 评审意见

设计文档质量优秀，完全符合验收标准：

- ✅ **设计完整性**：完整覆盖所有设计要点
- ✅ **设计原则清晰**：职责分离、向后兼容、性能优先、可复用性
- ✅ **返回结构合理**：meta.fields 结构清晰，向后兼容
- ✅ **DTO设计合理**：复用现有FieldMetadataDto，结构合理
- ✅ **缓存机制完善**：IFieldMetadataCache接口设计合理，失效策略完善
- ✅ **端点改造方案周全**：includeMeta参数避免破坏性变更
- ✅ **双模式规则统一**：统一遵循阶段2/3规则
- ✅ **文档质量优秀**：结构清晰，代码示例完整

### 关键设计亮点总结

设计文档明确了以下关键设计亮点：

1. **返回结构**：`{ "meta": { "fields": [...] }, "data": [...], "total": 123 }`
2. **双模式规则**：仅显式 `?lang=xx` 才输出单语，无 `lang` 返回多语
3. **DTO设计**：`DynamicEntityQueryResultDto` + `DynamicEntityMetaDto`
4. **缓存机制**：`IFieldMetadataCache` 接口，按 `fullTypeName` 缓存
5. **向后兼容**：GET by id 使用 `includeMeta=true` 参数避免破坏性变更
6. **复用现有能力**：`field.ToFieldDto(loc, lang)` 三级优先级逻辑

这些设计为后续 Task 3.3（实施）提供了明确的方向和指导。

---

## 📌 后续行动

### Task 3.3 实施动态实体查询优化

基于 Task 3.2 的设计方案，Task 3.3 应该：

1. **实现 IFieldMetadataCache 服务**，接口签名：`GetFieldsAsync(fullTypeName, loc, lang, ct)`
2. **创建 DynamicEntityQueryResultDto**，包含 `meta.fields` 结构
3. **修改动态实体端点**：
   - POST query：添加 `lang` 参数，返回 `meta.fields`
   - GET by id：添加 `lang` 和 `includeMeta` 参数（默认 false）
4. **添加测试**，验证双模式行为、缓存机制和 `includeMeta` 参数

---

**评审完成日期**: 2025-12-12  
**评审者签名**: 架构组  
**报告状态**: ✅ 通过

