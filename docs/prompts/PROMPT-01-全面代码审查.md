# PROMPT-01: BobCRM 全面代码审查 Prompt

**创建日期**: 2025-12-25
**适用范围**: 整个 BobCRM 项目
**项目根目录**: `c:\workspace\bobcrm\`
**预计执行时间**: 多轮交互（建议分阶段执行）

---

## 使用说明

将以下 Prompt 复制到 Claude Code 或其他 AI 工具中执行。建议按阶段分批执行，避免单次对话过长。

**重要**：所有文件路径均为绝对路径，确保 AI 能准确定位文件。

---

## 主 Prompt（完整版）

```
你是一位资深的软件架构师和代码审查专家。现在需要你对 BobCRM 项目进行全面的代码审查。

项目根目录：c:\workspace\bobcrm\

# 审查目标

对 BobCRM 项目进行系统性的功能评审和代码质量评审，确保代码实现与设计文档一致，符合项目规范标准。

# 第一阶段：文档学习与规范理解

## 1.1 必读文档清单

请按以下顺序阅读并理解项目文档（使用绝对路径）：

### 核心规范（必读）
1. `c:\workspace\bobcrm\CLAUDE.md` - 项目总览和AI助手指南
2. `c:\workspace\bobcrm\docs\process\STD-01-开发质量标准.md` - "Done" 的定义和质量评估维度
3. `c:\workspace\bobcrm\docs\process\STD-02-文档编写规范.md` - 文档命名和格式规范
4. `c:\workspace\bobcrm\docs\process\STD-05-多语言开发规范.md` - I18n First 原则和规范
5. `c:\workspace\bobcrm\docs\process\STD-06-集成测试规范.md` - 测试的 5 步铁律
6. `c:\workspace\bobcrm\docs\process\PROC-01-PR检查清单.md` - PR 提交前的完整检查项
7. `c:\workspace\bobcrm\docs\process\PROC-02-文档同步规范.md` - 文档与代码同步规范
8. `c:\workspace\bobcrm\docs\process\PROC-05-I18n扫描规则.md` - I18n 资源扫描和验证规则

### 架构设计（必读）
1. `c:\workspace\bobcrm\docs\design\ARCH-01-实体自定义与发布系统设计文档.md` - 实体定义与动态发布
2. `c:\workspace\bobcrm\docs\design\ARCH-10-AggVO系统指南.md` - 聚合值对象系统
3. `c:\workspace\bobcrm\docs\design\ARCH-11-动态实体指南.md` - 动态实体使用指南
4. `c:\workspace\bobcrm\docs\design\ARCH-12-对象存储（MinIO-S3）使用说明.md` - MinIO/S3 集成
5. `c:\workspace\bobcrm\docs\design\ARCH-13-系统与用户设置模型.md` - 配置管理设计
6. `c:\workspace\bobcrm\docs\design\ARCH-14-数据结构自动对齐系统设计文档.md` - DDL 自动同步
7. `c:\workspace\bobcrm\docs\design\ARCH-20-实体定义管理设计.md` - 实体定义管理（字段软删除等）
8. `c:\workspace\bobcrm\docs\design\ARCH-21-组织与权限体系设计.md` - RBAC 权限设计
9. `c:\workspace\bobcrm\docs\design\ARCH-22-标准实体模板化与权限联动设计.md` - 系统实体模板化
10. `c:\workspace\bobcrm\docs\design\ARCH-23-功能体系规划.md` - 功能树规划
11. `c:\workspace\bobcrm\docs\design\ARCH-24-紧凑型顶部菜单导航设计.md` - 导航菜单设计
12. `c:\workspace\bobcrm\docs\design\ARCH-25-通用主从模式设计.md` - 主从实体设计
13. `c:\workspace\bobcrm\docs\design\ARCH-26-动态枚举系统设计.md` - 动态枚举系统
14. `c:\workspace\bobcrm\docs\design\ARCH-30-实体字段显示名多语元数据驱动设计.md` - 元数据多语（如存在）

### 产品设计（必读）
1. `c:\workspace\bobcrm\docs\design\PROD-01-客户信息管理系统设计文档.md` - CRM 核心功能
2. `c:\workspace\bobcrm\docs\design\PROD-02-客户系统开发任务与接口文档.md` - 开发任务和接口

### UI/UX设计（必读）
1. `c:\workspace\bobcrm\docs\design\UI-01-UIUE设计说明书.md` - UI/UX 设计系统
2. `c:\workspace\bobcrm\docs\design\UI-05-数据网格组件重构设计.md` - DataGrid 重构

