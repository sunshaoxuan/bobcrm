# BobCRM v0.9.0 开发任务 - 系统完善与稳定性

## 项目概述

你正在为 **BobCRM** 项目开发 v0.9.0 版本。
此版本不引入新的大型功能模块，而是专注于**系统稳定性**、**文档完善**和**UI/UX 统一**，为 v1.5 (智能分析引擎) 打下坚实基础。

**技术栈**：
- 后端：.NET 8 (C#), Minimal API, EF Core
- 前端：Blazor Server, Ant Design Blazor
- 数据库：PostgreSQL
- 架构：动态实体系统、模板驱动UI、RBAC权限

## 当前状态

✅ **已完成里程碑**:
- **v0.7.0**: 模板系统闭环 (设计-应用-设置-显示)。
- **v0.8.0**: 架构重构 (TemplateService) & 字段级安全 (Field Security)。

🎯 **v0.9.0 目标**:
- **稳定性**: 核心服务单元测试覆盖率 > 80%。
- **文档**: 完善开发者指南和用户手册。
- **体验**: 统一 UI 风格，完成 I18n 国际化。

## 核心任务清单

### T1: 系统稳定性与测试 (高优先级)

#### 目标
为核心业务逻辑编写单元测试，确保系统稳定性。

#### 重点覆盖
1.  **TemplateService**:
    - 测试模板的 CRUD、复制、应用逻辑。
    - 测试系统默认与用户默认的优先级回退机制。
2.  **FieldPermissionService**:
    - 测试权限计算逻辑（Union 策略）。
    - 测试缓存失效机制。
3.  **DynamicEntityService**:
    - 测试实体定义的 CRUD 和同步逻辑。

#### 任务
- [ ] 创建 `BobCrm.Tests` 项目 (xUnit)。
- [ ] 编写 `TemplateServiceTests.cs`。
- [ ] 编写 `FieldPermissionServiceTests.cs`。

### T2: 文档完善 (中优先级)

#### 目标
建立完整的文档体系，方便开发者接手和用户使用。

#### 任务
1.  **开发者文档 (`docs/guides/`)**:
    - [ ] `GUIDE-01-架构概览.md`: 系统分层、核心模块说明。
    - [ ] `GUIDE-02-插件开发.md`: 如何开发新的 Widget。
    - [ ] `GUIDE-03-API指南.md`: 核心 API 使用说明。
2.  **用户手册 (`docs/manual/`)**:
    - [ ] `MANUAL-01-快速开始.md`。
    - [ ] `MANUAL-02-表单设计器使用.md`。
    - [ ] `MANUAL-03-权限管理.md`。

### T3: UI/UX 统一与 I18n (中优先级)

#### 目标
消除界面中的不一致，确保所有文本均已国际化。

#### 任务
1.  **UI 统一**:
    - [ ] 检查所有页面，确保使用 Ant Design 组件（替换原生 HTML 元素）。
    - [ ] 统一页面头部 (`PageHeader`)、面包屑导航。
    - [ ] 统一加载状态 (`Spin`) 和空状态 (`Empty`) 显示。
2.  **I18n 补全**:
    - [ ] 扫描 `src/BobCrm.App`，提取所有硬编码字符串到资源文件。
    - [ ] 重点检查：`FormDesigner.razor`, `RolePermissionTree.razor`。

## 开发规范

- **测试驱动**: 修复 Bug 前先编写复现测试。
- **文档优先**: 修改功能时同步更新文档。
- **代码风格**: 遵循 C# 标准命名规范，保持代码整洁。

---

**下一步 (v1.5)**: 智能分析与报表引擎 (Dashboard Engine)。
