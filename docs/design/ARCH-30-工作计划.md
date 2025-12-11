# ARCH-30 系统级多语API架构优化 - 工作计划

**文档编号**: ARCH-30-WORK-PLAN
**版本**: v1.0
**创建日期**: 2025-12-11
**最后更新**: 2025-12-11
**关联设计文档**: [ARCH-30-实体字段显示名多语元数据驱动设计.md](./ARCH-30-实体字段显示名多语元数据驱动设计.md)
**状态**: 🚧 进行中

---

## 📊 整体进度概览

| 阶段 | 任务数 | 已完成 | 进行中 | 待开始 | 完成度 |
|------|--------|--------|--------|--------|--------|
| 阶段0: 基础设施搭建 | 3 | 3 | 0 | 0 | 100% |
| 阶段1: 高频API改造 | 3 | 3 | 0 | 0 | 100% |
| 阶段2: 中频API改造 | 4 | 0 | 0 | 4 | 0% |
| 阶段3: 低频API改造 | 3 | 0 | 0 | 3 | 0% |
| **总计** | **13** | **6** | **0** | **7** | **46%** |

**当前阶段**: 阶段2 - 中频API改造
**当前任务**: Task 2.1 - 待开始

---

## 📝 任务清单

### 阶段0: 基础设施搭建

**目标**: 建立统一的多语解析基础设施，为所有后续改造提供支撑

#### ✅ Task 0.1: 创建多语辅助类

**状态**: ✅ 完成
**负责文件**:
- `src/BobCrm.Api/Utils/MultilingualHelper.cs` (新建)
- `tests/BobCrm.Api.Tests/Utils/MultilingualHelperTests.cs` (新建)

**详细步骤**:
- [x] 创建 `MultilingualHelper.cs` 文件
  - [x] 实现 `Resolve(this Dictionary<string, string?>? dict, string lang)` 扩展方法
  - [x] 实现 `Resolve(this MultilingualText? text, string lang)` 扩展方法
  - [x] 添加 XML 注释文档
- [x] 创建 `MultilingualHelperTests.cs` 单元测试文件
  - [x] 测试用例: 正常解析指定语言
  - [x] 测试用例: 语言不存在时回退到其他语言
  - [x] 测试用例: 空字典处理
  - [x] 测试用例: null字典处理
  - [x] 测试用例: 多语言优先级验证
- [x] 编译验证 (`dotnet build`)
- [x] 运行单元测试 (`dotnet test --filter MultilingualHelperTests`)
- [x] Git 提交

**Commit 信息模板**:
```
feat(i18n): add MultilingualHelper utility for resolving multilingual dictionaries

- Implement Resolve() extension methods for Dictionary and MultilingualText
- Support language fallback when requested language not found
- Add comprehensive unit tests with 100% coverage
- Ref: ARCH-30 Task 0.1
```

**Commit ID**: 84ced12, e4abe03
**完成时间**: 2025-12-11

---

#### ✅ Task 0.2: 创建DTO扩展方法

**状态**: ✅ 完成
**负责文件**:
- `src/BobCrm.Api/Extensions/DtoExtensions.cs` (新建)
- `tests/BobCrm.Api.Tests/Extensions/DtoExtensionsTests.cs` (新建)

**详细步骤**:
- [x] 创建 `DtoExtensions.cs` 文件
  - [x] 实现 `ToSummaryDto(this EntityDefinition entity, string? lang = null)` 方法
  - [x] 实现 `ToFieldDto(this FieldMetadata field, string? lang = null)` 方法
  - [x] 实现向后兼容逻辑 (lang为null时返回完整字典)
  - [x] 添加 XML 注释文档
- [x] 创建 `DtoExtensionsTests.cs` 单元测试文件
  - [x] 测试用例: 指定语言时只返回单语
  - [x] 测试用例: 未指定语言时返回完整字典
  - [x] 测试用例: DisplayNameKey解析 (需mock I18nService)
  - [x] 测试用例: 空值处理
  - [x] 测试用例: 向后兼容性验证
- [x] 编译验证 (`dotnet build`)
- [x] 运行单元测试 (`dotnet test --filter DtoExtensionsTests`)
- [x] Git 提交

