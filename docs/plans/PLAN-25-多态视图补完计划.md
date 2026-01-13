# 多态视图渲染补完计划 (The Missing Link)

## 1. 专项审计总结 (Audit Summary)

经过对 V1.0 代码库的专项审查，多态视图渲染功能目前处于“逻辑先行，工具滞后”的状态：

| 维度 | 子项 | 状态 | 审计结论 |
| :--- | :--- | :--- | :--- |
| **定义 (Definition)** | 实体状态驱动 | 🟠 部分实现 | 核心引擎支持匹配，但缺乏“档案式选择”的 UI 支持 (Theme 1)。 |
| **设计 (Design)** | 设计时绑定选择 | 🔴 缺失 | 模板设计器中无法配置规则，导致多态能力难以被普通管理员使用 (Theme 2)。 |
| **运行 (Runtime)** | 路由与数据联动 | 🟢 已闭环 | `TemplateRuntimeService` 的 `mid` 逻辑与规则引擎已合并。 |

## 2. 补完开发计划 (Development Plan)

为了达成 `ARCH-31` 的完整愿景并验证 Theme 1/2/3，需执行以下补足方案：

### A. 设计器集成 `StateBindingEditor` (Theme 2)
- 在 `FormDesigner.razor` 的属性面板中增加“渲染绑定”入口。
- 允许将当前正在编辑的模板直接关联到 `EntityType + ViewState + Rules`。

### B. 档案记录选择器 `RecordSelector` (Theme 1)
- 改进绑定逻辑中的“匹配值”输入，针对 Lookup 字段自动弹出记录搜索器。
- 支持基于已发布的实体档案库 (Inventory) 定义视图状态分支。

### C. 自动化闭环验证 (Theme 3)
- 编写集成测试，模拟“业务档案变更 -> 触发规则引擎 -> 视图动态位移”的全过程。

## 3. 补足开发 Prompt 建议 (Remediation Prompts)

请基于以下 Prompt 执行开发：

### Task 1: 绑定编辑器与设计器集成
> **目标**: 在 `FormDesigner` 中补齐绑定配置能力。
> **要求**:
> 1. 开发 `StateBindingEditor` 组件，集成进设计器侧边栏。
> 2. 实现基于实体字段的动态规则编辑器：
>    - Enum 类型：下拉选择。
>    - Lookup 类型：使用 `EntityRecordSelector` 挑选档案记录 ID。
> 3. 保存模板时利用 API 同步更新数据库中的 `TemplateStateBinding`。

### Task 2: 档案式状态切换测试
> **目标**: 验证 Theme 1 要求的“档案式定义”在运行时的有效性。
> **场景**: 创建规则“客户等级 = 黑金客户档案 ID -> 渲染黑金专场模板”。验证 API 是否正确裁剪出黑金模板字段。

## 4. 最终验证矩阵 (Priority: High)

- [ ] UI: 设计器中可见绑定配置面板
- [ ] UI: 支持从记录列表选择状态匹配值 (Record-based Binding)
- [ ] Logic: 运行时修改档案数据，页面刷新后视图自动切换
- [ ] QA: E2E 覆盖“档案状态驱动”的全链路逻辑
