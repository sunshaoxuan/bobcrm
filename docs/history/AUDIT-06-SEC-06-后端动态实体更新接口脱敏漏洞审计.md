# AUDIT-06: SEC-06 后端动态实体更新接口脱敏漏洞审计报告

**生效日期**: 2026-01-13
**状态**: 待纠正
**类型**: 安全审计
**责任人**: AI 架构审计员

---

## 1. 事件描述

在对 `ARCH-32` (Schema Evolution & Publishing) 进行审计期间，发现 `SEC-06` (后端字段过滤) 在动态实体更新接口 (`PUT /api/{entityPlural}/{id}`) 中未能有效执行。

**违规点/漏洞**:
1.  **权限旁路**: `DynamicEntityRouteEndpoints.cs` 的 `PUT` 分支直接接收客户端传入的 `payload.fields` 并通过 `ReflectionPersistenceService` 更新至数据库。
2.  **验证缺失**: 未调用 `FieldFilterService` 或 `IFieldPermissionService` 对待更新字段进行白名单校验。
3.  **结果**: 攻击者可以通过直接构造 API 请求来更新 UI 上无权查看或操作的敏感字段 (如 `Balance`, `Status` 等)，直接违反了 `SEC-06` “REST API 必须根据授权模板剥离响应数据且严格禁止未经授权字段披露/篡改”的要求。

## 2. 根因分析 (Root Cause Analysis - 5 Whys)

*   **Why 1?** 为什么用户能更新无权操作的字段？
    *   **答**: 因为后端 `PUT` 接口在执行持久化操作前，没有针对当前用户的模板权限进行字段白名单过滤。
*   **Why 2?** 为什么没有执行过滤？
    *   **答**: 在早期的 `SEC-06` 实施中，焦点在于“读操作脱敏” (Data Stripping)，动态实体的“写操作防御”被误认为可以通过 UI 隐藏字段来规避。
*   **Why 3?** 为什么会忽视 API 层的直接攻击风险？
    *   **答**: 缺乏统一的“安全操作闭环”规范。动态实体的短路由接口 (Short Route) 与静态编译接口采用了不同的权限验证逻辑，未能实现权限拦截器的全覆盖。
*   **Why 4?** 为什么逻辑没有对齐？
    *   **答**: `FieldFilterService` 设计之初主要服务于 JSON 返回值的过滤，虽然提供了 `ValidateWritePermissionsAsync` 方法，但未强制要求所有动态路由必须挂载此逻辑。
*   **Why 5?** 根本原因是什么？
    *   **答**: **架构设计未实现“默认安全 (Secure by Default)”**。动态实体系统在设计元注册时，未将字段级权限校验作为 CRUD 操作的原子过滤器强制集成在持久化中间件或 API 网关层。

## 3. 纠正措施 (Corrective Actions)

1.  **立即加固**: 在 `DynamicEntityRouteEndpoints.cs` 的 `PUT` 逻辑中，强制调用 `FieldFilterService.ValidateWriteFieldsAsync`。
2.  **强制 403**: 如果请求包含任何未授权字段，直接返回 `403 Forbidden`，不再“静默忽略”，以起到震慑与预警作用。
3.  **自动化测试**: 在 `test_role_view_segregation.py` 中增加“越权更新”测试用例，确保回归验证。

## 4. 预防措施 (Preventive Measures)

1.  **架构对齐**: 重新审查所有 `DynamicEntity` 自定义端点，确保其安全等级与 standard UI 路由对齐。
2.  **审计铁律**: 在后续的 `ARCH` 发布流程中，必须包含“读写对称性安全性检查”。

---
**核准**: 项目管理委员会 (PMC)
**导出路径**: `docs/history/AUDIT-06-SEC-06-后端动态实体更新接口脱敏漏洞审计.md`
