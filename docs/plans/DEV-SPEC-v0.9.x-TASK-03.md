# DEV-SPEC: v0.9.x-TASK-03 发布流程闭环 (Publishing Process Closure)

## 1. 任务综述
本规范定义了 OneCRM v0.9.0 里程碑中“发布流程闭环”的技术实施细节。目标是解决聚合对象 (AggVO) 的级联发布、全局权限拦截以及发布撤回的“最后一公里”问题。

## 2. 核心指令 (Context)
- **必须参考设计**: `docs/design/ARCH-01-实体元数据定义与生命周期设计.md`
- **必须参考计划**: `docs/plans/PLAN-15-v0.9.0-闭环开发计划.md` (Phase 3)
- **核心逻辑**: 发布主实体时，其引用的子实体（Lookup/AggVO）必须处于同步发布状态。

## 3. 技术要求 (Requirements)

### A. AggVO 级联发布 (Cascade Publishing)
- **目标文件**: `src/BobCrm.Api/Services/EntityPublishingService.cs`
- **修改内容**:
  - 增强 `PublishNewEntityAsync` 和 `PublishEntityChangesAsync`。
  - **自动识别**: 遍历实体的 `Fields`，识别所有依赖的 `LookupEntityName`。
  - **前置处理**: 如果依赖实体处于 `Draft` 或 `Withdrawn` 状态，尝试自动对其执行发布操作。
  - **策略控制**: 当实体作为“子实体”被级联发布时，记录 `Source=System`，并跳过 `EnsureTemplatesAsync`（由主实体负责或使用默认布局）。

### B. 全局权限拦截器 (Permission Interceptor)
- **目标文件**: `src/BobCrm.App/Components/Layout/MainLayout.razor` (或独立的 `PermissionService`)
- **修改内容**:
  - 实现在前端路由切换时的权限预检。
  - **逻辑**: 如果当前请求的是通过模板渲染的页面，则必须校验 `TemplateBinding` 或 `FormTemplate` 上的 `RequiredFunctionCode`。
  - **兜底**: 若无权限，重定向至 `403-Forbidden` 页面，而非仅仅在组件内隐藏。

### C. 发布撤回功能 (Withdrawal Functional)
- **目标文件**: `src/BobCrm.Api/Controllers/EntityDefinitionController.cs` & `EntityPublishingService.cs`
- **修改内容**:
  - 实现 `WithdrawAsync(Guid entityId)` 接口。
  - **逻辑**: 
    1. 将实体状态置为 `Withdrawn`。
    2. **物理/逻辑处理**: 根据配置决定是物理 `DROP TABLE` 为空表，还是仅在元数据层标记 `IsDeleted`。
    3. **引用检查**: 如果该实体被其他已发布的实体引用，则禁止撤回。

## 4. 质量门禁 (Quality Gates) - **强制性要求**

> [!IMPORTANT]
> **级联一致性测试**: 
> 1. 模拟“主实体(Draft) -> 子实体(Draft)”的场景，验证一次点击发布两个实体。
> 2. 验证“主实体已发布，子实体被撤回”时的系统稳定性（前端应优雅提示或回退）。

## 5. 验收标准 (Acceptance Criteria)
1. 发布一个包含 Lookup 字段的新实体，其引用的实体若为 Draft，会被同步发布。
2. 修改 `RequiredFunctionCode` 后，未授权用户通过 URL 访问该实体明细页会被拦截。
3. 实现“撤回”操作，状态同步更新，且受保护引用无法撤回。

---
**审批人 (Architect):** Antigravity
**发布日期:** 2025-12-23
