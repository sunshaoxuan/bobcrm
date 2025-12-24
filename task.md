# v0.9.x 开发计划

## Phase 1: 关系建模与基础 (已完成)
- [x] TASK-01: `FieldMetadata` 增加 `Lookup` 关系及 DDL 生成与校验。

## Phase 2: 多态视图渲染 (当前任务)
- [x] TASK-02: 扩展 `TemplateStateBinding` 支持 `MatchFieldName/Value/Priority`。
- [x] TASK-03: `TemplateRuntimeService` 实现优先级规则匹配引擎（支持 `EntityId/EntityData`）。
- [x] TASK-04: 前端 `PageLoader` 接入动态匹配模板。

## Phase 3: 发布流程闭环 (已完成)
- [x] TASK-05: 增强 `EntityPublishingService` 支持 AggVO 级联发布。
- [x] TASK-06: 权限拦截器集成与撤回发布功能。

## Phase 4: API 响应契约治理 (已完成)
- [x] TASK-07: 统一响应包装模型，移除 redundancy (PLAN-17)。
- [x] TASK-08: 消除 `EntityDefinition/User/DynamicEntity` 中的匿名对象。
- [x] TASK-09: 补全全量 API 端点的 Swagger 元数据标注。
## Phase 5: 系统治理与可观测性 (已完成)
- [x] TASK-10: 实现基于 ChangeTracker 的全量审计日志体系 (SNAPSHOT/DIFF)。
- [x] TASK-11: 集成健康检查 (Health Check) 与系统状态诊断 API。
- [x] TASK-12: 强化异常处理逻辑，实现 TraceId 链路追踪展示。

## Phase 6: 治理闭环与一致性验收 (已完成)
- [x] TASK-13: 全量端点契约对齐扫描与残留匿名对象清除。
- [x] TASK-14: Swagger Schema 命名冲突修复与 DTO 定义精化。
- [x] TASK-15: 测试框架全量适配 SuccessResponse 包装。