### API参考（必读）
1. `c:\workspace\bobcrm\docs\reference\API-01-接口文档.md` - API 接口文档
2. `c:\workspace\bobcrm\docs\reference\API-02-API契约测试覆盖率报告.md` - 契约测试覆盖

### 测试指南（必读）
1. `c:\workspace\bobcrm\docs\guides\TEST-01-测试指南.md` - 测试总体指南
2. `c:\workspace\bobcrm\docs\guides\TEST-02-测试覆盖率报告.md` - 覆盖率报告
3. `c:\workspace\bobcrm\docs\guides\TEST-10-测试综述.md` - 测试综合总结

### 多语言指南（必读）
1. `c:\workspace\bobcrm\docs\guides\I18N-01-多语机制设计文档.md` - 多语言机制
2. `c:\workspace\bobcrm\docs\guides\I18N-02-元数据多语机制设计文档.md` - 元数据多语

### 历史评审参考（必读）
1. `c:\workspace\bobcrm\docs\reviews\REVIEW-09-代码评审与改进清单-2025-11.md` - 代码评审
2. `c:\workspace\bobcrm\docs\reviews\REVIEW-10-多语言功能代码评审-2025-11.md` - 多语言评审
3. `c:\workspace\bobcrm\docs\history\GAP-01-架构与功能差距记录.md` - 差距记录

## 1.2 学习输出要求

阅读完成后，请按以下格式输出：

### 1. 项目核心功能模块清单

