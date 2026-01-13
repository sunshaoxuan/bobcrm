# PLAN-26: ARCH-32 及 SEC-06 功能补完与安全性加固计划

**生效日期**: 2026-01-13
**状态**: 计划中
**依据**: `ARCH-32`, `SEC-06`, `AUDIT-06`

---

## 1. 目标描述

本计划旨在完成实体发布生命周期的最后闭环 (`ARCH-32`)，并修复在审计期间发现的 `SEC-06` 严重安全性缺陷。

主要目标：
1.  **SEC-06 加固**: 确保动态实体 `PUT` 接口强制执行字段级权限过滤。
2.  **ARCH-32 闭环**: 在前端实现实体的“撤回 (Withdraw)”操作，并与其依赖保护逻辑集成。
3.  **合规性对齐**: 确保操作流程符合 `STD-06` 集成测试及发布规范。

## 2. 变更内容 (Proposed Changes)

### 2.1 安全加固层 (Security Hardening)

#### [MODIFY] [DynamicEntityRouteEndpoints.cs](file:///c:/workspace/bobcrm/src/BobCrm.Api/Endpoints/DynamicEntityRouteEndpoints.cs)
- 在 `UpdateDynamicEntityByShortRoute` (`PUT`) 中植入 `FieldFilterService.ValidateWriteFieldsAsync`。
- 如果请求体包含当前用户在选中模板中无权写的字段，立即终止并返回 `403 Forbidden`。

### 2.2 发布流程层 (Publishing Workflow)

#### [MODIFY] [EntityDefinitionService.cs](file:///c:/workspace/bobcrm/src/BobCrm.App/Services/EntityDefinitionService.cs)
- 新增 `WithdrawAsync(Guid id)` 方法，对接后端 `POST /api/entity-definitions/{id}/withdraw`。

#### [MODIFY] [EntityDefinitionsTable.razor](file:///c:/workspace/bobcrm/src/BobCrm.App/Components/Shared/EntityDefinitionsTable.razor)
- 为 `Status == EntityStatus.Published` 的实体增加“撤回”操作按钮。
- 增加关联关系风险提示（如被其他已发布实体引用时，按钮置灰并显示原因说明）。

#### [MODIFY] [EntityDefinitionPublish.razor](file:///c:/workspace/bobcrm/src/BobCrm.App/Components/Pages/EntityDefinitionPublish.razor)
- 头部操作区增加“撤回”按钮。
- 撤回成功后，自动刷新 DDL 历史与当前状态。

## 3. 验证计划 (Verification Plan)

### 3.1 自动化测试 (Automated Tests)
- **SEC-06 越权写入拦截测试**: 修改 `tests/e2e/cases/08_polymorphic/test_role_view_segregation.py`，模拟 SalesUser 篡改请求尝试更新 `Balance` 字段，预期结果为 `403`。
- **Withdrawal 逻辑覆盖**: 创建 `tests/e2e/cases/ARCH-32/test_entity_withdrawal.py`，验证从 `Published` 回退到 `Withdrawn` 的全链路，包括数据库 Table 状态检查。

### 3.2 手动验证流程 (Manual Verification)
1.  **配置验证**: 修改 `appsettings.json` 中的 `AllowPhysicalDeletion` 为 `true`，验证撤回时 Table 物理删除。
2.  **依赖冲突验证**: 创建引用关系，尝试撤回被引用的父实体，验证前端报错提示的准确性。

## 4. SOP 铁律执行 (QA Compliance)

严格遵循 `STD-06` 五步法进行集成验证：
1.  **Clean**: 执行 `scripts/reset-db.ps1` 清理环境。
2.  **Verify**: 检查 `EntityDefinitions` 初始状态。
3.  **Start**: 启动 API 与 App。
4.  **Test**: 运行 E2E。
5.  **Shutdown**: 任务结束清理临时文件并同步 Git。

---
**核准**: 项目管理委员会 (PMC)
**存放路径**: `docs/plans/PLAN-26-ARCH-32-及-SEC-06-功能补完与安全性加固计划.md`
