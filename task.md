# v0.9.x 开发计划

## Phase 1: 关系建模与基础 (已完成)
- [x] TASK-01: `FieldMetadata` 增加 `Lookup` 关系及 DDL 生成与校验。

## Phase 2: 多态视图渲染 (当前任务)
- [x] TASK-02: 扩展 `TemplateStateBinding` 支持 `MatchFieldName/Value/Priority`。
- [x] TASK-03: `TemplateRuntimeService` 实现优先级规则匹配引擎（支持 `EntityId/EntityData`）。
- [x] TASK-04: 前端 `PageLoader` 接入动态匹配模板。

## Phase 3: 发布流程闭环
- [x] TASK-05: 增强 `EntityPublishingService` 支持 AggVO 级联发布。
- [x] TASK-06: 权限拦截器集成与撤回发布功能。