| 模块名称 | 关键职责 | 核心代码文件（绝对路径） | 设计文档 |
|----------|----------|--------------------------|----------|
| 动态实体系统 | 运行时定义实体、Roslyn代码生成、DDL同步 | `c:\workspace\bobcrm\src\BobCrm.Api\Services\EntityDefinitionAggregateService.cs` | ARCH-01, ARCH-11 |
| AggVO系统 | 主子/主子孙结构事务一致性 | `c:\workspace\bobcrm\src\BobCrm.Api\Services\Aggregates\` | ARCH-10, ARCH-25 |
| 权限控制系统 | RBAC权限、功能节点、数据范围 | `c:\workspace\bobcrm\src\BobCrm.Api\Services\AccessService.cs` | ARCH-21, ARCH-22 |
| 多语言系统 | 三语支持、元数据多语 | `c:\workspace\bobcrm\src\BobCrm.Api\Services\I18nService.cs` | I18N-01, I18N-02, STD-05 |
| 表单模板系统 | 表单设计器、运行时渲染 | `c:\workspace\bobcrm\src\BobCrm.App\Components\Designer\` | ARCH-27（如存在） |
| 动态枚举系统 | 枚举定义管理、UI集成 | `c:\workspace\bobcrm\src\BobCrm.Api\Services\LookupService.cs` | ARCH-26 |
| 导航与布局 | 紧凑型菜单、域选择器 | `c:\workspace\bobcrm\src\BobCrm.App\Components\Layout\` | ARCH-24 |
| 对象存储 | MinIO/S3文件管理 | `c:\workspace\bobcrm\src\BobCrm.Api\Services\FileStorageService.cs` | ARCH-12 |
| 客户管理 | CRM核心业务 | `c:\workspace\bobcrm\src\BobCrm.Api\Services\CustomerService.cs` | PROD-01, PROD-02 |

### 2. 关键架构设计要点

按以下分类总结：
- **运行时动态性**：Roslyn编译、动态加载、DDL生成
- **DDD聚合模式**：AggVO、事务一致性、级联操作
- **I18n First**：资源键管理、元数据多语、三语标配
- **设计元即真理**：逻辑Schema、物理实现解耦

### 3. 必须遵守的规范清单

| 规范文档 | 核心要求 | 违规后果 |
|----------|----------|----------|
| STD-01 | 四维度质量标准（功能/易用/美观/可靠） | PR 不予合并 |
| STD-05 | 零硬编码、三语标配、KEY前缀命名 | I18n扫描失败 |
| STD-06 | 集成测试5步铁律 | 测试不稳定 |
| PROC-01 | PR 检查清单 | PR 不予合并 |
| PROC-02 | 文档同步规范 | 文档代码不一致 |

### 4. 历史遗留问题清单

从历史评审文档中提取，格式：
| 问题ID | 优先级 | 问题描述 | 涉及文件 | 来源文档 |
|--------|--------|----------|----------|----------|
| HIST-001 | P1 | 接口抽象缺失 | EntityLockService等 | REVIEW-09 |

---

# 第二阶段：功能完整性评审

## 2.1 评审维度

按照 STD-01 定义的四个维度进行评审：

### A. 功能性评审 (Functionality)
- **完整性**：对照设计文档，检查所有需求是否已实现
- **正确性**：验证功能在正常路径和边缘情况下的行为
- **集成性**：检查功能模块之间的交互是否正确

### B. 易用性评审 (Usability)
- **可配置性**：检查功能是否提供必要的配置选项
- **反馈机制**：检查操作是否有清晰的成功/失败/加载反馈（使用 ToastService）
- **效率**：评估常用任务的操作步骤是否最优
- **状态感知**：组件是否能适应不同上下文（只读/编辑）

### C. 美观性评审 (Aesthetics)
- **视觉一致性**：检查是否正确使用 Ant Design Blazor 组件
- **设计系统**：边距、填充、字体、颜色是否符合 UI-01 设计系统
- **响应式**：布局是否适应不同屏幕尺寸
- **禁止原生弹窗**：不得使用 alert/confirm/prompt

### D. 可靠性评审 (Reliability)
- **错误处理**：检查是否有静默失败的情况
- **数据完整性**：系统/自定义字段是否正确分类和保护（FieldSource）
- **国际化**：检查是否存在硬编码字符串

## 2.2 功能模块检查清单

请逐一检查以下模块（代码路径均为绝对路径）：

### 2.2.1 实体系统
核心代码：
- `c:\workspace\bobcrm\src\BobCrm.Api\Services\EntityDefinitionAggregateService.cs`
- `c:\workspace\bobcrm\src\BobCrm.Api\Services\DynamicEntityService.cs`
- `c:\workspace\bobcrm\src\BobCrm.Api\Services\EntityPublishingService.cs`
- `c:\workspace\bobcrm\src\BobCrm.Api\Endpoints\EntityDefinitionEndpoints.cs`

检查项：
- [ ] 实体定义 CRUD 完整
- [ ] 字段管理（添加/编辑/删除/软删除）
- [ ] 接口管理（启用/禁用，字段自动注入）
- [ ] 实体发布流程（代码生成→编译→DDL）
- [ ] 动态实体 CRUD
- [ ] 实体锁定机制（已发布实体保护）
- [ ] 字段来源标记（System/Interface/Custom）

### 2.2.2 权限系统
核心代码：
- `c:\workspace\bobcrm\src\BobCrm.Api\Services\AccessService.cs`
- `c:\workspace\bobcrm\src\BobCrm.Api\Endpoints\AccessEndpoints.cs`
- `c:\workspace\bobcrm\src\BobCrm.Api\Base\Models\RoleProfile.cs`
- `c:\workspace\bobcrm\src\BobCrm.Api\Base\Models\FunctionNode.cs`

检查项：
- [ ] 用户管理
- [ ] 角色管理（RoleProfile）
- [ ] 功能节点（FunctionNode 树形结构）
- [ ] 权限分配和继承
- [ ] 数据范围控制（All/Organization/Self）

### 2.2.3 表单系统
核心代码：
- `c:\workspace\bobcrm\src\BobCrm.Api\Services\FormTemplateService.cs`
- `c:\workspace\bobcrm\src\BobCrm.App\Components\Designer\FormDesigner.razor`
- `c:\workspace\bobcrm\src\BobCrm.App\Components\Designer\FormRuntime.razor`

检查项：
- [ ] 表单模板 CRUD
- [ ] 表单设计器功能
- [ ] 控件属性编辑器
- [ ] 表单绑定和渲染
- [ ] **实体元数据树形展示**（重点关注，历史P0问题）

### 2.2.4 菜单导航
核心代码：
- `c:\workspace\bobcrm\src\BobCrm.App\Components\Layout\MenuButton.razor`
- `c:\workspace\bobcrm\src\BobCrm.App\Components\Layout\DomainSelector.razor`
- `c:\workspace\bobcrm\src\BobCrm.App\Components\Layout\MenuPanel.razor`
- `c:\workspace\bobcrm\src\BobCrm.App\Services\LayoutState.cs`

检查项：
- [ ] 菜单配置
- [ ] 域选择器（图标模式）
- [ ] 菜单面板（4列网格）
- [ ] 导航状态管理

### 2.2.5 多语言系统
核心代码：
- `c:\workspace\bobcrm\src\BobCrm.Api\Services\I18nService.cs`
- `c:\workspace\bobcrm\src\BobCrm.Api\Resources\i18n-resources.json`
- `c:\workspace\bobcrm\src\BobCrm.App\Components\Shared\MultilingualInput.razor`
- `c:\workspace\bobcrm\scripts\check-i18n.ps1`

检查项：
- [ ] I18n 资源管理
- [ ] 前端多语切换
- [ ] 元数据多语支持（DisplayNameKey）
- [ ] 动态实体字段多语
- [ ] 资源键命名规范（BTN_/MSG_/ERR_等前缀）

### 2.2.6 枚举系统
核心代码：
- `c:\workspace\bobcrm\src\BobCrm.Api\Services\LookupService.cs`
- `c:\workspace\bobcrm\src\BobCrm.Api\Endpoints\LookupEndpoints.cs`
- `c:\workspace\bobcrm\src\BobCrm.App\Components\Shared\LookupSelect.razor`

检查项：
- [ ] 枚举定义管理（Lookup）
- [ ] 枚举项管理（LookupItem）
- [ ] 枚举 UI 集成
- [ ] 枚举多语支持

### 2.2.7 对象存储系统
核心代码：
- `c:\workspace\bobcrm\src\BobCrm.Api\Services\FileStorageService.cs`（如存在）

检查项：
- [ ] MinIO/S3 连接配置
- [ ] 文件上传/下载
- [ ] 文件删除
- [ ] 访问权限控制

### 2.2.8 客户管理系统
核心代码：
- `c:\workspace\bobcrm\src\BobCrm.Api\Services\CustomerService.cs`（如存在）

检查项：
- [ ] 客户 CRUD
- [ ] 客户分类
- [ ] 客户联系人
- [ ] 客户与组织关联

## 2.3 评审输出格式

```markdown
## 功能评审报告

