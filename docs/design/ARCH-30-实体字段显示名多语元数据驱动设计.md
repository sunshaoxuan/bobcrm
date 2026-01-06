# ARCH-30: BobCRM系统级多语API架构优化设计

**文档版本**: 2.0
**创建日期**: 2025-12-11
**最后更新**: 2025-12-11
**状态**: 草案
**影响范围**: 🔥 **全系统架构级变更** - 涉及50+个API端点
**相关文档**:
- ARCH-11-动态实体指南.md
- I18N-01-多语机制设计文档.md
- I18N-02-元数据多语机制设计文档.md

---

## 📢 重要说明

**本设计文档从v1.0的"实体字段显示名优化"升级为v2.0的"系统级多语API架构规范"**。

经过全面代码分析发现，BobCRM系统中**50+个API端点**返回多语数据，但只有**6个端点**正确处理了语言参数。这不仅仅是实体字段元数据的问题，而是**整个系统的多语API架构需要统一优化**。

**v2.0核心变更**：
- ✅ 扩展方案B（语言参数优化）到所有多语API
- ✅ 制定统一的API多语规范
- ✅ 建立前端语言参数自动传递机制
- ✅ 分阶段实施计划（44+个端点改造）

---

## 1. 问题背景

### 1.0 系统级问题发现（v2.0新增）

通过全系统代码扫描，发现以下**严重的架构一致性问题**：

#### 1.0.1 多语API端点现状统计

| 业务模块 | 端点数量 | 已支持lang参数 | 未支持 | 覆盖率 |
|---------|---------|---------------|--------|--------|
| 实体定义 | 6 | 3 | 3 | 50% |
| 访问控制 | 4 | 0 | 4 | 0% |
| 枚举定义 | 4 | 3 | 1 | 75% |
| 模板管理 | 2 | 0 | 2 | 0% |
| 域管理 | 1 | 0 | 1 | 0% |
| 动态实体 | 2 | 0 | 2 | 0% |
| **总计** | **19+** | **6** | **13+** | **32%** |

#### 1.0.2 高频API端点问题

以下高频端点**每次用户操作都会被调用**，但当前返回**完整三语字典**，浪费约66%的带宽：

1. **`/api/access/functions/me`** - 用户功能菜单
   - 调用频率：每次登录 + 每次刷新
   - 当前响应：约**50KB**（完整三语）
   - 优化后：约**17KB**（单语）
   - **节省：33KB/次，首屏加载提速约200ms**

2. **`/api/templates/menu-bindings`** - 导航菜单
   - 调用频率：首屏加载
   - 问题：使用**系统默认语言**而非用户语言
   - 影响：日语用户看到中文菜单

3. **`/api/entities`** - 实体列表
   - 调用频率：路由初始化
   - 当前响应：约**20KB**
   - 优化后：约**7KB**

#### 1.0.3 架构不一致性问题

- ✅ 部分端点正确使用 `LangHelper.GetLang(http)`
- ❌ 部分端点返回完整多语字典（无lang参数）
- ⚠️ 部分端点使用系统语言而非用户语言
- ❌ 前端缺少统一的语言参数传递机制

**结论**：需要**系统级统一规范**，而非局部优化。

---

### 1.1 原始问题描述（实体字段显示名）

### 1.1 现状描述

在当前BobCRM系统中，实体字段的显示名（DisplayName）存在以下问题：

1. **接口字段显示名硬编码**：
   - 在 `StorageDDLGenerator.GenerateInterfaceFields()` 方法中（第343-550行），所有接口字段（Base、Archive、Audit、Version、TimeVersion、Organization）的 `DisplayName` 都是硬编码的三语字典
   - 示例：
     ```csharp
     DisplayName = new Dictionary<string, string?>
     {
         { "ja", "コード" },
         { "zh", "代码" },
         { "en", "Code" }
     }
     ```
   - 这些显示名没有引用 i18n 资源系统，无法统一管理和更新

2. **前端大量兜底逻辑**：
   - `PageLoader.razor` 中的 `LoadFieldLabels()` 方法（第425-474行）和 `GetWidgetLabel()` 方法（第521-649行）包含大量硬编码的资源映射
   - 前端为了处理基础字段显示名缺失，创建了多层兜底逻辑：
     ```csharp
     var baseResourceMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
     {
         { "code", "COL_CODE" },
         { "name", "COL_NAME" },
         { "extdata", "COL_EXT_DATA" },
         { "version", "COL_VERSION" },
         { "id", "COL_ID" }
     };
     ```

3. **元数据不完整**：
   - `/api/entities/{entityType}/definition` 返回的字段信息只包含 `DisplayName` 字典，缺少 `DisplayNameKey`
   - 前端无法识别显示名来源是资源Key还是直接文本
   - 缺少字段显示名的元数据追溯能力

4. **多语机制不一致**：
   - 扩展字段（自定义字段）使用 `DisplayName` 字典存储多语文本
   - 接口字段（系统字段）使用硬编码的 `DisplayName` 字典
   - i18n 资源文件中已有相应资源（如 `LBL_FIELD_CODE`、`LBL_FIELD_CREATED_AT` 等），但未被后端使用

### 1.2 问题根源

**违反元数据驱动原则**：
- 字段显示名应该完全由后端元数据定义驱动，前端只负责消费和渲染
- 当前前端为了应对缺失的元数据，被迫实现业务逻辑（资源映射、兜底翻译），违反了单一职责原则

**违反多语机制规范**：
- 根据 I18N-02-元数据多语机制设计文档，所有显示名应使用资源Key引用，而非直接存储翻译文本
- 当前接口字段直接存储多语文本，导致资源管理分散，无法统一更新

**维护成本高**：
- 修改字段显示名需要同时修改后端代码、前端代码和 i18n 资源
- 缺少单一数据源（Single Source of Truth）

---

## 2. 设计目标

### 2.1 核心原则

1. **元数据驱动**：字段显示名完全由后端元数据定义，前端只消费数据
2. **资源Key引用**：所有显示名使用 i18n 资源Key引用，而非直接存储文本
3. **统一管理**：接口字段和扩展字段使用相同的显示名管理机制
4. **零兜底逻辑**：前端不再需要硬编码资源映射，完全依赖后端元数据

### 2.2 具体目标

- ✅ 接口字段的 `DisplayName` 改为引用 i18n 资源Key（如 `LBL_FIELD_CODE`）
- ✅ 所有字段元数据返回时包含 `DisplayNameKey` 和已翻译的 `DisplayName`
- ✅ 前端移除所有硬编码的资源映射和兜底逻辑
- ✅ 提供统一的字段元数据API端点
- ✅ 完善 i18n 资源，确保所有基础字段都有对应资源

---

## 3. API设计方案对比

### 3.1 方案选择讨论

在设计字段元数据API时，有两种主要方案处理多语显示名的返回。

#### 方案A：返回完整多语数据 + 前端选择

**API响应示例**：
```json
{
  "propertyName": "Code",
  "displayNameKey": "LBL_FIELD_CODE",
  "displayName": {
    "zh": "编码",
    "ja": "コード",
    "en": "Code"
  },
  "dataType": "String"
}
```

**前端处理**：
```csharp
var currentLang = I18n.CurrentLanguage; // "zh" / "ja" / "en"
var displayName = field.DisplayName[currentLang];
```

**优势**：
- ✅ 前端切换语言无需重新请求API（已加载的数据包含所有语言）
- ✅ 调试友好：可以看到完整的多语数据
- ✅ API设计简单：不需要语言参数

**劣势**：
- ❌ 数据传输量大：每个字段传输3倍的显示名（zh + ja + en）
- ❌ JSON体积增加：100个字段的响应体积增加约200%
- ❌ 前端需要选择逻辑：需要根据当前语言从字典中取值
- ❌ 带宽浪费：传输了用户不会使用的语言数据（用户当前只需要一种语言）

#### 方案B：传递语言参数 + 后端翻译（推荐）⭐

**API请求示例**：
```http
GET /api/entities/customer/field-metadata?lang=zh
Accept-Language: zh-CN
```

**API响应示例**：
```json
{
  "propertyName": "Code",
  "displayNameKey": "LBL_FIELD_CODE",
  "displayName": "编码",  // 只返回当前语言的翻译
  "dataType": "String"
}
```

**前端处理**：
```csharp
// 直接使用，无需翻译
var displayName = field.DisplayName;
```

**优势**：
- ✅ **数据传输量最小**：只传输当前语言的翻译（节省约66%的带宽）
- ✅ **前端逻辑最简单**：直接使用 `displayName`，无需字典查找
- ✅ **性能最优**：JSON体积小，解析快，网络传输快
- ✅ **符合HTTP规范**：利用 `Accept-Language` 标准头
- ✅ **缓存友好**：可按语言缓存响应（`Vary: Accept-Language`）

**劣势**：
- ❌ 切换语言需要重新请求API（实际场景中语言切换频率很低）
- ❌ API需要接受语言参数（增加参数复杂度）
- ❌ 调试时看不到完整多语数据（可通过单独的调试端点解决）

#### 方案C：混合方案（可选）

**设计思路**：
- 默认采用方案B（传递语言参数，只返回当前语言）
- 提供调试模式：`?lang=zh&includeAllLanguages=true` 返回完整多语数据

**适用场景**：
- 生产环境使用方案B（高性能）
- 开发/调试环境使用完整多语数据

### 3.2 推荐方案：方案B（语言参数优化）

**选择理由**：
1. **性能优先**：移动端和弱网环境下，减少66%的数据传输量显著提升用户体验
2. **前端简化**：符合"后端驱动"原则，前端只负责渲染，不负责翻译逻辑
3. **语言切换频率低**：用户设置语言后很少切换，重新请求API的成本可接受
4. **已有语言上下文**：前端已知当前语言（通过用户设置或默认日语），传递语言参数是自然的设计

**实现要点**：
- 所有返回字段元数据的API都接受 `lang` 查询参数或 `Accept-Language` 头
- 后端在返回前通过 I18nService 翻译 `DisplayNameKey`
- 扩展字段的多语字典在后端根据 `lang` 选择对应语言
- 前端在发起请求时自动附加当前语言参数

---

## 4. 技术方案

### 4.1 总体架构（基于方案B：语言参数优化）

