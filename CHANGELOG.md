# Changelog

本文档记录 BobCRM 项目的所有重要变更。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

---

## [0.5.3] - 2025-11-05

### 新增 (Added)
- **EntitySelector 通用实体选择器组件** (237行)
  - 输入框 + 放大镜图标的用户友好界面
  - Modal 弹出框卡片式选择界面
  - 泛型支持、懒加载、搜索过滤
  - 支持自定义渲染（图标/标题/描述/元数据）
- **ISelectableEntity 接口** - 规范可选择实体的必需属性
  - Value: 唯一标识
  - DisplayName: 显示名称（已翻译）
  - Description: 描述（已翻译，可选）
  - Icon: 图标（可选）

### 变更 (Changed)
- **EntityMetadata 结构规范化**：
  - EntityType（主键）：改为存储类全名（如 `BobCrm.Api.Domain.Customer`）
  - EntityName（新增）：类短名（如 `Customer`）
  - EntityRoute（新增）：URL路由名（如 `customer`）
  - 用于精确反射查找和反向检查
- **FormDesigner 实体类型选择**：
  - 使用 EntitySelector 替代下拉框
  - 实现实体类型锁定机制（已有组件的模板不可修改实体类型）
  - 数据加载时自动翻译 DisplayNameKey 为 DisplayName
  - 输入框正确显示翻译后的实体名称（如"顧客"）

### 修复 (Fixed)
- **全局布局滚动条问题**：
  - html/body 设置 `height: 100%` 和 `overflow: hidden`
  - app-shell 改为固定高度 `height: 100vh`
  - 各区域（侧边栏/内容区/设计器面板）独立滚动
  - 符合专业设计器布局规范
- **AntDesign Select 动态选项不显示问题**：
  - 尝试了10余种方案均无法稳定工作
  - 最终通过 EntitySelector 彻底解决

### 文档 (Documentation)
- 更新 `docs/实体元数据自动注册机制.md` - 反映 EntityMetadata 结构变更
- 更新 `docs/客户信息管理系统设计文档.md` - 添加 EntitySelector 组件说明
- 新增 `CHANGELOG.md` - 统一管理所有版本更新历史

---

## [0.5.2] - 2025-11-05

### 新增 (Added)
- **Checkbox 组件** - 复选框/复选框组
  - 支持单个复选框或复选框组
  - 支持按钮样式（ButtonStyle）
  - 支持横向/纵向布局（Direction）
  - 默认值支持（多选时逗号分隔）
- **Radio 组件** - 单选按钮组
  - 标准单选按钮组
  - 支持按钮样式（ButtonStyle）
  - 支持横向/纵向布局（Direction）
  - 默认值支持
- **模板实体类型动态选择**：
  - 从硬编码改为动态从 `/api/entities` 加载
  - FormDesigner 点击画布背景显示模板属性
  - 实体类型下拉框（后在 v0.5.3 改为 EntitySelector）

### 修复 (Fixed)
- **模板加载功能**：实现 `LoadTemplate()` 方法
- **多语言资源补充**：
  - Number, Select, Textarea, Button, Panel, Grid, Tab 组件
  - 模板、实体类型、新建模板等 FormDesigner UI 元素
- **WidgetRegistry 图标错误**：NumberOutlined → FieldNumber
- **Alert 组件属性**：添加 `ShowIcon="true"`

### 变更 (Changed)
- **实体元数据管理**：
  - 从硬编码改为数据库驱动（EntityMetadata 表）
  - 实现自动注册/反注册机制
  - Customer 实现 IEntityMetadataProvider 接口
- **脚本优化**：
  - `verify-setup.ps1` 接受 .NET 8 或更高版本
  - 移除未使用的变量警告

---

## [0.5.1] - 2025-11-05

### 新增 (Added)
- **字段动作（Field Actions）** 功能：
  - RDP 下载：根据字段值生成 .rdp 文件
  - 文件验证：验证文件路径是否存在
  - Mailto 链接：生成邮件链接
  - 后端 API：`/api/actions/rdp/download`, `/api/actions/file/validate`, `/api/actions/mailto/generate`
  - 前端服务：FieldActionService
  - 集成测试：12个测试用例
- **对齐线功能（Snap Guides）**：
  - 拖拽时显示蓝色对齐参考线
  - 支持左/右/中心/上/下/中间对齐
  - 吸附阈值 8px
  - 性能优化：使用 requestAnimationFrame