### 评审概要
- 评审日期：YYYY-MM-DD
- 评审范围：全模块/指定模块
- 总体评分：X/10

### 模块评审结果

| 模块 | 完整性 | 正确性 | 集成性 | 易用性 | 美观性 | 可靠性 | 总评 |
|------|--------|--------|--------|--------|--------|--------|------|
| 实体系统 | ✅/⚠️/❌ | | | | | | |
| 权限系统 | | | | | | | |
| 表单系统 | | | | | | | |
| 菜单导航 | | | | | | | |
| 多语言系统 | | | | | | | |
| 枚举系统 | | | | | | | |
| 对象存储 | | | | | | | |
| 客户管理 | | | | | | | |

### 发现的问题

#### 严重问题 (P0 - 阻断)
1. [问题描述]
   - 位置：`c:\workspace\bobcrm\[文件路径]:[行号]`
   - 影响：
   - 建议修复：

#### 重要问题 (P1 - 功能缺陷)
1. [问题描述]
   - 位置：`c:\workspace\bobcrm\[文件路径]:[行号]`
   - 影响：
   - 建议修复：

#### 一般问题 (P2 - 改进建议)
1. [问题描述]
   - 位置：`c:\workspace\bobcrm\[文件路径]`
   - 建议：

### 与设计文档的差距