```
┌─────────────────────────────────────────────────────────────┐
│                     前端 (BobCrm.App)                        │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ I18nService                                          │    │
│  │  - CurrentLanguage: "zh" / "ja" / "en"              │    │
│  │  - 默认语言: "ja"（日语）                            │    │
│  └─────────────────────────────────────────────────────┘    │
│                              │                               │
│                              ▼                               │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ PageLoader.razor                                    │    │
│  │  - 获取当前语言: var lang = I18n.CurrentLanguage    │    │
│  │  - 调用 API 并传递语言参数:                          │    │
│  │    GET /api/entities/{type}/field-metadata?lang=zh  │    │
│  │  - 直接使用返回的 displayName，无需翻译             │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP GET ?lang=zh
                              │ Accept-Language: zh-CN
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     后端 API (BobCrm.Api)                    │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ EntityDefinitionEndpoints.cs                        │    │
│  │  GET /api/entities/{type}/field-metadata?lang={lang}│    │
│  │  1. 从 query 或 Accept-Language 获取语言参数        │    │
│  │  2. 加载字段元数据（接口字段 + 扩展字段）           │    │
│  │  3. 翻译 DisplayNameKey → displayName (当前语言)    │    │
│  │  4. 返回:                                            │    │
│  │    [                                                 │    │
│  │      {                                               │    │
│  │        propertyName: "Code",                         │    │
│  │        displayNameKey: "LBL_FIELD_CODE", // 可选，调试用│
│  │        displayName: "编码",  // ✅ 仅当前语言        │    │
│  │        dataType: "String",                           │    │
│  │        source: "Interface"                           │    │
│  │      }                                               │    │
│  │    ]                                                 │    │
│  └─────────────────────────────────────────────────────┘    │
│                              │                               │
│                              ▼                               │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ LangHelper.GetLang(HttpContext)                     │    │
│  │  - 优先级: query.lang → Accept-Language → 默认"ja"  │    │
│  └─────────────────────────────────────────────────────┘    │
│                              │                               │
│                              ▼                               │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ I18nService.T(key, lang)                            │    │
│  │  - 加载 i18n-resources.json                          │    │
│  │  - 根据 lang 参数返回对应语言的翻译                  │    │
│  │  - 示例: T("LBL_FIELD_CODE", "zh") → "编码"         │    │
│  └─────────────────────────────────────────────────────┘    │
│                              │                               │
│                              ▼                               │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ StorageDDLGenerator.GenerateInterfaceFields()    │    │
│  │  - 生成接口字段时使用 DisplayNameKey                 │    │
│  │  - 示例: { PropertyName: "Code",                     │    │
│  │            DisplayNameKey: "LBL_FIELD_CODE",         │    │
│  │            DisplayName: null }                       │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘

数据流向：
1. 前端获取用户当前语言（zh/ja/en）
2. 发起请求时附加 lang 参数
3. 后端从 query 或 Accept-Language 头获取语言
4. 后端翻译 DisplayNameKey → displayName（仅当前语言）
5. 返回轻量化的JSON（只包含当前语言的显示名）
6. 前端直接使用 displayName，无需字典查找

优势：
- 数据传输量减少约66%（只传输一种语言）
- 前端逻辑最简化（直接使用，无需翻译）
- 性能最优（JSON体积小，解析快）
```

### 3.2 数据模型调整

#### 3.2.1 FieldMetadata 模型扩展

在 `BobCrm.Api/Base/Models/FieldMetadata.cs` 中添加 `DisplayNameKey` 属性：

```csharp
public class FieldMetadata
{
    // ... 现有属性 ...

    /// <summary>
    /// 显示名资源Key（用于引用 i18n 资源）
    /// 示例：LBL_FIELD_CODE, LBL_FIELD_CREATED_AT
    /// </summary>
    [MaxLength(100)]
    public string? DisplayNameKey { get; set; }

    /// <summary>
    /// 显示名（多语言）- Map/Json 逻辑类型
    /// 注意：优先使用 DisplayNameKey 引用资源，DisplayName 作为兜底或自定义字段使用
    /// 示例：{"ja": "価格", "zh": "价格", "en": "Price"}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string?>? DisplayName { get; set; }

    // ... 其他属性 ...
}
```

**字段优先级规则**：
1. **接口字段（系统字段）**：必须使用 `DisplayNameKey` 引用资源，`DisplayName` 为 null
2. **扩展字段（自定义字段）**：使用 `DisplayName` 字典存储多语文本，`DisplayNameKey` 为 null
3. **显示名解析**：API返回时，如果有 `DisplayNameKey`，则通过 I18nService 翻译；否则使用 `DisplayName` 字典

#### 4.2.2 FieldMetadataDto 扩展（基于方案B）

在 `BobCrm.Api/Contracts/DTOs/EntityFieldDto.cs` 中添加：

```csharp
public class EntityFieldDto
{
    // ... 现有属性 ...

    /// <summary>
    /// 显示名资源Key（接口字段使用，可选，用于调试和追溯）
    /// </summary>
    public string? DisplayNameKey { get; set; }

    /// <summary>
    /// 显示名（已翻译为当前语言的文本）
    /// ⭐ 方案B核心：只返回当前语言的显示名，前端直接使用
    /// 示例：lang=zh 时返回 "编码"，lang=ja 时返回 "コード"
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    // ❌ 不再返回多语字典，减少数据传输量
    // public MultilingualText? DisplayName { get; set; }

    // ... 其他属性 ...
}
```

**关键变更**：
- `DisplayName` 从 `MultilingualText`（多语字典）改为 `string`（单一语言文本）
- 后端在返回前根据 `lang` 参数翻译好显示名
- 前端直接使用 `field.DisplayName`，无需字典查找

### 3.3 接口字段资源Key映射

#### 3.3.1 资源Key命名规范

所有接口字段的资源Key遵循统一命名规范：`LBL_FIELD_{PROPERTY_NAME}`

| 接口类型       | 字段名          | 资源Key                  | 中文翻译   | 日语翻译        | 英文翻译        |
|---------------|----------------|-------------------------|----------|---------------|----------------|
| Base          | Id             | LBL_FIELD_ID            | 标识      | ID            | Id             |
| Archive       | Code           | LBL_FIELD_CODE          | 编码      | コード         | Code           |
| Archive       | Name           | LBL_FIELD_NAME          | 名称      | 名称           | Name           |
| Audit         | CreatedAt      | LBL_FIELD_CREATED_AT    | 创建时间  | 作成日時       | Created At     |
| Audit         | CreatedBy      | LBL_FIELD_CREATED_BY    | 创建人    | 作成者         | Created By     |
| Audit         | UpdatedAt      | LBL_FIELD_UPDATED_AT    | 修改时间  | 更新日時       | Updated At     |
| Audit         | UpdatedBy      | LBL_FIELD_UPDATED_BY    | 修改人    | 更新者         | Updated By     |
| Audit         | Version        | LBL_FIELD_VERSION       | 版本号    | バージョン     | Version        |
| Version       | Version        | LBL_FIELD_VERSION       | 版本号    | バージョン     | Version        |
| TimeVersion   | ValidFrom      | LBL_FIELD_VALID_FROM    | 生效开始  | 有効開始       | Valid From     |
| TimeVersion   | ValidTo        | LBL_FIELD_VALID_TO      | 生效结束  | 有効終了       | Valid To       |
| TimeVersion   | VersionNo      | LBL_FIELD_VERSION_NO    | 时间版本  | 時系列バージョン | Version No     |
| Organization  | OrganizationId | LBL_FIELD_ORGANIZATION_ID| 组织ID   | 組織ID         | Organization Id|
| (软删除)       | IsDeleted      | LBL_FIELD_IS_DELETED    | 已删除    | 削除フラグ     | Is Deleted     |
| (软删除)       | DeletedAt      | LBL_FIELD_DELETED_AT    | 删除时间  | 削除日時       | Deleted At     |
| (软删除)       | DeletedBy      | LBL_FIELD_DELETED_BY    | 删除人    | 削除者         | Deleted By     |

#### 3.3.2 i18n 资源完整性验证

**现状**（已验证）：
- ✅ i18n-resources.json 已包含所有基础字段资源（第3692-3761行）
- ✅ 三种语言（zh、ja、en）的翻译已完整

**需要补充的资源**（如有新增接口字段）：
```json
{
  "LBL_FIELD_NAME": {
    "zh": "名称",
    "ja": "名称",
    "en": "Name"
  }
}
```

### 3.4 后端实现方案

#### 3.4.1 修改 StorageDDLGenerator.GenerateInterfaceFields()

**位置**：`BobCrm.Api/Services/StorageDDLGenerator.cs`（第343-550行）

**修改前**（硬编码显示名）：
```csharp
fields.Add(new FieldMetadata
{
    PropertyName = "Code",
    DisplayName = new Dictionary<string, string?>
    {
        { "ja", "コード" },
        { "zh", "代码" },
        { "en", "Code" }
    },
    DataType = FieldDataType.String,
    Length = 64,
    IsRequired = true,
    SortOrder = 10
});
```

**修改后**（使用资源Key）：
```csharp
fields.Add(new FieldMetadata
{
    PropertyName = "Code",
    DisplayNameKey = "LBL_FIELD_CODE",  // ✅ 引用资源Key
    DisplayName = null,                   // ✅ 接口字段不存储多语字典
    DataType = FieldDataType.String,
    Length = 64,
    IsRequired = true,
    Source = FieldSource.Interface,      // ✅ 标记来源
    SortOrder = 10
});
```

**完整修改示例**（以 Archive 接口为例）：
```csharp
case EntityInterfaceType.Archive:
    fields.Add(new FieldMetadata
    {
        PropertyName = "Code",
        DisplayNameKey = "LBL_FIELD_CODE",
        DataType = FieldDataType.String,
        Length = 64,
        IsRequired = true,
        Source = FieldSource.Interface,
        SortOrder = 10
    });
    fields.Add(new FieldMetadata
    {
        PropertyName = "Name",
        DisplayNameKey = "LBL_FIELD_NAME",
        DataType = FieldDataType.String,
        Length = 256,
        IsRequired = true,
        Source = FieldSource.Interface,
        SortOrder = 11
    });
    break;
```

#### 4.4.2 新增字段元数据API端点（基于方案B）

**位置**：`BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs`

