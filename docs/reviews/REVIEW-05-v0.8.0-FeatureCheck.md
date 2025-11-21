# REVIEW-05: v0.8.0 功能验证 (最终)

**日期**: 2025-11-21
**评审人**: Antigravity
**范围**: 验证 PLAN-03 中规划的功能 (TemplateService, 仪表盘 MVP, 字段级安全)。

## 1. 执行摘要

经过第二次详尽的验证，v0.8.0 的状态为 **部分完成**。

*   ✅ **架构重构**: **通过**。`TemplateService` 已完全实现。
*   ⚠️ **字段级安全**: **部分**。后端（实体、服务、API）已实现，但 **UI 缺失**。
*   ❌ **仪表盘引擎 MVP**: **失败**。完全缺失。仅存在一个静态的 `Home.razor`。

## 2. 详细发现

### 2.1 架构: TemplateService 重构
*   **状态**: ✅ **完成**
*   **证据**:
    *   `src/BobCrm.Api/Services/TemplateService.cs` 包含所有业务逻辑。
    *   `src/BobCrm.Api/Endpoints/TemplateEndpoints.cs` 简化为薄控制器层。

### 2.2 字段级安全
*   **状态**: ⚠️ **仅后端**
*   **证据**:
    *   ✅ **后端**: `FieldPermission` 实体、`FieldPermissionService` 和 `FieldPermissionEndpoints` 存在并在 `Program.cs` 中注册。
    *   ❌ **前端**: `RolePermissionTree.razor` 仅处理 `FunctionIds`。它 **没有逻辑** 来显示或保存 `FieldPermissions`。`DefaultTemplateGenerator` 添加了一个 `permtree` 组件，但底层组件尚不支持该功能。

### 2.3 仪表盘引擎 MVP
*   **状态**: ❌ **缺失**
*   **证据**:
    *   `Home.razor` 是一个带有硬编码 `DashboardMetric` 记录的静态页面。
    *   `AppDbContext` 中没有 `DashboardTemplate` 实体。
    *   未找到 `DashboardDesigner` 组件。

---

## 3. 结论与建议

由于缺少字段安全 UI 和仪表盘功能，v0.8.0 版本 **尚未准备好** 进行全面部署。

**建议计划**:
1.  **接受** 架构重构和字段安全后端。
2.  **结转** 字段安全 UI 和仪表盘引擎到 v0.9.0。
3.  **立即行动**: 更新 `RolePermissionTree.razor` 以支持字段权限（唾手可得的成果）。