| 设计文档 | 设计要求 | 实际状态 | 差距说明 |
|----------|----------|----------|----------|
| ARCH-XX | | | |
```

---

# 第三阶段：代码质量评审

## ⚠️ 强制性完整性要求（不可违反）

**定义：100%覆盖 = 使用 Read 工具读取每个文件的完整内容，逐行检查。**

### 执行要求（必须全部满足）

1. **必须用 Glob 列出所有文件** - 先获取完整文件清单
2. **必须用 Read 读取每个文件** - 不是"扫描"，是读取全部内容
3. **必须对每个文件逐行检查** - 按3.2节检查项审查
4. **必须输出完整文件清单** - 列出所有文件，不是"示例"或"部分"
5. **分批执行** - 文件过多时分批，但最终必须覆盖全部

### 禁止行为（违反即审查无效）

- ❌ **禁止区分"核心文件"和"非核心文件"** - 所有文件同等对待
- ❌ **禁止"Pattern Scan"或"模式扫描"** - 必须读取完整内容
- ❌ **禁止"抽样检查"或"代表性文件"** - 每个文件都要检查
- ❌ **禁止"~XX个文件"的模糊表述** - 必须给出精确数字
- ❌ **禁止只列出部分文件清单** - 必须列出100%的文件
- ❌ **禁止用"等"或"..."省略文件** - 每个文件单独列出

### 有效性验证

审查报告必须满足以下条件才算有效：
- [ ] 文件清单数量 = Glob 返回的文件总数
- [ ] 每个文件都有"行数"和"发现问题数"记录
- [ ] 覆盖率 = 100%（不是"~100%"或"接近100%"）

## 3.1 评审范围（绝对路径）

### 3.1.1 后端代码（必须全量扫描）

首先执行以下命令获取完整文件清单：
```
Glob: c:\workspace\bobcrm\src\BobCrm.Api\**\*.cs
```

然后按目录分批检查：

| 批次 | 目录 | 说明 | 预估文件数 |
|------|------|------|------------|
| 1 | `c:\workspace\bobcrm\src\BobCrm.Api\Services\` | 业务服务（核心） | ~30 |
| 2 | `c:\workspace\bobcrm\src\BobCrm.Api\Endpoints\` | API 端点 | ~15 |
| 3 | `c:\workspace\bobcrm\src\BobCrm.Api\Base\` | 基础模型 | ~20 |
| 4 | `c:\workspace\bobcrm\src\BobCrm.Api\Abstractions\` | 接口定义 | ~10 |
| 5 | `c:\workspace\bobcrm\src\BobCrm.Api\Infrastructure\` | 基础设施 | ~15 |
| 6 | `c:\workspace\bobcrm\src\BobCrm.Api\Contracts\` | DTO | ~20 |
| 7 | `c:\workspace\bobcrm\src\BobCrm.Api\Core\` | 核心逻辑 | ~10 |
| 8 | `c:\workspace\bobcrm\src\BobCrm.Api\Application\` | 应用层 | ~10 |

### 3.1.2 前端代码（必须全量扫描）

```
Glob: c:\workspace\bobcrm\src\BobCrm.App\**\*.razor
Glob: c:\workspace\bobcrm\src\BobCrm.App\**\*.cs
```

| 批次 | 目录 | 说明 |
|------|------|------|
| 9 | `c:\workspace\bobcrm\src\BobCrm.App\Components\` | Blazor 组件 |
| 10 | `c:\workspace\bobcrm\src\BobCrm.App\Services\` | 前端服务 |

### 3.1.3 测试代码

```
Glob: c:\workspace\bobcrm\tests\**\*.cs
```

| 批次 | 目录 | 说明 |
|------|------|------|
| 11 | `c:\workspace\bobcrm\tests\BobCrm.Api.Tests\` | 集成测试 |

## 3.1.4 执行流程

```
步骤1: 使用 Glob 获取目标目录所有文件清单
步骤2: 统计文件总数，规划批次
步骤3: 逐批次读取文件内容
步骤4: 按检查项逐一审查
步骤5: 记录发现的问题（含文件路径和行号）
步骤6: 输出覆盖率报告
```

**禁止行为：**
- ❌ 只检查"代表性"文件
- ❌ 跳过"看起来没问题"的文件
- ❌ 不列出已检查文件清单

## 3.2 代码质量检查项

### A. OOP 与 SOLID 原则

- [ ] **单一职责原则 (SRP)**
  - 每个类是否只有一个变化原因？
  - 方法是否只做一件事？

- [ ] **开闭原则 (OCP)**
  - 是否对扩展开放、对修改关闭？
  - 是否使用抽象和接口？

- [ ] **里氏替换原则 (LSP)**
  - 派生类是否可以替换基类？
  - 是否有违反契约的继承？

- [ ] **接口隔离原则 (ISP)**
  - 接口是否过于庞大？
  - 是否有不需要的方法被强制实现？

- [ ] **依赖倒置原则 (DIP)**
  - 高层模块是否依赖抽象？
  - 是否通过 DI 注入依赖？
  - **重点**：检查服务类是否都有对应接口（历史P1问题）

### B. 代码规范检查

- [ ] **命名规范**
  - 类/接口：PascalCase（接口以 I 开头）
  - 方法/属性：PascalCase
  - 私有字段：_camelCase
  - 参数/变量：camelCase
  - API 路由：kebab-case

- [ ] **异步编程**
  - async/await 使用是否正确？
  - 是否有 async void（事件处理除外）？
  - 是否有阻塞调用（.Result, .Wait()）？

- [ ] **空值处理**
  - 是否使用可空引用类型？
  - 是否有适当的空值检查？

- [ ] **异常处理**（历史P1问题）
  - 是否有空 catch 块？
  - 是否捕获过于宽泛的异常？
  - 错误信息是否有意义？
  - 是否使用自定义业务异常？
  - 是否考虑 Result<T> 模式？

- [ ] **日志记录**
  - 关键操作是否有日志？
  - 日志级别是否合适？
  - 是否使用结构化日志？

- [ ] **魔术字符串**（历史P2问题）
  - 是否使用常量类管理配置键？
  - 属性名是否使用 nameof()？

### C. 安全性检查

- [ ] **输入验证**
  - API 参数是否验证？
  - 是否有 SQL 注入风险？
  - 是否有 XSS 风险？

- [ ] **认证授权**
  - API 是否需要认证（[Authorize]）？
  - 权限检查是否完整？
  - JWT 处理是否安全？

- [ ] **敏感数据**
  - 密码是否明文存储？
  - 日志是否泄露敏感信息？
  - 配置文件是否安全（appsettings.json）？

### D. 性能检查

- [ ] **数据库查询**
  - 是否有 N+1 查询问题？
  - 是否使用 AsNoTracking()？
  - 是否有不必要的 Include？
  - 是否投影到 DTO 避免 over-fetching？

- [ ] **内存管理**
  - 是否有内存泄漏风险？
  - IDisposable 是否正确处理？
  - 大对象是否及时释放？

- [ ] **缓存策略**
  - 是否合理使用 IMemoryCache？
  - 缓存失效策略是否正确？

### E. I18n 规范检查

- [ ] **硬编码检查**
  - 后端是否有硬编码中文/英文/日文？
  - 前端是否有硬编码文本？
  - 错误消息是否使用资源键？
  - 验证消息是否使用资源键？

- [ ] **资源管理**
  - 资源键命名是否规范（BTN_/MSG_/ERR_/LBL_）？
  - 是否提供所有三种语言翻译？
  - 是否使用 I18nService.T() 获取翻译？

## 3.3 代码质量评审输出格式

```markdown
## 代码质量评审报告

