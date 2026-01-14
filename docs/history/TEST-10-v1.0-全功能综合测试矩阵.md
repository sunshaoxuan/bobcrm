# TEST-01: v1.0 全功能综合测试矩阵 (Final Master Matrix)

> **生效日期**: 2026-01-13
> **版本**: v1.0 (Final)
> **状态**: 已准核 (STD-02 合规)
> **目标**: 100% 平台核心功能路径覆盖

---

## 1. 实体引擎 (Platform Core - PC)
验证实体作为系统“生长基石”的完备性。

| ID | 特性 | 测试点 (Checkpoint) | 验证方法 | 预期结果 |
|---|---|---|---|---|
| PC-001 | 实体定义 | [String, Int32, Int64, Decimal, Bool, Date, DateTime, Guid] 全类型创建 | API + DB | 保存成功，物理表字段类型精确匹配。 |
| PC-002 | 约束校验 | 必填项 (`IsRequired`) 拦截 | API (400) | 提交空数据时被拦截并返回 I18n 错误信息。 |
| PC-003 | 字段长度 | 超长字符串持久化与溢出校验 | Integration | 设置 5000 长度，前端输入 5001 字符，验证拦截。 |
| PC-004 | 实体关联 | 1:1 查找 (`Lookup`) 关联定义 | UI + API | 创建关联字段成功，引用 ID 指向正确。 |
| PC-005 | 实体关联 | 1:N 级联组合 (`Collection`) 定义 | UI + API | 创建子表单定义，元数据映射正确。 |
| PC-006 | 热发布 | 首次物理建表 (`CREATE TABLE`) | SQL Audit | 发布后数据库中物理表结构与元数据 100% 一致。 |
| PC-007 | 演进发布 | 非破坏性加列 (`ADD COLUMN`) | SQL Audit | 已有数据行不受影响，新记录支持新字段读写。 |
| PC-008 | 演进发布 | 字段重命名与属性变更 | SQL Audit | 验证映射关系更新，物理表状态符合配置。 |
| PC-009 | 发布撤回 | **Entity Withdrawal** (ARCH-32) | UI + SQL | 撤回后元数据失效，物理表按配置（物理/逻辑）处理。 |

## 2. 模板引擎 (UI Engine - UE)
验证“所见即所得”的配置弹性与运行时还原度。必须对每个组件进行独立测试。

| ID | 特性 | 测试点 (Checkpoint) | 验证方法 | 预期结果 |
|---|---|---|---|---|
| ID | 特性 | 测试点 (Checkpoint) | 验证方法 | 预期结果 |
|---|---|---|---|---|
| **UE-001** | 自动生成 | DefaultList 模板生成 | UI (List) | 无需配置，列表显示所有字段。 |
| **UE-002** | 自动生成 | DefaultDetail 模板生成 | UI (Detail) | 详情页自动平铺所有字段。 |
| **UE-003** | 布局调整 | 拖拽 Row/Col 及调整宽度 | Designer | 实时响应拖拽，JSON 数据更新。 |
| **UE-004** | 容器嵌套 | Recursion: Tab->Row->Card | Designer | 多层嵌套结构清晰。 |
| **UE-005** | 流式排版 | 宽度溢出自动换行 (Wrap) | Designer | 总宽度 > 100% 时换行。 |
| **UE-006** | 属性编辑 | 通用属性 (Label/Visible/Style) | Designer | 修改属性实时重绘。 |
| **UE-010** | 基础控件 | `Input` (文本) | Runtime | 验证防注入 (XSS) 与基础读写。 |
| **UE-011** | 基础控件 | `TextArea` (多行) | Runtime | 验证高度适配与换行回显。 |
| **UE-012** | 基础控件 | `InputNumber` (数值) | Runtime | 验证精度截断与非法字符拦截。 |
| **UE-013** | 基础控件 | `Switch` (布尔) | Runtime | 验证 Switch 状态切换与 Label。 |
| **UE-014** | 基础控件 | `Checkbox` (布尔) | Runtime | 验证 Checkbox 勾选与 Disabled 态。 |
| **UE-015** | 基础控件 | `Date/Calendar` (日期) | Runtime | 验证格式化 (Format) 与范围限制。 |
| **UE-016** | 基础控件 | `Button` (动作) | Runtime | 验证点击事件与 Action 触发。 |
| **UE-017** | 基础控件 | `Label` (静态文本) | Runtime | 验证纯文本渲染无 Input。 |
| **UE-020** | 选项控件 | `Select` (下拉) | Runtime | 验证枚举下拉与 Z-Index。 |
| **UE-021** | 选项控件 | `RadioGroup` (互斥) | Runtime | 验证单选互斥与必填校验。 |
| **UE-022** | 选项控件 | `ListBox` (多选) | Runtime | 验证多选逻辑与 Model 绑定。 |
| **UE-023** | 选项控件 | `EnumSelector` (自动) | Runtime | 验证系统枚举自动加载。 |
| **UE-024** | 引用控件 | `Lookup` (弹窗) | Runtime | 验证 1:1 关联选择与回填。 |
| **UE-030** | 数据控件 | `DataGrid` (子表) | Runtime | 验证 1:N 弹窗 CRUD 交互。 |
| **UE-031** | 数据控件 | `SubForm` (嵌套) | Runtime | 验证 1:1 内嵌表单编辑。 |
| **UE-032** | 数据控件 | `OrgTree` (部门树) | Runtime | 验证树形展开与 ID 选择。 |
| **UE-033** | 数据控件 | `PermTree` (权限树) | Runtime | 验证级联勾选逻辑。 |
| **UE-034** | 数据控件 | `UserRole` (穿梭框) | Runtime | 验证左右分配逻辑。 |
| **UE-040** | 布局容器 | `Grid` (栅格) | Runtime | 验证 Gutter/Span 渲染。 |
| **UE-041** | 布局容器 | `Card` (卡片) | Runtime | 验证 Title/Body 结构。 |
| **UE-042** | 布局容器 | `TabBox` (标签页) | Runtime | 验证 Tab 切换内容隔离。 |
| **UE-043** | 布局容器 | `Section` (折叠) | Runtime | 验证折叠/展开交互。 |
| **UE-044** | 布局容器 | `Panel` (面板) | Runtime | 验证背景色与边距。 |
| **UE-045** | 布局容器 | `Frame` (框架) | Runtime | 验证边框样式。 |
| **UE-050** | 交互行为 | Real-time Validation | Runtime | 验证输入时的即时校验反馈。 |