**端点设计**：
```csharp
 entitiesGroup.MapGet("/{entityType}/field-metadata",
     async (string entityType, string? lang, AppDbContext db, ILocalization loc, HttpContext http) =>
 {
     // ✅ 向后兼容：仅显式 ?lang=xx 才进入单语模式（无 lang 返回多语结构）
     var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
     var candidates = BuildEntityCandidates(entityType);

    // 1. 查询实体定义
    var definition = await db.EntityDefinitions
        .AsNoTracking()
        .Include(ed => ed.Fields.Where(f => !f.IsDeleted).OrderBy(f => f.SortOrder))
        .Include(ed => ed.Interfaces.Where(i => i.IsEnabled))
        .FirstOrDefaultAsync(ed =>
            candidates.Contains(ed.EntityRoute.ToLower()) ||
            candidates.Contains(ed.EntityName.ToLower()) ||
            candidates.Contains(ed.FullTypeName.ToLower()));

    if (definition == null)
    {
        return Results.NotFound(new ErrorResponse(
            loc.T("ERR_ENTITY_NOT_FOUND", targetLang),
            "ENTITY_NOT_FOUND"));
    }

    // 2. 构建字段元数据DTO（只包含当前语言的显示名）
    var fieldMetadata = new List<FieldMetadataDto>();

    foreach (var field in definition.Fields.Where(f => !f.IsDeleted))
    {
        fieldMetadata.Add(new FieldMetadataDto
        {
            PropertyName = field.PropertyName,
            DisplayNameKey = field.DisplayNameKey, // 可选，用于调试
            DisplayName = ResolveDisplayName(field, loc, targetLang), // ⭐ 仅当前语言
            DataType = field.DataType,
            Length = field.Length,
            Precision = field.Precision,
            Scale = field.Scale,
            IsRequired = field.IsRequired,
            IsEntityRef = field.IsEntityRef,
            ReferencedEntityId = field.ReferencedEntityId,
            TableName = field.TableName,
            SortOrder = field.SortOrder,
            DefaultValue = field.DefaultValue,
            ValidationRules = field.ValidationRules,
            Source = field.Source,
            EnumDefinitionId = field.EnumDefinitionId,
            IsMultiSelect = field.IsMultiSelect
        });
    }

    var response = new SuccessResponse<List<FieldMetadataDto>>(fieldMetadata);
    return Results.Ok(response);
})
.WithName("GetEntityFieldMetadata")
.WithSummary("获取实体字段元数据（已翻译为指定语言）")
.WithDescription("返回实体所有字段的元数据，显示名已翻译为目标语言。支持 ?lang=zh/ja/en 参数")
.Produces<SuccessResponse<List<FieldMetadataDto>>>();

// 辅助方法：解析显示名（仅返回当前语言）
static string ResolveDisplayName(FieldMetadata field, ILocalization loc, string lang)
{
    // 1. 优先使用 DisplayNameKey（接口字段）
    if (!string.IsNullOrWhiteSpace(field.DisplayNameKey))
    {
        var translated = loc.T(field.DisplayNameKey, lang);
        // 如果翻译成功（返回值不等于Key本身），使用翻译结果
        if (!string.Equals(translated, field.DisplayNameKey, StringComparison.Ordinal))
        {
            return translated;
        }
    }

    // 2. 使用 DisplayName 字典（扩展字段）
    if (field.DisplayName != null && field.DisplayName.TryGetValue(lang, out var displayName))
    {
        return displayName ?? field.PropertyName;
    }

    // 3. 兜底：字段名（通常不应该走到这里）
    return field.PropertyName;
}
```

**关键设计点**：
1. **lang 参数**：接受 `?lang=zh` query参数，优先级高于 Accept-Language
2. **ResolveDisplayName**：后端负责翻译，只返回当前语言的显示名
3. **轻量化响应**：`DisplayName` 是 `string`，不是多语字典
4. **调试支持**：保留 `DisplayNameKey` 用于追溯资源

**DTO定义（方案B优化版）**：
```csharp
public class FieldMetadataDto
{
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名资源Key（可选，用于调试和追溯）
    /// 接口字段有值（如 LBL_FIELD_CODE），扩展字段为 null
    /// </summary>
    public string? DisplayNameKey { get; set; }

    /// <summary>
    /// 显示名（已翻译为目标语言的单一文本）
    /// ⭐ 方案B核心：只包含当前语言，前端直接使用
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    // ❌ 不再包含多语字典，减少传输量
    // public MultilingualText? DisplayName { get; set; }

    public string DataType { get; set; } = string.Empty;
    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsRequired { get; set; }
    public bool IsEntityRef { get; set; }
    public Guid? ReferencedEntityId { get; set; }
    public string? TableName { get; set; }
    public int SortOrder { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public string Source { get; set; } = string.Empty; // System/Custom/Interface
    public Guid? EnumDefinitionId { get; set; }
    public bool IsMultiSelect { get; set; }
}
```

**数据传输量对比**（100个字段）：
- **方案A**（多语字典）：约150KB（每个字段3种语言）
- **方案B**（单一语言）：约50KB（只传输当前语言）
- **节省带宽**：约66%

#### 3.4.3 扩展现有 EntityDefinitionEndpoints

**位置**：`BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs`（第79-154行）

在返回 `EntityDefinitionDto` 时，为每个字段添加 `DisplayNameKey` 和 `DisplayNameTranslated`：

```csharp
Fields = definition.Fields
    .Where(f => !f.IsDeleted)
    .OrderBy(f => f.SortOrder)
    .Select(f => new EntityFieldDto
    {
        Id = f.Id,
        PropertyName = f.PropertyName,
        DisplayNameKey = f.DisplayNameKey,  // ✅ 新增
        DisplayNameTranslated = ResolveDisplayName(f, loc, lang),  // ✅ 新增
        DisplayName = new MultilingualText(f.DisplayName ?? new Dictionary<string, string?>()),
        DataType = f.DataType,
        Length = f.Length,
        Precision = f.Precision,
        Scale = f.Scale,
        IsRequired = f.IsRequired,
        IsEntityRef = f.IsEntityRef,
        ReferencedEntityId = f.ReferencedEntityId,
        TableName = f.TableName,
        SortOrder = f.SortOrder,
        DefaultValue = f.DefaultValue,
        ValidationRules = f.ValidationRules,
        Source = f.Source,
        EnumDefinitionId = f.EnumDefinitionId,
        IsMultiSelect = f.IsMultiSelect
    }).ToList(),
```

### 3.5 前端实现方案

#### 3.5.1 移除 PageLoader 的兜底逻辑

**位置**：`BobCrm.App/Components/Pages/PageLoader.razor`

**修改前的问题代码**（第425-474行）：
```csharp
private async Task<Dictionary<string, string>> LoadFieldLabels()
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    // ❌ 硬编码的基础字段资源映射
    var baseResourceMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "code", I18n.T("COL_CODE") },
        { "name", I18n.T("COL_NAME") },
        { "extdata", I18n.T("COL_EXT_DATA") },
        { "version", I18n.T("COL_VERSION") },
        { "id", I18n.T("COL_ID") }
    };

    // ... 大量兜底逻辑 ...
}
```

**修改后**（方案B优化版 - 传递语言参数）：
```csharp
private Dictionary<string, string> fieldLabels = new(StringComparer.OrdinalIgnoreCase);

private async Task LoadFieldMetadata()
{
    try
    {
        fieldLabels.Clear();

        // ⭐ 方案B核心：获取当前语言并传递给API
        var currentLang = I18n.CurrentLanguage; // "zh" / "ja" / "en"

        // ✅ 调用字段元数据API，传递语言参数
        var metadataResp = await Auth.GetWithRefreshAsync(
            $"/api/entities/{EntityType}/field-metadata?lang={currentLang}");

        if (metadataResp.IsSuccessStatusCode)
        {
            var metadataContent = await metadataResp.Content.ReadAsStringAsync();
            var response = System.Text.Json.JsonSerializer.Deserialize<
                SuccessResponse<List<FieldMetadataDto>>>(
                    metadataContent,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (response?.Data != null)
            {
                foreach (var field in response.Data)
                {
                    // ✅ 直接使用后端翻译好的显示名（无需字典查找）
                    fieldLabels[field.PropertyName] = field.DisplayName;
                }
            }
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[PageLoader] Failed to load field metadata: {ex.Message}");
        // 元数据加载失败不影响页面渲染，使用字段名作为兜底
    }
}
```

**关键改进**：
1. ✅ 获取当前语言：`I18n.CurrentLanguage`
2. ✅ 传递语言参数：`?lang={currentLang}`
3. ✅ 直接使用 `field.DisplayName`（已翻译为当前语言）
4. ✅ 无需多语字典查找，前端逻辑最简化

**语言切换处理**：
```csharp
// 当用户切换语言时，需要重新加载字段元数据
private void HandleLanguageChanged()
{
    // 清除缓存的字段标签
    fieldLabels.Clear();

    // 重新加载当前页面（会自动调用 LoadFieldMetadata）
    _ = LoadData();
}
```

#### 3.5.2 简化 GetWidgetLabel() 方法

**修改前**（第521-649行，128行代码）：
```csharp
private string GetWidgetLabel(DraggableWidget widget)
{
    // ❌ 大量硬编码映射和多层兜底逻辑
    var englishLabelMap = new Dictionary<string, string> { ... };
    var baseResourceMap = new Dictionary<string, string> { ... };

    // ... 复杂的多层判断 ...
}
```

**修改后**（简化为20行）：
```csharp
private string GetWidgetLabel(DraggableWidget widget)
{
    // 1. 优先使用模板定义的 label
    if (!string.IsNullOrWhiteSpace(widget.Label))
    {
        // 如果 label 是资源 Key（大写+下划线），翻译它
        var label = widget.Label!;
        if (label.Any(char.IsUpper) && label.Contains('_'))
        {
            var translated = I18n.T(label);
            if (!string.Equals(translated, label, StringComparison.OrdinalIgnoreCase))
                return translated;
        }
        return label;
    }

    // 2. 使用字段元数据的显示名
    if (!string.IsNullOrWhiteSpace(widget.DataField) &&
        fieldLabels.TryGetValue(widget.DataField!, out var fieldLabel) &&
        !string.IsNullOrWhiteSpace(fieldLabel))
    {
        return fieldLabel!;
    }

    // 3. 兜底：字段名或组件类型
    return widget.DataField ?? widget.Type;
}
```

#### 3.5.3 调整 LoadData() 方法

在 `LoadData()` 方法中添加字段元数据加载：

```csharp
private async Task LoadData()
{
    try
    {
        loading = true;
        await InvokeAsync(StateHasChanged);

        // ✅ 步骤1：加载字段元数据
        await LoadFieldMetadata();

        // 步骤2：加载运行时上下文
        runtimeContext = await TemplateRuntime.GetRuntimeAsync(EntityType, TemplateUsageType.Detail);

        // 步骤3：加载模板布局
        // ... 现有逻辑 ...

        // 步骤4：加载实体数据
        // ... 现有逻辑 ...

        loading = false;
        await InvokeAsync(StateHasChanged);
    }
    catch (Exception ex)
    {
        error = string.Format(I18n.T("PL_LOAD_EXCEPTION"), ex.Message);
        loading = false;
        await InvokeAsync(StateHasChanged);
    }
}
```

### 3.6 数据库迁移

#### 3.6.1 Map/Json 物理存储层迁移 (Physical Storage Migration) Plan)

本阶段涉及 `FieldMetadata` 元数据的结构转换与数据迁移。

