# ARCH-30: 实体字段显示名多语元数据驱动设计

**文档版本**: 2.1
**创建日期**: 2025-12-11
**最后更新**: 2026-01-06
**状态**: 实施中
**影响范围**: 🔥 **全系统架构级变更** - 涉及50+个API端点
**相关文档**:
- `docs/guides/GUIDE-11-实体定义与动态实体操作指南.md`
- `docs/design/I18N-01-多语机制设计文档.md`

---

## 1. 概述 (Overview)

本设计文档旨在解决 BobCRM 系统中多语字段显示名（DisplayName）的架构问题。通过从"返回完整多语字典"架构迁移到"基于请求参数返回单一语言"架构，实现数据传输量的最小化（-66%）和前端逻辑的零兜底。

**本次更新 (v2.1)**：整合了 Phase 3 (动态实体查询优化) 和 Phase 4 (文档治理规范)。

## 2. 问题背景 (Problem)

### 2.1 系统级问题
系统目前有 19+ 个涉及即时多语的 API 端点，但仅 6 个支持了 `lang` 参数。高频端点（如 `/api/access/functions/me` 和 `/api/entities`）返回了冗余的全量语言包，浪费带宽且拖慢首屏加载。

### 2.2 实体元数据问题
1.  **接口字段硬编码**: `StorageDDLGenerator` 中硬编码了三语字典，未引用 i18n 资源。
2.  **前端兜底逻辑**: 前端 `PageLoader` 为弥补元数据缺失，维护了大量硬编码映射 (`baseResourceMap`)。
3.  **元数据不完整**: 缺少 `DisplayNameKey` 字段，无法追溯资源来源。

## 3. 设计目标 (Goals)

1.  **元数据驱动**: 前端完全移除非业务性的翻译与映射逻辑。
2.  **资源Key引用**: 系统字段强制使用 `LBL_FIELD_{NAME}` 引用 i18n 资源。
3.  **性能优先**: API 响应只包含请求语言的文本，减少 66% 数据量。

---

## 4. API 设计方案 (API Design)

### 4.1 方案 B：语言参数优化 (Selected)

**核心逻辑**:
- **Request**: `GET /api/entities/{type}/field-metadata?lang=zh`
- **Response**: 返回结构中 `DisplayName` 为 `string` 类型（已翻译），而非 `Dictionary`。

**数据流**:
1.  前端从 `I18nService.CurrentLanguage` 获取语言。
2.  调用 API 显式传递 `lang` 参数。
3.  后端 `ResolveDisplayName()` 方法根据 `lang` 解析 `DisplayNameKey` 或 `DisplayName` 字典。
4.  返回轻量级 DTO。

### 4.2 扩展 DTO 定义

`EntityFieldDto` 变更：

```csharp
public class EntityFieldDto
{
    // ...
    
    // [新增] 资源 Key (仅系统/接口字段有值)
    public string? DisplayNameKey { get; set; }

    // [变更] 直接返回字符串
    public string DisplayName { get; set; } = string.Empty;
}
```

---

## 5. 技术实施细节 (Implementation)

### 5.1 数据模型扩展 (`FieldMetadata`)
新增 `DisplayNameKey` (varchar 100) 字段。
- **System Fields**: `DisplayNameKey`="LBL_..."
- **Custom Fields**: `DisplayNameKey`=null, 使用 `DisplayName` (jsonb)

### 5.2 后端适配 (`EntityDefinitionEndpoints`)
- **端点**: `GET /{type}/definition` 和 `GET /{type}/field-metadata`。
- **逻辑**: 通过 `LangHelper` 获取语言，调用 `ResolvedDisplayName` 辅助方法注入翻译。

### 5.3 前端适配 (`BobCrm.App`)
- **PageLoader**: 移除 `LoadFieldLabels` 中的所有硬编码映射。直接使用 API 返回的 `DisplayName`。
- **Widget**: 简化 `GetWidgetLabel`，直接读取属性。

---

## 6. Phase 3: 动态实体查询优化 (Dynamic Entity Query)

### 6.1 问题背景
动态实体查询 (`/api/dynamic/{entity}`) 目前返回原始值（枚举 Int 值或 Lookup GUID）。前端渲染表格时需要额外请求元数据来翻译。

### 6.2 详细设计

#### Task 3.1: 扩展结构
在动态实体 JSON 响应中注入 `__display` 字段：

```json
{
  "id": "item-001",
  "status": 10,
  "__display": {
    "status": "已发布" 
  }
}
```

#### Task 3.2: 服务层集成
- `DynamicEntityService` 集成 `I18nService`。
- 在 `Serialize` 阶段，如果有 `lang` 参数，则解析所有 Enum/Lookup 字段。

---

## 7. Phase 4: 文档治理规范 (Governance)

### 7.1 "设计即真理" (Design as Truth)
- 严禁在代码中定义未在 `ARCH` 文档中声明的数据结构变更。
- Schema 变更流程: `ARCH Document` -> `Migration Script` -> `Code Implementation`.

### 7.2 一致性检查
- 建立 `scripts/doc-check.ps1` 脚本，定期扫描 API 定义与文档的偏差。

---

## 8. 风险评估 (Risk Assessment)

| 风险点 | 影响 | 缓解措施 |
|-------|------|---------|
| 前端缓存失效 | 切换语言时若未刷新，可能显示旧语言 | 监听语言变更事件，强制 reload |
| 性能回退 | 大量实时翻译可能增加 CPU 负载 | 引入 `IMemoryCache` 缓存翻译结果 |

---

## 9. 更新记录 (Change Log)

| 版本 | 日期       | 作者 | 变更说明 |
|-----|-----------|------|---------|
| v1.0 | 2025-12-11 | AI   | 初始草案 |
| v2.0 | 2025-12-28 | AI   | 扩展为系统级多语 API 架构 |
| v2.1 | 2026-01-06 | AI   | 补充 Phase 3 (查询优化) 和 Phase 4 (文档规范)，修正文档结构 |
