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
| UE-001 | 自动生成 | 实体发布后默认列表 (`List`) 模板生成 | UI (List) | 无需任何配置，列表页正常显示所有可见字段。 |
| UE-002 | 自动生成 | 实体发布后默认详情 (`Detail`) 模板生成 | UI (Detail) | 详情页自动平铺所有字段，布局合理。 |
| UE-003 | 基础布局 | 拖拽 `Row/Col` 组件与列宽调整 (Percent/Px) | Designer | 拖拽至画布，调整宽度，DOM 实时响应且 JSON 正确。 |
| UE-004 | 容器嵌套 | `TabContainer` 内嵌 `Row` 再嵌 `Card` (递归测试) | Designer | 多层嵌套结构清晰，拖拽子组件不丢失父级上下文。 |
| UE-005 | 流式排版 | 连续添加多个 `Input` 控件触发强制换行 (Wrap) | Designer | 当宽度总和 > 100% 时，自动换行而不溢出容器。 |
| UE-006 | 属性编辑 | 修改控件 Visible, ReadOnly, Label, Style | Designer | 属性面板修改后，画布立即重绘反映变更。 |
| UE-007 | 运行时 | 高阶控件: `Lookup` (选择/回填/清空) | Runtime | 弹出模态框选择记录，ID/Name 正确回填。 |
| UE-008 | 运行时 | 高阶控件: `DatePicker` (格式/范围) | Runtime | 日期选择器按字段 `Format` 渲染，支持范围限制。 |
| UE-009 | 运行时 | 高阶控件: `Switch` (Boolean 交互) | Runtime | 切换开关状态，即时绑定并触发 `OnFieldChanged`。 |
| UE-010 | 运行时 | 表单校验: 必填/正则/数值范围 (Real-time) | Runtime | 输入时实时校验，错误提示精准定位到控件下方。 |

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

## 5. 系统核心服务 (System Services - SS) [NEW]
验证 ARCH-33 定义的系统级基础设施。

| ID | 特性 | 测试点 (Checkpoint) | 验证方法 | 预期结果 |
|---|---|---|---|---|
| SS-001 | 校验服务 | **IValidationService**: 扩展 Email/IP 规则 | API | 提交非法 Email 格式，后端服务返回标准 ValidationResult。 |
| SS-002 | 邮件服务 | **IEmailSender**: 发送测试邮件 | Integration | 调用 SendAsync，本地 Smtp4Dev 或日志中捕获到邮件内容。 |
| SS-003 | 通知服务 | **INotificationClient**: SignalR 推送 | UI | 后端调用 PushAsync，前端右上角弹出 AntDesign 通知。 |
| SS-004 | 消息队列 | **IBackgroundQueue**: 异步任务投递 | Integration | Enqueue 任务后，立即返回上下文，后台线程正确执行逻辑。 |

---

## 5. 准出标准 (Final Exit Criteria)
1.  **Blocker Case**: 必须 100% 通过 (PC-001~009, IS-001~006)。
2.  **存证要求**: 每一个 ID 必须对应一个 `docs/history/test-results/` 下的存证子目录。
3.  **代码覆盖**: 后端核心服务单元测试覆盖率需 > 85%。
4.  **环境洁癖**: 所有集成测试必须从物理空环境启动并成功清理。

---
**核准**: 项目管理委员会 (PMC)
**导出路径**: `docs/test-cases/TEST-01-v1.0-全功能综合测试矩阵.md`