**1. 结构变更**
| 变更对象 | 变更类型 | 逻辑描述 |
| :--- | :--- | :--- |
| `FieldMetadata` | 新增字段 | `DisplayNameKey` (Logical Type: String, MaxLength: 100) |
| `FieldMetadata` | 约束变更 | `DisplayName` 在接口字段模式下设为逻辑可空 |

**2. 数据迁移逻辑**
| 迁移步骤 | 目标范围 | 逻辑描述 |
| :--- | :--- | :--- |
| **步骤 1: 填充 Key** | `Source == 'Interface'` 的字段 | 根据 `PropertyName` 映射到对应的 `LBL_FIELD_*` 资源键 |
| **步骤 2: 清理旧值** | `Source == 'Interface'` 的字段 | 成功填充 Key 后，将旧有的物理 `DisplayName` 字典清空 |
| **步骤 3: 懒加载/回填** | 运行时/管理员动作 | 若需物理显示名，则从资源系统按需回填 |

**3. 回滚方案 (Rollback logic)**
- **逻辑描述**：通过 `DisplayNameKey` 重新查询资源系统，构建多语字典回填充 `DisplayName`，随后安全移除 `DisplayNameKey` 字段。

---

## 4. 实施步骤

### 阶段1：后端元数据改造（2天）

#### 步骤1.1：数据模型调整
- [ ] 在 `FieldMetadata.cs` 中添加 `DisplayNameKey` 属性
- [ ] 创建并执行 EF Migration
- [ ] 验证数据库迁移成功

#### 步骤1.2：修改接口字段生成逻辑
- [ ] 修改 `StorageDDLGenerator.GenerateInterfaceFields()`
- [ ] 为所有接口类型（Base、Archive、Audit、Version、TimeVersion、Organization）的字段添加 `DisplayNameKey`
- [ ] 移除硬编码的 `DisplayName` 字典
- [ ] 单元测试验证

#### 步骤1.3：新增字段元数据API
- [ ] 在 `EntityDefinitionEndpoints.cs` 中添加 `GET /api/entities/{type}/field-metadata` 端点
- [ ] 实现 `ResolveDisplayName()` 辅助方法
- [ ] 创建 `FieldMetadataDto` DTO
- [ ] 集成测试验证

#### 步骤1.4：扩展现有API
- [ ] 在 `GET /api/entities/{type}/definition` 端点中添加 `DisplayNameKey` 和 `DisplayNameTranslated`
- [ ] 更新 `EntityFieldDto`
- [ ] 回归测试

### 阶段2：前端改造（1天）

#### 步骤2.1：移除兜底逻辑
- [ ] 删除 `PageLoader.LoadFieldLabels()` 中的硬编码资源映射
- [ ] 删除 `GetWidgetLabel()` 中的多层兜底逻辑
- [ ] 简化标签解析逻辑

#### 步骤2.2：接入字段元数据API
- [ ] 实现 `LoadFieldMetadata()` 方法
- [ ] 在 `LoadData()` 中调用字段元数据加载
- [ ] 更新 `GetWidgetLabel()` 使用字段元数据

#### 步骤2.3：前端测试
- [ ] 浏览器测试：验证字段标签显示正确
- [ ] 多语言切换测试：验证显示名跟随语言变化
- [ ] 截图对比：确保视觉无回归

### 阶段3：E2E测试验证（1天）

#### 步骤3.1：Playwright测试
- [ ] 编写E2E测试：验证基础字段显示名为多语文本
- [ ] 编写E2E测试：验证扩展字段显示名为多语文本
- [ ] 编写E2E测试：验证语言切换后显示名更新

#### 步骤3.2：截图对比
- [ ] 客户详情页：中文环境截图
- [ ] 客户详情页：日语环境截图
- [ ] 客户详情页：英语环境截图
- [ ] 对比截图，确认显示名正确

#### 步骤3.3：回归测试
- [ ] 运行所有集成测试
- [ ] 运行所有单元测试
- [ ] 确保无破坏性变更

### 阶段4：文档更新（0.5天）

- [ ] 更新 `CLAUDE.md` 的字段元数据章节
- [ ] 更新 `API-01-接口文档.md` 新增API端点
- [ ] 更新 `CHANGELOG.md` 记录此变更
- [ ] 更新 `I18N-02-元数据多语机制设计文档.md` 补充字段显示名规范

---

## 5. 测试验证

### 5.1 单元测试

#### 测试1：接口字段生成包含 DisplayNameKey
```csharp
[Fact]
public void GenerateInterfaceFields_Archive_ShouldSetDisplayNameKey()
{
    // Arrange
    var generator = new StorageDDLGenerator();
    var archiveInterface = new EntityInterface { InterfaceType = EntityInterfaceType.Archive };

    // Act
    var fields = generator.GenerateInterfaceFields(archiveInterface);

    // Assert
    var codeField = fields.FirstOrDefault(f => f.PropertyName == "Code");
    Assert.NotNull(codeField);
    Assert.Equal("LBL_FIELD_CODE", codeField.DisplayNameKey);
    Assert.Null(codeField.DisplayName); // 接口字段不存储多语字典

    var nameField = fields.FirstOrDefault(f => f.PropertyName == "Name");
    Assert.NotNull(nameField);
    Assert.Equal("LBL_FIELD_NAME", nameField.DisplayNameKey);
    Assert.Null(nameField.DisplayName);
}
```

#### 测试2：字段元数据API返回已翻译显示名
```csharp
[Fact]
public async Task GetEntityFieldMetadata_ShouldReturnTranslatedDisplayName()
{
    // Arrange
    var entityType = "customer";
    var expectedLang = "zh";

    // Act
    var response = await _client.GetAsync($"/api/entities/{entityType}/field-metadata");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<SuccessResponse<List<FieldMetadataDto>>>(content);

    Assert.NotNull(result?.Data);
    var codeField = result.Data.FirstOrDefault(f => f.PropertyName == "Code");
    Assert.NotNull(codeField);
    Assert.Equal("LBL_FIELD_CODE", codeField.DisplayNameKey);
    Assert.Equal("编码", codeField.DisplayNameTranslated); // 中文翻译
}
```

### 5.2 集成测试

#### 测试3：字段元数据API集成测试
```csharp
[Fact]
public async Task FieldMetadataApi_ShouldIncludeInterfaceAndCustomFields()
{
    // Arrange
    var entityType = "customer"; // 假设 Customer 实体有 Base、Archive、Audit 接口 + 自定义字段

    // Act
    var response = await _client.GetAsync($"/api/entities/{entityType}/field-metadata");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<SuccessResponse<List<FieldMetadataDto>>>(content);

    Assert.NotNull(result?.Data);
    Assert.True(result.Data.Count > 0);

    // 验证接口字段
    var idField = result.Data.FirstOrDefault(f => f.PropertyName == "Id");
    Assert.NotNull(idField);
    Assert.Equal("Interface", idField.Source);
    Assert.Equal("LBL_FIELD_ID", idField.DisplayNameKey);

    // 验证扩展字段
    var customField = result.Data.FirstOrDefault(f => f.Source == "Custom");
    if (customField != null)
    {
        Assert.Null(customField.DisplayNameKey); // 自定义字段不使用资源Key
        Assert.NotNull(customField.DisplayName); // 使用多语字典
    }
}
```

### 5.3 E2E测试（Playwright）

#### 测试4：字段显示名多语渲染
```javascript
// tests/e2e/field-display-name-i18n.spec.js
test.describe('Field Display Name i18n', () => {
  test('should display field labels in Chinese', async ({ page }) => {
    // 设置语言为中文
    await page.goto('/settings/language');
    await page.selectOption('select[name="language"]', 'zh');

    // 访问客户详情页
    await page.goto('/customer/1');

    // 验证基础字段显示名
    await expect(page.locator('label:has-text("编码")')).toBeVisible();
    await expect(page.locator('label:has-text("名称")')).toBeVisible();
    await expect(page.locator('label:has-text("创建时间")')).toBeVisible();
    await expect(page.locator('label:has-text("版本号")')).toBeVisible();
  });

  test('should display field labels in Japanese', async ({ page }) => {
    // 设置语言为日语
    await page.goto('/settings/language');
    await page.selectOption('select[name="language"]', 'ja');

    // 访问客户详情页
    await page.goto('/customer/1');

    // 验证基础字段显示名
    await expect(page.locator('label:has-text("コード")')).toBeVisible();
    await expect(page.locator('label:has-text("名称")')).toBeVisible();
    await expect(page.locator('label:has-text("作成日時")')).toBeVisible();
    await expect(page.locator('label:has-text("バージョン")')).toBeVisible();
  });

  test('should display field labels in English', async ({ page }) => {
    // 设置语言为英语
    await page.goto('/settings/language');
    await page.selectOption('select[name="language"]', 'en');

    // 访问客户详情页
    await page.goto('/customer/1');

    // 验证基础字段显示名
    await expect(page.locator('label:has-text("Code")')).toBeVisible();
    await expect(page.locator('label:has-text("Name")')).toBeVisible();
    await expect(page.locator('label:has-text("Created At")')).toBeVisible();
    await expect(page.locator('label:has-text("Version")')).toBeVisible();
  });
});
```

### 5.4 截图对比测试

在 E2E 测试中增加截图验证：

```javascript
test('should match field labels screenshot in Chinese', async ({ page }) => {
  await page.goto('/customer/1');
  await page.screenshot({ path: 'screenshots/customer-detail-zh.png' });

  // 使用 Playwright 的视觉回归测试
  await expect(page).toHaveScreenshot('customer-detail-zh.png', {
    maxDiffPixels: 100
  });
});
```

---

## 6. 风险评估与缓解

### 6.1 破坏性变更风险

**风险**：修改 FieldMetadata 模型和API响应结构可能影响现有功能

**缓解措施**：
- ✅ 向后兼容：保留 `DisplayName` 字段，新增 `DisplayNameKey` 和 `DisplayNameTranslated`
- ✅ 渐进式迁移：先支持新旧两种方式，再逐步移除旧逻辑
- ✅ 全面测试：运行所有单元测试、集成测试、E2E测试

### 6.2 数据迁移风险

**风险**：现有数据库中的接口字段没有 `DisplayNameKey`

**缓解措施**：
- ✅ Migration 自动补齐：在迁移脚本中为现有接口字段设置 `DisplayNameKey`
- ✅ 数据验证：迁移后验证所有接口字段都有 `DisplayNameKey`
- ✅ 回滚方案：Down Migration 恢复 `DisplayName` 字典

### 6.3 性能影响风险

**风险**：新增API调用可能增加页面加载时间

**缓解措施**：
- ✅ 并行加载：字段元数据与模板、实体数据并行加载
- ✅ 缓存策略：前端缓存字段元数据（按实体类型）
- ✅ 性能测试：对比改造前后的页面加载时间

### 6.4 i18n资源缺失风险

