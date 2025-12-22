# PLAN-15: v0.9.0 闭环开发计划

## 1. 目标
完成实体自定义 -> 关系建模 -> 模板绑定 -> 多态展示 -> 权限闭环的最后一公里开发。

## 2. 关键任务拆解

### Phase 1: 关系建模与发布增强
*   **TASK-01**: 在 `FieldMetadata` 中显式支持 `Lookup` 配置，并更新 `PostgreSQLDDLGenerator` 以支持 1:1 外键。
*   **TASK-02**: 实现 N:N 关系的“自动中间实体”生成建议。
*   **TASK-08**: 实现 `EntityDefinitionHistory` 存储与回滚逻辑，支持“发布快照”。
*   **TASK-09**: 重构 `DDLGenerator` 接口，支持基于“设计元”的抽象描述分发，提升多数据库兼容性。

### Phase 2: 多态视图渲染
*   **TASK-03**: 扩展 `TemplateStateBinding` 数据模型，支持 `SelectionCriteria`。
*   **TASK-04**: 升级 `TemplateRuntimeService`，在获取模板时支持 `entityId` 参数并执行“状态-模板”匹配逻辑。
*   **TASK-05**: 前端 `PageLoader` 接入动态匹配 API，实现基于状态的模板自动切换。

### Phase 3: 发布流程闭环
*   **TASK-06**: 增强 `EntityPublishingService`，支持 AggVO 的级联发布功能：
    *   自动识别并发布处于 Draft/Withdrawn 状态的被引用实体。
    *   **策略控制**：当作为子实体被动发布时，跳过模板自动生成。
*   **TASK-07**: 在 `MainLayout` 中实现权限拦截器，确保通过模板路径访问时严格校验 `RequiredFunctionCode`。
*   **TASK-10**: 实现“撤回发布”功能（支持逻辑屏蔽与物理 DROP）。

## 3. 验收标准
1.  用户可定义带 Lookup 字段的实体，发布后在 UI 自动识别为下拉选择器。
2.  AggVO 实体的列表页自动生成，且能通过点击行跳转到对应的 AggVO 详情页。
3.  同一实体的明细页在不同状态（Status）下展示不同的 Form 布局。
4.  未授权用户无法通过 URL 直接访问特定模板驱动的页面。

## 4. 时间轴
*   **第 1 周**: Phase 1 & 2 (后端核心逻辑)。
*   **第 2 周**: Phase 2 (前端集成) & Phase 3 (自动化与清理)。