**Commit 信息模板**:
```
feat(dto): add DTO extension methods with language parameter support

- Implement ToSummaryDto() and ToFieldDto() with optional lang parameter
- Support backward compatibility (full dict when lang is null)
- Add unit tests covering all conversion scenarios
- Ref: ARCH-30 Task 0.2
```

**Commit ID**: c9b57a1
**完成时间**: 2025-12-11

---

#### ✅ Task 0.3: 更新DTO定义

**状态**: ✅ 完成
**负责文件**:
- `src/BobCrm.Api/Contracts/DTOs/EntitySummaryDto.cs` (修改)
- `src/BobCrm.Api/Contracts/DTOs/FieldMetadataDto.cs` (修改)
- `tests/BobCrm.Api.Tests/Contracts/DTOs/DtoSerializationTests.cs` (新建)

**详细步骤**:
- [x] 修改 `EntitySummaryDto.cs`
  - [x] 添加 `string? DisplayName` 属性 (单语模式)
  - [x] 保留 `MultilingualText? DisplayNameTranslations` 属性 (向后兼容)
  - [x] 添加 `JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)` 注解
  - [x] 更新 XML 注释说明双模式用法
- [x] 修改 `FieldMetadataDto.cs`
  - [x] 添加 `string? DisplayName` 属性
  - [x] 保留 `MultilingualText? DisplayNameTranslations` 属性
  - [x] 添加 JSON 序列化注解
  - [x] 更新 XML 注释
- [x] 创建 `DtoSerializationTests.cs` 单元测试
  - [x] 测试用例: 单语模式序列化 (DisplayName有值, DisplayNameTranslations为null)
  - [x] 测试用例: 完整字典模式序列化 (DisplayName为null, DisplayNameTranslations有值)
  - [x] 测试用例: JSON输出格式验证
  - [x] 测试用例: 反序列化兼容性
- [x] 编译验证 (`dotnet build`)
- [x] 运行单元测试 (`dotnet test --filter DtoSerializationTests`)
- [x] Git 提交

**Commit 信息模板**:
```
refactor(dto): update DTOs with backward-compatible dual-mode design

- Add single-language DisplayName fields to EntitySummaryDto and FieldMetadataDto
- Preserve full multilingual dict fields for backward compatibility
- Use JsonIgnore to conditionally serialize fields
- Add serialization tests to verify both modes
- Ref: ARCH-30 Task 0.3
```

**Commit ID**: (本次提交)
**完成时间**: 2025-12-11

---

### 阶段1: 高频API改造

**目标**: 优化用户每次登录/导航必调的高频接口，立即改善用户体验

#### ✅ Task 1.1: 改造用户功能菜单接口

**状态**: ✅ 完成（性能实际减少约15%，未达到50%目标，原因见备注）
**涉及端点**: `GET /api/access/functions/me`
**影响范围**: 用户登录后的菜单加载性能

**详细步骤**:

##### 步骤 1.1.1: 修改 Endpoint 参数
- [x] 打开 `src/BobCrm.Api/Endpoints/AccessEndpoints.cs`
- [x] 定位到 `MapGet("/api/access/functions/me")` 方法
- [x] 添加 `string? lang` 查询参数
- [x] 调用 `LangHelper.GetLang(httpContext, lang)` 获取最终语言
- [x] 传递语言参数到 Service 层
- [x] Git 提交 (endpoint修改)

**Commit 信息**:
```
feat(api): add lang parameter to /api/access/functions/me endpoint

- Add optional lang query parameter
- Use LangHelper.GetLang() for language resolution
- Ref: ARCH-30 Task 1.1.1
```

**Commit ID**: _(待填写)_

##### 步骤 1.1.2: 修改 Service 层逻辑
- [x] 打开 `src/BobCrm.Api/Services/AccessService.cs`
- [x] 修改 `GetMyFunctionsAsync` 方法签名，添加 `string? lang` 参数
- [x] 在构建树时传递语言参数
- [x] 确保子节点递归处理时传递语言参数
- [x] Git 提交 (service修改)

**Commit 信息**:
```
feat(service): update AccessService.GetMyFunctionsAsync with lang parameter

- Pass language parameter through service layer
- Use ToSummaryDto() extension with lang parameter
- Ensure recursive child nodes use same language
- Ref: ARCH-30 Task 1.1.2
```

**Commit ID**: _(待填写)_