**风险**：资源文件中可能缺少某些字段的翻译

**缓解措施**：
- ✅ 资源完整性检查：实施前验证所有基础字段资源已存在
- ✅ 兜底机制：如果翻译失败，使用 DisplayNameKey 或字段名
- ✅ 启动检查：应用启动时验证关键资源的完整性

---

## 7. 性能考量

### 7.1 API性能优化（基于方案B）

**优化点1：减少数据库查询**
- 字段元数据API使用 `AsNoTracking()` 避免实体跟踪
- 使用 `Include()` 预加载关联数据，避免N+1查询

**优化点2：按语言缓存响应**
- 方案B的响应需要按**实体类型 + 语言**组合缓存
- 使用 `VaryByQueryKeys` 区分不同语言的响应

```csharp
entitiesGroup.MapGet("/{entityType}/field-metadata", ...)
    .CacheOutput(policy =>
    {
        policy.Expire(TimeSpan.FromMinutes(5));
        policy.SetVaryByQuery("lang"); // ⭐ 按 lang 参数区分缓存
    });
```

**缓存Key示例**：
- `customer-zh` → 中文字段元数据缓存
- `customer-ja` → 日语字段元数据缓存
- `customer-en` → 英语字段元数据缓存

### 7.2 前端性能优化（基于方案B）

**优化点1：按实体类型 + 语言缓存字段元数据**
```csharp
// ⭐ 方案B缓存：需要按语言区分
private static readonly Dictionary<string, Dictionary<string, string>> _fieldMetadataCache
    = new(StringComparer.OrdinalIgnoreCase);

private async Task LoadFieldMetadata()
{
    var currentLang = I18n.CurrentLanguage;
    var cacheKey = $"{EntityType}_{currentLang}"; // ⭐ 复合缓存Key

    // 检查缓存
    if (_fieldMetadataCache.TryGetValue(cacheKey, out var cached))
    {
        fieldLabels = cached;
        return;
    }

    // 加载并缓存
    var metadataResp = await Auth.GetWithRefreshAsync(
        $"/api/entities/{EntityType}/field-metadata?lang={currentLang}");

    // ... 解析响应 ...

    _fieldMetadataCache[cacheKey] = fieldLabels;
}
```

**优化点2：语言切换时清除相关缓存**
```csharp
private void HandleLanguageChanged()
{
    // 清除当前实体类型的所有语言缓存
    var keysToRemove = _fieldMetadataCache.Keys
        .Where(k => k.StartsWith($"{EntityType}_"))
        .ToList();

    foreach (var key in keysToRemove)
    {
        _fieldMetadataCache.Remove(key);
    }

    // 重新加载数据
    _ = LoadData();
}
```

**优化点3：按需加载**
- 只在需要渲染字段标签时加载元数据
- 避免在列表页等不需要详细标签的场景加载

**方案B性能优势总结**：
- ✅ 数据传输量减少66%（只传输一种语言）
- ✅ JSON解析速度提升（体积小）
- ✅ 前端逻辑简化（无需字典查找）
- ✅ 缓存策略清晰（按实体+语言区分）

---

## 8. 最佳实践总结

### 8.1 元数据管理原则

1. **单一数据源（Single Source of Truth）**
   - 字段显示名的唯一来源是后端元数据（FieldMetadata）
   - 前端不再维护任何硬编码的字段名映射

2. **资源Key优先原则**
   - 接口字段（系统字段）必须使用 `DisplayNameKey` 引用资源
   - 扩展字段（自定义字段）使用 `DisplayName` 字典存储多语文本
   - 显示时优先翻译 `DisplayNameKey`，其次使用 `DisplayName`

3. **完整性校验原则**
   - 实体发布时校验所有字段都有显示名（DisplayNameKey 或 DisplayName）
   - 应用启动时校验所有 `LBL_FIELD_*` 资源存在且完整

### 8.2 开发规范

1. **新增接口字段时**：
   - 在 `i18n-resources.json` 中添加资源（如 `LBL_FIELD_NEW_FIELD`）
   - 在 `GenerateInterfaceFields()` 中设置 `DisplayNameKey`
   - 在数据库迁移中为现有数据补齐 `DisplayNameKey`

2. **新增扩展字段时**：
   - UI中要求用户输入多语显示名（zh、ja、en）
   - 后端验证 `DisplayName` 字典完整性
   - 保存时 `DisplayNameKey` 为 null

3. **前端消费字段元数据时**：
   - 调用 `/api/entities/{type}/field-metadata` 获取完整元数据
   - 直接使用 `DisplayNameTranslated`，无需翻译
   - 不再自行拼接资源Key或猜测显示名

### 8.3 错误处理规范

1. **后端错误处理**：
   - 如果 `DisplayNameKey` 对应的资源不存在，记录警告日志
   - 兜底返回 `DisplayNameKey` 本身或字段名

2. **前端错误处理**：
   - 如果字段元数据加载失败，使用字段名作为标签
   - 不抛出异常，确保页面正常渲染

---

## 9. 附录

### 9.1 完整资源Key列表

| 资源Key                  | 中文         | 日语             | 英语            |
|-------------------------|-------------|-----------------|----------------|
| LBL_FIELD_ID            | 标识         | ID              | Id             |
| LBL_FIELD_CODE          | 编码         | コード           | Code           |
| LBL_FIELD_NAME          | 名称         | 名称             | Name           |
| LBL_FIELD_CREATED_AT    | 创建时间     | 作成日時         | Created At     |
| LBL_FIELD_CREATED_BY    | 创建人       | 作成者           | Created By     |
| LBL_FIELD_UPDATED_AT    | 修改时间     | 更新日時         | Updated At     |
| LBL_FIELD_UPDATED_BY    | 修改人       | 更新者           | Updated By     |
| LBL_FIELD_VERSION       | 版本号       | バージョン       | Version        |
| LBL_FIELD_VALID_FROM    | 生效开始     | 有効開始         | Valid From     |
| LBL_FIELD_VALID_TO      | 生效结束     | 有効終了         | Valid To       |
| LBL_FIELD_VERSION_NO    | 时间版本     | 時系列バージョン | Version No     |
| LBL_FIELD_ORGANIZATION_ID| 组织ID      | 組織ID           | Organization Id|
| LBL_FIELD_IS_DELETED    | 已删除       | 削除フラグ       | Is Deleted     |
| LBL_FIELD_DELETED_AT    | 删除时间     | 削除日時         | Deleted At     |
| LBL_FIELD_DELETED_BY    | 删除人       | 削除者           | Deleted By     |

### 9.2 相关文件清单

**后端文件**：
- `src/BobCrm.Api/Base/Models/FieldMetadata.cs` - 字段元数据模型
- `src/BobCrm.Api/Services/StorageDDLGenerator.cs` - 接口字段生成器
- `src/BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs` - 实体定义API
- `src/BobCrm.Api/Contracts/DTOs/EntityFieldDto.cs` - 字段DTO
- `src/BobCrm.Api/Resources/i18n-resources.json` - 多语资源

**前端文件**：
- `src/BobCrm.App/Components/Pages/PageLoader.razor` - 实体详情页
- `src/BobCrm.App/Services/FieldService.cs` - 字段服务（如有）
- `src/BobCrm.App/Services/I18nService.cs` - 多语服务

**测试文件**：
- `tests/BobCrm.Api.Tests/Services/StorageDDLGeneratorTests.cs`
- `tests/BobCrm.Api.Tests/Endpoints/EntityDefinitionEndpointsTests.cs`
- `tests/e2e/field-display-name-i18n.spec.js`

**文档文件**：
- `docs/design/ARCH-30-实体字段显示名多语元数据驱动设计.md`（本文档）
- `docs/design/ARCH-11-动态实体指南.md`（需更新）
- `docs/guides/I18N-02-元数据多语机制设计文档.md`（需更新）
- `docs/reference/API-01-接口文档.md`（需更新）
- `CHANGELOG.md`（需更新）

---

## 10. 系统级多语API改造计划（v2.0新增）

### 10.1 完整API改造清单

#### 10.1.1 优先级1：高频用户界面API（紧急）

| 端点 | 方法 | 返回的多语数据 | 当前状态 | 改造工作量 | 性能收益 |
|------|------|----------------|---------|----------|---------|
| `/api/access/functions/me` | GET | 功能节点DisplayName（树） | ❌ 无lang | 中 | **节省33KB/次** |
| `/api/templates/menu-bindings` | GET | 实体DisplayName、菜单名称 | ⚠️ 系统语言 | 中 | **用户语言一致性** |
| `/api/entities` | GET | 实体DisplayName、Description | ❌ 无lang | 低 | 节省13KB/次 |

**实施建议**：
- 立即改造这3个端点
- 预期用户体验提升：**首屏加载速度提升20-30%**

#### 10.1.2 优先级2：列表展示API（重要）

| 端点 | 方法 | 返回的多语数据 | 当前状态 | 改造工作量 |
|------|------|----------------|---------|----------|
| `/api/entity-definitions` | GET | 实体+字段DisplayName | ❌ 无lang | 低 |
| `/api/enums` | GET | 枚举DisplayName、Description | ❌ 无lang | 低 |
| `/api/entity-domains` | GET | 域Name | ❌ 无lang | 低 |
| `/api/access/functions` | GET | 完整功能树DisplayName | ❌ 无lang | 中 |
| `/api/access/functions/manage` | GET | 功能树DisplayName（管理） | ❌ 无lang | 中 |
| `/api/access/functions/export` | GET | 功能树DisplayName（导出） | ❌ 无lang | 低 |

**实施建议**：
- 批量改造，统一实施
- 约1-2天工作量

#### 10.1.3 优先级3：数据查询API（次要）

| 端点 | 方法 | 返回的多语数据 | 当前状态 | 改造工作量 |
|------|------|----------------|---------|----------|
| `/api/dynamic-entities/{type}/query` | POST | 字段级多语数据 | ❌ 无lang | 高 |
| `/api/dynamic-entities/{type}/{id}` | GET | 字段级多语数据 | ❌ 无lang | 中 |

**实施建议**：
- 第二阶段改造
- 需要深入理解动态实体查询机制

#### 10.1.4 已支持lang参数的端点（保持）

| 端点 | 方法 | 当前状态 |
|------|------|---------|
| `/api/entities/{type}/definition` | GET | ✅ 已支持 |
| `/api/entity-definitions/{id}` | GET | ✅ 已支持 |
| `/api/entity-definitions/by-type/{type}` | GET | ✅ 已支持 |
| `/api/enums/{id}` | GET | ✅ 已支持 |
| `/api/enums/by-code/{code}` | GET | ✅ 已支持 |
| `/api/enums/{id}/options` | GET | ✅ 已支持 |