## 3. 组织与权限 (Identity & Security - IS)
验证系统的安全底座：谁在什么条件下能看到什么。

| ID | 特性 | 测试点 (Checkpoint) | 验证方法 | 预期结果 |
|---|---|---|---|---|
| IS-001 | 用户管理 | 全功能 CRUD：创建、激活、分配部门 | UI + API | 用户记录持久化，密码 Hash 加密。 |
| IS-002 | 角色管理 | 全功能 CRUD：角色代号、多语名称、描述 | UI + API | 角色记录持久化且支持 DataScope 配置。 |
| IS-003 | 关系分配 | 用户-角色分配 (Many-to-Many) | UI + SQL | 分配后用户立即解析获取对应角色的权限集。 |
| IS-004 | 权限架构 | 角色-功能节点授权 (Permission Tree) | UI | 勾选权限树节点，保存后 API 严格鉴权限。 |
| IS-005 | 状态鉴权 | **[NEW] 授权时勾选“适用状态” (e.g. Draft)** | UI + API | 仅当实体处于 Draft 态时，用户才拥有该权限。 |
| IS-006 | 菜单创建 | 动态菜单配置：图标、路由、多语标签 | UI | 侧边栏根据配置实时刷新。 |
| IS-007 | 导航拦截 | 无权用户直接访问路由地址 (Hard-link) | E2E | 系统响应 403 页面，且不加载受保护的数据 API。 |

## 4. 路由与多态逻辑 (Navigation & Logic - NL)
验证高级场景：如何根据上下文渲染不同的内容。

| ID | 特性 | 测试点 (Checkpoint) | 验证方法 | 预期结果 |
|---|---|---|---|---|
| NL-001 | 菜单关联 | 菜单节点（子系统入口）关联特定模板 ID | UI + E2E | 通过不同菜单进入同一实体时，渲染不同的 Layout。 |
| NL-002 | 多态渲染 | 规则匹配：字段值驱动模板切换 (`Rule Engine`) | E2E | 修改 `Status=Draft` 后页面重载为草稿版模板。 |
| NL-003 | 多态渲染 | 给定状态下的**属性覆盖** (`State Overrides`) | UI + E2E | 同一模板在“新增”态只读 A 字段，在“编辑”态可写 A 字段。 |
| NL-004 | 档案匹配 | 基于 Lookup (档案记录 ID) 的视图规则 | Integration | 当关联客户为 "VIP客户档案" 时，自动切换至 VIP 模板。 |
| NL-005 | 安全脱敏 | **SEC-06**: 后端基于当前模板权限剔除 Response 字段 | **SEC-06** | 即使使用 `mid` 攻击，API 返回的 `fields` 数组也不含无权字段。 |
| NL-006 | 异常回退 | **[NEW] 无模板时的友好提示 (Fallback UI)** | UI | 当命中规则但模板 ID 为空时，显示友好的 Empty State。 |

