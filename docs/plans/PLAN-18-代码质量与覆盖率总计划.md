# PLAN-18: 代码质量与覆盖率总计划

> **状态**: 已完成 ✅
> **目标**: 零编译警告, 90% 测试覆盖率, 高质量标准
> **说明**: 本计划整合了原 PLAN-20~28 的覆盖率迭代工作

---

## 1. 概述 (Overview)

本总计划整合了 2025 年 12 月"代码质量冲刺"的所有工作，旨在统一管理以下目标：
1.  **编译警告**: 实现并维持零警告 (Zero Warnings)。
2.  **测试覆盖率**: 将 `BobCrm.Api` 的行覆盖率从 ~75% 提升至 90%。
3.  **深度测试**: 超越"快乐路径" (Happy Path)，覆盖复杂逻辑、并发和边缘情况。

### 1.1 记分卡 (截至 2026-01-05)

| 指标 | 目标 | 当前 | 状态 | 说明 |
| :--- | :--- | :--- | :--- | :--- |
| **编译警告** | **0** | **0** | ✅ 已达成 | CI 基线已强制锁定 |
| **测试覆盖率** | **90%** | **90.00%** | ✅ 已达成 | `LineRate=90.00% Covered=16925 Valid=18805` |
| **测试通过率** | **100%** | **100%** | ✅ 稳定 | `dotnet test -c Release` 全通过（1229 个测试） |
| **分支覆盖率** | **70%** | **71.28%** | ✅ 已达成 | 超越目标 +1.28% |

---

## 2. 编译告警清零 (已完成)

### 2.1 问题分析
项目曾积累了 18 个持久性警告，主要是 `BL0006` ("不要访问 RenderTreeBuilder 的内部成员")，源于 `ButtonWidgetTests.cs` 中的旧测试代码。

### 2.2 解决方案
- **决策**: 拒绝使用 `#pragma` 抑制。统一采用官方推荐的 `bUnit` 框架进行组件测试。
- **行动**: 使用 `bUnit` 重构了 `ButtonWidgetTests.cs`。
- **结果**:
    - 警告数降为 0。
    - `scripts/check-warning-baseline.ps1` 已更新，任何新警告都会导致构建失败。

---

## 3. 深度测试策略 (覆盖率分析)

### 3.1 逻辑缺口 (The Logic Gap)
此前的覆盖率 (~82%) 存在虚高，大量使用了 Mock 绕过复杂逻辑的"快乐路径"测试。真正的"深度测试"需要验证：
1.  **副作用**: 数据库状态变更 (不仅仅是 HTTP 200)。
2.  **并发性**: 锁定机制 (`EntityLockService`)。
3.  **复杂流**: 模板生成算法、动态实体的递归处理。

### 3.2 重点攻坚目标 (Top 5 Uncovered Areas)
以下 5 个文件占了约 27% 的覆盖率缺口：

| 排名 | 文件 | 初始覆盖率 | 策略 |
| :--- | :--- | :--- | :--- |
| 1 | `EntityDefinitionEndpoints.cs` | 53.3% | **全量 CRUD 测试** (Phase 2) |
| 2 | `FunctionService.cs` | ~62% | 深度权限/菜单/树构建测试 |
| 3 | `LayoutEndpoints.cs` | ~79% | 布局端点边界条件覆盖 |
| 4 | `AdminEndpoints.cs` | ~72% | 系统运维测试 |
| 5 | `EntityPublishingService.cs` | ~81% | 发布失败/回滚/并发测试 |

---

## 4. 执行计划 (当前冲刺)

### Phase 1: 动态实体运行时 (高价值) ✅
**目标**: 覆盖核心引擎与并发逻辑。
- [x] **DynamicEntityEndpoints**: 验证 raw/count/delete/filter 路径 (`DynamicEntityEndpointsCrudTests.cs`).
- [x] **EntityLockService**: 验证递归锁定与过期机制 (`EntityLockServiceTests.cs`).

