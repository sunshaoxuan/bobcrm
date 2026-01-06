# PLAN-21: ARCH-30 阶段3-4 开发规划

**版本**: 1.0
**创建日期**: 2025-01-06
**状态**: 待执行
**预计工时**: 2-3 天
**目标**: 完成 ARCH-30 系统级多语 API 架构优化的最后阶段

---

## 1. 执行概览

```
┌─────────────────────────────────────────────────────────────────┐
│                    ARCH-30 阶段3-4 执行路线                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Day 1 (上午)          Day 1 (下午)         Day 2              │
│  ┌──────────┐         ┌──────────┐         ┌──────────┐        │
│  │ Task 3.1 │ ──────► │ Task 3.2 │ ──────► │ Task 3.3 │        │
│  │  研究    │         │  缓存    │         │   API    │        │
│  │ 0.5天   │         │  1天     │         │   1天    │        │
│  └──────────┘         └──────────┘         └──────────┘        │
│                                                   │             │
│                                                   ▼             │
│                                            ┌──────────┐        │
│                       Day 3                │ Task 4.x │        │
│                       ┌──────────┐         │  文档    │        │
│                       │  评审    │ ◄────── │ 0.5天   │        │
│                       │  合并    │         └──────────┘        │
│                       └──────────┘                              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. 任务分解

### 2.1 Task 3.1: 动态实体查询机制研究

| 项目 | 内容 |
|------|------|
| **文档** | [task-3.1-dynamic-entity-research.md](../history/ARCH-30/task-3.1-dynamic-entity-research.md) |
| **工时** | 0.5 天 |
| **类型** | 研究（不写代码） |
| **输出** | 技术调研报告 |

**执行步骤**：

```bash
# 1. 阅读源码（按顺序）
src/BobCrm.Api/Services/CodeGeneration/CSharpCodeGenerator.cs
src/BobCrm.Api/Services/RoslynCompiler.cs
src/BobCrm.Api/Services/DynamicEntityService.cs
src/BobCrm.Api/Base/Models/EntityDefinition.cs
src/BobCrm.Api/Base/Models/FieldMetadata.cs

# 2. 撰写调研报告
docs/history/AUDIT-04-ARCH-30-动态实体多语研究报告.md

# 3. 提交
git add docs/history/AUDIT-04-ARCH-30-动态实体多语研究报告.md
git commit -m "docs(research): add dynamic entity i18n research report

- Analyze codegen/compile/query pipeline
- Identify field metadata & i18n resolve points
- Recommend meta.fields approach for Stage 3
- Ref: ARCH-30 Task 3.1"
```

**验收标准**：
- [ ] 报告覆盖代码生成、编译、查询三个阶段
- [ ] 明确字段元数据注入点
- [ ] 给出 Task 3.2/3.3 的实施建议

---

### 2.2 Task 3.2: 字段元数据缓存服务

| 项目 | 内容 |
|------|------|
| **文档** | [task-3.2-field-metadata-cache.md](../history/ARCH-30/task-3.2-field-metadata-cache.md) |
| **工时** | 1 天 |
| **类型** | 编码 |
| **输出** | 缓存服务 + 5个测试 |

**执行步骤**：

```bash
# 1. 创建接口
# 文件: src/BobCrm.Api/Abstractions/IFieldMetadataCache.cs

# 2. 实现服务
# 文件: src/BobCrm.Api/Services/FieldMetadataCache.cs

# 3. 注册 DI
# 文件: src/BobCrm.Api/Program.cs
# 添加: builder.Services.AddSingleton<IFieldMetadataCache, FieldMetadataCache>();

# 4. 集成缓存失效
# 文件: src/BobCrm.Api/Services/EntityDefinitionAppService.cs
# 在实体/字段变更方法末尾调用: _fieldMetadataCache.Invalidate(entity.FullTypeName);

# 5. 编写测试
# 文件: tests/BobCrm.Api.Tests/FieldMetadataCacheTests.cs

# 6. 运行测试
dotnet test --filter "FullyQualifiedName~FieldMetadataCache"

