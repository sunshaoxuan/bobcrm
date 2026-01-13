# ARCH-32: 发布流程闭环实施计划 (Publishing Process Closure)

> **状态**: 此前为 DEV-SPEC-v0.9.x-TASK-03
> **类型**: 实施计划 (Implementation Plan)
> **目标**: 完善实体发布生命周期的"最后一公里"（级联发布、撤回、权限拦截）

---

## 1. 概述

本计划旨在解决实体发布流程中的三个关键痛点：
1.  **依赖地狱**: 发布主实体时，引用的子实体（如 Lookup）未发布导致报错。
2.  **权限漏洞**: 实体发布后，缺少前端路由级别的权限强制检查。
3.  **后悔药**: 缺少将已发布实体"撤回"到草稿状态的机制。

---

## 2. 详细实施步骤 (Implementation Steps)

### Phase A: 级联发布机制 (Cascade Publishing)

**目标**: 当发布实体 A 时，自动检测其引用的实体 B/C，若它们处于非发布状态，则一并发布。

**实施细则**:
1.  **依赖分析算法 (`EntityDependencyService`)**:
    - 输入: `EntityDefinition` (待发布)
    - 逻辑: 遍历 `Fields`，找出所有 `IsEntityRef=true` 的字段。
    - 递归收集: 获取这些 Field 指向的 `ReferencedEntityId`。
2.  **增强发布服务 (`EntityPublishingService`)**:
    - 在 `PublishEntityAsync` 入口处，调用依赖分析。
    - 过滤出状态为 `Draft` 或 `Withdrawn` 的依赖实体。
    - **递归发布**:
        - 针对每个依赖实体，调用 `PublishEntityAsync`。
        - 标记参数 `isCascade=true` (用于跳过非必要的模板生成步骤，仅发布元数据)。
3.  **安全检查**:
    - 检测循环依赖（A引用B，B引用A）。若检测到，视为同一批次发布。

### Phase B: 全局权限拦截器 (Permission Interceptor)

**目标**: 防止用户通过直接输入 URL 访问无权限的实体页面。

**实施细则**:
1.  **元数据扩展**:
    - 确保 `FormTemplate` 或 `EntityDefinition` 包含 `RequiredFunctionCode` 字段（如 `sales.orders.view`）。
2.  **前端拦截 (`MainLayout.razor` / `PermissionService`)**:
    - 监听 `NavigationManager.LocationChanged` 事件。
    - **逻辑**:
        - 解析目标 URL（如 `/app/orders/list`）。
        - 匹配对应的 `FunctionNode`。
        - 检查当前用户是否拥有该 FunctionCode。
    - **处置**:
        - 若无权限，立即重定向至 `/403` 或显示 `AntDesign.Message.Error("无权访问")`。
4.  **后端深层防御 (SEC-05/06)**:
    - **API 强制裁剪**: 动态实体路由端点 (`DynamicEntityRouteEndpoints`) 必须根据权限对返回的 `fields` 执行全量剪裁。
    - **脱敏逻辑**: 严禁返回模板中未定义的字段，除非该字段为系统核心字段 (Id/Code/Name)。

### Phase C: 发布撤回功能 (Withdrawal)

**目标**: 允许将 `Published` 实体回退到 `Withdrawn` (类似 Draft) 状态。

**实施细则**:
1.  **API 端点**: `POST /api/entities/{id}/withdraw`
2.  **业务逻辑**:
    - **引用检查**: 检查是否有*其他已发布实体*引用了当前实体。若有，**禁止撤回**并报错（"被 Customer 实体引用，无法撤回"）。
    - **状态更新**: 将 EntityStatus 更新为 `Withdrawn`。
    - **Schema 处理**: 
        - 策略 A (软撤回): 仅标记元数据，数据库 Table 保留（默认）。
        - 策略 B (硬撤回): 支持物理 `DROP TABLE` 或 `DROP COLUMN`。
        - **本次实施支持配置化演化**: 默认采用策略 A，但可通过配置 `AllowPhysicalDeletion` 开启策略 B。
3.  **前端适配**:
    - 在实体定义详情页，为"已发布"实体增加"撤回"按钮（需二次确认）。

### Phase D: 安全加固 (Security Hardening - SEC-06)

**目标**: 确保动态实体 `PUT` 接口强制执行字段级权限过滤，防止越权写入。

**实施细则**:
1.  **API 拦截 (`DynamicEntityRouteEndpoints`)**:
    - 在 `UpdateDynamicEntity` 中注入 `FieldFilterService.ValidateWriteFieldsAsync`。
    - 若请求包含无权字段，立即返回 `403 Forbidden`。
2.  **前端适配**:
    - 撤回按钮需增加关联风险提示（检查引用链）。

---

## 3. 验收标准 (Acceptance Criteria)

1.  **级联发布测试**:
    - 场景: 创建 Entity A (引用 B), Entity B (Draft).
    - 操作: 点击发布 A.
    - 预期: A 和 B 均变为 `Published` 状态。
2.  **权限拦截测试**:
    - 场景: 用户无 `sales.orders` 权限。
    - 操作: 浏览器地址栏输入 `/app/sales/orders`.
    - 预期: 跳转 403 页面。
3.  **撤回测试**:
    - 场景: 撤回已被引用的实体。
    - 预期: 报错提示引用关系。
    - 场景: 撤回无引用实体。
    - 预期: 状态变为 Withdrawn，前端显示"重新发布"按钮。
4.  **安全写入测试 (SEC-06)**:
    - 场景: 销售员尝试修改"审批状态"字段 (ReadOnly)。
    - 预期: API 返回 403 Forbidden，前端报错提示。
5.  **数据驱动验证 (Polymorphism)**:
    - 场景: 修改 `Status` 触发模板切换。
    - 预期: 页面无刷新或自动刷新加载新布局。

---

## 4. 参考文档

- `ARCH-01-实体元数据定义与生命周期设计.md`