### Phase 2: 模板系统 (高复杂度) 🔄
**目标**: 覆盖复杂的 UI 生成逻辑。
- [x] **Template Endpoints**: 验证 CRUD 及"系统到用户"的复制/应用逻辑 (`TemplateEndpointsCrudTests.cs`).
- [x] **Template Generator**: 验证布局计算 (`DefaultTemplateGeneratorTests.cs`).
- [x] **Entity Definitions**: 覆盖巨大的 `EntityDefinitionEndpoints.cs` (`EntityDefinitionEndpointsCrudTests.cs`).

### Phase 3: 系统与认证 (稳定性) 🔄
**目标**: 覆盖配置与边缘情况。
- [x] **Auth Endpoints**: 验证失败策略与锁定 (`AuthEndpointsTests.cs`).
- [x] **System Endpoints**: 验证菜单/枚举结构 (`SystemEndpointsTests.cs`, `EnumDefinitionEndpointsCrudTests.cs`).
- [x] **Service Layer**: 覆盖 `ReflectionPersistenceService` (`ReflectionPersistenceServiceTests.cs`).

### Phase 4: 收尾 (Final Sweep)
- [x] **Aggregation**: 覆盖 `EntityAggregateEndpoints` 和 `AggVOService` (`EntityAggregateEndpointsCrudTests.cs`, `AggVOServiceIntegrationTests.cs`).
- [x] **Final Report**: 生成最终 Cobertura 报告并验证 90% 指标（`LineRate=90.00%`）。

---

## 4.1 Phase 5 (已完成)

- [x] `EntityDefinitionEndpointsCrudTests.cs` 扩展
- [x] `EntityAggregateEndpointsCrudTests.cs` 新建
- [x] `AggVOServiceIntegrationTests.cs` 新建
- [x] `ReflectionPersistenceServiceTests.cs` 强化
- [x] `ReflectionPersistenceService.cs` 动态查询修复

---

## 4.2 Phase 6 (已完成)

- [x] `FunctionServiceDeepTests.cs` 新建 - 树构建、权限继承测试
- [x] `LayoutEndpointsEdgeTests.cs` 新建 - 边界条件测试
- [x] `AdminEndpointsErrorPathTests.cs` 新建 - 异常路径测试
- [x] `EntityDefinitionFieldCrudTests.cs` 新建 - 字段级 CRUD
- [x] `EntityPublishingServiceTests.cs` 扩展 - 回滚/锁定/破坏性变更

**成果**: 覆盖率从 82.65% → 87.0% (+4.35%)

---

## 4.3 Phase 7 (已完成)

- [x] `DDLExecutionServiceSqliteTests.cs` 新建 - 真实 SQLite 执行/批量事务回滚
- [x] `DefaultI18nServiceTests.cs` 新建 - 语言切换/事件触发
- [x] `EntityDefinitionAppServicePhase7Tests.cs` 新建 - 状态转换/字段保护/锁定
- [x] `DynamicEntityServicePhase7Tests.cs` 新建 - 类型加载/批量编译
- [x] `EntityMenuRegistrarPhase7Tests.cs` 新建 - 层级规范化/权限绑定
- [x] `AccessServicePhase7Tests.cs` 新建 - 时间窗口/数据范围评估

**成果**: 测试数 1010 → 1038 (+28)

---

## 4.4 Phase 8 任务规划

**目标**: 从 86.66% → 90%，需覆盖约 1,386 行代码。✅ 已达成（`LineRate=90.00%`）

### 已完成 (2026-01-05)

- [x] `EntityDefinitionEndpointsPhase8Tests.cs` - 端点异常/边界分支
- [x] `EntityDefinitionSynchronizerTests.cs` - 同步/补全/Reset/模板绑定
- [x] `FieldFilterExtensionsTests.cs` - 0%→高覆盖，过滤器全覆盖
- [x] `FieldPermissionEndpointsTests.cs` - 端点层 Mock 测试

### 任务清单 (历史记录)

以下为 Phase 8 规划期的目标清单，现已全部完成并通过门禁（`check-warning-baseline=0/0`, `dotnet test -c Release`, `LineRate=90.00%`）。