### 评审概要
- 评审日期：YYYY-MM-DD
- 评审批次：X/11（如分批执行）
- 代码行数：约 XX,XXX 行
- 文件数量：XX 个（已检查） / XX 个（总计）
- **覆盖率：XX%**

### 文件覆盖清单

#### 已检查文件（必须列出）
| 序号 | 文件路径 | 行数 | 发现问题数 |
|------|----------|------|------------|
| 1 | `c:\workspace\bobcrm\src\BobCrm.Api\Services\I18nService.cs` | 450 | 3 |
| 2 | `c:\workspace\bobcrm\src\BobCrm.Api\Services\AccessService.cs` | 380 | 2 |
| ... | ... | ... | ... |

#### 未检查文件（如有，必须说明原因）
| 文件路径 | 跳过原因 |
|----------|----------|
| （无，已全量覆盖） | - |

### 质量评分

| 维度 | 评分 | 说明 |
|------|------|------|
| OOP/SOLID | X/10 | |
| 命名规范 | X/10 | |
| 异常处理 | X/10 | |
| 安全性 | X/10 | |
| 性能 | X/10 | |
| I18n | X/10 | |
| 测试覆盖 | X/10 | |
| **总体** | **X/10** | |

### 发现的代码问题

#### 严重 (必须修复)
1. [问题描述]
   - 文件：`c:\workspace\bobcrm\[路径]:[行号]`
   - 问题代码：
   ```csharp
   // 问题代码
   ```
   - 建议修复：
   ```csharp
   // 修复后代码
   ```

#### 重要 (建议修复)
...

#### 改进 (可选优化)
...

### 正面发现
- 列出代码中的优秀实践
```

---

# 第四阶段：文档一致性审查

## 4.1 检查项

- [ ] **设计文档与代码一致性**
  - ARCH 文档描述的架构是否与实现一致？
  - API 文档是否与实际端点一致？
  - 数据模型是否与设计一致？

- [ ] **文档完整性**
  - 新增功能是否有对应文档？
  - 修改功能是否更新文档？
  - 已删除功能是否清理文档？

- [ ] **文档规范符合性**
  - 文档命名是否符合 STD-02？
  - 文档格式是否规范？
  - 文档存放位置是否正确？

## 4.2 输出格式

```markdown
## 文档一致性报告

### 不一致项清单

| 文档 | 章节 | 描述内容 | 实际代码 | 差异说明 |
|------|------|----------|----------|----------|
| `c:\workspace\bobcrm\docs\design\ARCH-XX.md` | X.X | | | |

### 缺失文档

| 功能/模块 | 应有文档 | 状态 |
|-----------|----------|------|
| | | |

### 过时文档