### 变更 (Changed)
- **CustomerDetail 重构**：
  - 提取工具类：FileNameHelper, WidgetStyleHelper, WidgetSerializationHelper
  - 提取管理类：EditValueManager, TabStateManager
  - 提取辅助类：WidgetNavigationHelper, WidgetLabelHelper
  - 从 2750 行减少到 2348 行

### 文档 (Documentation)
- 更新 README.md - 标记字段动作功能为已完成
- 更新 `docs/接口文档.md` - 添加字段动作 API 文档
- 更新 `docs/客户信息管理系统设计文档.md` - 添加对齐线验证用例

---

## [0.5.0] - 2025-11-05

### 架构重构 (Refactoring)

**重大变更**：从单体架构重构为单一职责架构

#### 问题
- ❌ CustomerDetail.razor 混合了设计器、浏览器、编辑器三种职责
- ❌ 2750+ 行代码，难以维护
- ❌ 所有功能都与 Customer 实体强耦合

#### 解决方案
1. **FormDesigner.razor** (452行) - 通用表单设计器
   - 纯粹的布局设计，不依赖任何实体
   - 从 FieldDefinitions API 加载字段
   - 路由：`/designer` 或 `/designer/{templateId}`

2. **PageLoader.razor** (430行) - 通用页面加载器
   - 根据模板动态渲染任何实体
   - 路由：`/{entityType}/{id}`
   - 支持 Browse 和 Edit 模式

3. **CustomerDetail.razor** (17行 → 删除)
   - 简化为路由别名，后完全移除
   - 功能完全由 PageLoader 承担

#### 成果
- ✅ 代码量：2750行 → 882行（三个组件总和）
- ✅ 职责分离：设计 | 渲染 | 数据加载
- ✅ 可复用性：支持任意实体类型
- ✅ 可维护性：大幅提升

### 新增 (Added)
- **布局组件化**：
  - MainLayout, SiderLayout, SimpleLayout
  - EntityListSiderBase 抽象基类
- **FormTemplate 模型** - 表单模板元数据
- **WidgetRegistry** - 中央化Widget注册表

---

## [0.4.0] - 2025-11-04

### 新增 (Added)
- **Widget 系统完善**：
  - Number, Select, Textarea, Button, Calendar, Listbox
  - Panel, Grid, TabContainer, Tab
  - 共17种Widget类型
- **拖拽设计器**：
  - 组件工具栏（基础组件/布局组件）
  - 属性面板（动态编辑Widget属性）
  - 拖拽添加、调整大小、删除组件
- **运行态渲染**：
  - Browse 模式（只读）
  - Edit 模式（可编辑）
  - 字段数据绑定

### 文档 (Documentation)
- 初始版本的系统设计文档
- API 接口文档
- 测试指南

---

## [0.3.0] - 2025-11-03

### 新增 (Added)
- **国际化（i18n）**：中文、日文、英文三语支持
- **用户偏好设置**：主题、主题色、语言持久化
- **主题系统**：浅色/深色主题切换，自定义主题色
- **PostgreSQL 支持**：主数据库切换到 PostgreSQL
- **Docker Compose**：一键启动开发环境

---

## [0.2.0] - 2025-11-02

### 新增 (Added)
- **认证系统**：ASP.NET Identity + JWT
  - 注册、登录、登出、刷新令牌
  - 会话重连（服务器重启不掉线）
- **动态字段系统**：
  - FieldDefinition, FieldValue
  - JSONB 存储
- **客户访问控制**：CustomerAccess 表

---

## [0.1.0] - 2025-11-01

### 新增 (Added)
- **项目初始化**：
  - Blazor Server 前端
  - ASP.NET Core Web API 后端
  - EF Core + SQLite
- **基础实体**：
  - Customer（客户）
  - User（用户）
- **基础 CRUD**：
  - 客户列表、详情、创建、编辑

---

## 约定说明

### 变更类型
- `Added` - 新增功能
- `Changed` - 既有功能的变更
- `Deprecated` - 即将移除的功能
- `Removed` - 已移除的功能
- `Fixed` - 问题修复
- `Security` - 安全相关修复
- `Documentation` - 文档变更
- `Refactoring` - 代码重构（不改变功能）

### 版本号规则
- **主版本号（Major）**：不兼容的 API 修改
- **次版本号（Minor）**：向下兼容的功能性新增
- **修订号（Patch）**：向下兼容的问题修正

---

**维护者**：BobCRM 开发团队  
**最后更新**：2025-11-05
