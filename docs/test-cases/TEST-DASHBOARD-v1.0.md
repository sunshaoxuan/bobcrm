# TEST-DASHBOARD: v1.0 全功能测试看板 (Comprehensive Scoreboard)

> **当前阶段**: v1.0 全量集成测试回归
> **汇总统计**: 🟢 0% (0 / 33 Cases)
> **存证根目录**: `docs/history/test-results/` (符合 STD-02)
> **依据**: [TEST-01: 测试矩阵](file:///c:/workspace/bobcrm/docs/test-cases/TEST-01-v1.0-全功能测试矩阵.md)

---

## 🟢 Batch 1: 平台地基 (Foundation) - [TEST-02]
**重点**: 验证元数据定义、数据库生成、Schema 演进及发布闭环。

| ID | 特性 | 场景 | 状态 | 存证 (Evidence) |
|---|---|---|---|---|
| **ENT-001** | 字段类型 | 支持所有基元类型 (String/Int/Dec/Bool/...) | ⚪ Pending | [Evidence](file:///c:/workspace/bobcrm/docs/history/test-results/Batch1/ENT-001/) |
| **ENT-002** | 约束条件 | `IsRequired` 必填性后端校验 | ⚪ Pending | - |
| **ENT-004** | 实体引用 | 1:1 关联引用配置 | ⚪ Pending | - |
| **ENT-005** | 实体引用 | 1:N 组合依赖配置 | ⚪ Pending | - |
| **PUB-001** | 首次发布 | 物理建表状态核验 | ⚪ Pending | - |
| **PUB-002** | 演进 | 增量字段 (Add Column) | ⚪ Pending | - |
| **PUB-003** | 演进 | 字段长度平滑变更 (Modify Length) | ⚪ Pending | - |
| **PUB-007** | 级联 | 依赖项自动级联发布 | ⚪ Pending | - |
| **MOD-001** | 撤回 | **Entity Withdrawal** (ARCH-32) | ⚪ Pending | - |

## 🟢 Batch 2: 应用组装 (Assembly) - [TEST-03]
**重点**: 验证“所见即所得”设计、代码生成、及运行时表单渲染。

| ID | 特性 | 场景 | 状态 | 存证 (Evidence) |
|---|---|---|---|---|
| **TPL-001** | 自动生成 | DefaultList/Detail 模板生成 | ⚪ Pending | [Evidence](file:///c:/workspace/bobcrm/docs/history/test-results/Batch2/TPL-001/) |
| **TPL-002** | 设计器 | 容器组件 (Tab/Row/Col) 布局 | ⚪ Pending | - |
| **TPL-004** | 设计器 | 布局持久化与重加载核验 | ⚪ Pending | - |
| **TPL-005** | 运行时 | 高阶控件 (Switch/Selector) 渲染 | ⚪ Pending | - |
| **TPL-006** | 运行时 | 表单校验视觉反馈 (Validation UI) | ⚪ Pending | - |
| **POLY-002**| **状态覆盖** | **State-Aware Property Overrides** | ⚪ Pending | - |

## 🟢 Batch 3: 业务闭环 (Business Loop) - [TEST-04]
**重点**: 验证权限拦截、复杂业务流、数据驱动引擎。

| ID | 特性 | 场景 | 状态 | 存证 (Evidence) |
|---|---|---|---|---|
| **SEC-004** | 菜单权限 | 侧边栏动态过滤显示 | ⚪ Pending | [Evidence](file:///c:/workspace/bobcrm/docs/history/test-results/Batch3/SEC-004/) |
| **SEC-005** | 安全守卫 | 无权 URL 硬路由拦截 (403) | ⚪ Pending | - |
| **SEC-006** | 字段级 | **SEC-06 Backend Field Stripping** | ⚪ Pending | - |
| **POLY-001**| **规则匹配** | **Data-driven Polymorphic Switching** | ⚪ Pending | - |
| **UX-001**  | 列表操作 | 分页、模糊搜索、多列过滤 | ⚪ Pending | - |
| **UX-003**  | 关联选择 | 档案记录弹出式选择与回填 | ⚪ Pending | - |
| **UX-004**  | 业务流 | "Create -> Edit -> Close" 完整链路 | ⚪ Pending | - |

## 🟢 Batch 4: 标准业务模块 (Standard Modules)
**重点**: 验证系统内置的基础业务逻辑、组织架构及配置弹性。

| ID | 特性 | 场景 | 状态 | 存证 (Evidence) |
|---|---|---|---|---|
| **AUTH-001** | 身份认证 | 多因素/多模式登录流程验证 | ⚪ Pending | - |
| **ORG-001**  | 组织管理 | 级联组织树创建与用户归属记录 | ⚪ Pending | - |
| **CFG-001**  | 系统配置 | 全局 I18n 资源热切换与缓存失效 | ⚪ Pending | - |
| **CRM-001**  | 线索/客户 | 核心业务模型实体全生命周期 | ⚪ Pending | - |

---

## 4. 存证执行建议
- **自动化流**: 每次测试运行 (`pytest --e2e-save`) 自动生成存证包。
- **回放文件**: 使用 Playwright Trace Viewer 查看失败场景。
- **DB Dump**: 记录 schema 变更前后的 SQL Diff。

---
**核准**: 项目质量总监 (QC)
**导出路径**: `docs/test-cases/TEST-DASHBOARD-v1.0.md`