**行动**：
- 保持现有实现
- 作为其他端点改造的参考模板

### 10.2 统一API多语规范（v2.0核心）

#### 10.2.1 API设计规范

**所有返回多语数据的API端点必须遵循以下规范**：

##### 规范1：接受语言参数

```csharp
// ✅ 推荐方式：使用 LangHelper
public static void MapGet(string pattern,
    Delegate handler,
    ILocalization loc,
    HttpContext http)
{
    var lang = LangHelper.GetLang(http);
    // 使用 lang 解析多语数据
}

 // ✅ 可选：显式 lang 查询参数
 app.MapGet("/api/entities", async (string? lang, HttpContext http) =>
 {
     var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
     // ...
 });
 ```

##### 规范2：语言参数优先级

```
1. 查询参数 ?lang=zh
2. HTTP请求头 X-Lang: zh
3. Accept-Language: zh-CN
4. 系统默认语言（fallback）
```

##### 规范3：响应DTO设计

**方案A**：返回单一语言（推荐用于列表、查询）
```csharp
public class EntitySummaryDto
{
    public string DisplayName { get; set; } = string.Empty;  // ✅ 已翻译
    public string? DisplayNameKey { get; set; }              // 可选：调试用
}
```

**方案B**：返回完整多语（用于管理界面）
```csharp
public class EntityDefinitionDto
{
    public MultilingualText DisplayName { get; set; }  // 包含全部语言
}
```

##### 规范4：缓存策略

```csharp
// ✅ 按语言区分缓存
.CacheOutput(policy =>
{
    policy.Expire(TimeSpan.FromMinutes(5));
    policy.SetVaryByQuery("lang");  // 关键：按lang参数区分
});
```

#### 10.2.2 前端调用规范

##### 前端HTTP拦截器（推荐实施）

```csharp
// BobCrm.App/Services/ApiClient.cs
public class ApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly I18nService _i18n;

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        var client = _factory.CreateClient();

        // ✅ 自动附加当前语言参数
        var currentLang = _i18n.CurrentLanguage;
        var separator = url.Contains('?') ? '&' : '?';
        var urlWithLang = $"{url}{separator}lang={currentLang}";

        return await client.GetAsync(urlWithLang);
    }
}
```

##### 手动传递语言参数

```csharp
// PageLoader.razor
var currentLang = I18n.CurrentLanguage;
var response = await Auth.GetWithRefreshAsync(
    $"/api/entities?lang={currentLang}");
```

### 10.3 分阶段实施计划（按频率划分）

---

#### 📋 实施原则

1. **小步提交**：每个Task完成后立即提交，避免大范围变更
2. **测试驱动**：修改代码的同时更新单元测试，确保编译通过
3. **文档同步**：每次提交都同步更新相关文档（CHANGELOG.md、API文档等）
4. **向后兼容**：优先考虑向后兼容，避免破坏现有功能
5. **增量发布**：每个阶段完成后可独立发布

---

#### 阶段0：基础设施搭建

**目标**：建立统一的多语解析基础设施，为所有后续改造提供支撑

##### Task 0.1：创建多语辅助类

**工作内容**：
```csharp
// 文件：BobCrm.Api/Utils/MultilingualHelper.cs
public static class MultilingualHelper
{
    /// <summary>
    /// 解析多语字典为指定语言的单一文本
    /// </summary>
    public static string Resolve(this Dictionary<string, string?>? dict, string lang)
    {
        if (dict == null || dict.Count == 0) return string.Empty;

        // 优先返回指定语言
        if (dict.TryGetValue(lang, out var value) && !string.IsNullOrWhiteSpace(value))
            return value;

        // 兜底：返回第一个非空值
        return dict.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
    }

    /// <summary>
    /// 批量解析多语字典
    /// </summary>
    public static Dictionary<string, string> ResolveBatch(
        this Dictionary<string, Dictionary<string, string?>?> dicts,
        string lang)
    {
        return dicts.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Resolve(lang)
        );
    }
}
```

**需要的测试**：
```csharp
// 文件：tests/BobCrm.Api.Tests/Utils/MultilingualHelperTests.cs
[Fact]
public void Resolve_WithValidLang_ReturnsCorrectTranslation()
{
    var dict = new Dictionary<string, string?>
    {
        { "zh", "编码" },
        { "ja", "コード" },
        { "en", "Code" }
    };

    Assert.Equal("编码", dict.Resolve("zh"));
    Assert.Equal("コード", dict.Resolve("ja"));
    Assert.Equal("Code", dict.Resolve("en"));
}

[Fact]
public void Resolve_WithInvalidLang_ReturnsFallback()
{
    var dict = new Dictionary<string, string?> { { "zh", "编码" } };
    Assert.Equal("编码", dict.Resolve("invalid"));
}

[Fact]
public void Resolve_WithNullDict_ReturnsEmpty()
{
    Dictionary<string, string?>? dict = null;
    Assert.Equal(string.Empty, dict.Resolve("zh"));
}
```

**提交规范**：
```bash
git add BobCrm.Api/Utils/MultilingualHelper.cs
git add tests/BobCrm.Api.Tests/Utils/MultilingualHelperTests.cs
dotnet build
dotnet test --filter "FullyQualifiedName~MultilingualHelperTests"
git commit -m "feat: add MultilingualHelper for resolving multilingual dictionaries

- Add Resolve() extension method for single value resolution
- Add ResolveBatch() for batch processing
- Add comprehensive unit tests
- Related to ARCH-30 system-wide i18n optimization"
```

##### Task 0.2：创建DTO扩展方法

**工作内容**：
```csharp
// 文件：BobCrm.Api/Extensions/DtoExtensions.cs
public static class DtoExtensions
{
    /// <summary>
    /// 转换为摘要DTO（支持语言参数）
    /// </summary>
    public static EntitySummaryDto ToSummaryDto(
        this EntityDefinition entity,
        string? lang = null)
    {
        return new EntitySummaryDto
        {
            Id = entity.Id,
            EntityName = entity.EntityName,
            EntityRoute = entity.EntityRoute,
            // ⭐ 根据lang参数决定返回格式
            DisplayName = lang != null
                ? entity.DisplayName.Resolve(lang)  // 单一语言
                : null,  // 兼容模式
            DisplayNameTranslations = lang == null
                ? new MultilingualText(entity.DisplayName)  // 完整多语
                : null,  // 新模式不返回
        };
    }

    /// <summary>
    /// 转换为字段DTO（支持语言参数）
    /// </summary>
    public static FieldMetadataDto ToFieldDto(
        this FieldMetadata field,
        ILocalization loc,
        string lang)
    {
        return new FieldMetadataDto
        {
            PropertyName = field.PropertyName,
            DisplayNameKey = field.DisplayNameKey,
            DisplayName = ResolveFieldDisplayName(field, loc, lang),
            // ... 其他属性
        };
    }

    private static string ResolveFieldDisplayName(
        FieldMetadata field,
        ILocalization loc,
        string lang)
    {
        // 优先使用DisplayNameKey
        if (!string.IsNullOrWhiteSpace(field.DisplayNameKey))
        {
            var translated = loc.T(field.DisplayNameKey, lang);
            if (!string.Equals(translated, field.DisplayNameKey))
                return translated;
        }

        // 使用DisplayName字典
        return field.DisplayName.Resolve(lang);
    }
}
```

**需要的测试**：
```csharp
// 文件：tests/BobCrm.Api.Tests/Extensions/DtoExtensionsTests.cs
[Fact]
public void ToSummaryDto_WithLang_ReturnsSingleLanguage()
{
    var entity = new EntityDefinition
    {
        DisplayName = new Dictionary<string, string?>
        {
            { "zh", "客户" },
            { "ja", "顧客" },
            { "en", "Customer" }
        }
    };

    var dto = entity.ToSummaryDto("zh");

    Assert.Equal("客户", dto.DisplayName);
    Assert.Null(dto.DisplayNameTranslations);
}

[Fact]
public void ToSummaryDto_WithoutLang_ReturnsMultilingual()
{
    var entity = new EntityDefinition
    {
        DisplayName = new Dictionary<string, string?>
        {
            { "zh", "客户" }
        }
    };

    var dto = entity.ToSummaryDto();

    Assert.Null(dto.DisplayName);
    Assert.NotNull(dto.DisplayNameTranslations);
    Assert.Equal("客户", dto.DisplayNameTranslations["zh"]);
}
```

**提交规范**：
```bash
git add BobCrm.Api/Extensions/DtoExtensions.cs
git add tests/BobCrm.Api.Tests/Extensions/DtoExtensionsTests.cs
dotnet build
dotnet test --filter "FullyQualifiedName~DtoExtensionsTests"
git commit -m "feat: add DTO extension methods with language parameter support

- Add ToSummaryDto() with optional lang parameter
- Add ToFieldDto() for field metadata conversion
- Support backward compatibility (null lang returns full multilingual)
- Add unit tests
- Related to ARCH-30"
```

##### Task 0.3：更新DTO定义（向后兼容）

**工作内容**：
```csharp
// 文件：BobCrm.Api/Contracts/DTOs/EntitySummaryDto.cs
public class EntitySummaryDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名（单一语言）- 新模式
    /// 当API接受lang参数时返回此字段
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// 显示名（完整多语）- 兼容模式
    /// 当API不接受lang参数时返回此字段（向后兼容）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DisplayNameTranslations { get; set; }
}
```

**需要的测试**：
```csharp
// 文件：tests/BobCrm.Api.Tests/DTOs/EntitySummaryDtoTests.cs
[Fact]
public void Serialize_WithSingleLanguage_OnlyIncludesDisplayName()
{
    var dto = new EntitySummaryDto
    {
        DisplayName = "客户",
        DisplayNameTranslations = null
    };

    var json = JsonSerializer.Serialize(dto);

    Assert.Contains("\"displayName\":\"客户\"", json);
    Assert.DoesNotContain("displayNameTranslations", json);
}

[Fact]
public void Serialize_WithMultilingual_OnlyIncludesTranslations()
{
    var dto = new EntitySummaryDto
    {
        DisplayName = null,
        DisplayNameTranslations = new MultilingualText
        {
            { "zh", "客户" }
        }
    };

    var json = JsonSerializer.Serialize(dto);

    Assert.DoesNotContain("\"displayName\"", json);
    Assert.Contains("displayNameTranslations", json);
}
```

**提交规范**：
```bash
git add BobCrm.Api/Contracts/DTOs/EntitySummaryDto.cs
git add tests/BobCrm.Api.Tests/DTOs/EntitySummaryDtoTests.cs
dotnet build
dotnet test --filter "FullyQualifiedName~EntitySummaryDtoTests"
git commit -m "feat: update EntitySummaryDto with backward-compatible design

- Add DisplayName (single language, new mode)
- Keep DisplayNameTranslations (multilingual, legacy mode)
- Use JsonIgnore to exclude null fields
- Add serialization tests
- Related to ARCH-30"
```

