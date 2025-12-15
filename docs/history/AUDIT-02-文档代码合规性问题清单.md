# AUDIT-02: 文档代码合规性问题清单

**版本**: 1.1
**创建日期**: 2025-12-15
**最后更新**: 2025-12-15
**审计依据**: STD-01~06 开发规范
**状态**: ✅ 已完成

---

## 1. 概述

本文档记录了依据 BobCRM 开发规范（STD-01~06）对项目文档和代码进行合规性审计的结果。所有问题按优先级分类，待下次迭代解决。

---

## 2. 问题统计

### 2.1 审计发现（原始统计）

| 类别 | P0（阻断） | P1（重要） | P2（建议） | 总计 |
|------|-----------|-----------|-----------|------|
| 文档命名/归类 | 0 | 15 | 24 | 39 |
| 代码单一类型原则 | 0 | 21 | 0 | 21 |
| **总计** | **0** | **36** | **24** | **60** |

### 2.2 当前剩余（更新后）

| 类别 | P0（阻断） | P1（重要） | P2（建议） | 总计 |
|------|-----------|-----------|-----------|------|
| 文档命名/归类 | 0 | 0 | 0 | 0 |
| 代码单一类型原则 | 0 | 0 | 0 | 0 |
| **总计** | **0** | **0** | **0** | **0** |

---

## 3. 文档合规性问题（STD-02）

### 3.1 目录错误 - P1

以下文档存放在错误的目录中，已移动到正确位置：

| # | 原路径 | 新路径 | 原因 | 进度 |
|---|--------|--------|------|------|
| 1 | `docs/history/GUIDE-08-中优先级I18n指南.md` | `docs/guides/GUIDE-08-中优先级I18n指南.md` | GUIDE 类型应在 guides 目录 | 已完成 |
| 2 | `docs/history/OPS-02-容器问题-修复历史.md` | `docs/guides/OPS-02-容器问题-修复历史.md` | OPS 类型应在 guides 目录 | 已完成 |
| 3 | `docs/history/PROC-04-最终清理指南.md` | `docs/process/PROC-04-最终清理指南.md` | PROC 类型应在 process 目录 | 已完成 |
| 4 | `docs/history/PROC-05-I18n扫描规则.md` | `docs/process/PROC-05-I18n扫描规则.md` | PROC 类型应在 process 目录 | 已完成 |
| 5 | `docs/history/PLAN-06-I18n最终清理计划.md` | `docs/plans/PLAN-06-I18n最终清理计划.md` | PLAN 类型应在 plans 目录 | 已完成 |
| 6 | `docs/history/PLAN-07-I18n清理进度.md` | `docs/plans/PLAN-07-I18n清理进度.md` | PLAN 类型应在 plans 目录 | 已完成 |
| 7 | `docs/history/UI-05-阶段3范围调整分析.md` | `docs/design/UI-06-阶段3范围调整分析.md` | UI 类型应在 design 目录 | 已完成 |

### 3.2 无效类型代码 - P1

以下文档使用了非标准的类型代码，已按规范完成更名：

| # | 原文件 | 当前类型 | 新文件 | 进度 |
|---|--------|----------|--------|------|
| 1 | `docs/history/PROJ-01-项目现状分析报告.md` | `PROJ` | `docs/history/AUDIT-03-项目现状分析报告.md` | 已完成 |
| 2 | `docs/history/PROJ-02-模板设计器进度跟踪.md` | `PROJ` | `docs/history/PHASE-06-模板设计器进度跟踪.md` | 已完成 |
| 3 | `docs/reviews/REV-01-v0.7.0-项目审计.md` | `REV` | `docs/reviews/REVIEW-08-v0.7.0-项目审计.md` | 已完成 |
| 4 | `docs/guides/DEV-02-Roslyn环境配置.md` | `DEV` | `docs/guides/GUIDE-10-Roslyn环境配置.md` | 已完成 |
| 5 | `docs/guides/FRONT-01-实体定义与动态实体操作指南.md` | `FRONT` | `docs/guides/GUIDE-11-实体定义与动态实体操作指南.md` | 已完成 |

### 3.3 编号格式错误 - P1

以下文档编号格式不符合规范（应为 01-99 两位数），已按规范完成更名：

| # | 原文件 | 当前编号 | 新文件 | 进度 |
|---|--------|----------|--------|------|
| 1 | `docs/history/UI-00-阶段0-差距报告.md` | `00` | `docs/history/UI-07-阶段0-差距报告.md` | 已完成 |
| 2 | `docs/guides/TEST-00-测试综述.md` | `00` | `docs/guides/TEST-10-测试综述.md` | 已完成 |
| 3 | `docs/migrations/MIGRATION-001-添加模板版本字段.md` | `001` | `docs/migrations/MIGRATION-02-添加模板版本字段.md` | 已完成 |