| 文档 | 过时内容 | 建议操作 |
|------|----------|----------|
| | | |
```

---

# 第五阶段：生成改进计划

## 5.1 改进计划模板

基于评审发现，生成以下文档，保存到：
`c:\workspace\bobcrm\docs\plans\PLAN-XX-审查改进计划-YYYY-MM.md`

```markdown
# PLAN-XX-代码审查改进计划-YYYY-MM

**创建日期**: YYYY-MM-DD
**基于审查**: REVIEW-XX-全面代码审查-YYYY-MM
**优先级说明**: P0(阻断)>P1(重要)>P2(一般)>P3(优化)

---

## 一、问题汇总

| 类别 | P0 | P1 | P2 | P3 | 总计 |
|------|----|----|----|----|------|
| 功能缺陷 | | | | | |
| 代码质量 | | | | | |
| 安全问题 | | | | | |
| 性能问题 | | | | | |
| I18n问题 | | | | | |
| 文档问题 | | | | | |
| **总计** | | | | | |

---

## 二、P0 问题修复计划

### P0-001: [问题标题]
- **问题描述**：
- **影响范围**：
- **修复方案**：
- **涉及文件**：`c:\workspace\bobcrm\[路径]`
- **验收标准**：
- **负责人**：
- **完成状态**：⬜ 待开始

---

## 三、P1 问题修复计划
...

## 四、P2 改进计划
...

## 五、P3 优化建议
...

## 六、执行检查清单

- [ ] P0 问题全部修复
- [ ] P1 问题全部修复
- [ ] 单元测试通过
- [ ] 集成测试通过
- [ ] 文档已同步更新
- [ ] 代码已提交审查
```

---

# 执行检查点

完成每个阶段后，请确认：

## 阶段一完成检查
- [ ] 已阅读所有必读文档
- [ ] 已理解项目规范要求
- [ ] 已识别历史遗留问题
- [ ] 已输出格式化的学习总结

## 阶段二完成检查
- [ ] 已完成所有模块功能评审
- [ ] 已记录功能问题清单（含绝对路径）
- [ ] 已与设计文档对比差距

## 阶段三完成检查
- [ ] 已完成代码质量评审
- [ ] 已记录代码问题清单（含绝对路径和行号）
- [ ] 已提供修复建议

## 阶段四完成检查
- [ ] 已完成文档一致性检查
- [ ] 已记录文档问题清单

## 阶段五完成检查
- [ ] 已生成改进计划文档
- [ ] 问题已分类排序
- [ ] 修复方案已明确

---

# 输出文件清单

完成审查后，应生成以下文档：

1. `c:\workspace\bobcrm\docs\reviews\REVIEW-XX-全面代码审查-YYYY-MM.md` - 审查报告
2. `c:\workspace\bobcrm\docs\plans\PLAN-XX-审查改进计划-YYYY-MM.md` - 改进计划
3. `c:\workspace\bobcrm\docs\history\AUDIT-XX-代码审查问题清单-YYYY-MM.md` - 问题详细清单（可选）

文档命名规则：
- XX 为序号，需查看目录中现有最大序号后递增
- YYYY-MM 为审查年月
```

---

## 分阶段执行 Prompts（简化版）

如果一次性执行太长，可分阶段执行：

### 阶段一 Prompt：文档学习

```
请阅读 BobCRM 项目的核心规范文档，包括：
1. c:\workspace\bobcrm\CLAUDE.md
2. c:\workspace\bobcrm\docs\process\STD-01-开发质量标准.md
3. c:\workspace\bobcrm\docs\process\STD-05-多语言开发规范.md
4. c:\workspace\bobcrm\docs\process\STD-06-集成测试规范.md
5. c:\workspace\bobcrm\docs\design\ARCH-20-实体定义管理设计.md
6. c:\workspace\bobcrm\docs\design\ARCH-21-组织与权限体系设计.md
7. c:\workspace\bobcrm\docs\design\ARCH-26-动态枚举系统设计.md
8. c:\workspace\bobcrm\docs\reviews\REVIEW-09-代码评审与改进清单-2025-11.md

阅读完成后，按照 PROMPT-01 第一阶段要求的格式输出：
- 项目核心功能模块清单（含绝对路径）
- 关键架构设计要点
- 必须遵守的规范清单
- 历史遗留问题清单
```

### 阶段二 Prompt：功能评审