# 7. 提交
git add src/BobCrm.Api/Abstractions/IFieldMetadataCache.cs
git add src/BobCrm.Api/Services/FieldMetadataCache.cs
git add src/BobCrm.Api/Program.cs
git add src/BobCrm.Api/Services/EntityDefinitionAppService.cs
git add tests/BobCrm.Api.Tests/FieldMetadataCacheTests.cs

git commit -m "feat(cache): add field metadata cache service

- Add IFieldMetadataCache interface
- Implement FieldMetadataCache with memory cache
- Support single/multi-language modes
- Integrate cache invalidation on entity changes
- Add unit tests with 5 test cases
- Ref: ARCH-30 Task 3.2"
```

**验收标准**：
- [ ] 接口定义完成
- [ ] 实现类完成（含日志）
- [ ] DI 注册完成
- [ ] 缓存失效集成完成
- [ ] 5个测试用例全部通过
- [ ] 编译无警告

---

### 2.3 Task 3.3: 动态实体查询 API 改造

| 项目 | 内容 |
|------|------|
| **文档** | [task-3.3-dynamic-entity-api.md](../history/ARCH-30/task-3.3-dynamic-entity-api.md) |
| **工时** | 1 天 |
| **类型** | 编码 |
| **输出** | 2个 DTO + API 改造 + 5个测试 |

**执行步骤**：

```bash
# 1. 创建 DTO
# 文件: src/BobCrm.Api/Contracts/DTOs/DynamicEntityMetaDto.cs
# 文件: src/BobCrm.Api/Contracts/DTOs/DynamicEntityQueryResultDto.cs

# 2. 改造 Query 端点
# 文件: src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs
# 端点: POST /api/dynamic-entities/{fullTypeName}/query
# 变更: 新增 lang 参数，返回 meta.fields

# 3. 改造 GetById 端点
# 文件: src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs
# 端点: GET /api/dynamic-entities/{fullTypeName}/{id}
# 变更: 新增 lang, includeMeta 参数

# 4. 编写测试
# 文件: tests/BobCrm.Api.Tests/DynamicEntityEndpointsPhase3Tests.cs

# 5. 运行测试
dotnet test --filter "FullyQualifiedName~DynamicEntityEndpoints"

# 6. 运行全量测试确保无回归
dotnet test

# 7. 提交
git add src/BobCrm.Api/Contracts/DTOs/DynamicEntityMetaDto.cs
git add src/BobCrm.Api/Contracts/DTOs/DynamicEntityQueryResultDto.cs
git add src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs
git add tests/BobCrm.Api.Tests/DynamicEntityEndpointsPhase3Tests.cs

git commit -m "feat(api): add meta.fields to dynamic entity query endpoints

- Add lang parameter to /query endpoint
- Add includeMeta parameter to /{id} endpoint
- Return field metadata with displayName/displayNameKey
- Maintain backward compatibility when params not provided
- Add 5 test cases for new functionality
- Ref: ARCH-30 Task 3.3"
```

**验收标准**：
- [ ] Query 端点支持 lang 参数
- [ ] Query 响应包含 meta.fields
- [ ] GetById 支持 includeMeta 参数
- [ ] 向后兼容（不传参数时行为不变）
- [ ] 5个测试用例全部通过
- [ ] 全量测试无回归

---

### 2.4 Task 4.1-4.3: 文档同步

| 项目 | 内容 |
|------|------|
| **文档** | [task-4.1](../history/ARCH-30/task-4.1-changelog-update.md), [task-4.2](../history/ARCH-30/task-4.2-api-docs-update.md), [task-4.3](../history/ARCH-30/task-4.3-test-docs-update.md) |
| **工时** | 0.5 天 |
| **类型** | 文档 |
| **输出** | 3个文档更新 |

**执行步骤**：

```bash
# 1. 更新 CHANGELOG.md (参考 task-4.1)
# 添加 ARCH-30 所有变更条目

# 2. 更新 API 文档 (参考 task-4.2)
# 文件: docs/reference/API-01-接口文档.md
# 添加多语 API 规范章节
# 更新所有改造端点的文档

# 3. 更新测试指南 (参考 task-4.3)
# 文件: docs/guides/TEST-01-测试指南.md
# 添加多语 API 测试规范章节

# 4. 提交
git add CHANGELOG.md
git add docs/reference/API-01-接口文档.md
git add docs/guides/TEST-01-测试指南.md