### 3.4 编号重复 - P2

以下文档存在编号冲突，已完成清理：

| # | 冲突编号 | 原文件 | 调整后 | 处理方式 | 进度 |
|---|----------|--------|--------|----------|------|
| 1 | `ARCH-24` | `docs/design/ARCH-24-紧凑型顶部菜单导航-实施计划.md` | `docs/plans/PLAN-11-紧凑型顶部菜单导航-实施计划.md` | 类型归类调整（实施计划归入 PLAN） | 已完成 |
| 2 | `ARCH-30` | `docs/design/ARCH-30-工作计划.md` | `docs/plans/PLAN-12-系统级多语API架构优化-工作计划.md` | 类型归类调整（工作计划归入 PLAN） | 已完成 |
| 3 | `UI-05` | `docs/design/UI-05-数据网格组件重构-开发提示.md` | `docs/guides/GUIDE-12-DataGrid组件重构-开发提示.md` | 类型归类调整（开发提示归入 GUIDE） | 已完成 |
| 4 | `UI-05` | `docs/design/UI-05-阶段3范围调整分析.md` | `docs/design/UI-06-阶段3范围调整分析.md` | 重新编号 | 已完成 |
| 5 | `PLAN-01` | `docs/plans/PLAN-01-附录-模板系统详细设计.md` | `docs/design/ARCH-27-模板系统详细设计.md` | 类型归类调整（详细设计归入 ARCH） | 已完成 |
| 6 | `PLAN-03` | `docs/plans/PLAN-03-v0.7.0-表单设计器恢复计划.md` | `docs/plans/PLAN-13-v0.7.0-表单设计器恢复计划.md` | 重新编号 | 已完成 |
| 7 | `PLAN-06` | `docs/plans/PLAN-06-v0.7.0-模板运行链路修复.md` | `docs/plans/PLAN-14-v0.7.0-模板运行链路修复.md` | 重新编号 | 已完成 |
| 8 | `REVIEW-01/02` | `docs/history/REVIEW-01-代码评审与改进清单-2025-11.md`、`docs/history/REVIEW-02-多语言功能代码评审-2025-11.md` | `docs/reviews/REVIEW-09-代码评审与改进清单-2025-11.md`、`docs/reviews/REVIEW-10-多语言功能代码评审-2025-11.md` | 类型归类调整（评审归入 reviews）+ 重新编号 | 已完成 |

### 3.5 非标准目录结构 - P2

以下目录/文件不符合 STD-02 标准结构：

| # | 路径 | 问题 | 处理结果 | 进度 |
|---|------|------|----------|------|
| 1 | `docs/tasks/` | 非标准目录 | 已归档到 `docs/history/ARCH-30/`（ARCH-30 任务执行记录与评审材料统一归入 history） | 已完成 |
| 2 | `docs/test-cases/` | 使用 TC- 前缀 | 已纳入标准：`docs/process/STD-02-文档编写规范.md` 新增 2.4，定义 `TC-{DOMAIN}-{NNN}-...` 独立命名体系 | 已完成 |
| 3 | `docs/research/` | 非标准目录 | 已将研究输出归档到 `docs/history/AUDIT-04-ARCH-30-动态实体多语研究报告.md`，并不再使用 `docs/research/` 作为长期目录 | 已完成 |

---

## 4. 代码合规性问题（STD-04）

### 4.1 单一类型原则违规 - P1

以下文件违反了「每个 .cs 文件只包含一个公共类型」的原则，已完成拆分修复：

#### 4.1.1 Services 目录 (14个违规文件)

