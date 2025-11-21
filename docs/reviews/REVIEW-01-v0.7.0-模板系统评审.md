# 代码评审报告: v0.7.0 模板系统

**日期:** 2025-11-20
**评审人:** Antigravity
**范围:** v0.7.0 模板系统实现 (T1-T7)

## 1. 概述
本次评审涵盖了“设计-应用-配置-展示”闭环模板系统的实现，包括菜单编辑器、表单设计器增强、默认模板生成器、模板管理和页面加载器。

**总体状态:** 功能正常，但在发布生产环境前需要对国际化 (I18n) 和架构解耦进行重大重构。

## 2. 关键问题 (必须修复)

### 2.1 国际化 (I18n) 缺失
代码库包含大量硬编码的中文与英文混合字符串，违反了项目的多语言要求。
*   **`Templates.razor`**: UI 标签（如 "全部用途", "新建模板"）、消息（"加载中...", "模板已复制"）和对话框提示均为硬编码。
*   **`TemplateEndpoints.cs`**: API 错误消息硬编码为中文（如 "模板不存在", "系统默认模板不允许删除"）。
*   **`WidgetRegistry.cs`**: "Tab 1", "Tab 2" 默认标签是硬编码的。
*   **`DefaultTemplateGenerator.cs`**: 虽然使用了翻译键作为按钮（"BTN_ADD"），但硬编码了布局样式和部分逻辑。

### 2.2 硬编码样式与魔术数字
*   **`DefaultTemplateGenerator.cs`**: 硬编码的颜色 (`#fafafa`, `#ffffff`) 和宽度 (`10%`, `30%`, `48%`, `150px`)。这将破坏主题和响应式设计调整。
*   **`Templates.razor`**: 大量使用内联样式 (`style="..."`) 而非 CSS 类。

### 2.3 安全与验证
*   **`TemplateEndpoints.cs`**: `GetMenuTemplateIntersections` 端点执行了复杂的权限逻辑，应集中在服务中以确保一致性和可测试性。

## 3. 架构改进

### 3.1 生成器与实体解耦
*   **问题**: `DefaultTemplateGenerator.cs` 显式检查 "users" 和 "roles" 实体路由（第 357, 376 行）以注入特定组件。
*   **建议**: 实现 `IDefaultTemplateContributor` 模式或类似机制，允许模块注册自己的默认组件，而无需修改核心生成器。

### 3.2 服务层提取
*   **问题**: `TemplateEndpoints.cs` 包含大量业务逻辑，特别是在 `GetMenuTemplateIntersections` 和 `ApplyTemplate` 中。
*   **建议**: 将此逻辑提取到 `TemplateService` 或 `MenuService`。API 端点应仅处理 HTTP 请求/响应映射。

### 3.3 UI/UX 现代化
*   **问题**: `Templates.razor` 使用了 `window.prompt` 和 `window.confirm`。
*   **建议**: 替换为 Ant Design Blazor 的 `Modal` 服务，以获得一致且专业的用户体验。

## 4. 代码质量与次要问题

*   **控制台日志**: `PageLoader.razor` 包含过多的 `console.log` 语句。应删除或包装在调试标志中。
*   **错误处理**: `PageLoader.razor` 吞掉了布局解析和数据绑定期间的一些异常，这可能会向开发人员隐藏配置错误。
*   **组件注册**: 已验证 `CardWidget` 和 `SubFormWidget` 在 `WidgetRegistry.cs` 中正确注册。

## 5. 行动计划 (v0.7.1)

1.  **I18n 重构**: 将所有硬编码字符串提取到资源文件并使用 `I18nService`。
2.  **重构端点**: 将逻辑从 `TemplateEndpoints.cs` 移动到 `TemplateService`。
3.  **UI 润色**: 用 Ant Design 组件替换原生 alert/prompt，并将内联样式移动到 CSS。
4.  **解耦生成器**: 重构 `DefaultTemplateGenerator` 以支持插件式组件注入。
