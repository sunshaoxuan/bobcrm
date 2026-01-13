# AUDIT-07: PLAN-25 多态视图渲染先序评审报告

**日期**: 2026-01-13
**类型**: 先序架构评审 (Pre-order Review)
**状态**: 已完成 (发现缺陷)
**依据**: `ARCH-31`, `PLAN-25`, `STD-02`

---

## 1. 评审范围与方法

本次评审针对 `PLAN-25` (多态视图渲染补完) 的全生命周期进行回溯，包括：
1.  **需求对齐**: 业务场景 (草稿、审批、档案驱动) 是否被定义。
2.  **设计合规**: `ARCH-31` 的优先级算法与存储模型是否合理。
3.  **代码实现**: `TemplateRuntimeService` 与 `StateBindingEditor` 的逻辑一致性。
4.  **验证闭环**: E2E 测试是否覆盖了所有设计场景。

## 2. 评审发现 (Findings)

### 2.1 需求与设计一致性 (Requirement vs Design)
- **结论**: **🟢 吻合**。
- **详述**: `ARCH-31` 提出的“优先级链 (Explicit > Rule > Default)”在设计中清晰明确，满足了菜单驱动、数据驱动及兜底渲染的业务诉求。

### 2.2 设计与实现一致性 (Design vs Implementation)
- **结论**: **🟢 吻合**。
- **详述**: 
  - **后端**: `TemplateStateBindingRuleEngine.cs` 完美实现了对动态字段 (`fields` 数组) 和静态属性的兼容匹配。
  - **前端**: `StateBindingEditor.razor` 已集成至 `FormDesigner` 属性面板，并支持 `Lookup` 档案选择器，解决了“工具滞后”问题。
  - **运行时**: `PageLoader` 已实现保存后的 `LoadDataAsync` 重加载，确保视图能随数据变更动态“位移”。

### 2.3 开发与测试闭环性 (Dev vs QA)
- **结论**: **🔴 存在缺口 (Critical Gap)**。
- **详述**: 
  - 现有的 `test_role_view_segregation.py` 仅验证了**基于角色/菜单**的多态性。
  - **缺失验证**: 未包含“数据驱动 (Data-driven)”的多态切换测试。例如：修改实体状态字段后，验证 UI 是否自动从“编辑版”切换为“审批版”。
  - **风险**: 规则引擎的字段匹配逻辑在复杂类型 (Decimal, Lookup) 下的边界情况未通过 E2E 验证。

## 3. 5 Whys 根因分析 (针对测试缺口)

*   **Why 1?** 为什么测试用例没有覆盖数据驱动场景？
    *   **答**: 因为早期的测试关注点在于 `SEC-05/06` 的权限拦截，多态性被视为权限的一种表现形式。
*   **Why 2?** 为什么忽视了规则引擎的独立验证？
    *   **答**: 开发过程中认为 Unit Test 已经覆盖了 `RuleEngine` 逻辑，忽视了前端 `PageLoader` 与后端 `RuntimeService` 联动的集成校验。

## 4. 改进措施 (Corrective Actions)

1.  **补强 E2E**: 将在 `PLAN-26` 或独立任务中增加 `test_polymorphic_data_driven.py`。
2.  **验证档案匹配**: 特别增加对 `Lookup` 字段（档案 ID）配置规则的 E2E 验证。

---
**核准**: 项目管理委员会 (PMC)
**导出路径**: `docs/history/AUDIT-07-PLAN-25-多态视图渲染先序评审.md`