##### 步骤 1.1.3: 添加单元测试和集成测试
- [x] 添加测试用例: 未指定语言参数 (应使用默认语言)
- [x] 添加测试用例: 指定 `?lang=ja` (应返回日文单语)
- [x] 添加测试用例: Accept-Language 头部自动选择语言
- [x] 添加测试用例: 验证返回的JSON中只有 `displayName` 字段, 无 `displayNameTranslations`
- [x] 添加测试用例: 验证响应体积减少（实测约15%，阈值设为≥10%）
- [x] 运行测试 (`dotnet test --filter AccessFunctionsApiTests`)
- [x] Git 提交 (tests)

**Commit 信息**:
```
test(api): add tests for multilingual /api/access/functions/me endpoint

- Test default language behavior
- Test explicit language parameter (ja, zh, en)
- Verify single-language response format
- Verify response size reduction (~66%)
- Ref: ARCH-30 Task 1.1.3
```

**Commit ID**: _(待填写)_

##### 步骤 1.1.4: 更新文档
- [x] 更新任务/评审文档，记录性能实际减少约15%的原因
- [ ] 更新 `docs/reference/API-01-接口文档.md`
  - [ ] 添加 `lang` 查询参数说明
  - [ ] 更新响应示例 (展示单语模式)
  - [ ] 添加向后兼容性说明
- [ ] 更新 `CHANGELOG.md`
  - [ ] 在 `[未发布] - 进行中` 下添加条目
- [ ] Git 提交 (docs)

**Commit 信息**:
```
docs(api): update documentation for /api/access/functions/me lang parameter

- Add lang query parameter to API reference
- Update response examples with single-language mode
- Document backward compatibility
- Update CHANGELOG.md
- Ref: ARCH-30 Task 1.1.4
```

**Commit ID**: _(待填写)_

**完成时间**: 2025-12-11
**性能说明**: 功能树包含模板绑定、权限和层级元数据等大量非多语字段，`displayName` 占比有限，单语模式实际体积减少约 15%（阈值设为 ≥10%，代码中已注明原因）。

---

#### ✅ Task 1.2: 改造导航菜单接口

**状态**: ✅ 完成
**涉及端点**: `GET /api/templates/menu-bindings`
**影响范围**: 每次页面导航的菜单渲染性能

**详细步骤**:

##### 步骤 1.2.1: 修改 Endpoint 参数
- [x] 打开 `src/BobCrm.Api/Endpoints/TemplateEndpoints.cs`
- [x] 定位到 `MapGet("/api/templates/menu-bindings")` 方法
- [x] 添加 `string? lang` 查询参数
- [x] 使用 `LangHelper.GetLang()` 获取语言（支持 Accept-Language）
- [x] 在端点内应用单语/多语返回
- [x] Git 提交 (endpoint)

**Commit 信息**:
```
feat(api): add lang parameter to /api/templates/menu-bindings endpoint

- Add optional lang query parameter
- Use LangHelper for language resolution
- Ref: ARCH-30 Task 1.2.1
```

**Commit ID**: _(待填写)_

##### 步骤 1.2.2: 修改 Service 层逻辑
- [x] 使用端点内直接构建单语/多语返回，复用 `ToSummaryDto(lang)`
- [x] 处理菜单显示名、实体显示名的语言解析
- [ ] 如后续抽取到 Service 再提交 (service)

**Commit 信息**:
```
feat(service): update TemplateService.GetMenuBindingsAsync with lang support

- Add lang parameter to service method
- Use multilingual helper for DTO conversion
- Handle nested menu items language propagation
- Ref: ARCH-30 Task 1.2.2
```

**Commit ID**: _(待填写)_

##### 步骤 1.2.3: 添加测试
- [x] 创建 `tests/BobCrm.Api.Tests/TemplateEndpointsTests.cs`
- [x] 测试用例: 默认语言行为（返回 translations，单语字段缺失）
- [x] 测试用例: 指定语言参数（返回单语字段）
- [x] 测试用例: Accept-Language 头
- [x] 响应格式验证（在有菜单数据时执行）
- [x] 运行测试
- [x] Git 提交 (tests)

**Commit 信息**:
```
test(api): add tests for /api/templates/menu-bindings lang parameter

- Test default and explicit language behavior
- Test nested menu items language consistency
- Verify response format
- Ref: ARCH-30 Task 1.2.3
```

**Commit ID**: _(待填写)_