## 5. 系统管理 (System Management - SM) [NEW]
验证系统运维与配置功能。

| ID | 特性 | 测试点 (Checkpoint) | 验证方法 | 预期结果 |
|---|---|---|---|---|
| SM-001 | 系统初始化 | **Setup**: 首次运行向导 | E2E | 从空库启动 -> 创建管理员 -> 初始化基础数据。 |
| SM-002 | 多语管理 | **I18nEditor**: 资源键值在线编辑 | UI | 修改 Key 对应的值 -> 界面实时刷新(或缓存失效)。 |
| SM-003 | 任务监控 | **JobMonitor**: 后台任务状态查看 | UI | 查看 Enqueue/Processing/Failed 任务队列。 |
| SM-004 | 审计日志 | **AuditLog**: 全局操作记录查询 | UI | 按时间/用户/实体筛选日志 -> 详情展示变更前后 Diff。 |
| SM-005 | 个人中心 | **Profile**: 修改密码与基本信息 | UI | 修改头像/密码 -> 重新登录验证生效。 |
| SM-006 | 激活流程 | **Activate**: 邮件链接激活账户 | E2E | 访问激活 Token 链接 -> 账号状态变更为 Active。 |

## 6. 系统核心服务 (System Services - SS)
验证 ARCH-33 定义的系统级基础设施。

| ID | 特性 | 测试点 (Checkpoint) | 验证方法 | 预期结果 |
|---|---|---|---|---|
| SS-001 | 校验服务 | **IValidationService**: 扩展 Email/IP 规则 | API | 提交非法 Email 格式，后端服务返回标准 ValidationResult。 |
| SS-002 | 邮件服务 | **IEmailSender**: 发送测试邮件 | Integration | 调用 SendAsync，本地 Smtp4Dev 或日志中捕获到邮件内容。 |
| SS-003 | 通知服务 | **INotificationClient**: SignalR 推送 | UI | 后端调用 PushAsync，前端右上角弹出 AntDesign 通知。 |
| SS-004 | 消息队列 | **IBackgroundQueue**: 异步任务投递 | Integration | Enqueue 任务后，立即返回上下文，后台线程正确执行逻辑。 |

---

## 7. 执行策略 (Execution Strategy) - [Merged from PLAN-24]

我们采用 **"分批推进、严格清洗"** 的策略 (Compliance with STD-06)。

### Batch 1: 平台地基 (Foundation)
*   **Scope**: Entity (PC-*) & Publishing (PC-*)
*   **Blocker**: 如果实体造不出来 (ENT-01)，后续全部暂停。
*   **Key Cases**: `test_entity_types`, `test_publish_basic`, `test_publish_evolution`.

### Batch 2: 应用组装 (Assembly)
*   **Scope**: Templates (UE-*) & Design (UE-*)
*   **Focus**: 验证 "所见即所得"，特别是设计器 (UE-003~006)。
*   **Key Cases**: `test_template_codegen`, `test_form_designer`, `test_runtime_render`.

### Batch 3: 业务闭环 (Business Loop)
*   **Scope**: Security (IS-*) & Logic (NL-*)
*   **Focus**: 验证 "最终用户体验" 和 "安全边界"。
*   **Key Cases**: `test_security_menus`, `test_security_access`, `test_polymorphic_flow` (NL-002).

---

## 8. 准出标准 (Final Exit Criteria)
1.  **Blocker Case**: 必须 100% 通过 (PC-001~009, IS-001~006)。
2.  **存证要求**: 每一个 ID 必须对应一个 `docs/history/test-results/` 下的存证子目录。
3.  **代码覆盖**: 后端核心服务单元测试覆盖率需 > 85%。
4.  **环境洁癖**: 所有集成测试必须从物理空环境启动并成功清理。

---
**核准**: 项目管理委员会 (PMC)
**导出路径**: `docs/test-cases/TEST-01-v1.0-全功能综合测试矩阵.md`
