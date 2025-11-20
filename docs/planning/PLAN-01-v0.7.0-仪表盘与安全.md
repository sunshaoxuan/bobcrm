# v0.7.0 开发计划 - 仪表盘引擎与安全增强

## 概述

本文档是 BobCRM v0.7.0 的详细开发计划，聚焦于数据可视化（仪表盘引擎）和安全性增强。

**来源**：基于 [ROADMAP.md](../ROADMAP.md) 1.3、1.4 节的功能规划

**版本目标**：
1. 提供拖拽式仪表盘设计器，支持图表组件和实时数据展示
2. 实现字段级安全控制，增强数据访问权限管理
3. 为后续的数据导入/导出功能打下基础

---

## 任务优先级

| 编号 | 任务名称 | 优先级 | 估算时间 | 依赖 | 负责人 |
|------|----------|--------|----------|------|--------|
| T1 | 仪表盘数据源适配器 | 高 | 2天 | 无 | TBD |
| T2 | 图表组件库集成 | 高 | 2天 | 无 | TBD |
| T3 | 仪表盘设计器 MVP | 高 | 3天 | T1, T2 | TBD |
| T4 | 仪表盘运行时渲染 | 高 | 2天 | T3 | TBD |
| T5 | 字段权限模型与API | 中 | 2天 | 无 | TBD |
| T6 | 字段权限配置 UI | 中 | 2天 | T5 | TBD |
| T7 | 表单运行时权限过滤 | 中 | 1天 | T5, T6 | TBD |

---

## T1: 仪表盘数据源适配器

### 目标
为仪表盘组件提供统一的数据源接口，支持从现有 DataSet 基础设施获取数据。

### 任务清单
- [ ] 定义 `IDashboardDataSource` 接口
- [ ] 实现 `EntityDataSource`（基于 DataSet）
- [ ] 实现 `AggregationDataSource`（聚合查询）
  - [ ] Count, Sum, Average, Min, Max
  - [ ] Group By 支持
- [ ] 添加数据缓存机制（可选）
- [ ] 单元测试覆盖

### 验收标准
- ✅ 可以通过配置化的 JSON 定义数据源
- ✅ 支持实体查询和聚合查询
- ✅ 数据源可以被仪表盘设计器引用
- ✅ 包含至少 10 个单元测试

### 实施细节
**文件**:
- `src/BobCrm.Api/Abstractions/IDashboardDataSource.cs`
- `src/BobCrm.Api/Services/Dashboard/EntityDataSource.cs`
- `src/BobCrm.Api/Services/Dashboard/AggregationDataSource.cs`

**数据源配置示例**:
```json
{
  "type": "entity",
  "entityType": "customers",
  "filters": [{"field": "status", "operator": "eq", "value": "active"}],
  "aggregations": [{"field": "revenue", "function": "sum", "alias": "totalRevenue"}]
}
```

---

## T2: 图表组件库集成

### 目标
集成图表库（Blazor-ApexCharts 或 Ant Design Charts），提供基础图表组件。

### 任务清单
- [ ] 评估并选择图表库（ApexCharts vs Ant Design Charts）
- [ ] 安装 NuGet 包并配置
- [ ] 封装图表组件：
  - [ ] LineChart（折线图）
  - [ ] BarChart（柱状图）
  - [ ] PieChart（饼图）
  - [ ] KPICard（KPI 卡片）
- [ ] 实现图表与数据源的绑定逻辑
- [ ] 添加图表主题支持（Light/Dark）

### 验收标准
- ✅ 4 种图表组件可独立使用
- ✅ 图表支持动态数据绑定
- ✅ 图表在 Light/Dark 主题下显示正常
- ✅ 提供示例页面展示所有图表

### 实施细节
**推荐方案**: Blazor-ApexCharts
- 理由：功能丰富、文档完善、社区活跃
- NuGet: `Blazor-ApexCharts`

**文件**:
- `src/BobCrm.App/Components/Dashboard/LineChart.razor`
- `src/BobCrm.App/Components/Dashboard/BarChart.razor`
- `src/BobCrm.App/Components/Dashboard/PieChart.razor`
- `src/BobCrm.App/Components/Dashboard/KPICard.razor`

---

## T3: 仪表盘设计器 MVP

### 目标
实现拖拽式仪表盘设计器，允许用户创建和配置仪表盘布局。

### 任务清单
- [ ] 复用表单设计器的拖拽基础设施
- [ ] 创建 `DashboardDesigner.razor` 页面
- [ ] 实现组件面板（图表、KPI 卡片）
- [ ] 实现画布区域（支持网格布局）
- [ ] 实现属性面板：
  - [ ] 图表类型选择
  - [ ] 数据源配置
  - [ ] 标题、图例、颜色配置
- [ ] 实现保存/加载功能
- [ ] 添加预览模式

### 验收标准
- ✅ 可以拖拽图表组件到画布
- ✅ 可以配置图表的数据源和样式
- ✅ 可以保存仪表盘配置到数据库
- ✅ 可以预览仪表盘效果

### 实施细节
**文件**:
- `src/BobCrm.App/Components/Pages/DashboardDesigner.razor`
- `src/BobCrm.App/Models/Dashboard/DashboardWidget.cs`
- `src/BobCrm.Api/Base/Models/Dashboard.cs`