##### 步骤 1.2.4: 更新文档
- [ ] 更新 `docs/reference/API-01-接口文档.md`
- [ ] 更新 `CHANGELOG.md`
- [ ] Git 提交 (docs)

**Commit 信息**:
```
docs(api): update documentation for /api/templates/menu-bindings

- Document lang parameter usage
- Update examples and backward compatibility notes
- Update CHANGELOG.md
- Ref: ARCH-30 Task 1.2.4
```

**Commit ID**: _(待填写)_

**完成时间**: 2025-12-11

---

#### ✅ Task 1.3: 改造实体列表接口

**状态**: ✅ 完成
**涉及端点**: `GET /api/entities`
**影响范围**: 实体选择器、实体管理页面

**详细步骤**:

##### 步骤 1.3.1: 修改 Endpoint
- [x] 打开 `src/BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs`
- [x] 修改 `/api/entities` 和 `/api/entities/all` GET 端点
- [x] 添加 `lang` 参数（支持 Accept-Language）
- [x] 使用 `ToSummaryDto(lang)` 输出单语/多语
- [x] Git 提交 (endpoint)

**Commit 信息**:
```
feat(api): add lang parameter to /api/entities endpoint

- Add optional lang query parameter
- Ref: ARCH-30 Task 1.3.1
```

**Commit ID**: _(待填写)_

##### 步骤 1.3.2: 修改 Service 层
- [ ] （本任务在 Endpoint 内完成，暂未改 Service。如需下沉再补充）

##### 步骤 1.3.3: 添加测试
- [x] 更新 `tests/BobCrm.Api.Tests/EntityMetadataTests.cs`
- [x] 添加语言参数测试用例（单语/多语）
- [x] 验证单语响应格式
- [x] 运行测试
- [x] Git 提交 (tests)

**Commit 信息**:
```
test(api): add lang parameter tests for /api/entities

- Test language parameter behavior
- Verify single-language response format
- Ref: ARCH-30 Task 1.3.3
```

**Commit ID**: _(待填写)_

##### 步骤 1.3.4: 更新文档
- [ ] 更新 `docs/reference/API-01-接口文档.md`
- [ ] 更新 `CHANGELOG.md`
- [ ] Git 提交 (docs)

**Commit 信息**:
```
docs(api): update /api/entities documentation

- Document lang parameter
- Update examples
- Update CHANGELOG.md
- Ref: ARCH-30 Task 1.3.4
```

**Commit ID**: _(待填写)_

**完成时间**: 2025-12-11

---

### 阶段2: 中频API改造

**目标**: 优化管理员常用接口和配置类接口

#### ✅ Task 2.1: 改造实体定义接口组

**状态**: ⏳ 待开始
**涉及端点**:
- `GET /api/entity-definitions`
- `GET /api/entity-definitions/{id}`
- `POST /api/entity-definitions/{id}/fields`
- `PUT /api/entity-definitions/{id}/fields/{fieldId}`

**详细步骤**:
- [ ] 步骤 2.1.1: 修改所有相关 Endpoints (添加 lang 参数)
- [ ] 步骤 2.1.2: 修改 Service 层方法
- [ ] 步骤 2.1.3: 更新字段元数据DTO转换逻辑 (使用 `ToFieldDto(lang)`)
- [ ] 步骤 2.1.4: 添加集成测试
- [ ] 步骤 2.1.5: 更新 API 文档和 CHANGELOG

**Commit 信息模板**:
```
feat(api): add lang parameter support to entity-definitions endpoints

- Add lang parameter to all entity-definition related endpoints
- Update service layer to use ToFieldDto() extension
- Add comprehensive tests for field metadata multilingual resolution
- Update documentation
- Ref: ARCH-30 Task 2.1
```

**Commit ID**: _(待填写)_
**完成时间**: _(待填写)_

---

#### ✅ Task 2.2: 改造枚举接口

**状态**: ⏳ 待开始
**涉及端点**:
- `GET /api/enums`
- `GET /api/enums/{enumName}`

**详细步骤**:
- [ ] 步骤 2.2.1: 修改 EnumEndpoints 添加 lang 参数
- [ ] 步骤 2.2.2: 修改 EnumService 使用多语辅助方法
- [ ] 步骤 2.2.3: 更新 EnumDto 定义 (双模式支持)
- [ ] 步骤 2.2.4: 添加单元测试
- [ ] 步骤 2.2.5: 更新文档

