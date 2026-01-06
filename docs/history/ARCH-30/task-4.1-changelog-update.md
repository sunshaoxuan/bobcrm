# ARCH-30 Task 4.1: 更新 CHANGELOG.md

**任务编号**: Task 4.1
**阶段**: 阶段4 - 文档同步
**状态**: 待开始
**预计工时**: 0.5 小时
**依赖**: 阶段0-3 实施完成

---

## 1. 任务目标

将 ARCH-30 项目的所有变更记录到 `CHANGELOG.md`，遵循 [Keep a Changelog](https://keepachangelog.com/) 规范。

---

## 2. 变更内容汇总

### 2.1 阶段0：基础设施

| 任务 | 变更类型 | 变更内容 |
|------|----------|----------|
| Task 0.1 | Added | 新增 `MultilingualHelper` 多语解析工具类 |
| Task 0.2 | Added | 新增 `DtoExtensions` 扩展方法（`ToSummaryDto`, `ToFieldDto`） |
| Task 0.3 | Changed | DTO 支持双模式字段（单语/多语） |

### 2.2 阶段1：高频 API 改造

| 任务 | 变更类型 | 变更内容 |
|------|----------|----------|
| Task 1.1 | Changed | `/api/access/functions/me` 支持 `lang` 参数 |
| Task 1.2 | Fixed | `/api/templates/menu-bindings` 使用用户语言替代系统语言 |
| Task 1.3 | Changed | `/api/entities` 支持 `lang` 参数 |

### 2.3 阶段2：中频 API 改造

| 任务 | 变更类型 | 变更内容 |
|------|----------|----------|
| Task 2.1 | Changed | `/api/entity-definitions/*` 支持 `lang` 参数 |
| Task 2.2 | Changed | `/api/enums/*` 支持 `lang` 参数 |
| Task 2.3 | Changed | `/api/entity-domains` 支持 `lang` 参数 |
| Task 2.4 | Changed | `/api/access/functions` 系列支持 `lang` 参数 |

### 2.4 阶段3：低频 API 改造

| 任务 | 变更类型 | 变更内容 |
|------|----------|----------|
| Task 3.2 | Added | 新增 `IFieldMetadataCache` 字段元数据缓存服务 |
| Task 3.3 | Changed | `/api/dynamic-entities/*/query` 返回 `meta.fields` |
| Task 3.3 | Changed | `/api/dynamic-entities/*/{id}` 支持 `includeMeta` 参数 |

---

## 3. CHANGELOG 条目模板

在 `CHANGELOG.md` 的 `## [未发布] - 进行中` 部分添加：

```markdown
## [未发布] - 进行中

### Added (新增)
- [ARCH-30] 新增 `MultilingualHelper` 多语解析工具类，支持 `DisplayNameKey` 优先解析
- [ARCH-30] 新增 `DtoExtensions` 扩展方法（`ToSummaryDto`, `ToFieldDto`），统一 DTO 转换逻辑
- [ARCH-30] 新增 `IFieldMetadataCache` 字段元数据缓存服务，优化动态实体查询性能
- [ARCH-30] 动态实体查询 API 支持 `meta.fields` 字段元数据返回

### Changed (变更)
- [ARCH-30] DTO 支持双模式字段：单语模式返回 `displayName` 字符串，多语模式返回 `displayNameTranslations` 字典
- [ARCH-30] 以下 API 端点新增 `lang` 参数支持：
  - `/api/access/functions/me` - 用户功能菜单
  - `/api/templates/menu-bindings` - 导航菜单绑定
  - `/api/entities` - 实体列表
  - `/api/entity-definitions/*` - 实体定义管理
  - `/api/enums/*` - 枚举定义管理
  - `/api/entity-domains` - 实体域管理
  - `/api/access/functions` 系列 - 权限功能管理
  - `/api/dynamic-entities/*/query` - 动态实体查询
- [ARCH-30] `/api/dynamic-entities/*/{id}` 新增 `includeMeta` 参数，支持返回字段元数据

### Fixed (修复)
- [ARCH-30] 修复 `/api/templates/menu-bindings` 使用系统语言而非用户语言的问题

### Performance (性能优化)
- [ARCH-30] API 响应体积优化：单语模式下响应体积减少约 66%（不再返回完整三语字典）
- [ARCH-30] 字段元数据缓存：避免重复查询数据库，30分钟滑动过期
```

---

## 4. 验收标准

- [ ] CHANGELOG.md 已更新
- [ ] 条目按 Added/Changed/Fixed/Performance 分类
- [ ] 每条变更有 `[ARCH-30]` 前缀标记
- [ ] API 端点变更列表完整

---

## 5. Git 提交规范

```bash
git add CHANGELOG.md
git commit -m "docs(changelog): add ARCH-30 system-level i18n API changes

- Document 4 phases of API optimization
- List all affected endpoints with lang parameter support
- Note performance improvements (66% response size reduction)
- Ref: ARCH-30 Task 4.1"
```

---

**创建日期**: 2025-01-06
**最后更新**: 2025-01-06
**维护者**: ARCH-30 项目组
