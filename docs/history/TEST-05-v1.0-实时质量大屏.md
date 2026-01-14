# TEST-05: v1.0 实时质量大屏 (Real-time Quality Dashboard)

> **当前阶段**: v1.0 全功能深度验收
> **质量总分**: 🔴 0 / 55 (通过率 0.0%)
> **存证根目录**: `docs/history/test-results/` (已推送 Git)
> **依据**: [TEST-01: 终版综合测试矩阵](file:///c:/workspace/bobcrm/docs/test-cases/TEST-01-v1.0-全功能综合测试矩阵.md)

---

## 🟢 Batch 1: 实体与存储基石 (Platform Core) - [PC-***]
**目标**: 确保元数据到物理层的 100% 准确性，支持可扩展校验。

| ID | 特性路径 | 场景描述 | 状态 | 存证 (Evidence) |
|---|---|---|---|---|
| **PC-001** | 全类型定义 | 支持 String/Int/Decimal/Guid/Date 等所有基元类型 | 🔴 TIMEOUT | [Evidence](file:///c:/workspace/bobcrm/docs/history/test-results/PC-001/) |
| **PC-002** | 基础约束 | `IsRequired` / `MaxLength` 数据库级强校验 | 🔴 TIMEOUT | - |
| **PC-003** | **[NEW]扩展校验**| **Email/IP/Regex** 后端接口与前端规则应用 | 🔴 TIMEOUT | - |
| **PC-004** | 关联关系 | 1:1 Lookup & 1:N Collection 定义正确性 | 🔴 TIMEOUT | - |
| **PC-005** | **关联定义** | **[GAP] 1:N 级联集合定义** | 🔴 TIMEOUT | - |
| **PC-006** | 物理同步 | `CREATE TABLE` / `ALTER TABLE` DDL 执行准确性 | 🔴 TIMEOUT | - |
| **PC-007** | **Schema演进** | **[GAP] 非破坏性加列 (Add Column)** | 🔴 TIMEOUT | - |
| **PC-008** | **Schema重构** | **[GAP] 字段重命名与属性变更** | 🔴 TIMEOUT | - |
| **PC-009** | 发布撤回 | Withdrawal 模式下的物理/逻辑删除行为验证 | 🔴 TIMEOUT | - |


## 🟢 Batch 2: UI 渲染与设计引擎 (UI Engine) - [UE-***]
**目标**: 验证组件级的渲染保真度与容器嵌套能力。

| ID | 特性路径 | 场景描述 | 状态 | 存证 (Evidence) |
|---|---|---|---|---|
| **UE-001** | 自动生成 | DefaultList 模板生成 | 🔴 0% COV | [Evidence](file:///c:/workspace/bobcrm/docs/history/test-results/UE-001/) |
| **UE-002** | 自动生成 | DefaultDetail 模板生成 | 🔴 0% COV | - |
| **UE-003** | 布局调整 | 拖拽 Row/Col 及调整宽度 | 🔴 0% COV | - |
| **UE-004** | 容器嵌套 | Recursion: Tab->Row->Card | 🔴 0% COV | - |
| **UE-005** | 流式排版 | 宽度溢出自动换行 (Wrap) | 🔴 0% COV | - |
| **UE-006** | 属性编辑 | 通用属性 (Label/Visible/Style) | 🔴 0% COV | - |
| **UE-010** | 基础控件 | `Input` (文本) | 🔴 0% COV | - |
| **UE-011** | 基础控件 | `TextArea` (多行) | 🔴 0% COV | - |
| **UE-012** | 基础控件 | `InputNumber` (数值) | 🔴 0% COV | - |
| **UE-013** | 基础控件 | `Switch` (布尔) | 🔴 0% COV | - |
| **UE-014** | 基础控件 | `Checkbox` (布尔) | 🔴 0% COV | - |
| **UE-015** | 基础控件 | `Date/Calendar` (日期) | 🔴 0% COV | - |
| **UE-016** | 基础控件 | `Button` (动作) | 🔴 0% COV | - |
| **UE-017** | 基础控件 | `Label` (静态文本) | 🔴 0% COV | - |
| **UE-020** | 选项控件 | `Select` (下拉) | 🔴 0% COV | - |
| **UE-021** | 选项控件 | `RadioGroup` (互斥) | 🔴 0% COV | - |
| **UE-022** | 选项控件 | `ListBox` (多选) | 🔴 0% COV | - |
| **UE-023** | 选项控件 | `EnumSelector` (自动) | 🔴 0% COV | - |
| **UE-024** | 引用控件 | `Lookup` (弹窗) | 🔴 0% COV | - |
| **UE-030** | 数据控件 | `DataGrid` (子表) | 🔴 0% COV | - |
| **UE-031** | 数据控件 | `SubForm` (嵌套) | 🔴 0% COV | - |
| **UE-032** | 数据控件 | `OrgTree` (部门树) | 🔴 0% COV | - |
| **UE-033** | 数据控件 | `PermTree` (权限树) | 🔴 0% COV | - |
| **UE-034** | 数据控件 | `UserRole` (穿梭框) | 🔴 0% COV | - |
| **UE-040** | 布局容器 | `Grid` (栅格) | 🔴 0% COV | - |
| **UE-041** | 布局容器 | `Card` (卡片) | 🔴 0% COV | - |
| **UE-042** | 布局容器 | `TabBox` (标签页) | 🔴 0% COV | - |
| **UE-043** | 布局容器 | `Section` (折叠) | 🔴 0% COV | - |
| **UE-044** | 布局容器 | `Panel` (面板) | 🔴 0% COV | - |
| **UE-045** | 布局容器 | `Frame` (框架) | 🔴 0% COV | - |
| **UE-050** | 交互行为 | Real-time Validation | 🔴 0% COV | - |

## 🟢 Batch 3: 身份、权限与导航 (IAM & Security) - [IS-***]
**目标**: 验证安全闭环与状态感知鉴权。

| ID | 特性路径 | 场景描述 | 状态 | 存证 (Evidence) |
|---|---|---|---|---|
| **IS-001** | 身份管理 | 用户创建 -> 激活 -> 登录 (Hash校验) | 🔴 TIMEOUT | [Evidence](file:///c:/workspace/bobcrm/docs/history/test-results/IS-001/) |
| **IS-003** | 关系分配 | 用户-角色多对多关联及权限继承 | 🔴 TIMEOUT | - |
| **IS-004** | 功能授权 | 权限树勾选与 API 鉴权拦截 | 🔴 TIMEOUT | - |
| **IS-005** | **状态鉴权** | **[NEW] 授权指定 Resource 仅在 Draft 态可用** | 🔴 TIMEOUT | - |
| **IS-006** | 动态导航 | 菜单项创建、图标配置与路由生成 | 🔴 TIMEOUT | - |
| **IS-007** | 路由守卫 | 硬路由访问 (URL Hacking) 的 403 拦截 | 🔴 TIMEOUT | - |

## 🟢 Batch 4: 逻辑、多态与服务 (Logic & Services) - [NL/SS-***]
**目标**: 验证业务逻辑、多态渲染及系统基础设施。

| ID | 特性路径 | 场景描述 | 状态 | 存证 (Evidence) |
|---|---|---|---|---|
| **NL-001** | 菜单多态 | 不同菜单入口关联同一实体的不同模板ID | 🔴 NO DATA | [Evidence](file:///c:/workspace/bobcrm/docs/history/test-results/NL-001/) |
| **NL-002** | 规则多态 | 数据值 (Status) 变更驱动模板动态切换 | 🔴 NO DATA | - |
| **NL-003** | **属性覆盖** | **State Overrides**: 同模板不同态的 ReadOnly 差异 | 🔴 NO DATA | - |
| **NL-004** | **档案多态** | **[GAP] 基于 Lookup 记录 ID 的模板切换 (Theme 1)** | 🔴 NO DATA | - |
| **NL-005** | **安全脱敏** | **[GAP] SEC-06 后端字段级权限裁剪** | 🔴 NO DATA | - |
| **NL-006** | **异常回退** | **Fallback UI**: 无模板时的友好提示 | 🔴 NO DATA | - |
| **SS-001** | **校验服务** | `IValidationService` 对扩展规则的后端执行 | 🔴 NO DATA | - |
| **SS-002** | **邮件服务** | `IEmailSender` 投递测试 (Smtp/Mock) | 🔴 NO DATA | - |
| **SS-003** | **通知服务** | **[GAP] INotificationClient 全局消息推送** | 🔴 NO DATA | - |
| **SS-004** | **消息队列** | `IBackgroundQueue` 异步任务编排 | 🔴 NO DATA | - |

## 🟢 Batch 5: 系统管理 (System Management) - [SM-***]
**目标**: 验证系统运维、配置与用户中心。

| ID | 特性路径 | 场景描述 | 状态 | 存证 (Evidence) |
|---|---|---|---|---|
| **SM-001** | **Setup** | 系统首次初始化向导 | 🔴 NO DATA | - |
| **SM-002** | **I18n** | 多语资源在线编辑与生效 | 🔴 NO DATA | - |
| **SM-003** | **Jobs** | 后台任务队列监控 | 🔴 NO DATA | - |
| **SM-004** | **Audit** | 全局审计日志查询 | 🔴 NO DATA | - |
| **SM-005** | **Profile** | 个人设置与密码修改 | 🔴 NO DATA | - |
| **SM-006** | **Activate** | 账户邮件激活流程 | 🔴 NO DATA | - |

---

## 4. 存证过程记录
- **自动化包**: 每次 `git push` 前必须运行 `verify-all.ps1`。
- **归档要求**: 存证必须包含截屏、控制台日志、DB diff 文件。

---
**核准**: BobCRM 交付质量组 (QA-Lead)
**更新路径**: `docs/test-cases/TEST-05-v1.0-实时质量大屏.md`
