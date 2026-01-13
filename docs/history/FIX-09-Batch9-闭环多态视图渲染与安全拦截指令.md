# FIX-09: Batch 9 - 闭环多态视图渲染与安全拦截指令

**生效日期**: 2026-01-13
**状态**: 待执行

---

## 1. 任务背景
在 [AUDIT-GAP-01](file:///c:/workspace/bobcrm/docs/reviews/AUDIT-GAP-01-v1.0-目标对齐偏差分析.md) 中，我们识别出 PageLoader 与 菜单/多态上下文 之间存在链路断裂。
本指令集旨在通过代码级别的修改，彻底闭环“权限-菜单-模板-数据”的平台全链路。

## 2. 核心修改指令 (Development Tasks)

### 2.1 [FRONTEND] PageLoaderContext 级联支持
*   **目标文件**: `src/BobCrm.App/ViewModels/PageLoaderViewModel.cs`
*   **修改内容**:
    1.  在 `LoadDataAsync` 方法签名中增加可选参数 `int? templateId = null` 和 `string? viewState = null`。
    2.  前端路由跳转时（如 `MainLayout` 或 `NavMenu`），从 URL QueryString 中提取 `tid` (TemplateId) 或 `vs` (ViewState)。
    3.  在调用 `_templateRuntime.GetRuntimeAsync` 时，将这些参数传递给后端。
*   **逻辑控制**: 如果 URL 明确指定了 `tid`，PageLoader 应强行要求后端返回该模板，并在权限校验失败时显示“无权访问该视图”。

### 2.2 [BACKEND] TemplateRuntime 强指定逻辑
*   **目标文件**: `src/BobCrm.Api/Services/TemplateRuntimeService.cs`
*   **修改内容**:
    1.  更新 `BuildRuntimeContextAsync`，使其优先检查 `TemplateRuntimeRequest` 是否携带了 `TemplateId`。
    2.  如果指定了 `TemplateId`，则直接通过 `_db.FormTemplates` 加载，并跳过 `ApplyPolymorphicViewBindingAsync`。
    3.  **核心安全校验**: 在返回结果前，必须调用 `_accessService.EnsureFunctionAccessAsync` 校验用户对该特定模板绑定的访问权。

### 2.3 [SECURITY] 后端动态数据字段过滤
*   **目标文件**: `src/BobCrm.Api/Services/DynamicEntityService.cs`
*   **修改内容**:
    1.  在 `GetByIdAsync` 返回数据前，调用 `IFieldPermissionService` 根据“当前使用的模板”对 `JsonElement` 或 DTO 进行字段剔除。
    2.  确保用户无法通过 F12 查看到模板中隐藏的敏感字段数据。

## 3. 验收验收 E2E 测试用例 (TC-CORE-CLOSURE)

### 3.1 跨角色多态测试 `tests/e2e/cases/08_polymorphic/test_role_view_segregation.py`
1.  **准备**: 创建实体 `Account`。
2.  **模板 A (Admin)**: 包含字段 `Balance`, `Name`。关联菜单 `M_ADMIN`。
3.  **模板 B (Sales)**: 仅包含字段 `Name`。关联菜单 `M_SALES`。
4.  **步骤**:
    *   以 `SalesUser` 登录，点击 `M_SALES`。验证页面**仅显示** `Name` 字段，且 API 返回的原始 JSON 中**不包含** `Balance`。
    *   手动尝试修改 URL 参数为 `tid=[AdminTemplateID]`，验证系统返回 `403 Forbidden`。

## 4. 交付要求
1. **代码同步**: 完成后必须先执行全量回归 (FIX-06)，确保未破坏现有功能。
2. **文档更新**: 在 `task.md` 中标记 Phase 8 为 Passed。

---
**核准人**: Antigravity (Architect)
