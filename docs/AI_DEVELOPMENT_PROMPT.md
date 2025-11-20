# BobCRM v0.7.0 开发任务 - AI 开发指引

## 项目概述

你正在为 **BobCRM** 项目开发 v0.7.0 版本，这是一个基于 .NET 8 + Blazor Server + PostgreSQL 的 No-Code/Low-Code CRM 系统。

**技术栈**：
- 后端：.NET 8 (C#), Minimal API, EF Core
- 前端：Blazor Server, Ant Design Blazor
- 数据库：PostgreSQL
- 架构：动态实体系统、模板驱动UI、RBAC权限

## 当前任务

请按照 `docs/planning/PLAN-01-v0.7.0-菜单导航完善.md` 执行开发，聚焦于**完善菜单导航系统**。

**核心目标**：
1. 完善菜单编辑器的用户体验
2. 编写完整的使用指南文档
3. 优化性能和稳定性

## 开发任务清单

### T1: 编写菜单编辑器使用指南 (高优先级)

**目标**：更新 `docs/guides/GUIDE-06-菜单编辑器使用指南.md`

**当前问题**：
- 文档开头写着"计划中 - 功能尚未实现"，但实际 `MenuManagement.razor` 已完整实现（782行代码）
- 内容是"规划步骤"而不是"实际使用步骤"

**任务**：
1. 将文档状态从"计划中"改为"已实现"
2. 编写实际使用步骤：
   - 如何访问菜单编辑器 (`/menus`)
   - 如何新增菜单节点（根节点、子节点）
   - 如何配置路由/模板双导航模式
   - 如何拖拽排序（before/after/into三种位置）
   - 如何设置图标（目前是文本输入）
   - 如何保存和验证
3. 添加常见问题（FAQ）
4. 参考代码：`src/BobCrm.App/Components/Pages/MenuManagement.razor`

**验收标准**：
- 文档准确反映当前实现
- 用户可以按文档操作使用菜单编辑器

---

### T2: 图标选择器组件 (高优先级)

**目标**：替代当前的文本输入，提供可视化图标选择器

**任务**：
1. 创建 `src/BobCrm.App/Components/Shared/IconSelector.razor`
2. 列出 Ant Design Blazor 常用图标（参考 IconType.Outline）
3. 实现图标搜索功能
4. 集成到 `MenuManagement.razor` 的图标输入字段
5. 向后兼容文本输入

**参考**：
- Ant Design Blazor 图标库：https://antblazor.com/en-US/components/icon
- 当前实现：`MenuManagement.razor` 第 127-129 行（文本输入）

**验收标准**：
- 用户可以通过选择器选择图标
- 支持搜索过滤
- 选择后实时预览

---

### T3: 菜单导入/导出功能 (中优先级)

**目标**：支持菜单配置的导入/导出

**后端任务**：
1. 创建 API 端点：
   - `GET /api/access/functions/export`（导出菜单树为JSON）
   - `POST /api/access/functions/import`（导入菜单树）
2. 实现在 `src/BobCrm.Api/Endpoints/AccessEndpoints.cs`

**前端任务**：
1. 在 `MenuManagement.razor` 添加"导出"和"导入"按钮
2. 导出功能：下载 JSON 文件
3. 导入功能：上传 JSON 文件，预览后确认导入
4. 检测冲突的功能码并提示

**JSON 格式示例**：
```json
{
  "version": "1.0",
  "exportDate": "2025-11-20T16:00:00Z",
  "functions": [
    {
      "code": "SYS",
      "displayName": {"zh": "系统管理", "en": "System"},
      "icon": "setting",
      "children": [...]
    }
  ]
}
```

---

### T4-T6: 次优先级任务

- **T4**: 菜单实时预览优化（保存后自动刷新左侧菜单）
- **T5**: 错误处理增强（更友好的错误提示）
- **T6**: 权限验证测试（自动化测试）

详见 `docs/planning/PLAN-01-v0.7.0-菜单导航完善.md`

## 开发规范

### 代码规范

参考 `CLAUDE.md` 中的约定：

1. **命名规范**：
   - 文件名：PascalCase（如 `IconSelector.razor`）
   - API 端点：小写+连字符（如 `/api/access/functions/export`）
   - 服务方法：PascalCase（如 `ExportFunctionsAsync`）

2. **多语言**：
   - 所有用户可见文本使用 `I18n.T("KEY")`
   - 在 `src/BobCrm.Api/Resources/locales/*.json` 添加多语言键值

3. **Git 提交**：
   - 格式：`<type>(<scope>): <subject>`
   - 类型：feat, fix, docs, refactor, test
   - 示例：`feat(menu): add icon selector component`

### 关键文件路径

- **前端组件**：`src/BobCrm.App/Components/`
  - Pages: 页面组件
  - Shared: 共享组件
- **后端 API**：`src/BobCrm.Api/`
  - Endpoints: API 端点
  - Services: 业务逻辑
- **模型**：`src/BobCrm.Api/Base/Models/`
- **文档**：`docs/`
  - guides: 使用指南
  - planning: 开发计划
  - design: 架构设计

## 工作流程

### 开发流程

1. **阅读文档**：
   - 查看 `PLAN-01-v0.7.0-菜单导航完善.md` 了解任务
   - 查看 `CLAUDE.md` 了解项目结构和规范
   - 查看相关 ARCH 文档了解架构

2. **实现功能**：
   - 按任务清单逐项实现
   - 编写代码时遵循项目规范
   - 添加必要的注释和文档

3. **测试验证**：
   - 手动测试功能
   - 编写单元测试（如需）
   - 验证多语言支持

4. **更新文档**：
   - 更新相关使用指南（如 GUIDE-06）
   - 更新 CHANGELOG.md（添加新功能条目）
   - 标记 PLAN-01 中的任务为完成

5. **提交代码**：
   - 使用规范的 commit 消息
   - 一个功能一个 commit
   - 重要变更提 PR

### 完成一个任务后

1. 在 `PLAN-01-v0.7.0-菜单导航完善.md` 中标记任务为完成（✅）
2. 更新 `CHANGELOG.md` 添加变更记录
3. 如果完成了里程碑，更新 `ROADMAP.md`
4. 提交代码到 git

## 参考文档

**必读文档**：
- `CLAUDE.md` - AI 开发者指南（项目结构、规范）
- `docs/planning/PLAN-01-v0.7.0-菜单导航完善.md` - 详细任务清单
- `docs/ROADMAP.md` - 产品路线图

**架构文档**：
- `docs/design/ARCH-24-紧凑型顶部菜单导航设计.md` - 菜单导航设计
- `docs/design/ARCH-21-组织与权限体系设计.md` - 权限体系

**关键代码**：
- `src/BobCrm.App/Components/Pages/MenuManagement.razor` - 菜单编辑器（已实现）
- `src/BobCrm.Api/Endpoints/AccessEndpoints.cs` - 权限和菜单 API
- `src/BobCrm.App/Services/MenuService.cs` - 菜单服务

## 开始开发

**推荐顺序**：

1. **先做 T1（文档）**
   - 这是最简单的任务
   - 帮助你熟悉现有功能
   - 无需写代码

2. **再做 T2（图标选择器）**
   - 独立组件，影响范围小
   - 提升用户体验明显
   - 技术难度适中

3. **然后 T3（导入导出）**
   - 需要前后端配合
   - 功能相对独立

4. **最后 T4-T6（优化和测试）**
   - 在核心功能完成后进行
   - 可根据实际情况调整优先级

## 提示

- 遇到问题时，先查看 `CLAUDE.md` 和相关 ARCH 文档
- 参考现有代码风格（如 `MenuManagement.razor`）
- 保持与现有功能的一致性
- 多写注释，特别是复杂逻辑
- 及时更新文档

---

**祝开发顺利！如有疑问，请参考项目文档或咨询项目负责人。**