| # | 文件 | 未覆盖行 | 当前覆盖 | 目标 | 测试文件 |
|---|------|---------|---------|------|---------|
| 1 | `EntityPublishingService.cs` | 131 | 80.9% | 90% | `EntityPublishingServicePhase8Tests.cs` |
| 2 | `TemplateRuntimeService.cs` | 84 | 57.1% | 80% | `TemplateRuntimeServiceTests.cs` (新建) |
| 3 | `FieldPermissionService.cs` | 89 | 64.1% | 80% | `FieldPermissionServiceTests.cs` (新建) |
| 4 | `SettingsEndpoints.cs` | 60 | 48.7% | 75% | `SettingsEndpointsTests.cs` (新建) |
| 5 | `SetupEndpoints.cs` | 64 | 64.8% | 80% | `SetupEndpointsTests.cs` (新建) |
| 6 | `AccessEndpoints.cs` | 96 | 69.5% | 85% | `AccessEndpointsPhase8Tests.cs` (新建) |
| 7 | `AggVOService.cs` | 103 | 67.2% | 80% | `AggVOServicePhase8Tests.cs` (新建) |

### 测试策略要点

1. **EntityPublishingService**: 发布流程异常路径、DDL 生成失败处理
2. **TemplateRuntimeService**: 运行时模板解析、字段绑定、验证
3. **FieldPermissionService**: 字段级权限检查、缓存逻辑
4. **SettingsEndpoints**: 系统设置读写、验证
5. **SetupEndpoints**: 初始化流程、配置检查
6. **AccessEndpoints**: 权限端点边界条件、数据范围
7. **AggVOService**: 聚合操作深度测试、级联删除

### 预期成果

- 覆盖 ~700-900 行新代码
- 行覆盖率提升至 ~89-90%
- 分支覆盖率提升至 ~70%

---

## 4.5 Final Sprint (90% 冲刺)

**目标**: 从 88.57% → 90%，需覆盖约 491 行代码。

> ✅ **分支覆盖率已达成**: 71.28% > 70% 目标

### 低覆盖文件分析 (2026-01-05 最新 Cobertura)

| # | 文件 | 未覆盖行 | 当前覆盖 | 目标覆盖 | 测试文件 |
|---|------|---------|---------|---------|---------|
| 1 | `EntityDefinitionEndpoints.cs` | 165 | 83.3% | 90% | `EntityDefinitionEndpointsFinalTests.cs` |
| 2 | `LayoutEndpoints.cs` | 85 | 87.6% | 92% | `LayoutEndpointsFinalTests.cs` |
| 3 | `EntityPublishingService.cs` | 82 | 84.9% | 92% | `EntityPublishingServiceFinalTests.cs` |
| 4 | `EntityDefinitionAppService.cs` | 79 | 70.6% | 85% | `EntityDefinitionAppServiceFinalTests.cs` |
| 5 | `AdminEndpoints.cs` | 76 | 84.2% | 90% | `AdminEndpointsFinalTests.cs` |
| 6 | `ReflectionPersistenceService.cs` | 68 | 76.6% | 85% | `ReflectionPersistenceFinalTests.cs` |
| 7 | `FunctionService.cs` | 67 | 84.0% | 90% | `FunctionServiceFinalTests.cs` |
| 8 | `AggVOService.cs` | 58 | 74.4% | 85% | `AggVOServiceFinalTests.cs` |

### 测试策略要点

1. **EntityDefinitionEndpoints**: 剩余 CRUD 边界、批量操作、错误路径
2. **LayoutEndpoints**: 布局生成、权限边界、scope 处理
3. **EntityPublishingService**: DDL 生成失败、状态回滚、并发处理
4. **EntityDefinitionAppService**: 更新/删除边界、接口保护
5. **AdminEndpoints**: 系统管理操作、重置/诊断路径
6. **ReflectionPersistenceService**: 动态查询、分页、排序
7. **FunctionService**: 树构建、权限继承、缓存失效
8. **AggVOService**: 级联操作、事务一致性

### 预期成果

