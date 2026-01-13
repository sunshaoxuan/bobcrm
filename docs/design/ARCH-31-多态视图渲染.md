# ARCH-31: 多态视图渲染实施计划 (Polymorphic View Rendering)

> **状态**: 此前为 DEV-SPEC-v0.9.x-TASK-02
> **类型**: 实施计划 (Implementation Plan)
> **目标**: 根据实体状态动态加载不同视图模板

---

## 1. 概述

本计划详细说明"多态视图渲染"的实施步骤。该功能允许系统根据实体的特定字段值（如 `Status`）动态选择不同的显示模板（DetailView/DetailEdit）。

**核心场景**：
- **场景A (草稿态)**: 当 `Order` 状态为 `Draft` 时，显示"包含大量编辑控件"的草稿模板。
- **场景B (审批态)**: 当 `Order` 状态为 `InReview` 时，自动切换为"只读+审批按钮"的审批模板。
- **场景C (菜单驱动)**: 通过"财务菜单"进入看到财务视图，通过"销售菜单"看到详情视图。
- **场景D (档案式匹配-Theme 1)**: 当客户属于特定"重要档案"（如 VIP 记录）时，自动切换为高级黑金模板。

## 2. 架构设计

### 2.1 数据库架构 (Schema)

新增 `TemplateStateBinding` 表，用于定义状态到模板的映射规则。

```markdown
| Column          | Type    | Description                          |
|-----------------|---------|--------------------------------------|
| Id              | Guid    | PK                                   |
| EntityType      | String  | 目标实体类型 (如 "Order")             |
| ViewState       | String  | 视图状态 (如 "Detail", "Edit")        |
| MatchFieldName  | String? | 匹配字段名 (如 "Status"，空表示默认)   |
| MatchFieldValue | String? | 匹配值 (如 "Draft"，空表示默认)        |
| TemplateId      | Guid    | 关联的模板 ID                        |
| Priority        | Int     | 优先级 (值越大越优先)                 |
| IsDefault       | Bool    | 是否为该 ViewState 的默认兜底模板      |### 2.2 共享模板与状态覆盖 (Shared Templates & State Overrides) [NEW]

为了支持“一套模板，多种表现”，抽象出以下增强设计：

#### 1. 状态感知的渲染上下文 (State-Aware Context)
在 `FormRuntimeContext` 中增加 `ViewState` 属性。系统根据 URL 参数 (`usage`) 或 `TemplateStateBinding` 的匹配结果，将当前视图的状态（如 `Create`, `DetailEdit`）注入上下文。

#### 2. 控件级属性覆盖 (Widget-Level Overrides)
在 `DraggableWidget` 基类中增加 `StateOverrides` 模型：
```json
{
  "Code": "CustomerBalance",
  "Visible": true,
  "ReadOnly": false,
  "StateOverrides": {
    "Create": { "Visible": false },
    "DetailView": { "ReadOnly": true }
  }
}
```
渲染引擎在解析控件属性时，优先检查 `StateOverrides[currentViewState]`，实现同一份 Layout JSON 在不同状态下的差异化表现。

#### 3. 绑定模型扩展 (Binding Multi-selection)
`TemplateStateBinding` 的 `ViewState` 字段支持存储逗号分隔的状态列表（或通过 UI 勾选多个状态映射到同一模板 ID），从而减少冗余配置。

`TemplateRuntimeService.BuildRuntimeContextAsync` 执行以下优先级过滤：

**输入**: `EntityType`, `ViewState`, `EntityData`, `MenuNodeId?`, `TemplateId?`

**优先级算法 (Priority Chain)**:
1.  **显式上下文 (mid)**: 优先级最高。直接从菜单节点加载。
2.  **规则引擎 (Rule Engine)**: 
    - 加载该实体 + 视图状态下的所有 `TemplateStateBinding`。
    - **档案式匹配 (Entity Record)**: 若规则涉及 Lookup 字段，则比对记录 ID。
    - 按 `Priority` 降序遍历并执行 `EntityData` 条件匹配。
3.  **默认回退**: 兜底 `IsDefault=true` 绑定。
4.  **安全聚合 (Inference)**: API 级别字段并集过滤。

---

## 3. 详细实施步骤 (Implementation Steps)

### Phase 1: 领域模型与存储 (预计 2h)

1.  **创建实体**: `src/BobCrm.Api/Base/Models/TemplateStateBinding.cs`
2.  **迁移脚本**: 使用 EF Core 生成 Migration。
3.  **DTO定义**:
    - `TemplateStateBindingDto`: 用于管理端 CRUD。
    - `CreateTemplateStateBindingRequest`: 创建请求。

### Phase 2: 后端服务与规则引擎 (预计 4h)

1.  **服务层 (`TemplateStateBindingService`)**:
    - 实现 CRUD 逻辑。
    - 验证逻辑：确保同一 ViewState 下只有一个 `Default`。
2.  **运行时改造 (`TemplateRuntimeService`)**:
    - 修改 `GetTemplateAsync` 签名，增加 `object? entityData` 参数。
    - 实现上述 2.2 节的匹配算法。
3.  **API端点升级 (`TemplateEndpoints`)**:
    - `/api/templates/runtime/{entityType}/{viewState}` 接受 `entityId` 参数。
    - 后端先根据 `entityId` 加载数据，再调用匹配算法。

### Phase 3: 前端适配 (预计 4h)

1.  **PageLoader参数传递**:
    - 修改 `PageLoader.razor.cs`。
    - 在 load 数据的同时（或之后），重新请求模板元数据。
    - **优化**: 后端 `/api/entities/{type}/{id}` 响应中直接带上 `RecommendedTemplateId`，避免二次请求。

2.  **视图动态切换**:
    - 当数据保存并导致状态变更时（Draft -> Submitted），前端需检测到模板变更信号，并重新渲染 UI。

---

## 4. 验收标准 (Acceptance Criteria)

1.  **配置验证**:
    - 对 `Order` 实体配置两条规则：
        - Rule 1: Status="Draft" -> Template A (Priority 10)
        - Rule 2: Default -> Template B (Priority 0)
2.  **运行时验证**:
    - 打开 Draft 状态的 Order -> 显示 Template A。
    - 修改 Status 为 "Paid" 并保存 -> 页面自动刷新为 Template B。
3.  **单元测试**:
    - 覆盖规则匹配逻辑，包括空值、大小写敏感性、优先级排序。

---

## 5. 参考文档

- `ARCH-22-标准实体模板化与权限联动设计.md`