---

#### 阶段1：高频API改造（首屏性能优化）

**范围**：用户每次登录/刷新都会调用的API，优化收益最大

##### Task 1.1：改造 `/api/access/functions/me`

**影响范围**：用户功能菜单（每次登录调用）

**工作内容**：

1. **修改端点接受lang参数**
```csharp
// 文件：BobCrm.Api/Endpoints/AccessEndpoints.cs (约第120行)
 app.MapGet("/api/access/functions/me", async (
     string? lang,  // ⭐ 新增参数
     HttpContext http,
     ILocalization loc,
     /* ... 其他参数 */) =>
 {
     // 注：该端点属于高频路径，可允许 Accept-Language 作为默认语言来源（无 lang 时仍可能进入单语模式）
     var targetLang = LangHelper.GetLang(http, lang);
     // ... 后续逻辑
 });
 ```

2. **修改 FunctionTreeBuilder 应用语言过滤**
```csharp
// 文件：BobCrm.Api/Services/FunctionTreeBuilder.cs
public async Task<List<FunctionNodeDto>> BuildTreeAsync(
    /* ... 参数 */
    string lang)  // ⭐ 新增参数
{
    // ... 构建树逻辑

    // 应用语言过滤
    foreach (var node in nodes)
    {
        node.DisplayName = node.DisplayNameTranslations.Resolve(lang);
        node.DisplayNameTranslations = null;  // 清除完整字典
    }

    return nodes;
}
```

**需要的测试**：
```csharp
// 文件：tests/BobCrm.Api.Tests/Endpoints/AccessEndpointsTests.cs
[Fact]
public async Task GetMyFunctions_WithLangParameter_ReturnsSingleLanguage()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/access/functions/me?lang=zh");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<SuccessResponse<List<FunctionNodeDto>>>(content);

    Assert.NotNull(result?.Data);
    var firstNode = result.Data.FirstOrDefault();
    Assert.NotNull(firstNode?.DisplayName);  // 应该有单语显示名
    Assert.Null(firstNode?.DisplayNameTranslations);  // 不应有多语字典
}

[Fact]
public async Task GetMyFunctions_WithoutLangParameter_ReturnsMultilingual()
{
    // Backward compatibility test
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/api/access/functions/me");

    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<SuccessResponse<List<FunctionNodeDto>>>(content);

    var firstNode = result.Data.FirstOrDefault();
    Assert.NotNull(firstNode?.DisplayNameTranslations);  // 兼容模式应返回多语
}
```

**提交规范**：
```bash
# 第一次提交：端点修改
git add BobCrm.Api/Endpoints/AccessEndpoints.cs
dotnet build
git commit -m "feat: add lang parameter to /api/access/functions/me

- Accept optional lang query parameter
- Use LangHelper.GetLang() as fallback
- Related to ARCH-30"

# 第二次提交：服务层修改
git add BobCrm.Api/Services/FunctionTreeBuilder.cs
dotnet build
git commit -m "feat: apply language filtering in FunctionTreeBuilder

- Add lang parameter to BuildTreeAsync()
- Resolve multilingual display names to single language
- Related to ARCH-30"

# 第三次提交：测试
git add tests/BobCrm.Api.Tests/Endpoints/AccessEndpointsTests.cs
dotnet test --filter "FullyQualifiedName~AccessEndpointsTests"
git commit -m "test: add lang parameter tests for /api/access/functions/me

- Test single language mode (with lang param)
- Test multilingual mode (backward compatibility)
- Related to ARCH-30"
```

##### Task 1.2：改造 `/api/templates/menu-bindings`

**影响范围**：导航菜单（首屏加载）

**工作内容**：

1. **修改系统语言为用户语言**
```csharp
// 文件：BobCrm.Api/Endpoints/TemplateEndpoints.cs (约第254行)
 app.MapGet("/api/templates/menu-bindings", async (
     string? lang,  // ⭐ 新增参数
     HttpContext http,
     ILocalization loc,
     AppDbContext db,
     CancellationToken ct) =>
 {
    // ❌ 移除：使用系统语言
    // var systemLanguage = await db.SystemSettings...

     // ✅ 使用用户语言
     var targetLang = LangHelper.GetLang(http, lang);

     // ... 后续使用targetLang解析显示名
 });
 ```

**需要的测试**：
```csharp
// 文件：tests/BobCrm.Api.Tests/Endpoints/TemplateEndpointsTests.cs
[Fact]
public async Task GetMenuBindings_WithLangZh_ReturnsChineseNames()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/api/templates/menu-bindings?lang=zh");

    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();

    // 验证返回的显示名是中文
    Assert.Contains("客户", content);  // 假设有客户实体
    Assert.DoesNotContain("Customer", content);
    Assert.DoesNotContain("顧客", content);
}

[Fact]
public async Task GetMenuBindings_WithLangJa_ReturnsJapaneseNames()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/api/templates/menu-bindings?lang=ja");

    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();

    Assert.Contains("顧客", content);
}
```

**提交规范**：
```bash
git add BobCrm.Api/Endpoints/TemplateEndpoints.cs
git add tests/BobCrm.Api.Tests/Endpoints/TemplateEndpointsTests.cs
dotnet build
dotnet test --filter "FullyQualifiedName~TemplateEndpointsTests"
git commit -m "fix: use user language instead of system language in menu bindings

- Replace system default language with user's lang parameter
- Add lang parameter to /api/templates/menu-bindings
- Add language-specific tests
- Fixes language inconsistency issue
- Related to ARCH-30"
```

##### Task 1.3：改造 `/api/entities`

**影响范围**：实体列表（路由初始化）

**工作内容**：
```csharp
// 文件：BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs (约第50行)
 entitiesGroup.MapGet("", async (
     string? lang,  // ⭐ 新增参数
     HttpContext http,
     AppDbContext db) =>
 {
     var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);

    var entities = await db.EntityDefinitions
        .Where(ed => ed.IsEnabled && ed.Status == "Published")
        .AsNoTracking()
        .ToListAsync();

    // ✅ 使用扩展方法转换
    var dtos = entities.Select(e => e.ToSummaryDto(targetLang)).ToList();

    return Results.Ok(new SuccessResponse<List<EntitySummaryDto>>(dtos));
})
.WithName("GetEntities")
.WithSummary("获取所有已启用实体（支持语言参数）");
```

**需要的测试**：
```csharp
// 文件：tests/BobCrm.Api.Tests/Endpoints/EntityDefinitionEndpointsTests.cs
[Fact]
public async Task GetEntities_WithLang_ReturnsOptimizedResponse()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/api/entities?lang=zh");

    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<SuccessResponse<List<EntitySummaryDto>>>(content);

    Assert.NotNull(result?.Data);
    foreach (var entity in result.Data)
    {
        Assert.NotNull(entity.DisplayName);  // 应有单语
        Assert.Null(entity.DisplayNameTranslations);  // 不应有多语字典
    }

    // ⭐ 验证响应体积减小
    var originalSize = await GetResponseSizeWithoutLang();
    var optimizedSize = content.Length;
    Assert.True(optimizedSize < originalSize * 0.5);  // 应减少50%以上
}
```

**提交规范**：
```bash
git add BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs
git add tests/BobCrm.Api.Tests/Endpoints/EntityDefinitionEndpointsTests.cs
dotnet build
dotnet test --filter "FullyQualifiedName~EntityDefinitionEndpointsTests.GetEntities"
git commit -m "feat: optimize /api/entities with lang parameter

- Add lang parameter support
- Use ToSummaryDto() extension method
- Reduce response size by ~66%
- Add performance test
- Related to ARCH-30"
```

---

#### 阶段2：中频API改造（列表展示优化）

**范围**：用户在管理界面频繁访问的列表API

##### Task 2.1：改造 `/api/entity-definitions`

**工作内容**：
```csharp
// 文件：BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs
 app.MapGet("/api/entity-definitions", async (
     string? lang,
     HttpContext http,
     AppDbContext db) =>
 {
     var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
     var definitions = await db.EntityDefinitions.ToListAsync();
     var dtos = definitions.Select(d => d.ToSummaryDto(targetLang)).ToList();
     return Results.Ok(new SuccessResponse<List<EntitySummaryDto>>(dtos));
 });
 ```

**测试+提交**：（模式同Task 1.3）

##### Task 2.2：改造 `/api/enums`

**工作内容**：
```csharp
// 文件：BobCrm.Api/Endpoints/EnumDefinitionEndpoints.cs
 app.MapGet("/api/enums", async (
     string? lang,
     HttpContext http,
     AppDbContext db) =>
 {
     // ✅ 向后兼容：仅显式 ?lang=xx 才进入单语模式
     var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
     var enums = await db.EnumDefinitions.ToListAsync();
     var dtos = enums.Select(e => e.ToSummaryDto(targetLang)).ToList();
     return Results.Ok(new SuccessResponse<List<EnumSummaryDto>>(dtos));
 });
 ```

**测试+提交**：（模式同Task 1.3）

##### Task 2.3：改造 `/api/entity-domains`

（模式同上）

##### Task 2.4：改造 `/api/access/functions`（及其变体）

包含：
- `/api/access/functions` (GET)
- `/api/access/functions/manage` (GET)
- `/api/access/functions/export` (GET)

（模式同Task 1.1）

---

#### 阶段3：低频API改造（数据查询优化，按需实施）