```
基于已学习的规范，对 BobCRM 进行功能评审。

检查以下模块的功能完整性：
1. 实体系统 (c:\workspace\bobcrm\src\BobCrm.Api\Services\EntityDefinitionAggregateService.cs)
2. 权限系统 (c:\workspace\bobcrm\src\BobCrm.Api\Services\AccessService.cs)
3. 表单系统 (c:\workspace\bobcrm\src\BobCrm.App\Components\Designer\)
4. 多语言系统 (c:\workspace\bobcrm\src\BobCrm.Api\Services\I18nService.cs)

按照 STD-01 的四个维度（功能性、易用性、美观性、可靠性）进行评估。
输出问题清单，标注优先级(P0/P1/P2)，问题位置使用绝对路径。
```

### 阶段三 Prompt：代码质量评审（分批执行）

由于代码量大，建议分 3-4 次对话完成。每次聚焦一个批次。

**⚠️ 每个批次 Prompt 必须以读取规范文档开头。**

**批次一：后端服务层**
```
请先阅读审查规范文档：c:\workspace\bobcrm\docs\prompts\PROMPT-01-全面代码审查.md

然后执行第三阶段代码质量评审 - 批次1/3：后端服务层

范围：c:\workspace\bobcrm\src\BobCrm.Api\Services\

执行步骤：
1. 用 Glob 列出该目录下所有 .cs 文件
2. 逐个读取并按 PROMPT-01 第3.2节的检查项审查
3. 按 PROMPT-01 第3.3节的格式输出报告
4. 必须列出已检查文件清单和覆盖率
```

**批次二：API端点和基础设施**
```
请先阅读审查规范文档：c:\workspace\bobcrm\docs\prompts\PROMPT-01-全面代码审查.md

然后执行第三阶段代码质量评审 - 批次2/3：API端点和基础设施

范围：
- c:\workspace\bobcrm\src\BobCrm.Api\Endpoints\
- c:\workspace\bobcrm\src\BobCrm.Api\Infrastructure\
- c:\workspace\bobcrm\src\BobCrm.Api\Base\

执行步骤：
1. 用 Glob 列出上述目录所有 .cs 文件
2. 逐个读取并按 PROMPT-01 第3.2节的检查项审查
3. 按 PROMPT-01 第3.3节的格式输出报告
4. 必须列出已检查文件清单和覆盖率
```

**批次三：前端组件**
```
请先阅读审查规范文档：c:\workspace\bobcrm\docs\prompts\PROMPT-01-全面代码审查.md

然后执行第三阶段代码质量评审 - 批次3/3：前端组件

范围：
- c:\workspace\bobcrm\src\BobCrm.App\Components\
- c:\workspace\bobcrm\src\BobCrm.App\Services\

执行步骤：
1. 用 Glob 列出上述目录所有 .razor 和 .cs 文件
2. 逐个读取并按 PROMPT-01 第3.2节的检查项审查
3. 按 PROMPT-01 第3.3节的格式输出报告
4. 必须列出已检查文件清单和覆盖率
```

**汇总报告**
```
请先阅读审查规范文档：c:\workspace\bobcrm\docs\prompts\PROMPT-01-全面代码审查.md

汇总前3批次的审查结果，生成完整的代码质量评审报告。

要求：
1. 合并所有批次的已检查文件清单
2. 计算总覆盖率
3. 合并问题清单，按优先级(P0>P1>P2>P3)排序
4. 按 PROMPT-01 第3.3节格式输出
5. 保存到 c:\workspace\bobcrm\docs\reviews\REVIEW-XX-代码质量评审-YYYY-MM.md
```

### 阶段四 Prompt：生成改进计划

```
基于前面的评审结果，生成改进计划文档。

要求：
1. 问题按优先级分类（P0>P1>P2>P3）
2. 每个问题提供修复方案
3. 提供验收标准
4. 按照 STD-02 文档规范格式化
5. 所有文件路径使用绝对路径

输出文件保存到：c:\workspace\bobcrm\docs\plans\PLAN-XX-审查改进计划-YYYY-MM.md
```

---

## 使用建议

1. **分阶段执行**：建议分 4-5 次对话完成，每次聚焦一个阶段
2. **保存上下文**：每阶段完成后保存输出，供下一阶段参考
3. **迭代审查**：首次审查后，针对发现的问题进行定向深入审查
4. **定期执行**：建议每个版本发布前执行一次全面审查
5. **路径一致性**：所有文件引用统一使用绝对路径 `c:\workspace\bobcrm\`

---

**文档版本**: v1.3
**更新日期**: 2025-12-25
**更新内容**:
- v1.2: 增加第三阶段完整性要求（100%覆盖、分批执行、覆盖率报告）
- v1.3: 所有分批Prompt增加"请先阅读规范文档"前置步骤
**维护者**: BobCRM 开发团队