| # | 文件 | 公共类型数 | 类型列表 | 拆分后新增文件（公共类型） | 进度 |
|---|------|-----------|----------|---------------------------|------|
| 1 | `Services/AccessService.cs` | 3 | AccessService, ScopeBinding, DataScopeEvaluationResult | `src/BobCrm.Api/Services/Access/ScopeBinding.cs`; `src/BobCrm.Api/Services/Access/DataScopeEvaluationResult.cs` | 已完成 |
| 2 | `Services/DDLExecutionService.cs` | 2 | DDLExecutionService, TableColumnInfo | `src/BobCrm.Api/Services/DDL/TableColumnInfo.cs`; `src/BobCrm.Api/Services/DDL/DDLScriptType.cs`; `src/BobCrm.Api/Services/DDL/DDLScriptStatus.cs` | 已完成 |
| 3 | `Services/DynamicEntityService.cs` | 3 | DynamicEntityService, EntityTypeInfo, PropertyTypeInfo | `src/BobCrm.Api/Services/DynamicEntities/EntityTypeInfo.cs`; `src/BobCrm.Api/Services/DynamicEntities/PropertyTypeInfo.cs` | 已完成 |
| 4 | `Services/DefaultTemplateService.cs` | 2 | IDefaultTemplateService, DefaultTemplateService | `src/BobCrm.Api/Services/IDefaultTemplateService.cs` | 已完成 |
| 5 | `Services/EntityPublishingService.cs` | 6 | EntityPublishingService, EnumValidationResult, PublishResult, ChangeAnalysis, PublishedTemplateInfo, PublishedTemplateBindingInfo, PublishedMenuInfo | `src/BobCrm.Api/Services/Publishing/EnumValidationResult.cs`; `src/BobCrm.Api/Services/Publishing/PublishResult.cs`; `src/BobCrm.Api/Services/Publishing/ChangeAnalysis.cs`; `src/BobCrm.Api/Services/Publishing/PublishedTemplateInfo.cs`; `src/BobCrm.Api/Services/Publishing/PublishedTemplateBindingInfo.cs`; `src/BobCrm.Api/Services/Publishing/PublishedMenuInfo.cs` | 已完成 |
| 6 | `Services/EntityMenuRegistrar.cs` | 2 | EntityMenuRegistrar, EntityMenuRegistrationResult | `src/BobCrm.Api/Services/Menus/EntityMenuRegistrationResult.cs` | 已完成 |
| 7 | `Services/EntitySchemaAlignmentService.cs` | 3 | EntitySchemaAlignmentService, AlignmentResult, DeleteFieldResult | `src/BobCrm.Api/Services/Schema/AlignmentResult.cs`; `src/BobCrm.Api/Services/Schema/DeleteFieldResult.cs` | 已完成 |
| 8 | `Services/FieldMetadataCache.cs` | 2 | IFieldMetadataCache, FieldMetadataCache | `src/BobCrm.Api/Services/IFieldMetadataCache.cs` | 已完成 |
| 9 | `Services/ReflectionPersistenceService.cs` | 4 | ReflectionPersistenceService, QueryOptions, FilterCondition, FilterOperator | `src/BobCrm.Api/Services/Querying/QueryOptions.cs`; `src/BobCrm.Api/Services/Querying/FilterCondition.cs`; `src/BobCrm.Api/Services/Querying/FilterOperator.cs` | 已完成 |
| 10 | `Services/RoslynCompiler.cs` | 4 | RoslynCompiler, CompilationResult, CompilationError, ValidationResult | `src/BobCrm.Api/Services/Roslyn/CompilationResult.cs`; `src/BobCrm.Api/Services/Roslyn/CompilationError.cs`; `src/BobCrm.Api/Services/Roslyn/ValidationResult.cs` | 已完成 |
| 11 | `Services/DataMigration/MigrationImpact.cs` | 4 | MigrationImpact, MigrationOperation, MigrationOperationType, RiskLevel | `src/BobCrm.Api/Services/DataMigration/MigrationOperation.cs`; `src/BobCrm.Api/Services/DataMigration/MigrationOperationType.cs`; `src/BobCrm.Api/Services/DataMigration/RiskLevel.cs` | 已完成 |
| 12 | `Services/DataSources/EntityDataSourceHandler.cs` | 2 | EntityDataSourceHandler, EntityDataSourceConfig | `src/BobCrm.Api/Services/DataSources/EntityDataSourceConfig.cs` | 已完成 |
| 13 | `Services/EntityLocking/EntityLockModels.cs` | 3 | EntityLockInfo, EntityLockValidationResult, EntityDefinitionUpdateRequest | `src/BobCrm.Api/Services/EntityLocking/EntityLockInfo.cs`; `src/BobCrm.Api/Services/EntityLocking/EntityLockValidationResult.cs`; `src/BobCrm.Api/Services/EntityLocking/EntityDefinitionUpdateRequest.cs` | 已完成 |
| 14 | `Services/Storage/IFileStorageService.cs` | 2 | IFileStorageService, S3Options | `src/BobCrm.Api/Services/Storage/S3Options.cs` | 已完成 |

#### 4.1.2 Endpoints 目录 (5个违规文件)