git commit -m "docs: complete ARCH-30 documentation sync

- Update CHANGELOG with all ARCH-30 changes
- Update API reference for 15+ endpoints
- Add multilingual API testing specification
- Ref: ARCH-30 Task 4.1-4.3"
```

**验收标准**：
- [ ] CHANGELOG 包含完整变更记录
- [ ] API 文档覆盖所有改造端点
- [ ] 测试指南包含多语 API 测试规范

---

## 3. 每日执行计划

### Day 1

| 时段 | 任务 | 产出 |
|------|------|------|
| 上午 | Task 3.1 研究 | 调研报告 |
| 下午 | Task 3.2 开发 | 缓存服务 + 测试 |

**Day 1 检查点**：
```bash
# 确认 Task 3.1 报告已提交
git log --oneline -1 | grep "research"

# 确认 Task 3.2 测试通过
dotnet test --filter "FullyQualifiedName~FieldMetadataCache"
```

### Day 2

| 时段 | 任务 | 产出 |
|------|------|------|
| 全天 | Task 3.3 API 改造 | DTO + 端点 + 测试 |

**Day 2 检查点**：
```bash
# 确认所有测试通过
dotnet test

# 确认覆盖率
pwsh scripts/coverage-summary.ps1
```

### Day 3 (半天)

| 时段 | 任务 | 产出 |
|------|------|------|
| 上午 | Task 4.1-4.3 文档 | 文档更新 |
| 上午 | 代码评审 | 评审报告 |
| 上午 | 合并主分支 | PR 合并 |

**Day 3 检查点**：
```bash
# 确认文档已更新
git diff --name-only HEAD~1 | grep -E "CHANGELOG|API-01|TEST-01"

# 确认 ARCH-30 阶段3-4 完成
# 更新 docs/history/ARCH-30/README.md 将 Task 3.x/4.x 标记为 ✅
```

---

## 4. 质量门禁

### 4.1 每个 Task 提交前

```bash
# 1. 编译检查
dotnet build

# 2. 单元测试
dotnet test

# 3. 代码风格
pwsh scripts/check-style-tokens.ps1

# 4. 覆盖率检查（可选）
dotnet test /p:CollectCoverage=true
```

### 4.2 阶段完成后

```bash
# 1. 全量测试
dotnet test

# 2. 覆盖率报告
pwsh scripts/coverage-summary.ps1

# 3. 确认覆盖率 ≥ 90%
# LineRate >= 0.90
```

---

## 5. 风险与应对

| 风险 | 概率 | 应对措施 |
|------|------|----------|
| DynamicEntityService 改造复杂 | 中 | Task 3.1 先研究，确定最小改动方案 |
| 缓存失效遗漏 | 低 | 设置 30 分钟过期兜底 |
| 向后兼容问题 | 低 | 新增参数默认值保持旧行为 |

---

## 6. 完成标志

### 6.1 代码产出

- [ ] `IFieldMetadataCache` 接口
- [ ] `FieldMetadataCache` 实现
- [ ] `DynamicEntityMetaDto` DTO
- [ ] `DynamicEntityQueryResultDto` DTO
- [ ] 改造后的 `DynamicEntityEndpoints`
- [ ] 10+ 测试用例

### 6.2 文档产出

- [ ] 调研报告 `AUDIT-04-ARCH-30-动态实体多语研究报告.md`
- [ ] CHANGELOG 更新
- [ ] API 文档更新
- [ ] 测试指南更新

### 6.3 状态更新

- [ ] [ARCH-30 README](../history/ARCH-30/README.md) Task 3.x/4.x 标记为 ✅
- [ ] [PLAN-09](PLAN-09-系统级多语API架构优化-工作计划.md) 标记为 ✅ 完成

---

## 7. Git 提交汇总

完成后应有以下提交：

```
1. docs(research): add dynamic entity i18n research report (Task 3.1)
2. feat(cache): add field metadata cache service (Task 3.2)
3. feat(api): add meta.fields to dynamic entity query endpoints (Task 3.3)
4. docs: complete ARCH-30 documentation sync (Task 4.1-4.3)
```

---

**创建日期**: 2025-01-06
**执行负责人**: 待分配
**评审人**: 架构组
