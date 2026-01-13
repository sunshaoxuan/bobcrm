# TEST-DASHBOARD: v1.0 实时质量大屏 (Real-time Quality Dashboard)

> **当前阶段**: v1.0 全功能深度验收
> **质量总分**: 🟢 0 / 35 (通过率 0.0%)
> **存证根目录**: `docs/history/test-results/` (已推送 Git)
> **依据**: [TEST-01: 终版综合测试矩阵](file:///c:/workspace/bobcrm/docs/test-cases/TEST-01-v1.0-全功能综合测试矩阵.md)

---

## 🟢 Batch 1: 实体与存储基石 (Platform Core)
**目标**: 确保元数据到物理层的 100% 准确性。

| ID | 特性路径 | 场景描述 | 状态 | 存证 (Evidence) |
|---|---|---|---|---|
| **PC-001** | 实体定义 | [全类型] 支持 String/Int/Decimal/Guid 等基元类型 | ⚪ Pending | [Evidence](file:///c:/workspace/bobcrm/docs/history/test-results/PC-001/) |
| **PC-002** | 约束校验 | `IsRequired` 必填性后端强校验 | ⚪ Pending | - |
| **PC-004** | 实体关联 | `Lookup` (1:1) 指针定义与导航属性 | ⚪ Pending | - |
| **PC-006** | 热发布 | 物理库 `CREATE TABLE` 状态对齐 | ⚪ Pending | - |
| **PC-007** | 演进发布 | `ADD COLUMN` 增量更新而不丢失旧数据 | ⚪ Pending | - |
| **PC-009** | 发布撤回 | **Withdrawal Mode** (物理/逻辑删除核验) | ⚪ Pending | - |

## 🟢 Batch 2: UI 渲染与设计引擎 (UI Engine)
**目标**: 验证模板由“零代码”生成到“高级定制”的转换。

| ID | 特性路径 | 场景描述 | 状态 | 存证 (Evidence) |
|---|---|---|---|---|
| **UE-001** | 自动生成 | `DefaultList / DefaultDetail` 零配置即时可用 | ⚪ Pending | [Evidence](file:///c:/workspace/bobcrm/docs/history/test-results/UE-001/) |
| **UE-003** | 模板定制 | 复杂布局 (Tab/Row/Col) 拖拽保存与回显 | ⚪ Pending | - |
| **UE-004** | 控件修改 | Label/Placeholder/ReadOnly 属性重构 | ⚪ Pending | - |
| **UE-006** | 渲染保真 | 高阶控件 (Switch/Date/Selector) 运行时精准转换 | ⚪ Pending | - |
| **UE-007** | 表单校验 | UI 级反馈：字段标红、气泡提示、Save 阻断 | ⚪ Pending | - |

## 🟢 Batch 3: 身份、权限与导航 (IAM & Security)
**目标**: 验证安全闭环——用户、角色、菜单、模板的死锁关联。

| ID | 特性路径 | 场景描述 | 状态 | 存证 (Evidence) |
|---|---|---|---|---|
| **IS-001** | 用户角色 | 用户创建 -> 角色定义 -> 多对多权限关联过程 | ⚪ Pending | [Evidence](file:///c:/workspace/bobcrm/docs/history/test-results/IS-001/) |
| **IS-004** | 功能授权 | 给角色分配权限树节点，并验证 API 拦截 | ⚪ Pending | - |
| **IS-005** | 菜单导航 | **菜单项创建 -> 关联特定 Template ID** | ⚪ Pending | - |
| **IS-006** | 路由守卫 | 直接路径访问 403 校验 (Front+Back) | ⚪ Pending | - |

## 🟢 Batch 4: 多态动态渲染 (Dynamic Logic)
**目标**: 验证“数据驱动”与“多态属性覆盖”的最高级能力。

| ID | 特性路径 | 场景描述 | 状态 | 存证 (Evidence) |
|---|---|---|---|---|
| **NL-002** | 规则驱动 | **Rule Matching**: Status 变更触发模板位移 | ⚪ Pending | [Evidence](file:///c:/workspace/bobcrm/docs/history/test-results/NL-002/) |
| **NL-003** | 属性覆盖 | **State Overrides**: 同一模板不同态的可编辑性差异 | ⚪ Pending | - |
| **NL-004** | 档案匹配 | 基于 `Lookup` 记录 ID 的精准渲染匹配 | ⚪ Pending | - |
| **NL-005** | 安全脱敏 | **SEC-06**: 后端基于当前模板权限剔除 Response 字段 | ⚪ Pending | - |

---

## 4. 存证过程记录
- **自动化包**: 每次 `git push` 前必须运行 `verify-all.ps1`。
- **归档要求**: 存证必须包含截屏、控制台日志、DB diff 文件。

---
**准核**: BobCRM 交付质量组 (QA-Lead)
**更新路径**: `docs/test-cases/TEST-DASHBOARD-v1.0.md`