| # | 文件 | 公共类型数 | 类型列表 | 拆分后新增文件（公共类型） | 进度 |
|---|------|-----------|----------|---------------------------|------|
| 1 | `Endpoints/FieldActionEndpoints.cs` | 4 | FieldActionEndpoints, RdpDownloadRequest, FileValidationRequest, MailtoRequest | `src/BobCrm.Api/Endpoints/FieldActions/RdpDownloadRequest.cs`; `src/BobCrm.Api/Endpoints/FieldActions/FileValidationRequest.cs`; `src/BobCrm.Api/Endpoints/FieldActions/MailtoRequest.cs` | 已完成 |
| 2 | `Endpoints/FieldPermissionEndpoints.cs` | 3 | FieldPermissionEndpoints, UpsertFieldPermissionRequest, BulkUpsertFieldPermissionsRequest | `src/BobCrm.Api/Endpoints/FieldPermissions/UpsertFieldPermissionRequest.cs`; `src/BobCrm.Api/Endpoints/FieldPermissions/BulkUpsertFieldPermissionsRequest.cs` | 已完成 |
| 3 | `Endpoints/EntityAggregateEndpoints.cs` | 4 | EntityAggregateEndpoints, SaveEntityDefinitionAggregateRequest, SubEntityDto, FieldMetadataDto | `src/BobCrm.Api/Endpoints/EntityAggregates/SaveEntityDefinitionAggregateRequest.cs`; `src/BobCrm.Api/Endpoints/EntityAggregates/SubEntityDto.cs`; `src/BobCrm.Api/Endpoints/EntityAggregates/FieldMetadataDto.cs` | 已完成 |
| 4 | `Endpoints/SetupEndpoints.cs` | 2 | SetupEndpoints, AdminSetupDto | `src/BobCrm.Api/Contracts/DTOs/AdminSetupDto.cs` | 已完成 |
| 5 | `Endpoints/TemplateEndpoints.cs` | 2 | TemplateEndpoints, EntityMenuMetadata | 无需拆分（已确认 `EntityMenuMetadata` 为私有类型，不属于公共类型） | 已完成 |

#### 4.1.3 Base 目录 (2个违规文件)

| # | 文件 | 公共类型数 | 类型列表 | 拆分后新增文件（公共类型） | 进度 |
|---|------|-----------|----------|---------------------------|------|
| 1 | `Base/Aggregates/EntityDefinitionAggregate.cs` | 5 | EntityDefinitionAggregate, ValidationResult, ValidationError, DomainException, ValidationException | `src/BobCrm.Api/Base/Aggregates/ValidationResult.cs`; `src/BobCrm.Api/Base/Aggregates/ValidationError.cs`; `src/BobCrm.Api/Base/Aggregates/DomainException.cs`; `src/BobCrm.Api/Base/Aggregates/ValidationException.cs` | 已完成 |
| 2 | `Base/Models/EntityInterface.cs` | 5 | EntityInterface, InterfaceType, EntityInterfaceType, InterfaceFieldMapping, InterfaceFieldDefinition | `src/BobCrm.Api/Base/Models/InterfaceType.cs`; `src/BobCrm.Api/Base/Models/EntityInterfaceType.cs`; `src/BobCrm.Api/Base/Models/InterfaceFieldMapping.cs`; `src/BobCrm.Api/Base/Models/InterfaceFieldDefinition.cs` | 已完成 |

---

## 5. 修复建议

### 5.1 文档修复优先级

**P1（本迭代修复，已完成）**:
1. 移动 7 个错误目录的文档
2. 重命名 5 个使用无效类型代码的文档
3. 修正 3 个编号格式错误的文档

**P2（下迭代修复）**:
1. 解决 5 组编号重复问题
2. 规范化特殊目录结构

### 5.2 代码修复优先级

**P1（已完成）**:
1. 拆分 Services 目录下的 14 个违规文件
2. 拆分 Endpoints 目录下的 5 个违规文件
3. 拆分 Base 目录下的 2 个违规文件

**建议拆分方式**:
- 将 DTO/Request/Response 类移到 `Contracts/` 目录
- 将接口单独提取到 `I{ServiceName}.cs` 文件
- 将 Result/Info 类移到独立文件

---

## 6. 排除项（已确认合规）

以下已检查确认合规：

| 目录 | 文件数 | 状态 |
|------|--------|------|
| `Contracts/` | 126 | ✅ 全部合规 |
| `Infrastructure/` | 已抽查 | ✅ 合规 |
| `Migrations/` | 系统生成 | ✅ 不适用 |

---

## 7. 下一步行动

### 7.1 立即行动（本周）

- [x] 移动 7 个错误目录的文档
- [x] 重命名 5 个使用无效类型代码的文档
- [x] 修正 3 个编号格式错误的文档

### 7.2 计划行动（下迭代）

- [x] 拆分代码文件（21 个文件）
- [x] 解决文档编号冲突
- [x] 整理非标准目录（docs/tasks、docs/test-cases、docs/research）

---

## 附录 A：STD 规范速查

| 规范 | 核心要求 |
|------|----------|
| STD-02 | 文档命名：`[类型]-[编号]-[中文名称].md` |
| STD-04 | 单一类型原则：每个 .cs 文件只含一个公共类型 |
| STD-05 | 多语言资源键：`{类型}_{模块}_{描述}` |

---

**文档版本**: 1.0
**审计人**: Claude Code
**审计日期**: 2025-12-15
