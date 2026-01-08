# TEST-04: Batch 3 业务闭环验证

> **关联计划**: [PLAN-24](file:///c:/workspace/bobcrm/docs/plans/PLAN-24-v1.0-全功能测试执行方案.md)
> **测试范围**: SEC-001~006, UX-001~004
> **前置依赖**: Batch 2 Passed

## 1. 环境准备
*   **Fixture**: `helpdesk_full_setup` (Entity + Template + Menu Configured)
*   **Actors**: `AdminUser`, `AgentUser`

## 2. 测试用例

### Case 3.1: 菜单与导航 (Menu & Navigation)
**目标**: 验证动态菜单构建。
*   **操作**: Admin 添加根菜单 "HelpDesk" -> 子菜单 "Tickets" (路由: `/app/ticket`)。
*   **验证**:
    *   `AgentUser` 登录。
    *   侧边栏显示 "HelpDesk" 分组。
    *   点击 "Tickets" 导航至 `/app/ticket`。

### Case 3.2: 角色访问控制 (Role-Based Access Control)
**目标**: 验证安全边界。
*   **设置**: 角色 `Agent` 拥有 `ticket.view`。**无** `ticket.delete` 或 `settings.view`。
*   **Action 1 (菜单过滤)**: 验证 "Settings" 菜单**不可见**。
*   **Action 2 (硬路由拦截)**: 手动浏览器输入 `/settings`。
    *   **预期**: 403 Forbidden 页面 (系统拦截导航)。
*   **Action 3 (按钮守卫)**: 进入 Ticket 列表。选中行。
    *   **预期**: "Delete" 按钮隐藏或禁用。

### Case 3.3: 引用关联 (Reference Association)
**目标**: 验证复杂数据交互。
*   **场景**: 分配工单给用户。
*   **操作**:
    1.  打开 Ticket。点击 `AssignedTo` 放大镜。
    2.  模态框打开，列出 Users。
    3.  选择 `AgentUser`。
*   **验证**:
    *   输入框回填 "AgentUser"。
    *   Save -> DB `AssignedTo` 列存储 `AgentUser.Id`。

### Case 3.4: 列表操作 (List Operations)
**目标**: 验证 DataGrid 能力。
*   **设置**: 预置 15 条 Tickets。PageSize=10。
*   **操作**:
    1.  检查页脚: "Page 1 of 2"。
    2.  搜索 "Critical"。
    3.  **预期**: 表格刷新，仅显示匹配行。

## 3. 准出标准
*   安全机制(前端+后端)有效拦截越权访问。
*   业务流 ("Create Ticket -> Assign -> Search") 跑通。