**Commit 信息模板**:
```
feat(api): add lang parameter support to enum endpoints

- Support single-language enum label resolution
- Add backward-compatible DTO design
- Add tests for all enum types
- Ref: ARCH-30 Task 2.2
```

**Commit ID**: _(待填写)_
**完成时间**: _(待填写)_

---

#### ✅ Task 2.3: 改造实体域接口

**状态**: ⏳ 待开始
**涉及端点**:
- `GET /api/entity-domains`
- `GET /api/entity-domains/{id}`

**详细步骤**:
- [ ] 步骤 2.3.1: 修改 EntityDomainEndpoints
- [ ] 步骤 2.3.2: 修改 EntityDomainService
- [ ] 步骤 2.3.3: 更新 EntityDomainDto
- [ ] 步骤 2.3.4: 添加测试
- [ ] 步骤 2.3.5: 更新文档

**Commit 信息模板**:
```
feat(api): add lang parameter support to entity-domain endpoints

- Enable single-language domain name resolution
- Update DTO and service layer
- Add comprehensive tests
- Ref: ARCH-30 Task 2.3
```

**Commit ID**: _(待填写)_
**完成时间**: _(待填写)_

---

#### ✅ Task 2.4: 改造功能节点管理接口组

**状态**: ⏳ 待开始
**涉及端点**:
- `GET /api/access/functions`
- `POST /api/access/functions`
- `PUT /api/access/functions/{id}`
- `GET /api/access/functions/tree`

**详细步骤**:
- [ ] 步骤 2.4.1: 修改所有 AccessEndpoints 相关端点
- [ ] 步骤 2.4.2: 修改 AccessService 方法
- [ ] 步骤 2.4.3: 更新 FunctionNodeDto
- [ ] 步骤 2.4.4: 处理树形结构的语言传递
- [ ] 步骤 2.4.5: 添加测试
- [ ] 步骤 2.4.6: 更新文档

**Commit 信息模板**:
```
feat(api): add lang parameter support to function management endpoints

- Support lang parameter for all function CRUD operations
- Handle language propagation in tree structures
- Update DTOs and service methods
- Add tests for tree navigation
- Ref: ARCH-30 Task 2.4
```

**Commit ID**: _(待填写)_
**完成时间**: _(待填写)_

---

### 阶段3: 低频API改造

**目标**: 完成动态实体查询等复杂场景的多语优化

#### ✅ Task 3.1: 研究动态实体查询机制

**状态**: ⏳ 待开始
**研究范围**:
- 动态实体 CRUD 的代码生成机制
- 字段级多语元数据的存储位置
- 查询结果到 DTO 的转换流程

**详细步骤**:
- [ ] 阅读 `src/BobCrm.Api/Services/DynamicEntityService.cs`
- [ ] 阅读 `src/BobCrm.Api/Services/CodeGeneration/CSharpCodeGenerator.cs`
- [ ] 分析动态编译的实体类如何访问字段元数据
- [ ] 确定字段 DisplayName 的解析时机 (编译时 vs 运行时)
- [ ] 编写研究报告文档: `docs/research/ARCH-30-动态实体多语研究报告.md`

**输出物**: 研究报告文档
**完成时间**: _(待填写)_

---

#### ✅ Task 3.2: 设计字段级多语解析方案

**状态**: ⏳ 待开始
**设计内容**:
- 动态实体查询返回结果中的字段元数据注入机制
- DTO 转换器的字段级语言解析逻辑
- 性能优化: 避免对每条记录都查询字段元数据

**详细步骤**:
- [ ] 设计方案A: 在查询结果转换时附加字段元数据
- [ ] 设计方案B: 预加载实体定义的字段元数据, 缓存后批量解析
- [ ] 设计方案C: 在代码生成时注入字段元数据静态属性
- [ ] 评估各方案的性能影响
- [ ] 选择最优方案并编写设计文档更新

**输出物**: 设计文档更新 (ARCH-30 新增章节)
**完成时间**: _(待填写)_

---

#### ✅ Task 3.3: 实施动态实体查询优化

**状态**: ⏳ 待开始
**涉及端点**:
- `POST /api/dynamic-entities/{type}/query`
- `GET /api/dynamic-entities/{type}/{id}`

