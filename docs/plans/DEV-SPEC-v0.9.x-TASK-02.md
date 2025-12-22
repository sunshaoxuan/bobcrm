# DEV-SPEC: v0.9.x-TASK-02 多态视图渲染 (Polymorphic View Rendering)

## 1. 任务综述
本规范定义了 OneCRM v0.9.0 里程碑中“多态视图”的技术实施细节。目标是实现根据实体的特定字段值（如 `Status`）动态选择不同的显示模板（DetailView/DetailEdit）。

## 2. 核心指令 (Context)
- **必须参考设计**: `docs/design/ARCH-22-标准实体模板化与权限联动设计.md`
- **必须参考计划**: `docs/plans/PLAN-15-v0.9.0-闭环开发计划.md` (Phase 2)
- **核心逻辑**: 当请求一个实体的明细页时，系统应检查是否有匹配该实体当前数据状态的绑定规则。

## 3. 技术要求 (Requirements)

### A. 领域模型扩展 (Domain Model)
- **目标文件**: `src/BobCrm.Api/Base/Models/TemplateStateBinding.cs`
- **修改内容**:
  - 新增 `MatchFieldName` (string): 用于判断条件的字段名（如 "Status"）。
  - 新增 `MatchFieldValue` (string): 匹配的目标值（如 "Draft"）。
  - 新增 `Priority` (int): 匹配优先级（值越大越优先，默认绑定优先级最低）。
- **约束**: 
  - 维持向后兼容：当 `MatchFieldName` 为空时，视为该状态的通用/默认绑定。
  - 更新唯一索引逻辑：支持同一 `EntityType` + `ViewState` 下的多条规则。

### B. 运行时匹配逻辑 (Runtime Matching)
- **目标文件**: `src/BobCrm.Api/Services/TemplateRuntimeService.cs` (或对应服务)
- **修改内容**:
  - 升级 `GetTemplateAsync` 接口，支持传入 `entityId` 或 `JObject` 数据。
  - **算法逻辑**:
    1. 获取该 `EntityType` + `ViewState` 下的所有绑定记录。
    2. 如果提供了数据，则遍历规则，按 `Priority` 降序检查 `MatchFieldName` 是否匹配 `MatchFieldValue`。
    3. 返回匹配成功的第一个 `TemplateId`；若无匹配，则返回 `IsDefault=true` 的默认绑定。

### C. 前端 PageLoader 适配
- **目标文件**: `src/BobCrm.App/Services/PageLoader.cs` (或组件逻辑)
- **修改内容**:
  - 在加载数据后，根据返回的实体状态，动态向后端请求最新的视图模板，支持页面由“普通详情”无缝切换为“审批专用模板”等。

## 4. 质量门禁 (Quality Gates) - **强制性要求**

> [!IMPORTANT]
> **单元测试覆盖率必须 > 90%**
> 1. **规则引擎测试**: 编写测试用例验证多种优先级下的匹配结果是否符合预期。
> 2. **回滚处理**: 确保数据缺失或字段不存在时，系统能优雅回退到默认模板。

## 5. 验收标准 (Acceptance Criteria)
1. 定义一个实体的两个详情模板：T1 (状态=Draft), T2 (默认)。
2. 当实体数据为 Draft 时，页面自动载入 T1。
3. 当手动修改数据状态后，页面刷新能正确加载 T2。
4. 单元测试全覆盖。

---
**审批人 (Architect):** Antigravity
**发布日期:** 2025-12-22
