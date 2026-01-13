# FIX-08: Batch 8 - 平台全链路功能对齐与验收指令

**生效日期**: 2026-01-13
**状态**: 待执行

---

## 任务背景
虽然 Batch 6 实现了全量回归，但针对“平台全功能”（特别是权限闭环与多态渲染）的覆盖仍有死角。为了达到 [STD-08](file:///c:/workspace/bobcrm/docs/process/STD-08-v1.0-平台全功能验收标准.md) 的验收标准，必须执行本指令。

## 目标任务 (Goals)

### 1. 权限与菜单全链路对齐 (Security Closed Loop)
*   **任务**: 开发并执行一个新的 E2E 用例 `tests/e2e/cases/02_user_management/test_user_005_security_loop.py`。
*   **流程**:
    1.  **角色创建**: 使用 API 创建一个名为 `TestSalesRole` 的角色。
    2.  **菜单绑定**: 创建一个功能节点（菜单），将其关联到一个自定义实体（如 `Lead`）的 **Template Binding**。
    3.  **权限分配**: 为 `TestSalesRole` 分配该菜单的访问权限。
    4.  **用户分配**: 创建一个测试用户 `sales_user`，并授予 `TestSalesRole` 角色。
    5.  **前端验证**: 以 `sales_user` 登录，验证：
        *   侧边栏是否出现了该菜单。
        *   点击菜单是否能正确渲染对应的实体 List 页面。
        *   **安全拦截**: 尝试访问 admin 的系统设置 URL，验证是否被 403 或重定向。

### 2. 模板多态显示验证 (Polymorphic Rendering)
*   **任务**: 验证不同场景下的模板切换逻辑。
*   **流程**:
    1.  为一个实体创建两个模板：`Template_Standard` (包含所有字段) 和 `Template_Simple` (仅包含 Name)。
    2.  创建两个 **Template Binding**，根据角色或条件划分。
    3.  验证 E2E 在切换用户身份后，同一实体的详情页渲染出的 DOM 结构是否符合对应的模板定义。

### 3. 模板全生命周期完善 (Template Lifecycle)
*   **任务**: 
    1.  修改已发布的实体字段（例如给 `Product` 增加 `Weight` 字段）。
    2.  触发“增量发布”，验证物理表更新。
    3.  手动点击“重新生成模板”，验证 `Weight` 字段自动出现在默认模板中。
    4.  在设计器中将 `Weight` 移动到第一行，保存并验证运行态位置更新。

## 验收标准 (Definition of Done)
1. **全链路 Pass**: `test_user_005_security_loop.py` 在 `FIX-06` 的清理环境下 100% 通过。
2. **文档对齐**: 更新 `AUDIT` 报告，标注 `STD-08` 中所有 ENT/TMP/SEC 项均为 Passed。
3. **零残留**: 验证 `sales_user` 产生的临时元数据被完全清理。

---
**核准人**: Antigravity (Architect)