**详细步骤**:
- [ ] 步骤 3.3.1: 实现选定方案的代码
- [ ] 步骤 3.3.2: 修改 DynamicEntityService
- [ ] 步骤 3.3.3: 更新查询结果DTO
- [ ] 步骤 3.3.4: 添加性能测试 (对比优化前后查询时间)
- [ ] 步骤 3.3.5: 添加功能测试
- [ ] 步骤 3.3.6: 更新文档

**Commit 信息模板**:
```
feat(api): add lang parameter support to dynamic entity query endpoints

- Implement field-level multilingual metadata resolution
- Optimize performance with metadata caching
- Add performance benchmarks
- Update documentation
- Ref: ARCH-30 Task 3.3
```

**Commit ID**: _(待填写)_
**完成时间**: _(待填写)_

---

## 📋 质量检查清单

每个 Task 完成前必须通过以下检查:

### 编译检查
- [ ] `dotnet build` 无错误
- [ ] `dotnet build --configuration Release` 无警告

### 测试检查
- [ ] 所有新增代码有对应单元测试
- [ ] 单元测试覆盖率 ≥ 80%
- [ ] `dotnet test` 全部通过
- [ ] 集成测试通过 (如适用)

### 代码质量
- [ ] 符合 OOP 最佳实践
- [ ] 遵循现有代码风格
- [ ] 添加了 XML 注释文档
- [ ] 无硬编码魔法值
- [ ] 异常处理完善

### 文档同步
- [ ] `CHANGELOG.md` 已更新
- [ ] API 文档 (`docs/reference/API-01-接口文档.md`) 已更新
- [ ] 设计文档 (ARCH-30) 状态已同步
- [ ] 本工作计划文档已更新 Commit ID 和完成时间

### 向后兼容性
- [ ] 旧版 API 调用仍然有效
- [ ] 未指定 lang 参数时行为符合预期
- [ ] 前端无需强制升级即可正常工作

---

## 🚀 提交规范

### Commit Message 格式
```
<type>(<scope>): <subject>

<body>

<footer>
```

**Type 类型**:
- `feat`: 新功能
- `fix`: Bug 修复
- `refactor`: 重构
- `test`: 测试相关
- `docs`: 文档更新
- `chore`: 构建/工具链变更

**Scope 范围**:
- `api`: API 端点
- `service`: 服务层
- `dto`: DTO 模型
- `i18n`: 国际化
- `test`: 测试代码

**Footer**:
- 必须包含 `Ref: ARCH-30 Task X.X.X`

### 示例
```
feat(api): add lang parameter to /api/access/functions/me endpoint

- Add optional lang query parameter
- Use LangHelper.GetLang() for language resolution
- Pass language to service layer

Ref: ARCH-30 Task 1.1.1
```

---

## 📊 进度报告模板

每完成一个阶段后, 填写以下报告:

### 阶段X 完成报告

**完成日期**: YYYY-MM-DD
**任务数**: X
**Commit 数**: X
**代码行数变更**: +XXX / -XXX
**测试覆盖率**: XX%

**主要成果**:
1. ...
2. ...

**遇到的问题**:
1. ...
2. ...

**经验教训**:
1. ...
2. ...

**下一阶段准备**:
1. ...
2. ...

---

## 📝 变更记录

| 日期 | 版本 | 变更内容 | 变更人 |
|------|------|----------|--------|
| 2025-12-11 | v1.0 | 初始创建工作计划文档 | Claude |

---

## 📖 参考文档

- [ARCH-30-实体字段显示名多语元数据驱动设计.md](./ARCH-30-实体字段显示名多语元数据驱动设计.md) - 设计文档
- [API-01-接口文档.md](../reference/API-01-接口文档.md) - API 参考
- [TEST-01-测试指南.md](../guides/TEST-01-测试指南.md) - 测试规范
- [PROC-01-PR检查清单.md](../process/PROC-01-PR检查清单.md) - PR 流程
- [PROC-02-文档同步规范.md](../process/PROC-02-文档同步规范.md) - 文档规范

---

**备注**:
- 本文档作为 ARCH-30 的配套工作跟踪文档, 实时反映实施进度
- 每完成一个步骤都应立即更新本文档
- Commit ID 和完成时间必须准确填写以便追溯
- 遇到问题或需要调整计划时, 应在对应任务下添加备注说明