**路由**: `/dashboard/designer` 或 `/dashboard/designer/{id}`

---

## T4: 仪表盘运行时渲染

### 目标
实现仪表盘的运行时渲染，展示实时数据。

### 任务清单
- [ ] 创建 `DashboardRuntime.razor` 组件
- [ ] 实现网格布局渲染
- [ ] 实现图表组件动态加载
- [ ] 实现数据定时刷新（可配置间隔）
- [ ] 实现全屏模式
- [ ] 添加加载状态和错误处理

### 验收标准
- ✅ 可以正确渲染保存的仪表盘配置
- ✅ 图表显示实时数据
- ✅ 支持自动刷新（30s/1min/5min）
- ✅ 响应式布局，适配不同屏幕尺寸

### 实施细节
**文件**:
- `src/BobCrm.App/Components/Dashboard/DashboardRuntime.razor`

**路由**: `/dashboard/{id}` 或首页 `/`

---

## T5: 字段权限模型与API

### 目标
设计并实现字段级权限控制的后端模型和 API。

### 任务清单
- [ ] 设计 `FieldPermission` 数据模型
  - [ ] RoleId, EntityType, FieldName
  - [ ] CanRead, CanWrite
- [ ] 创建数据库迁移
- [ ] 实现 `FieldPermissionService`
- [ ] 实现 API 端点：
  - [ ] GET `/api/access/roles/{id}/field-permissions`
  - [ ] PUT `/api/access/roles/{id}/field-permissions`
- [ ] 实现 API 响应过滤逻辑（根据权限剔除字段）

### 验收标准
- ✅ 可以为角色配置字段级权限
- ✅ API 响应自动过滤无权访问的字段
- ✅ 权限配置持久化到数据库
- ✅ 单元测试覆盖核心逻辑

### 实施细节
**文件**:
- `src/BobCrm.Api/Base/Models/FieldPermission.cs`
- `src/BobCrm.Api/Services/FieldPermissionService.cs`
- `src/BobCrm.Api/Endpoints/FieldPermissionEndpoints.cs`

---

## T6: 字段权限配置 UI

### 目标
在角色管理页面提供字段权限配置界面。

### 任务清单
- [ ] 在 `Roles.razor` 或详情页添加"字段权限"Tab
- [ ] 实现字段权限配置组件：
  - [ ] 实体类型选择
  - [ ] 字段列表展示（Table）
  - [ ] 读/写权限 Checkbox
- [ ] 实现批量操作（全部可读/可写）
- [ ] 集成保存逻辑

### 验收标准
- ✅ 管理员可以为角色配置字段权限
- ✅ UI 显示当前权限状态
- ✅ 保存后立即生效
- ✅ 提供友好的错误提示

### 实施细节
**文件**:
- `src/BobCrm.App/Components/Shared/FieldPermissionEditor.razor`

---

## T7: 表单运行时权限过滤

### 目标
在表单运行时根据用户角色的字段权限隐藏/禁用字段。

### 任务清单
- [ ] 在 `PageLoader` 加载时获取字段权限
- [ ] 实现字段渲染过滤逻辑：
  - [ ] 无读权限 → 隐藏字段
  - [ ] 无写权限 → 禁用字段（只读）
- [ ] 更新 `RuntimeWidgetRenderer` 支持只读模式
- [ ] 添加权限不足提示（可选）

### 验收标准
- ✅ 用户无读权限的字段不显示
- ✅ 用户无写权限的字段显示为只读
- ✅ 不会泄露敏感字段数据
- ✅ 权限变更后立即生效

### 实施细节
**文件**:
- `src/BobCrm.App/Components/Pages/PageLoader.razor`
- `src/BobCrm.App/Components/Runtime/RuntimeWidgetRenderer.razor`

---

## 里程碑

| 里程碑 | 包含任务 | 完成标志 | 目标日期 | 状态 |
|--------|----------|----------|----------|------|
| **M1: 仪表盘基础设施** | T1, T2 | 数据源和图表组件可用 | Week 1 | ⏳ 计划中 |
| **M2: 仪表盘 MVP** | T3, T4 | 可创建和查看仪表盘 | Week 2 | ⏳ 计划中 |
| **M3: 字段级安全** | T5, T6, T7 | 字段权限功能完整 | Week 3 | ⏳ 计划中 |
| **M4: v0.7.0 发布** | 所有任务 | 集成测试通过，文档更新 | Week 4 | ⏳ 计划中 |

---

## 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| **图表库兼容性问题** | 延期 2-3 天 | 提前进行 POC 验证，准备备选方案 |
| **字段权限性能影响** | API 响应变慢 | 使用缓存机制，优化查询逻辑 |
| **表单设计器复用难度** | 仪表盘设计器延期 | 简化 MVP 功能，先实现静态布局 |

---

## 参考文档

- [ROADMAP.md](../ROADMAP.md) - 产品路线图（方向）
- [ARCH-22-标准实体模板化与权限联动设计](../design/ARCH-22-标准实体模板化与权限联动设计.md) - 权限体系架构
- [CHANGELOG.md](../../CHANGELOG.md) - 版本变更记录

---

## 更新日志

| 日期 | 更新内容 | 更新人 |
|------|----------|--------|
| 2025-11-20 | 初始创建 | AI Assistant |
