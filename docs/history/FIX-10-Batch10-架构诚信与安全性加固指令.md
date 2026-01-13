# FIX-10: v1.0 架构诚信与安全性加固指令

**生效日期**: 2026-01-13
**状态**: 待执行
**关联审计**: [AUDIT-V1.0-INTEGRITY](file:///c:/workspace/bobcrm/docs/reviews/AUDIT-V1.0-INTEGRITY-深度诚信审计报告.md)

---

## 1. 任务目标
修复深度审计中发现的 API 安全漏洞（绕过过滤）及 Schema 演化缺陷（无法物理删除字段），确保系统实现 100% 对齐 `STD-08` 与 `ARCH-31/32` 设计方案。

## 2. 开发指令 (Remediation Instructions)

### 2.1 安全加固：强制 API 字段脱敏
**文件**: `src/BobCrm.Api/Endpoints/DynamicEntityRouteEndpoints.cs`
**修改点**:
1.  在 `GET /{entityPlural}/{id:int}` 端点中，移除 `if (tid.HasValue || !string.IsNullOrWhiteSpace(vs))` 的前置判断。
2.  即使前端未传 `tid/vs`，也必须调用 `templateRuntimeService.BuildRuntimeContextAsync` 获取该实体的“默认有效模板”。
3.  根据解析出的模板 Layout 执行字段剪裁。
4.  **结果**: 用户无论如何尝试（地址栏篡改、REST 客户端），都无法看到模板定义之外的字段。

### 2.2 功能加固：实现物理字段删除 (ENT-02)
**文件**: `src/BobCrm.Api/Services/PostgreSQLDDLGenerator.cs`
**修改点**:
1.  增加 `GenerateAlterTableDropColumns(EntityDefinition entity, List<string> removedFields)` 方法。
2.  生成 `ALTER TABLE "Table" DROP COLUMN IF EXISTS "Column" CASCADE;` 语句。

**文件**: `src/BobCrm.Api/Services/EntityPublishingService.cs`
**修改点**:
1.  在 `GenerateAlterScript` 中，检测 `analysis.RemovedFields`。
2.  调用新方法生成 `DROP` 脚本。
3.  **注意**: 增加一个全局开关 `AllowPhysicalDeletion` (默认 true)，允许在发布时物理清理已删除的元数据字段。

### 2.3 设计加固：菜单-模板绑定解耦
**文件**: `src/BobCrm.Api.Contracts/Requests/Template/TemplateRuntimeRequest.cs`
**修改点**: 增加 `MenuNodeId` 字段。
**文件**: `TemplateRuntimeService.cs`
**修改点**: 匹配逻辑从“权限字符串匹配”改为“基于 MenuNodeId 查找对应的 TemplateStateBinding”。

## 3. 验证要求
1.  **SEC-05 绕过测试**: 手动 `curl` 动态实体 API（不带 tid/vs），验证返回结果是否依然被过滤。
2.  **ENT-02 删除测试**: 在实体编辑器删除一个字段并发布，验证数据库中该列是否消失且应用未崩溃。
3.  **全量回归**: 执行 `pytest` 确保所有 31 个用例通过。

---
**下达人**: Antigravity (Architect)