- 覆盖 ~500 行新代码
- 行覆盖率达成 **90%** 目标
- 分支覆盖率维持 **>70%**

---

## 5. 执行记录日志

- **2025-12-28**: 编译告警清零完成
- **2025-12-29 (AM)**: 深度测试策略确立
- **2025-12-29 (PM)**:
    - Phase 1 & Phase 2 (Part A) 完成
    - 新增测试: `DynamicEntityEndpointsCrudTests`, `EntityLockServiceTests`, `TemplateEndpointsCrudTests`, `DefaultTemplateGeneratorTests`
    - 缺陷修复: `TemplateService.ApplyTemplateAsync` (临时主键问题)
    - 工具新增: `scripts/coverage-summary.ps1`
- **2025-12-31**: 覆盖率达到 82.91%，测试数量 905 个
- **2025-12-31 (PM)**:
    - 补齐：`EntityDefinitionEndpointsCrudTests.cs`、`EntityAggregateEndpointsCrudTests.cs`、`AggVOServiceIntegrationTests.cs`、`ReflectionPersistenceServiceTests.cs`
    - 修复：`ReflectionPersistenceService` 的动态查询（Where/OrderBy/Skip/Take）以保证可翻译并可测试
    - 验证：`pwsh scripts/check-warning-baseline.ps1` = 0/0；`dotnet test -c Release` 全通过
    - 覆盖率：BobCrm.Api（排除项生效）当前 82.65%（XPlat Cobertura 输出）

- **2025-12-31 (Late)**:
    - Phase 6 深度测试补齐：`FunctionServiceDeepTests.cs`、`LayoutEndpointsEdgeTests.cs`、`AdminEndpointsErrorPathTests.cs`、`EntityDefinitionFieldCrudTests.cs`
    - Phase 6 增强：`EntityPublishingServiceTests.cs`（回滚/循环依赖/锁定+破坏性变更分支）
    - 门禁：`pwsh scripts/check-warning-baseline.ps1` = 0/0；`dotnet test -c Release` 全通过

- **2026-01-03**:
    - Phase 7 测试补齐：`DDLExecutionServiceSqliteTests.cs`、`DefaultI18nServiceTests.cs`、`EntityDefinitionAppServicePhase7Tests.cs`、`DynamicEntityServicePhase7Tests.cs`、`EntityMenuRegistrarPhase7Tests.cs`、`AccessServicePhase7Tests.cs`
    - 门禁：`pwsh scripts/check-warning-baseline.ps1` = 0/0；`dotnet test -c Release` 全通过

- **2026-01-05**:
    - Phase 8（部分）补齐：`EntityDefinitionEndpointsPhase8Tests.cs`、`FieldFilterExtensionsTests.cs`、`FieldPermissionEndpointsTests.cs`；扩展 `EntityDefinitionSynchronizerTests.cs`
    - 门禁：`pwsh scripts/check-warning-baseline.ps1` = 0/0；`dotnet test -c Release` 全通过

- **2026-01-05 (late)**:
    - Phase 8（剩余项推进）：`EntityPublishingServicePhase8Tests.cs`、`AggVOServicePhase8Tests.cs`、`AccessEndpointsPhase8Tests.cs`、`TemplateRuntimeServiceTests.cs`；扩展 `FieldPermissionServiceTests.cs`、`SetupEndpointsTests.cs`、`SettingsEndpointsTests.cs`
    - 门禁：`pwsh scripts/check-warning-baseline.ps1` = 0/0；`dotnet test -c Release` 全通过（1095 tests）

- **2026-01-05 (night)**:
    - 追加补齐：`EntityDefinitionEndpointsPhase9Tests.cs`、`LayoutEndpointsPhase10Tests.cs`、`AdminEndpointsPhase10Tests.cs`、`DbSmtpEmailSenderTests.cs`、`InterfaceFieldMappingTests.cs`
    - 覆盖率：BobCrm.Api（排除项生效）当前 87.39%（XPlat Cobertura 输出）
    - 门禁：`pwsh scripts/check-warning-baseline.ps1` = 0/0；`dotnet test -c Release` 全通过（1136 tests）
