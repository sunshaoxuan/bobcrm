# TEST-01: v1.0 全功能测试矩阵

> **生效日期**: 2026-01-08
> **状态**: 草稿
> **范围**: 平台全功能 (v1.0)
> **目标**: 标准化验收测试用例

本测试矩阵分解了 v1.0 的核心功能模块，作为 QA 验收的唯一依据。

## 模块 1: 实体定义 (Entity Definition)
**目标**: 验证元数据定义的完整性与合法性。

| ID | 特性 (Feature) | 场景 (Scenario) | 测试数据 / 操作 | 预期结果 |
|---|---|---|---|---|
| ENT-001 | 字段类型 | 支持所有基元类型 | 创建字段: String, Int32, Int64, Decimal, Bool, Date, DateTime, Guid | 保存成功，类型映射正确。 |
| ENT-002 | 约束条件 | 必填校验 | 设置 `IsRequired=true`。提交空值。 | API 拒绝 (400)。 |
| ENT-003 | 约束条件 | 字符串长度 | 设置 `Length=5000`。 | API 接受并持久化。 |
| ENT-004 | 实体引用 | 1:1 关联 | 创建 `RefField` -> 指向 `User`。 | 保存为 Lookup 控件。 |
| ENT-005 | 实体引用 | 1:N 组合 | 创建 `Items` -> 指向 `OrderItem`, 设为 `IsEntityRef=true`。 | 保存为 Collection。 |
| ENT-006 | 标准接口 | 内置接口 | 勾选 `Archive`, `Audit`, `Version`。 | 自动生成 Code, Name, CreatedBy 等字段。 |
| ENT-007 | 校验逻辑 | 重复字段名 | 添加同名 "Name" 字段两次。 | 报错 "Duplicate property name"。 |
| ENT-008 | 校验逻辑 | 保留字冲突 | 手动添加 "Id" 字段。 | 报错或忽略 "Id is reserved"。 |

## 模块 2: 热发布与演进 (Hot Publishing & Evolution)
**目标**: 验证 Schema 变更的正确性与数据安全性。

| ID | 特性 (Feature) | 场景 (Scenario) | 测试数据 / 操作 | 预期结果 |
|---|---|---|---|---|
| PUB-001 | 首次发布 | 建表 | 发布新实体 `Ticket`。 | 数据库中创建 `Tickets` 表。 |
| PUB-002 | 演进 | 加列 (Add Column) | 增加 `Priority` (Int)。重新发布。 | 列已添加，旧数据保留。 |
| PUB-003 | 演进 | 改长 (Modify Length) | `Title` 长度从 50 增至 100。重新发布。 | 列定义更新，旧数据无损。 |
| PUB-004 | 演进 | 安全缩减 | 减少 `Summary` 长度 (数据未溢出)。 | 发布成功。 |
| PUB-005 | 演进 | 危险缩减 | 减少 `Summary` 长度 (数据溢出)。 | **发布被阻断/警告**。 |
| PUB-006 | 演进 | 类型变更 | `Score` 从 Int 改为 String。 | **报错** (不支持类型变更)。 |
| PUB-007 | 级联 | 聚合根发布 | 发布 `Order`, `OrderItem` 处于 Draft。 | 两者均被发布，表均被创建。 |

## 模块 3: 模板系统 (Template System)
**目标**: 验证表单自动生成与所见即所得设计。

| ID | 特性 (Feature) | 场景 (Scenario) | 测试数据 / 操作 | 预期结果 |
|---|---|---|---|---|
| TPL-001 | 自动生成 | 默认布局 | 发布新实体。 | 自动生成 `DefaultDetail` & `DefaultList`。 |
| TPL-002 | 设计器 | 布局组件 | 拖拽 `2-Col Row`, `TabContainer`。 | 画布正确渲染。 |
| TPL-003 | 设计器 | 字段绑定 | 拖拽 `Title` 字段到画布。 | 控件绑定到 `Title` 属性。 |
| TPL-004 | 设计器 | 持久化 | 保存模板 -> 重新加载。 | 布局完全还原。 |
| TPL-005 | 运行时 | 输入控件 | 打开表单。检查控件类型。 | String->Input, Bool->Switch, Date->DatePicker。 |
| TPL-006 | 运行时 | 校验 UI | 提交必填字段为空。 | 控件变红，显示错误提示。 |

## 模块 4: 菜单与权限 (Menu & Security)
**目标**: 验证访问控制的颗粒度。

| ID | 特性 (Feature) | 场景 (Scenario) | 测试数据 / 操作 | 预期结果 |
|---|---|---|---|---|
| SEC-001 | 菜单定义 | 根节点 | 创建根节点 "HelpDesk"。 | 侧边栏显示该节点。 |
| SEC-002 | 菜单定义 | 子节点 | 创建子节点 "Tickets"。 | 侧边栏支持折叠/展开。 |
| SEC-003 | 角色管理 | 权限分配 | 角色 `Support`: 允许 `ticket.view`, 拒绝 `ticket.delete`。 | 分配保存成功。 |
| SEC-004 | 运行时 | 菜单过滤 | 以 `Support` 登录。 | "Settings" 菜单隐藏。"Tickets" 可见。 |
| SEC-005 | 运行时 | 路由守卫 | 直接访问 `/settings`。 | **403 Forbidden** (或跳转)。 |
| SEC-006 | 运行时 | 按钮守卫 | 在 Ticket 列表页。 | "Delete" 按钮隐藏或禁用。 |

## 模块 5: 用户体验 (User Experience)
**目标**: 验证业务操作的流畅性。

| ID | 特性 (Feature) | 场景 (Scenario) | 测试数据 / 操作 | 预期结果 |
|---|---|---|---|---|
| UX-001 | 列表 | 搜索/过滤 | 搜索 "Bug"。 | 表格仅显示匹配行。 |
| UX-002 | 列表 | 分页 | 20 条记录, PageSize=10。 | 第 2 页可访问。 |
| UX-003 | 详情 | 引用选择器 | 点击 `UserId` 放大镜图标。 | 弹出选择模态框 -> 选中用户 -> 回填。 |
| UX-004 | 状态 | 生命周期 | New -> Draft -> Save -> Edit -> Update。 | 状态流转正确并持久化。 |
