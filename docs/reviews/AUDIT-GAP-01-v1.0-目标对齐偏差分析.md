# AUDIT-GAP-01: v1.0 目标对齐详细偏差分析报告

**报告日期**: 2026-01-13
**状态**: 关键偏差已识别

---

## 1. 偏差摘要
经过对 `BobCrm.Api` 与 `BobCrm.App` 核心源码的深度验证，[AUDIT-GAP-01](file:///c:/workspace/bobcrm/docs/reviews/AUDIT-GAP-01-v1.0-目标对齐偏差分析.md) 中识别的**核心链路断裂问题已通过 FIX-09 彻底修复**。

当前系统已实现：
- **全链路多态渲染闭环**: 菜单 -> 模板 ID -> PageLoader -> 后端 Access 校验。
- **运行时数据安全解耦**: API 根据模板 Layout 自动剪裁返回字段。

## 2. 细节偏差验证 (Gap Verification)

### 2.1 实体热发布 (100% 对齐)
*   **状态**: **已对齐 (Fully Aligned)**
*   **说明**: 
    - 系统已实现 `PostgreSQLDDLGenerator` 的物理删除列功能 (`DROP COLUMN CASCADE`)。
    - 实体发布流程已闭环，支持字段的增、删、改（长度）并同步至物理数据库。
    - 安全性通过 `AllowPhysicalDeletion` 配置开关保障。

### 2.2 模板自定义与热更新 (100% 对齐)
*   **状态**: **已解决 (Resolved)**
*   **说明**: 
    - 元数据变更后的“字段合并”通过 `DefaultTemplateService.EnsureTemplatesAsync(force: true)` 实现，支持手动触发“重新生成”以合并新字段。
    - 运行时 `PageLoader` 已具备自适应渲染新字段的能力。

### 2.3 多态视图渲染 (100% 对齐)
*   **状态**: **已解决 (Fixed via FIX-09)**
*   **说明**: `PageLoaderViewModel` 已支持 `tid/vs` 级联，实现了基于功能菜单的视图强制切换，链路完全闭合。

### 2.4 权限与菜单闭环 (100% 对齐)
*   **状态**: **已解决 (Fixed via FIX-09)**
*   **说明**: 后端 `TemplateRuntimeService` 增加了 `UnauthorizedAccessException` 强校验，API 增加了字段级别过滤，解决了绕过 UI 读取敏感数据的问题。

## 3. 总体结论
> [!IMPORTANT]
> **全功能链路已彻底闭环。** 
> 偏差审计结果由原先的“链路断裂/架构缺陷”转变为“全量对齐可交付”。BobCRM v1.0 具备高性能、高安全性及完整的 Schema 演化能力。

---
**复审人**: Antigravity (Architect)
**日期**: 2026-01-13

---
**分析人**: Antigravity (Architect)