**范围**：动态实体查询相关API，使用频率较低，改造复杂度高

 ##### Task 3.1：研究动态实体查询机制
 
 **工作内容**：
 1. 阅读 `DynamicEntityService` 源码
 2. 理解查询结果的多语字段处理机制
 3. 编写技术调研文档
 
 **输出**：技术调研报告（`docs/history/AUDIT-04-ARCH-30-动态实体多语研究报告.md`）
 
 **提交规范**：
 ```bash
 git add docs/history/AUDIT-04-ARCH-30-动态实体多语研究报告.md
 git commit -m "docs(research): add dynamic entity i18n research report
 
 - Analyze codegen/compile/query pipeline
 - Identify field metadata & i18n resolve points
 - Recommend meta.fields approach for Stage 3
 - Ref: ARCH-30 Task 3.1"
 ```
 
 ##### Task 3.2：设计字段级多语解析方案
 
 **目标**：为动态实体“数据查询”端点补齐字段级显示名能力，同时保持“数据值（data）/元数据标签（meta.fields）”职责分离。
 
 **核心结论（承接 Task 3.1）**：
 - 动态实体查询链路当前不做 DTO 转换，直接返回运行时实体对象；字段显示名不可能“自然出现”在结果中
 - 字段显示名解析最佳落点是元数据层（EntityDefinition/FieldMetadata DTO），而不是数据查询返回本体
 - 若查询响应需要列信息，推荐在端点层拼装 `meta.fields`，并配套字段元数据缓存（按 `fullTypeName`）
 
 ###### 3.2.1 设计原则
 
 - **职责分离**：`data` 仅包含实体数据；`meta.fields` 仅包含字段元数据（显示名、类型等）
 - **向后兼容（lang 规则）**：仅显式 `?lang=xx` 才输出单语字符串；无 `lang` 返回多语结构（忽略 `Accept-Language`）
 - **性能优先**：字段元数据按 `fullTypeName` 缓存；i18n 解析复用 `ILocalization` 内部缓存
 - **复用现有能力**：复用 `DtoExtensions.ToFieldDto(field, loc, lang)` 的三级优先级逻辑（`DisplayNameKey` → `DisplayName` → `PropertyName`）
 
 ###### 3.2.2 返回结构（meta.fields）
 
 **Query（列表查询）**：在现有结构基础上增加 `meta`（增量字段，兼容旧客户端忽略未知字段）
 
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
 - 为避免破坏既有使用方，建议通过 `includeMeta=true` 控制是否包裹：
   - `includeMeta=false`（默认）：保持现状，返回实体对象
   - `includeMeta=true`：返回 `{ meta, data }`
 
 ```json
 {
   "meta": { "fields": [ /* 同上 */ ] },
   "data": { "...": "..." }
 }
 ```
 
 ###### 3.2.3 双模式规则（字段显示名）
 
 统一采用阶段1/2 的规则：
 
 ```csharp
 var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
 var uiLang = LangHelper.GetLang(http); // 仅用于错误消息
 ```
 
 - `targetLang != null`（单语模式）：`FieldMetadataDto.DisplayName` 输出单语字符串；`DisplayNameTranslations` 为 null
 - `targetLang == null`（多语模式，向后兼容）：
   - 接口字段：输出 `DisplayNameKey`（不展开多语字典）
   - 自定义字段：输出 `DisplayNameTranslations`（字典）；`DisplayName` 为 null
 
 ###### 3.2.4 DTO 设计（建议）
 
 复用已有的 `FieldMetadataDto`（已支持双模式）作为 `meta.fields` 元素类型，并新增查询结果 DTO：
 
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
 
 ###### 3.2.5 字段元数据缓存机制（建议）
 
 **缓存 Key**：
 - 基础元数据：`FieldMetadata:{fullTypeName}`（存 FieldMetadata 的最小必要集合）
 - 可选：按语言缓存 DTO 视图：`FieldMetadata:{fullTypeName}:{lang}:{i18nVersion}`
 
 **失效策略**（推荐组合）：
 - **主动失效**：实体定义/字段变更后，调用 `Invalidate(fullTypeName)`
 - **被动过期**：30 分钟滑动/绝对过期（防止遗漏失效）
 - **i18n 版本**：如缓存单语 DTO 视图，则使用 `ILocalization.GetCacheVersion()` 作为 version
 
 **缓存服务接口（建议）**：
 
 ```csharp
 public interface IFieldMetadataCache
 {
     Task<IReadOnlyList<FieldMetadataDto>> GetFieldsAsync(string fullTypeName, ILocalization loc, string? lang, CancellationToken ct);
     void Invalidate(string fullTypeName);
 }
 ```
 
 实现要点：
 - DB 查询：按 `fullTypeName` 加载 `EntityDefinition`（含 `Fields`），一次性取全字段
 - DTO 映射：对每个字段调用 `field.ToFieldDto(loc, lang)`（复用已有逻辑）
 - 避免 N+1：接口字段翻译走 `ILocalization` 内部缓存；如需批量资源可选用 `MultilingualFieldService.LoadResourcesAsync(...)`
 
 ###### 3.2.6 端点修改方案（Task 3.3 将实现）
 
 - `POST /api/dynamic-entities/{fullTypeName}/query`
   - 新增查询参数：`lang`（可选）
   - 追加响应字段：`meta.fields`
 
 - `GET /api/dynamic-entities/{fullTypeName}/{id}`
   - 新增查询参数：`lang`（可选）、`includeMeta`（可选，默认 false）
   - `includeMeta=true` 时返回 `{ meta, data }`
 
 ##### Task 3.3：实施改造
 
 - `/api/dynamic-entities/{type}/query` (POST)
 - `/api/dynamic-entities/{type}/{id}` (GET)

---

#### 📝 阶段4：文档同步（贯穿整个过程）

每个Task完成后都需要更新相关文档：

##### Task 4.1：更新 CHANGELOG.md

**在每次提交后**添加条目：
```markdown
## [未发布] - 进行中

### Added (新增)
- [ARCH-30] 新增 MultilingualHelper 多语解析工具类
- [ARCH-30] /api/access/functions/me 支持 lang 参数

### Changed (变更)
- [ARCH-30] /api/templates/menu-bindings 使用用户语言替代系统语言

### Fixed (修复)
- [ARCH-30] 修复菜单绑定语言不一致问题
```

##### Task 4.2：更新 API-01-接口文档.md

每个端点改造后，更新API文档：
```markdown
### GET /api/access/functions/me

获取当前用户的功能菜单。

**查询参数**：
- `lang` (可选): 语言代码（zh/ja/en），默认使用 Accept-Language 头

**响应示例**（lang=zh）：
\```json
{
  "data": [
    {
      "code": "CUSTOMER",
      "displayName": "客户管理",  // ✅ 单一语言
      "children": [...]
    }
  ]
}
\```

**v2.0变更**：
- 新增 `lang` 参数支持
- 当提供 lang 参数时，只返回指定语言的显示名
- 不提供lang参数时，保持向后兼容（返回完整多语）
```

##### Task 4.3：更新单元测试文档

在 `docs/guides/TEST-01-测试指南.md` 中添加多语API测试规范：
```markdown
### 多语API测试规范

所有支持 lang 参数的API都应包含以下测试：

1. **单语模式测试**：提供lang参数，验证只返回该语言
2. **多语模式测试**：不提供lang参数，验证返回完整多语（向后兼容）
3. **语言切换测试**：测试不同lang参数返回不同语言
4. **性能测试**：验证单语模式响应体积减小
```

---

#### 🔄 持续集成检查清单

每次提交前必须通过：

```bash
# 1. 编译检查
dotnet build

# 2. 运行相关单元测试
dotnet test --filter "FullyQualifiedName~[YourTestClass]"

# 3. 运行所有单元测试（提交前）
dotnet test

# 4. 代码风格检查
pwsh scripts/check-style-tokens.ps1

# 5. Git提交
git add [files]
git commit -m "[type]: [message]

- [详细说明]
- Related to ARCH-30"
```

---

### 10.4 工作计划跟踪（WORK-PLAN.md）

**建议创建独立的工作计划文档**：`docs/plans/PLAN-09-系统级多语API架构优化-工作计划.md`

内容包括：
```markdown
# ARCH-30 系统级多语API优化工作计划

## 进度概览

| 阶段 | 总Tasks | 已完成 | 进行中 | 待开始 | 完成率 |
|------|--------|--------|--------|--------|--------|
| 阶段0 | 3 | 0 | 0 | 3 | 0% |
| 阶段1 | 3 | 0 | 0 | 3 | 0% |
| 阶段2 | 4 | 0 | 0 | 4 | 0% |
| 阶段3 | 3 | 0 | 0 | 3 | 0% |

## 详细进度

### 阶段0：基础设施

- [ ] Task 0.1：MultilingualHelper（预计：小）
  - [ ] 编写代码
  - [ ] 编写测试
  - [ ] 提交 (Commit: xxx)
  - 负责人：
  - 状态：待开始
  - 开始时间：
  - 完成时间：

- [ ] Task 0.2：DTO扩展方法（预计：小）
  - [ ] 编写代码
  - [ ] 编写测试
  - [ ] 提交 (Commit: xxx)
  - 负责人：
  - 状态：待开始

...（详细列出每个Task）

## 变更日志

| 日期 | 变更内容 | 负责人 |
|------|---------|--------|
| 2025-12-11 | 创建工作计划 | - |
```

### 10.4 风险评估与缓解

#### 风险1：破坏性变更

**风险**：修改API响应结构可能破坏现有前端代码

**缓解措施**：
- ✅ 使用**向后兼容**的DTO设计（同时支持单语和多语）
- ✅ 分阶段上线，先改造低频端点验证
- ✅ 增加**功能开关**，可动态切换新旧实现

**向后兼容示例**：
```csharp
public class EntitySummaryDto
{
    // ✅ 新字段：单一语言（优先使用）
    public string? DisplayName { get; set; }

    // ✅ 旧字段：多语字典（兼容旧前端）
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DisplayNameTranslations { get; set; }
}
```

#### 风险2：性能影响

**风险**：增加语言参数处理可能增加响应时间

**缓解措施**：
- ✅ 使用缓存（按实体+语言组合缓存）
- ✅ 性能基准测试：改造前后对比
- ✅ 监控高频端点的响应时间

#### 风险3：测试覆盖不足

**风险**：改造44+个端点，测试成本高

**缓解措施**：
- ✅ 编写**参数化测试**，覆盖所有语言
- ✅ E2E测试验证语言切换功能
- ✅ 自动化截图对比（三种语言）

### 10.5 成功指标

改造完成后，预期达到以下目标：

| 指标 | 当前 | 目标 | 提升 |
|------|------|------|------|
| **lang参数支持覆盖率** | 32% (6/19) | 100% (19/19) | +212% |
| **首屏加载时间** | 约800ms | 约600ms | **-25%** |
| **平均API响应体积** | 约100KB | 约35KB | **-65%** |
| **前端硬编码兜底逻辑** | 225行 | 0行 | **-100%** |
| **语言一致性问题** | 存在 | 消除 | ✅ |

---

## 11. 更新记录

| 版本 | 日期       | 作者          | 变更说明               |
|-----|-----------|--------------|----------------------|
| 1.0 | 2025-12-11| AI Assistant | 初始版本，实体字段显示名多语优化设计 |
| 2.0 | 2025-12-11| AI Assistant | 🔥 **重大升级**：扩展为系统级多语API架构规范<br/>- 新增：50+个API端点全面分析<br/>- 新增：统一API多语规范（章节10.2）<br/>- 新增：分阶段实施计划（章节10.3）<br/>- 新增：前端HTTP拦截器设计<br/>- 覆盖：所有返回多语数据的API端点 |

---

**审批**：待项目负责人审批
**实施计划**：预计4.5天完成（后端2天 + 前端1天 + 测试1天 + 文档0.5天）
**优先级**：高（元数据驱动核心机制，影响多语体验）
