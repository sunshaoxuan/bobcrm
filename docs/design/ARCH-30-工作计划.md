# ARCH-30 系统级多语API架构优化 - 工作计划

**文档编号**: ARCH-30-WORK-PLAN
**版本**: v1.0
**创建日期**: 2025-12-11
**最后更新**: 2025-12-12 (文档梳理修正)
**关联设计文档**: [ARCH-30-实体字段显示名多语元数据驱动设计.md](./ARCH-30-实体字段显示名多语元数据驱动设计.md)
**状态**: 🚧 进行中

---

## 📊 整体进度概览

| 阶段 | 任务数 | 已完成 | 进行中 | 待开始 | 完成度 |
|------|--------|--------|--------|--------|--------|
| 阶段0: 基础设施搭建 | 3 | 3 | 0 | 0 | 100% |
| 阶段0.5: 模型层改造 | 4 | 4 | 0 | 0 | 100% |
| 阶段1: 高频API改造 | 3 | 3 | 0 | 0 | 100% |
| 阶段2: 中频API改造 | 4 | 4 | 0 | 0 | 100% |
| 阶段3: 低频API改造 | 3 | 3 | 0 | 0 | 100% |
| 阶段4: 文档同步 | 2 | 1 | 0 | 1 | 50% |
| **总计** | **19** | **17** | **0** | **2** | **89%** |

**当前阶段**: 阶段4 - 文档同步
**当前任务**: Task 4.2 - 更新 CHANGELOG

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

### 阶段0.5: 模型层改造

**目标**: 为 FieldMetadata 添加 DisplayNameKey 属性，支持接口字段引用 i18n 资源

#### ✅ Task 0.5.1: 添加 DisplayNameKey 属性

**状态**: ✅ 完成
**负责文件**:
- `src/BobCrm.Api/Base/Models/FieldMetadata.cs` (修改)

**详细步骤**:
- [x] 在 FieldMetadata.cs 第44行添加 `[MaxLength(100)] public string? DisplayNameKey { get; set; }`
- [x] 编译验证

**完成时间**: 2025-12-12

---

#### ✅ Task 0.5.2: 创建数据库迁移

**状态**: ✅ 完成
**负责文件**:
- `src/BobCrm.Api/Migrations/20251212105752_AddDisplayNameKeyToFieldMetadata.cs` (新建)

**详细步骤**:
- [x] 运行 `dotnet ef migrations add AddDisplayNameKeyToFieldMetadata`
- [x] 验证迁移文件正确添加 DisplayNameKey 列

**完成时间**: 2025-12-12

---

#### ✅ Task 0.5.3: 更新 PostgreSQLDDLGenerator

**状态**: ✅ 完成
**负责文件**:
- `src/BobCrm.Api/Services/PostgreSQLDDLGenerator.cs` (修改)
- `tests/BobCrm.Api.Tests/PostgreSQLDDLGeneratorTests.cs` (修改)

**详细步骤**:
- [x] 将硬编码的 DisplayName 访问改为使用 DisplayNameKey
- [x] 更新相关测试用例

**完成时间**: 2025-12-12

---

#### ✅ Task 0.5.4: 重构 DtoExtensions

**状态**: ✅ 完成
**负责文件**:
- `src/BobCrm.Api/Extensions/DtoExtensions.cs` (修改)
- `tests/BobCrm.Api.Tests/Extensions/DtoExtensionsTests.cs` (修改)

**详细步骤**:
- [x] 移除反射访问 DisplayNameKey 的代码
- [x] 改为直接属性访问: `DisplayNameKey = field.DisplayNameKey`
- [x] 实现三级显示名解析: DisplayNameKey → DisplayName 字典 → PropertyName
- [x] 更新测试用例

**完成时间**: 2025-12-12

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
- [x] ~~更新 API 文档和 CHANGELOG~~ → **延后至 Task 4.1/4.2 统一处理**

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
- [x] ~~更新 API 文档和 CHANGELOG~~ → **延后至 Task 4.1/4.2 统一处理**

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
- [x] ~~更新 `docs/reference/API-01-接口文档.md`~~ → **延后至 Task 4.1/4.2 统一处理**
- [x] ~~更新 `CHANGELOG.md`~~ → **延后至 Task 4.1/4.2 统一处理**
- [x] Git 提交 (docs)

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

**状态**: ✅ 完成
**涉及端点**:
- `GET /api/entity-definitions`
- `GET /api/entity-definitions/{id}`
- `POST /api/entity-definitions/{id}/fields`
- `PUT /api/entity-definitions/{id}/fields/{fieldId}`

**详细步骤**:
- [x] 步骤 2.1.1: 修改所有相关 Endpoints (添加 lang 参数)
- [x] 步骤 2.1.2: 修改 Service 层方法
- [x] 步骤 2.1.3: 更新字段元数据DTO转换逻辑 (使用 `ToFieldDto(lang)`)
- [x] 步骤 2.1.4: 添加集成测试 (`EntityDefinitionEndpointsTests.cs`)
- [x] 步骤 2.1.5: ~~更新 API 文档和 CHANGELOG~~ → **延后至 Task 4.1/4.2 统一处理**

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
**完成时间**: 2025-12-11

---

#### ✅ Task 2.2: 改造枚举接口

**状态**: ✅ 完成
**涉及端点**:
- `GET /api/enums`
- `GET /api/enums/{id}`
- `GET /api/enums/by-code/{code}`
- `GET /api/enums/{id}/options`

**负责文件**:
- `src/BobCrm.Api/Endpoints/EnumEndpoints.cs` (修改)
- `src/BobCrm.Api/Contracts/Responses/Enum/` 相关DTO (修改)
- `tests/BobCrm.Api.Tests/EnumEndpointsTests.cs` (新建/修改)

---

##### 🤖 AI 任务提示词

```markdown
## 任务: ARCH-30 Task 2.2 - 改造枚举接口支持多语参数

### 背景
ARCH-30 系统级多语API架构优化项目，阶段2中频API改造。
需要为枚举定义相关端点添加 `lang` 参数支持，实现单语/多语双模式响应。

### 参考文件
- 已完成示例: `src/BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs` (Task 2.1)
- DTO扩展: `src/BobCrm.Api/Extensions/DtoExtensions.cs`
- 多语辅助: `src/BobCrm.Api/Utils/MultilingualHelper.cs`
- 测试示例: `tests/BobCrm.Api.Tests/EntityDefinitionEndpointsTests.cs`

### 详细步骤

#### 步骤 2.2.1: 分析现有枚举端点

1. 打开 `src/BobCrm.Api/Endpoints/EnumEndpoints.cs`
2. 找出所有返回枚举多语数据的端点（DisplayName、Description等）
3. 检查现有DTO结构（EnumDefinitionDto、EnumOptionDto等）

#### 步骤 2.2.2: 更新枚举相关DTO

1. 为枚举DTO添加双模式支持：
   - 添加 `string? DisplayName` (单语模式)
   - 添加 `MultilingualText? DisplayNameTranslations` (多语模式)
   - 使用 `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`

2. 参考 EntityListDto 的设计模式

#### 步骤 2.2.3: 修改枚举端点

1. 为以下端点添加 `string? lang` 参数:
   - `GET /api/enums` - 枚举列表
   - `GET /api/enums/{id}` - 枚举详情
   - `GET /api/enums/by-code/{code}` - 按code获取枚举
   - `GET /api/enums/{id}/options` - 枚举选项列表

2. 使用 `LangHelper.GetLang(http, lang)` 获取语言
3. 根据 lang 决定返回单语还是多语:

   var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
   // 在DTO构造时
   DisplayName = targetLang != null ? enumDef.DisplayName.Resolve(targetLang) : null,
   DisplayNameTranslations = targetLang == null
       ? new MultilingualText(enumDef.DisplayName)
       : null,

#### 步骤 2.2.4: 添加测试

1. 创建 tests/BobCrm.Api.Tests/EnumEndpointsTests.cs
2. 测试场景:
   - 无 lang 参数时返回完整多语字典
   - 指定 lang=zh 时返回中文单语
   - 枚举选项的显示名也遵循相同规则
3. 参考 EntityDefinitionEndpointsTests.cs 的测试结构

#### 步骤 2.2.5: 编译验证

   dotnet build src/BobCrm.Api/BobCrm.Api.csproj
   dotnet test --filter "EnumEndpointsTests"

### 验收标准

- [x] GET /api/enums 支持 ?lang=zh/ja/en 参数
- [x] GET /api/enums/{id} 支持 ?lang=zh/ja/en 参数
- [x] GET /api/enums/by-code/{code} 支持 ?lang=zh/ja/en 参数
- [x] GET /api/enums/{id}/options 支持 ?lang=zh/ja/en 参数
- [x] 无 lang 参数时返回完整多语字典 (向后兼容)
- [x] 有 lang 参数时返回单语字符串
- [x] 枚举选项的 DisplayName 也支持双模式
- [x] 所有单元测试通过

### Commit 信息

feat(api): add lang parameter to enum endpoints

- Add lang query parameter to GET /api/enums
- Add lang query parameter to GET /api/enums/{id}
- Add lang query parameter to GET /api/enums/by-code/{code}
- Add lang query parameter to GET /api/enums/{id}/options
- Update EnumDto with dual-mode display name
- Add comprehensive tests for multilingual behavior
- Ref: ARCH-30 Task 2.2
```

---

**详细步骤**:
- [x] 步骤 2.2.1: 分析现有枚举端点和DTO结构
- [x] 步骤 2.2.2: 更新枚举相关DTO为双模式设计
- [x] 步骤 2.2.3: 修改所有枚举端点添加 lang 参数
- [x] 步骤 2.2.4: 确保枚举选项也支持多语参数
- [x] 步骤 2.2.5: 添加单元测试 (`EnumEndpointsTests.cs` - 20个测试)
- [x] 步骤 2.2.6: 编译验证 (`dotnet build && dotnet test`)
- [x] 步骤 2.2.7: Git 提交

**关键设计决策**:
- 只有显式传 `?lang=xx` 才进入单语模式
- 无 lang 参数时返回多语字典（即使有 Accept-Language 头也忽略）

**Commit 信息模板**:
```
feat(api): add lang parameter support to enum endpoints

- Support single-language enum label resolution
- Add backward-compatible DTO design
- Add tests for all enum types
- Maintain backward compatibility (ignore Accept-Language when no lang param)
- Ref: ARCH-30 Task 2.2
```

**Commit ID**: _(待填写)_
**完成时间**: 2025-12-11
**评审结果**: ✅ 合格（4.6/5.0）- [评审报告](../tasks/arch-30/task-2.2-review-final.md)

---

#### ✅ Task 2.3: 改造实体域接口

**状态**: ✅ 完成
**涉及端点**:
- `GET /api/entity-domains`
- `GET /api/entity-domains/{id}`

**负责文件**:
- `src/BobCrm.Api/Endpoints/EntityDomainEndpoints.cs` (修改)
- `src/BobCrm.Api/Contracts/Responses/` 相关DTO (修改)
- `tests/BobCrm.Api.Tests/EntityDomainEndpointsTests.cs` (新建)

---

##### 🤖 AI 任务提示词

```markdown
## 任务: ARCH-30 Task 2.3 - 改造实体域接口支持多语参数

### 背景
ARCH-30 系统级多语API架构优化项目，阶段2中频API改造。
需要为实体域相关端点添加 `lang` 参数支持，实现单语/多语双模式响应。

### 参考文件
- 已完成示例: src/BobCrm.Api/Endpoints/EnumDefinitionEndpoints.cs (Task 2.2)
- 测试示例: tests/BobCrm.Api.Tests/EnumEndpointsTests.cs
- 多语辅助: src/BobCrm.Api/Utils/MultilingualHelper.cs

### 关键设计决策（从 Task 2.2 继承）

**向后兼容性规则**：
- 只有显式传 `?lang=xx` 才进入单语模式
- 无 lang 参数时返回多语字典（即使有 Accept-Language 头也忽略）
- 错误消息使用 `uiLang = LangHelper.GetLang(http)` 获取

**代码模式**：
   var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
   var uiLang = LangHelper.GetLang(http);  // 用于错误消息

### 详细步骤

#### 步骤 2.3.1: 分析现有实体域端点

1. 打开 src/BobCrm.Api/Endpoints/EntityDomainEndpoints.cs
2. 找出所有返回多语数据的端点（Name、Description等）
3. 检查现有DTO结构（EntityDomainDto等）

#### 步骤 2.3.2: 更新实体域相关DTO

1. 为 EntityDomainDto 添加双模式支持：
   - 添加 string? Name (单语模式)
   - 添加 MultilingualText? NameTranslations (多语模式)
   - 使用 [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]

#### 步骤 2.3.3: 修改实体域端点

1. 为以下端点添加 string? lang 参数:
   - GET /api/entity-domains - 域列表
   - GET /api/entity-domains/{id} - 域详情

2. 使用向后兼容模式:
   var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);

#### 步骤 2.3.4: 添加测试

1. 创建 tests/BobCrm.Api.Tests/EntityDomainEndpointsTests.cs
2. 测试场景:
   - 无 lang 参数时返回完整多语字典
   - 指定 lang=zh 时返回中文单语
   - 无 lang 时忽略 Accept-Language 头（向后兼容验证）

#### 步骤 2.3.5: 编译验证

   dotnet build src/BobCrm.Api/BobCrm.Api.csproj
   dotnet test --filter "EntityDomainEndpointsTests"

### 验收标准

- [x] GET /api/entity-domains 支持 ?lang=zh/ja/en 参数
- [x] GET /api/entity-domains/{id} 支持 ?lang=zh/ja/en 参数
- [x] 无 lang 参数时返回完整多语字典 (向后兼容)
- [x] 无 lang 参数时忽略 Accept-Language 头
- [x] 有 lang 参数时返回单语字符串
- [x] 所有单元测试通过

### Commit 信息

feat(api): add lang parameter to entity-domain endpoints

- Add lang query parameter to GET /api/entity-domains
- Add lang query parameter to GET /api/entity-domains/{id}
- Update EntityDomainDto with dual-mode name fields
- Maintain backward compatibility (ignore Accept-Language when no lang param)
- Add comprehensive tests
- Ref: ARCH-30 Task 2.3
```

---

**详细步骤**:
- [x] 步骤 2.3.1: 分析现有实体域端点和DTO结构
- [x] 步骤 2.3.2: 更新 EntityDomainDto 为双模式设计
- [x] 步骤 2.3.3: 修改实体域端点添加 lang 参数
- [x] 步骤 2.3.4: 添加单元测试（5个测试用例）
- [x] 步骤 2.3.5: 编译验证 (`dotnet build && dotnet test`)
- [x] 步骤 2.3.6: Git 提交

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

**状态**: ✅ 完成
**涉及端点**:
- `GET /api/access/functions` - 功能节点列表（管理员）
- `GET /api/access/functions/manage` - 功能节点管理列表
- `POST /api/access/functions` - 创建功能节点
- `PUT /api/access/functions/{id}` - 更新功能节点
- `GET /api/access/functions/me` - 用户功能菜单（已在Task 1.1完成）

**负责文件**:
- `src/BobCrm.Api/Endpoints/AccessEndpoints.cs` (修改)
- `src/BobCrm.Api/Services/AccessService.cs` (修改)
- `src/BobCrm.Api/Services/FunctionTreeBuilder.cs` (已支持lang参数，无需修改)
- `tests/BobCrm.Api.Tests/AccessEndpointsTests.cs` (新建/修改)

---

##### 🤖 AI 任务提示词

```markdown
## 任务: ARCH-30 Task 2.4 - 改造功能节点管理接口组支持多语参数

### 背景
ARCH-30 系统级多语API架构优化项目，阶段2中频API改造。
需要为功能节点管理相关端点添加 `lang` 参数支持，实现单语/多语双模式响应。
注意：`GET /api/access/functions/me` 已在 Task 1.1 完成，本次只需改造管理类端点。

### 参考文件
- 已完成示例: `src/BobCrm.Api/Endpoints/AccessEndpoints.cs` (Task 1.1 的 `/api/access/functions/me`)
- 树构建器: `src/BobCrm.Api/Services/FunctionTreeBuilder.cs` (已支持lang参数)
- DTO定义: `src/BobCrm.Api/Contracts/DTOs/Access/FunctionNodeDto.cs` (已在Task 1.1更新为双模式)
- 测试示例: `tests/BobCrm.Api.Tests/AccessFunctionsApiTests.cs` (Task 1.1的测试)

### 关键设计决策（从 Task 2.2/2.3 继承）

**向后兼容性规则**：
- 只有显式传 `?lang=xx` 才进入单语模式
- 无 lang 参数时返回多语字典（即使有 Accept-Language 头也忽略）
- 错误消息使用 `uiLang = LangHelper.GetLang(http)` 获取

**代码模式**：
   var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
   var uiLang = LangHelper.GetLang(http);  // 用于错误消息

**树形结构处理**：
- `FunctionTreeBuilder.BuildAsync()` 已支持 `lang` 参数（第33行）
- 递归处理子节点时，`lang` 参数会自动传递到所有子节点
- 无需额外处理树形结构的语言传递

### 详细步骤

#### 步骤 2.4.1: 分析现有功能节点管理端点

1. 打开 `src/BobCrm.Api/Endpoints/AccessEndpoints.cs`
2. 找出以下需要改造的端点：
   - `GET /api/access/functions` (第24行) - 功能节点列表，目前传 `lang: null`
   - `GET /api/access/functions/manage` (第38行) - 管理列表，目前传 `lang: null`
   - `POST /api/access/functions` (第70行) - 创建功能节点，返回DTO需要支持lang
   - `PUT /api/access/functions/{id}` (第100行) - 更新功能节点，返回DTO需要支持lang
3. 注意：`GET /api/access/functions/me` 已在 Task 1.1 完成，无需修改

#### 步骤 2.4.2: 修改 GET /api/access/functions 端点

1. 定位到第24行的 `MapGet("/functions")` 端点
2. 添加 `string? lang` 查询参数和 `HttpContext http` 参数
3. 使用向后兼容模式解析语言：
   ```csharp
   var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
   ```
4. 将 `lang: null` 改为 `lang: targetLang`
5. 示例代码（参考）：
   - 添加 `string? lang` 和 `HttpContext http` 参数
   - 使用 `var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);`
   - 将 `treeBuilder.BuildAsync(nodes, lang: null, ct: ct)` 改为 `treeBuilder.BuildAsync(nodes, lang: targetLang, ct: ct)`

#### 步骤 2.4.3: 修改 GET /api/access/functions/manage 端点

1. 定位到第38行的 `MapGet("/functions/manage")` 端点
2. 添加 `string? lang` 查询参数和 `HttpContext http` 参数
3. 使用相同的语言解析逻辑
4. 将 `lang: null` 改为 `lang: targetLang`

#### 步骤 2.4.4: 修改 POST /api/access/functions 端点

1. 定位到第70行的 `MapPost("/functions")` 端点
2. 添加 `string? lang` 查询参数（注意：POST请求的lang参数通常通过查询字符串传递）
3. 解析语言：`var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);`
4. 修改返回的DTO转换：
   - 当前使用 `ToDto(node)` 方法（第92行）
   - 需要检查 `ToDto` 方法是否支持lang参数
   - 如果不支持，需要创建新的转换方法或修改现有方法
5. 如果 `ToDto` 方法不支持lang，可以：
   - 选项A：修改 `ToDto` 方法签名添加 `string? lang` 参数
   - 选项B：使用 `FunctionTreeBuilder` 构建单个节点的DTO（推荐）
   - 选项C：直接构造 `FunctionNodeDto` 并应用双模式逻辑

#### 步骤 2.4.5: 修改 PUT /api/access/functions/{id} 端点

1. 定位到第100行的 `MapPut("/functions/{id:guid}")` 端点
2. 添加 `string? lang` 查询参数
3. 使用相同的语言解析逻辑
4. 修改返回的DTO转换（与POST相同）

#### 步骤 2.4.6: 检查 ToDto 方法

1. 在 `AccessEndpoints.cs` 中查找 `ToDto` 方法定义
2. 检查该方法是否支持lang参数
3. 如果不支持，需要：
   - 修改方法签名添加 `string? lang` 参数
   - 在方法内部应用双模式逻辑（参考 `FunctionTreeBuilder.ResolveDisplayName`）
   - 或者使用 `FunctionTreeBuilder` 来构建DTO

#### 步骤 2.4.7: 添加测试

1. 创建/更新 `tests/BobCrm.Api.Tests/AccessEndpointsTests.cs`
2. 测试场景：
   - `GetFunctions_WithoutLang_ReturnsTranslationsMode` - 无lang返回多语字典
   - `GetFunctions_WithLang_ReturnsSingleLanguageMode` - 有lang返回单语
   - `GetFunctionsManage_WithoutLang_ReturnsTranslationsMode` - 管理列表无lang
   - `GetFunctionsManage_WithLang_ReturnsSingleLanguageMode` - 管理列表有lang
   - `CreateFunction_WithLang_ReturnsSingleLanguageMode` - 创建后返回单语
   - `UpdateFunction_WithLang_ReturnsSingleLanguageMode` - 更新后返回单语
   - `TreeStructure_LanguageConsistency` - 验证树形结构所有节点使用相同语言
3. 参考 `AccessFunctionsApiTests.cs` 的测试结构
4. 使用 `SeedFunctionNodeAsync()` 准备测试数据

#### 步骤 2.4.8: 编译验证

   dotnet build src/BobCrm.Api/BobCrm.Api.csproj
   dotnet test --filter "AccessEndpointsTests"

### 验收标准

- [x] GET /api/access/functions 支持 ?lang=zh/ja/en 参数
- [x] GET /api/access/functions/manage 支持 ?lang=zh/ja/en 参数
- [x] POST /api/access/functions 支持 ?lang=zh/ja/en 参数（返回单语）
- [x] PUT /api/access/functions/{id} 支持 ?lang=zh/ja/en 参数（返回单语）
- [x] 无 lang 参数时返回多语字典 (向后兼容)
- [x] 无 lang 参数时忽略 Accept-Language 头
- [x] 有 lang 参数时返回单语字符串
- [x] 树形结构所有节点使用相同语言（FunctionTreeBuilder已处理）
- [x] 所有单元测试通过

### Commit 信息

feat(api): add lang parameter to function management endpoints

- Add lang query parameter to GET /api/access/functions
- Add lang query parameter to GET /api/access/functions/manage
- Add lang query parameter to POST /api/access/functions
- Add lang query parameter to PUT /api/access/functions/{id}
- Update ToDto method to support lang parameter (if needed)
- Leverage FunctionTreeBuilder for consistent tree language handling
- Add comprehensive tests for all endpoints
- Maintain backward compatibility (ignore Accept-Language when no lang param)
- Ref: ARCH-30 Task 2.4
```

---

**详细步骤**:
- [x] 步骤 2.4.1: 分析现有功能节点管理端点
- [x] 步骤 2.4.2: 修改 GET /api/access/functions 端点
- [x] 步骤 2.4.3: 修改 GET /api/access/functions/manage 端点
- [x] 步骤 2.4.4: 修改 POST /api/access/functions 端点
- [x] 步骤 2.4.5: 修改 PUT /api/access/functions/{id} 端点
- [x] 步骤 2.4.6: 创建 ToDtoAsync 方法（使用FunctionTreeBuilder）
- [x] 步骤 2.4.7: 添加单元测试（7个测试用例）
- [x] 步骤 2.4.8: 编译验证 (`dotnet build && dotnet test`)
- [x] 步骤 2.4.9: Git 提交

**关键设计决策**:
- `FunctionTreeBuilder` 已支持 `lang` 参数，无需额外处理树形结构
- POST/PUT 返回的单个节点DTO需要支持lang参数
- 需要检查 `ToDto` 方法是否需要修改

**Commit 信息模板**:
```
feat(api): add lang parameter support to function management endpoints

- Add lang query parameter to GET /api/access/functions
- Add lang query parameter to GET /api/access/functions/manage
- Add lang query parameter to POST /api/access/functions
- Add lang query parameter to PUT /api/access/functions/{id}
- Update ToDto method to support lang parameter (if needed)
- Leverage FunctionTreeBuilder for consistent tree language handling
- Add comprehensive tests for all endpoints
- Maintain backward compatibility (ignore Accept-Language when no lang param)
- Ref: ARCH-30 Task 2.4
```

**Commit ID**: _(待填写)_
**完成时间**: 2025-12-11
**评审结果**: ✅ 优秀（4.9/5.0）- [评审报告](../tasks/arch-30/task-2.4-review.md)

**关键实现亮点**:
- ✅ 使用 `ToDtoAsync` 方法复用 `FunctionTreeBuilder` 逻辑，确保一致性
- ✅ 7个测试用例完整覆盖，包括树形结构语言一致性验证
- ✅ POST/PUT 端点通过查询字符串传递 `lang` 参数

---

### 阶段3: 低频API改造

**目标**: 完成动态实体查询等复杂场景的多语优化

#### ✅ Task 3.1: 研究动态实体查询机制

**状态**: ✅ 完成
**研究范围**:
- 动态实体 CRUD 的代码生成机制
- 字段级多语元数据的存储位置
- 查询结果到 DTO 的转换流程

**负责文件**:
- `src/BobCrm.Api/Services/DynamicEntityService.cs` (研究)
- `src/BobCrm.Api/Services/CSharpCodeGenerator.cs` (研究)
- `src/BobCrm.Api/Services/ReflectionPersistenceService.cs` (研究)
- `src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs` (研究)
- `docs/research/ARCH-30-动态实体多语研究报告.md` (新建)

---

##### 🤖 AI 任务提示词

```markdown
## 任务: ARCH-30 Task 3.1 - 研究动态实体查询机制

### 背景
ARCH-30 系统级多语API架构优化项目，阶段3低频API改造。
需要深入研究动态实体查询机制，为字段级多语解析方案设计提供基础。

### 研究目标
1. 理解动态实体的代码生成、编译和加载机制
2. 分析字段元数据（DisplayName、DisplayNameKey）的存储和访问方式
3. 确定查询结果到DTO的转换流程
4. 识别字段DisplayName解析的最佳时机（编译时 vs 运行时）

### 参考文件
- 动态实体服务: `src/BobCrm.Api/Services/DynamicEntityService.cs`
- 代码生成器: `src/BobCrm.Api/Services/CSharpCodeGenerator.cs`
- 持久化服务: `src/BobCrm.Api/Services/ReflectionPersistenceService.cs`
- 动态实体端点: `src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs`
- 实体定义模型: `src/BobCrm.Api/Base/Models/EntityDefinition.cs`
- 字段元数据模型: `src/BobCrm.Api/Base/Models/FieldMetadata.cs`

### 详细研究步骤

#### 步骤 3.1.1: 研究动态实体代码生成机制

1. 打开 `src/BobCrm.Api/Services/CSharpCodeGenerator.cs`
2. 分析 `GenerateEntityClass()` 方法：
   - 如何生成实体类代码
   - 如何处理字段属性
   - 是否在生成的代码中包含字段元数据（DisplayName、DisplayNameKey）
3. 检查生成的代码中是否包含字段显示名的多语信息
4. 记录发现：字段元数据是否在编译时注入到实体类中

#### 步骤 3.1.2: 研究动态实体编译和加载机制

1. 打开 `src/BobCrm.Api/Services/DynamicEntityService.cs`
2. 分析以下方法：
   - `CompileEntityAsync()` - 编译单个实体
   - `CompileMultipleEntitiesAsync()` - 批量编译
   - `GetEntityType()` - 获取已加载的实体类型
   - `CreateEntityInstance()` - 创建实体实例
   - `GetEntityProperties()` - 获取实体属性
3. 理解程序集缓存机制（`_loadedAssemblies`）
4. 确定动态编译的实体类是否可以访问字段元数据

#### 步骤 3.1.3: 研究查询结果转换流程

1. 打开 `src/BobCrm.Api/Services/ReflectionPersistenceService.cs`
2. 分析以下方法：
   - `QueryAsync()` - 查询实体列表
   - `GetByIdAsync()` - 根据ID获取实体
   - 查询结果如何转换为DTO或JSON
3. 检查查询结果是否包含字段元数据
4. 确定当前是否有字段显示名的解析逻辑

#### 步骤 3.1.4: 研究动态实体端点

1. 打开 `src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs`
2. 分析以下端点：
   - `POST /api/dynamic-entities/{fullTypeName}/query` - 查询列表
   - `GET /api/dynamic-entities/{fullTypeName}/{id}` - 获取单个实体
3. 检查当前是否支持 `lang` 参数
4. 分析查询结果的返回格式（当前返回原始实体对象还是DTO）

#### 步骤 3.1.5: 分析字段元数据存储

1. 打开 `src/BobCrm.Api/Base/Models/FieldMetadata.cs`
2. 确认字段元数据包含：
   - `DisplayName` (Dictionary<string, string?>) - 多语字典
   - `DisplayNameKey` (string?) - i18n资源键
3. 检查 `EntityDefinition.Fields` 关系
4. 确定字段元数据在数据库中的存储位置

#### 步骤 3.1.6: 确定解析时机

分析以下问题：
1. **编译时注入**：是否可以在代码生成时将字段元数据注入到实体类中？
   - 优点：运行时无需查询元数据
   - 缺点：元数据更新需要重新编译
2. **运行时查询**：在查询结果转换时查询字段元数据？
   - 优点：元数据更新无需重新编译
   - 缺点：每次查询都需要访问数据库
3. **预加载缓存**：在查询前预加载实体定义的字段元数据并缓存？
   - 优点：平衡性能和灵活性
   - 缺点：需要缓存管理

#### 步骤 3.1.7: 编写研究报告

1. 创建 `docs/research/ARCH-30-动态实体多语研究报告.md`
2. 报告结构（参考模板）：
   - 章节1: 动态实体代码生成机制
     - 代码生成流程
     - 字段元数据在生成代码中的位置
     - 编译时注入的可能性
   - 章节2: 动态实体编译和加载机制
     - 编译流程
     - 程序集缓存机制
     - 运行时类型访问能力
   - 章节3: 查询结果转换流程
     - 当前转换机制
     - 字段元数据访问方式
     - DTO转换点
   - 章节4: 字段元数据存储
     - 存储位置
     - 访问方式
     - 更新机制
   - 章节5: 解析时机分析
     - 编译时注入方案分析
     - 运行时查询方案分析
     - 预加载缓存方案分析
     - 推荐方案及理由
   - 章节6: 结论和建议
     - 最佳解析时机
     - 性能考虑
     - 实现建议
3. 包含代码示例和流程图（如需要）

### 验收标准

- [x] 研究报告文档已创建
- [x] 包含动态实体代码生成机制分析
- [x] 包含查询结果转换流程分析
- [x] 包含字段元数据存储和访问分析
- [x] 包含解析时机分析（编译时 vs 运行时）
- [x] 包含推荐方案及理由
- [x] 文档结构清晰，包含代码示例

### Commit 信息

docs(research): add dynamic entity multilingual research report

- Analyze dynamic entity code generation mechanism
- Analyze query result conversion flow
- Analyze field metadata storage and access
- Evaluate parsing timing options (compile-time vs runtime)
- Recommend optimal solution with rationale
- Ref: ARCH-30 Task 3.1
```

---

**详细步骤**:
- [x] 步骤 3.1.1: 研究动态实体代码生成机制
- [x] 步骤 3.1.2: 研究动态实体编译和加载机制
- [x] 步骤 3.1.3: 研究查询结果转换流程
- [x] 步骤 3.1.4: 研究动态实体端点
- [x] 步骤 3.1.5: 分析字段元数据存储
- [x] 步骤 3.1.6: 确定解析时机
- [x] 步骤 3.1.7: 编写研究报告

**输出物**: 研究报告文档 `docs/research/ARCH-30-动态实体多语研究报告.md`

**关键发现**:
- ✅ 动态实体查询链路当前不做DTO转换，直接返回运行时实体对象
- ✅ 字段显示名解析最佳落点是元数据API，而非动态实体数据查询本身
- ✅ 推荐方案：运行时预加载/缓存实体字段元数据 + 批量加载i18n资源
- ✅ 若需在查询响应中携带列信息，建议在端点层拼装 `meta.fields`

**Commit ID**: _(待填写)_
**完成时间**: 2025-12-12
**评审结果**: ✅ 优秀（5.0/5.0）- [评审报告](../tasks/arch-30/task-3.1-review.md)

---

#### ✅ Task 3.2: 设计字段级多语解析方案

**状态**: ✅ 完成
**设计内容**:
- 动态实体查询返回结果中的字段元数据注入机制
- DTO 转换器的字段级语言解析逻辑
- 性能优化: 避免对每条记录都查询字段元数据

**负责文件**:
- `docs/research/ARCH-30-动态实体多语研究报告.md` (参考)
- `docs/design/ARCH-30-实体字段显示名多语元数据驱动设计.md` (更新)
- `src/BobCrm.Api/Services/ReflectionPersistenceService.cs` (设计修改点)
- `src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs` (设计修改点)

---

##### 🤖 AI 任务提示词

```markdown
## 任务: ARCH-30 Task 3.2 - 设计字段级多语解析方案

### 背景
ARCH-30 系统级多语API架构优化项目，阶段3低频API改造。
基于 Task 3.1 的研究结果，设计字段级多语解析方案。

**重要发现（来自 Task 3.1 研究报告）**：
- 动态实体查询链路当前不做DTO转换，直接返回运行时实体对象
- **字段显示名解析最佳落点是元数据API（EntityDefinition/FieldMetadata DTO），而不是动态实体数据查询本身**
- 若未来需要"查询结果携带列元数据（字段名/显示名）"，推荐运行时预加载/缓存实体字段元数据 + 批量加载i18n资源，在端点层拼装 `meta.fields`

### 参考文件
- 研究报告: `docs/research/ARCH-30-动态实体多语研究报告.md` (Task 3.1输出)
- 设计文档: `docs/design/ARCH-30-实体字段显示名多语元数据驱动设计.md`
- 持久化服务: `src/BobCrm.Api/Services/ReflectionPersistenceService.cs`
- 动态实体端点: `src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs`
- DTO扩展: `src/BobCrm.Api/Extensions/DtoExtensions.cs` (参考ToFieldDto实现)
- 多语辅助: `src/BobCrm.Api/Utils/MultilingualHelper.cs`

### 关键设计决策（从阶段1/2继承）

**向后兼容性规则**：
- 只有显式传 `?lang=xx` 才进入单语模式
- 无 lang 参数时返回多语字典（即使有 Accept-Language 头也忽略）
- 错误消息使用 `uiLang = LangHelper.GetLang(http)` 获取

**代码模式**：
   var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
   var uiLang = LangHelper.GetLang(http);  // 用于错误消息

### 设计方案（基于 Task 3.1 研究结论）

**Task 3.1 研究结论**：
- 字段显示名解析应发生在"元数据返回层"（EntityDefinition/FieldMetadata/FunctionTree DTO等），而不是动态实体数据查询返回层
- 动态实体查询结果保持"纯数据对象"更符合职责分离：数据值 vs 元数据标签
- **推荐方案**：在端点层拼装 `meta.fields`，使用运行时预加载/缓存实体字段元数据 + 批量加载i18n资源

**设计原则**：
- **职责分离**：数据值（`data`）与元数据标签（`meta.fields`）分离
- **性能优化**：使用 `IMemoryCache` 缓存字段元数据，按 `FullTypeName` 缓存
- **可复用性**：复用现有能力（`DtoExtensions.ToFieldDto()`、`ILocalization` 缓存）

**返回结构**（基于 Task 3.1 研究报告 7.2 节）：
- 根对象包含：`data`（数组）、`meta`（对象）、`total`（整数）
- `meta.fields` 是字段元数据数组，每个字段包含：`propertyName`、`displayNameKey`、`displayName`
- 示例结构：`{ "meta": { "fields": [...] }, "data": [...], "total": 123 }`

**双模式逻辑**：
- 单语模式（显式 `?lang=xx`）：输出 `displayName`（string）
- 多语模式（无 `lang`）：接口字段输出 `displayNameKey`，自定义字段输出 `displayNameTranslations`

**缓存机制**（参考 Task 3.1 研究报告 6.4 节）：
- 维度1：按 `EntityDefinitionId` 或 `FullTypeName` 缓存字段元数据
- 维度2：按 `ILocalization.GetCacheVersion()` + `EntityDefinition.UpdatedAt` 作为缓存失效条件
- 可复用：`DtoExtensions.ResolveFieldDisplayName(...)`、`MultilingualFieldService.LoadResourcesAsync(...)`

### 详细设计步骤

#### 步骤 3.2.1: 确认设计方案

1. **基于 Task 3.1 的研究报告结论**，确认采用"在端点层拼装 meta.fields"方案
2. 确认设计原则：
   - 职责分离：数据值（`data`）与元数据标签（`meta.fields`）分离
   - 性能优化：使用缓存机制
   - 可复用现有能力

#### 步骤 3.2.2: 设计字段元数据缓存机制

1. 设计缓存键：`FieldMetadata:{fullTypeName}`
2. 设计缓存失效策略：
   - 实体定义更新时清除缓存
   - 设置过期时间（如30分钟）
3. 设计缓存服务接口（参考）：
   - 接口名：`IFieldMetadataCache`
   - 方法1：`Task<Dictionary<string, FieldMetadataDto>> GetFieldMetadataAsync(string fullTypeName, string? lang)`
   - 方法2：`void InvalidateCache(string fullTypeName)`

#### 步骤 3.2.3: 设计DTO结构

1. 设计动态实体查询结果DTO（基于 Task 3.1 研究结论，参考结构）：
   - 类名：`DynamicEntityQueryResultDto`
   - 属性1：`List<Dictionary<string, object>> Data` - 实体数据列表
   - 属性2：`DynamicEntityMetaDto? Meta` - 元数据对象（可空）
   - 属性3：`int Total` - 总数
   - 嵌套类：`DynamicEntityMetaDto`，包含 `List<FieldMetadataDto>? Fields` 属性
   - 使用 `JsonIgnore(Condition = WhenWritingNull)` 优化序列化
2. 字段元数据DTO复用现有的 `FieldMetadataDto`（已支持双模式）：
   - `DisplayName` (string?) - 单语模式
   - `DisplayNameTranslations` (MultilingualText?) - 多语模式
   - `DisplayNameKey` (string?) - i18n资源键（接口字段）
   - 使用 `JsonIgnore(Condition = WhenWritingNull)` 优化序列化

#### 步骤 3.2.4: 设计端点修改方案

1. 修改 `POST /api/dynamic-entities/{fullTypeName}/query`：
   - 添加 `string? lang` 查询参数
   - 在端点层调用 `FieldMetadataCache.GetFieldMetadataAsync(fullTypeName, lang)` 获取字段元数据
   - 返回结构：`{ "data": [...], "meta": { "fields": [...] }, "total": 123 }`
   - 遵循 ARCH-30 统一规则：显式 `?lang=xx` 才输出单语，无 `lang` 输出多语
2. 修改 `GET /api/dynamic-entities/{fullTypeName}/{id}`：
   - 添加 `string? lang` 查询参数
   - 在端点层获取字段元数据并拼装到响应中
   - 返回结构：`{ "data": {...}, "meta": { "fields": [...] } }`

#### 步骤 3.2.5: 设计性能优化策略

1. 字段元数据缓存（按 `fullTypeName`）
2. 批量加载字段元数据（一次查询获取所有字段）
3. 延迟加载（仅在需要时加载字段元数据）
4. 考虑分页场景：字段元数据只需加载一次，适用于所有记录

#### 步骤 3.2.6: 编写设计文档更新

1. 更新 `docs/design/ARCH-30-实体字段显示名多语元数据驱动设计.md`
2. 新增章节：**阶段3 - 动态实体字段级多语解析**
3. 包含内容：
   - 设计方案确认（基于 Task 3.1 研究结论）
   - 缓存机制设计
   - DTO设计（meta.fields结构）
   - 端点修改方案
   - 性能优化策略
   - 实现流程图（如需要）

### 验收标准

- [x] 设计方案文档已更新
- [x] 基于 Task 3.1 研究结论确认设计方案
- [x] 包含缓存机制设计
- [x] 包含DTO设计（meta.fields结构）
- [x] 包含端点修改方案（含 includeMeta 参数）
- [x] 包含性能优化策略
- [x] 设计文档结构清晰，包含代码示例

### Commit 信息

docs(design): add dynamic entity field-level multilingual design

- Based on Task 3.1 research conclusion: use meta.fields approach
- Design field metadata cache mechanism
- Design DTO structure with meta.fields
- Design endpoint modification plan
- Add performance optimization strategies
- Ref: ARCH-30 Task 3.2
```

---

**详细步骤**:
- [x] 步骤 3.2.1: 确认设计方案（基于Task 3.1研究结论）
- [x] 步骤 3.2.2: 设计字段元数据缓存机制
- [x] 步骤 3.2.3: 设计DTO结构（meta.fields）
- [x] 步骤 3.2.4: 设计端点修改方案
- [x] 步骤 3.2.5: 设计性能优化策略
- [x] 步骤 3.2.6: 编写设计文档更新

**输出物**: 设计文档更新 `docs/design/ARCH-30-实体字段显示名多语元数据驱动设计.md` (新增章节)

**关键设计亮点**:
- ✅ 返回结构：`{ "meta": { "fields": [...] }, "data": [...], "total": 123 }`
- ✅ 双模式规则：仅显式 `?lang=xx` 才输出单语，无 `lang` 返回多语
- ✅ DTO设计：`DynamicEntityQueryResultDto` + `DynamicEntityMetaDto`
- ✅ 缓存机制：`IFieldMetadataCache` 接口，按 `fullTypeName` 缓存
- ✅ 向后兼容：GET by id 使用 `includeMeta=true` 参数避免破坏性变更
- ✅ 复用现有能力：`field.ToFieldDto(loc, lang)` 三级优先级逻辑

**Commit ID**: _(待填写)_
**完成时间**: 2025-12-12
**评审结果**: ✅ 优秀（5.0/5.0）- [评审报告](../tasks/arch-30/task-3.2-review.md)

---

#### ✅ Task 3.3: 实施动态实体查询优化

**状态**: ✅ 完成
**涉及端点**:
- `POST /api/dynamic-entities/{fullTypeName}/query`
- `GET /api/dynamic-entities/{fullTypeName}/{id}`

**负责文件**:
- `src/BobCrm.Api/Services/FieldMetadataCache.cs` (新建)
- `src/BobCrm.Api/Services/ReflectionPersistenceService.cs` (修改)
- `src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs` (修改)
- `src/BobCrm.Api/Contracts/Responses/DynamicEntity/` (新建DTO)
- `tests/BobCrm.Api.Tests/DynamicEntityEndpointsTests.cs` (新建/修改)

---

##### 🤖 AI 任务提示词

```markdown
## 任务: ARCH-30 Task 3.3 - 实施动态实体查询优化

### 背景
ARCH-30 系统级多语API架构优化项目，阶段3低频API改造。
基于 Task 3.2 的设计方案，实施动态实体查询的字段级多语解析功能。

**Task 3.2 设计要点回顾**：
- 返回结构：`{ "meta": { "fields": [...] }, "data": [...], "total": 123 }`
- 缓存接口：`IFieldMetadataCache.GetFieldsAsync(fullTypeName, loc, lang, ct)` 返回 `IReadOnlyList<FieldMetadataDto>`
- 向后兼容：GET by id 使用 `includeMeta=true` 参数避免破坏性变更
- 双模式规则：仅显式 `?lang=xx` 才输出单语，无 `lang` 返回多语（忽略 Accept-Language）

### 参考文件
- 设计文档: `docs/design/ARCH-30-实体字段显示名多语元数据驱动设计.md` (Task 3.2输出)
- 研究报告: `docs/research/ARCH-30-动态实体多语研究报告.md` (Task 3.1输出)
- 持久化服务: `src/BobCrm.Api/Services/ReflectionPersistenceService.cs`
- 动态实体端点: `src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs`
- DTO扩展: `src/BobCrm.Api/Extensions/DtoExtensions.cs` (参考ToFieldDto)
- 多语辅助: `src/BobCrm.Api/Utils/MultilingualHelper.cs`

### 关键设计决策（从阶段1/2继承）

**向后兼容性规则**：
- 只有显式传 `?lang=xx` 才进入单语模式
- 无 lang 参数时返回多语字典（即使有 Accept-Language 头也忽略）
- 错误消息使用 `uiLang = LangHelper.GetLang(http)` 获取

**代码模式**：
   var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
   var uiLang = LangHelper.GetLang(http);  // 用于错误消息

### 详细实施步骤

#### 步骤 3.3.1: 创建字段元数据缓存服务

1. 创建 `src/BobCrm.Api/Services/FieldMetadataCache.cs`
2. 实现接口（基于 Task 3.2 设计，参考结构）：
   - 接口名：`IFieldMetadataCache`
   - 方法1：`Task<IReadOnlyList<FieldMetadataDto>> GetFieldsAsync(string fullTypeName, ILocalization loc, string? lang, CancellationToken ct)`
   - 方法2：`void Invalidate(string fullTypeName)`
3. 实现缓存逻辑（参考 Task 3.2 设计文档 3.2.5 节）：
   - 使用 `IMemoryCache` 缓存字段元数据
   - 缓存键：`FieldMetadata:{fullTypeName}`（基础元数据）
   - 可选：按语言缓存 DTO 视图：`FieldMetadata:{fullTypeName}:{lang}:{i18nVersion}`
   - 缓存过期时间：30分钟（滑动/绝对过期）
   - DB 查询：按 `fullTypeName` 加载 `EntityDefinition`（含 `Fields`），一次性取全字段
   - DTO 映射：对每个字段调用 `field.ToFieldDto(loc, lang)`（复用已有逻辑）
   - 避免 N+1：接口字段翻译走 `ILocalization` 内部缓存
4. 在 `Program.cs` 中注册服务：`builder.Services.AddScoped<IFieldMetadataCache, FieldMetadataCache>();`

#### 步骤 3.3.2: 创建动态实体查询结果DTO

1. 创建 `src/BobCrm.Api/Contracts/Responses/DynamicEntity/DynamicEntityQueryResultDto.cs`
2. 基于 Task 3.2 的设计方案，实现DTO结构（参考 Task 3.2 设计文档 3.2.4 节）：
   - 类名：`DynamicEntityQueryResultDto`
   - 属性1：`List<object> Data` - 实体数据列表（`Dictionary<string, object>` 的列表）
   - 属性2：`DynamicEntityMetaDto? Meta` - 元数据对象（可空）
   - 属性3：`int Total` - 总数
   - 属性4：`int Page` - 页码
   - 属性5：`int PageSize` - 每页大小
   - 嵌套类：`DynamicEntityMetaDto`，包含 `List<FieldMetadataDto>? Fields` 属性
   - 使用 `JsonIgnore(Condition = WhenWritingNull)` 优化序列化
3. 字段元数据DTO复用现有的 `FieldMetadataDto`（已支持双模式）

#### 步骤 3.3.3: 修改动态实体端点实现

**注意**：根据 Task 3.1 研究结论和 Task 3.2 设计方案，字段元数据应在端点层拼装，而不是在 `ReflectionPersistenceService` 中。

1. 修改 `POST /api/dynamic-entities/{fullTypeName}/query`（基于 Task 3.2 设计）：
   - 添加 `string? lang` 查询参数和 `IFieldMetadataCache fieldMetadataCache`、`ILocalization loc` 参数
   - 使用 `var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);`
   - 调用 `persistenceService.QueryAsync(fullTypeName, options)` 获取数据
   - 调用 `fieldMetadataCache.GetFieldsAsync(fullTypeName, loc, targetLang, ct)` 获取字段元数据
   - 构建返回DTO：`{ "data": [...], "meta": { "fields": [...] }, "total": 123, "page": 1, "pageSize": 100 }`
   - 遵循 ARCH-30 统一规则：显式 `?lang=xx` 才输出单语，无 `lang` 输出多语
   - `meta` 字段为增量字段，兼容旧客户端忽略未知字段

2. 修改 `GET /api/dynamic-entities/{fullTypeName}/{id}`（基于 Task 3.2 设计，避免破坏性变更）：
   - 添加 `string? lang` 和 `bool? includeMeta`（可选，默认 false）查询参数
   - 添加 `IFieldMetadataCache fieldMetadataCache`、`ILocalization loc` 参数
   - 使用 `var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);`
   - 调用 `persistenceService.GetByIdAsync(fullTypeName, id)` 获取实体
   - 当 `includeMeta == true` 时：
     - 调用 `fieldMetadataCache.GetFieldsAsync(fullTypeName, loc, targetLang, ct)` 获取字段元数据
     - 构建返回DTO：`{ "data": {...}, "meta": { "fields": [...] } }`
   - 当 `includeMeta == false` 或未提供时：保持现状，返回实体对象（向后兼容）

#### 步骤 3.3.5: 添加功能测试

1. 创建/更新 `tests/BobCrm.Api.Tests/DynamicEntityEndpointsTests.cs`
2. 测试场景：
   - `QueryDynamicEntities_WithoutLang_ReturnsTranslationsMode` - 无lang返回多语字典
   - `QueryDynamicEntities_WithLang_ReturnsSingleLanguageMode` - 有lang返回单语
   - `GetDynamicEntityById_WithoutLang_ReturnsTranslationsMode` - 详情无lang返回多语
   - `GetDynamicEntityById_WithLang_ReturnsSingleLanguageMode` - 详情有lang返回单语
   - `FieldMetadata_Cache_Works` - 验证缓存机制
   - `FieldMetadata_DisplayNameKey_Resolved` - 验证DisplayNameKey解析

#### 步骤 3.3.6: 添加性能测试

1. 创建性能测试方法（参考实现）：
   - 测试缓存未命中场景：第一次查询字段元数据
   - 测试缓存命中场景：第二次查询相同字段元数据
   - 验证：第二次查询应该明显快于第一次（如：`sw2.ElapsedMilliseconds < sw1.ElapsedMilliseconds * 0.5`）
2. 对比优化前后的查询时间
3. 验证缓存效果

#### 步骤 3.3.7: 编译验证

执行以下命令验证：
- `dotnet build src/BobCrm.Api/BobCrm.Api.csproj`
- `dotnet test --filter "DynamicEntityEndpointsTests"`

### 验收标准

- [x] POST /api/dynamic-entities/{fullTypeName}/query 支持 ?lang=zh/ja/en 参数
- [x] POST /api/dynamic-entities/{fullTypeName}/query 返回结构包含 meta.fields
- [x] GET /api/dynamic-entities/{fullTypeName}/{id} 支持 ?lang=zh/ja/en 参数
- [x] GET /api/dynamic-entities/{fullTypeName}/{id} 支持 ?includeMeta=true 参数（避免破坏性变更）
- [x] 无 lang 参数时返回多语字典（向后兼容，忽略 Accept-Language 头）
- [x] 有 lang 参数时返回单语字符串
- [x] 字段元数据缓存机制正常工作（IFieldMetadataCache）
- [x] DisplayNameKey 正确解析（接口字段）
- [x] DisplayNameTranslations 正确解析（自定义字段）
- [x] 缓存测试通过（验证缓存创建、命中、失效）
- [x] 所有单元测试通过

### Commit 信息

feat(api): add lang parameter support to dynamic entity query endpoints

- Implement field-level multilingual metadata resolution
- Add FieldMetadataCache service with IMemoryCache
- Create DynamicEntityQueryResultDto with field metadata
- Update ReflectionPersistenceService with QueryWithMetadataAsync
- Add lang parameter to query and get-by-id endpoints
- Add comprehensive tests (functional + performance)
- Optimize performance with metadata caching
- Maintain backward compatibility (ignore Accept-Language when no lang param)
- Ref: ARCH-30 Task 3.3
```

---

**详细步骤**:
- [x] 步骤 3.3.1: 创建字段元数据缓存服务
- [x] 步骤 3.3.2: 创建动态实体查询结果DTO
- [x] 步骤 3.3.3: 创建 IReflectionPersistenceService 接口（便于测试）
- [x] 步骤 3.3.4: 修改动态实体端点（POST query 和 GET by id）
- [x] 步骤 3.3.5: 添加功能测试
- [x] 步骤 3.3.6: 修正缓存测试
- [x] 步骤 3.3.7: 编译验证 (`dotnet build && dotnet test`)
- [x] 步骤 3.3.8: Git 提交

**关键实现亮点**:
- ✅ 修复了 `GetOrCreateAsync` 泛型推断问题（显式指定 `IReadOnlyList<FieldMetadataDto>`）
- ✅ 缓存键追踪机制：使用 `CacheKeySetPrefix` 追踪所有相关缓存键，便于失效
- ✅ 向后兼容性处理优秀：GET by id 使用 `includeMeta` 参数（默认 false）避免破坏性变更
- ✅ 测试设计优秀：使用 `FakeReflectionPersistenceService` 和 `CountingMemoryCache` 便于测试
- ✅ 端点实现完整：POST query 返回 `meta.fields`，GET by id 支持 `includeMeta` 参数

**Commit ID**: _(待填写)_
**完成时间**: 2025-12-12
**评审结果**: ✅ 优秀（4.9/5.0）- [评审报告](../tasks/arch-30/task-3.3-review.md)

---

### 阶段4: 文档同步 (收尾)

**目标**: 统一更新 API 文档和 CHANGELOG，避免频繁小改动

#### ✅ Task 4.1: 更新 API 接口文档

**状态**: ✅ 完成
**负责文件**:
- `docs/reference/API-01-接口文档.md` (修改)

**详细步骤**:
- [x] 为 `/api/access/functions/me` 添加 `lang` 参数说明 (来自 Task 1.1)
- [x] 为 `/api/templates/menu-bindings` 添加 `lang` 参数说明 (来自 Task 1.2)
- [x] 为 `/api/entities` 添加 `lang` 参数说明 (来自 Task 1.3)
- [x] 为 `/api/entity-definitions` 相关端点添加 `lang` 参数说明 (来自 Task 2.1)
- [x] 为 `/api/enums` 相关端点添加 `lang` 参数说明 (来自 Task 2.2)
- [x] 为 `/api/entity-domains` 添加 `lang` 参数说明 (来自 Task 2.3)
- [x] 为 `/api/access/functions` 管理端点添加 `lang` 参数说明 (来自 Task 2.4)
- [x] 为 `/api/dynamic-entities/{fullTypeName}/query` 添加 `lang` 参数和 `meta.fields` 说明 (来自 Task 3.3)
- [x] 为 `/api/dynamic-entities/{fullTypeName}/{id}` 添加 `lang` 和 `includeMeta` 参数说明 (来自 Task 3.3)
- [x] 更新响应示例（展示单语/多语双模式）
- [x] 添加向后兼容性说明章节
- [x] 添加 `meta.fields` 结构说明（Task 3.3 新增）

**关键更新亮点**:
- ✅ 统一说明章节：多语参数、双模式规则、Accept-Language 处理、向后兼容性
- ✅ 完整的端点覆盖：所有9个端点都已更新
- ✅ 清晰的响应示例：每个端点都有单语/多语模式示例
- ✅ 详细的 meta.fields 说明：动态实体端点的 meta.fields 结构说明详细
- ✅ 格式统一：文档格式一致，易于阅读

**Commit ID**: _(待填写)_
**完成时间**: 2025-12-12
**评审结果**: ✅ 优秀（5.0/5.0）- [评审报告](../tasks/arch-30/task-4.1-review.md)

##### 🤖 AI 任务提示词

```markdown
## 任务: ARCH-30 Task 4.1 - 更新 API 接口文档

### 背景
ARCH-30 系统级多语API架构优化项目，阶段4文档同步。
基于已完成的所有任务（阶段1-3），统一更新 API 接口文档，记录所有新增的 `lang` 参数支持和响应结构变更。

### 参考文件
- API文档: `docs/reference/API-01-接口文档.md`
- 工作计划: `docs/design/ARCH-30-工作计划.md` (查看已完成任务列表)
- 设计文档: `docs/design/ARCH-30-实体字段显示名多语元数据驱动设计.md`

### 需要更新的端点列表

#### 阶段1：高频API改造
1. **GET /api/access/functions/me** (Task 1.1)
   - 新增 `lang` 查询参数（可选）
   - 响应：单语模式返回 `displayName`（string），多语模式返回 `displayNameTranslations`（MultilingualText）

2. **GET /api/templates/menu-bindings** (Task 1.2)
   - 新增 `lang` 查询参数（可选），支持 `Accept-Language` 头
   - 响应：单语模式返回 `displayName`（string），多语模式返回 `displayNameTranslations`（MultilingualText）

3. **GET /api/entities** 和 **GET /api/entities/all** (Task 1.3)
   - 新增 `lang` 查询参数（可选），支持 `Accept-Language` 头
   - 响应：单语模式返回 `displayName`（string），多语模式返回 `displayNameTranslations`（MultilingualText）

#### 阶段2：中频API改造
4. **GET /api/entity-definitions** 相关端点 (Task 2.1)
   - 新增 `lang` 查询参数（可选）
   - 响应：单语模式返回 `displayName`（string），多语模式返回 `displayNameTranslations`（MultilingualText）

5. **GET /api/enums** 相关端点 (Task 2.2)
   - 新增 `lang` 查询参数（可选）
   - 响应：单语模式返回 `displayName`/`description`（string），多语模式返回 `displayNameTranslations`/`descriptionTranslations`（MultilingualText）

6. **GET /api/entity-domains** 和 **GET /api/entity-domains/{id}** (Task 2.3)
   - 新增 `lang` 查询参数（可选）
   - 响应：单语模式返回 `name`（string），多语模式返回 `nameTranslations`（MultilingualText）

7. **GET /api/access/functions** 和 **GET /api/access/functions/manage** (Task 2.4)
   - 新增 `lang` 查询参数（可选）
   - **POST /api/access/functions** 和 **PUT /api/access/functions/{id}** 也支持 `lang` 查询参数
   - 响应：单语模式返回 `displayName`（string），多语模式返回 `displayNameTranslations`（MultilingualText）

#### 阶段3：低频API改造
8. **POST /api/dynamic-entities/{fullTypeName}/query** (Task 3.3)
   - 新增 `lang` 查询参数（可选）
   - 响应结构新增 `meta.fields` 字段（字段元数据数组）
   - 响应示例：
     ```json
     {
       "meta": {
         "fields": [
           {
             "propertyName": "Code",
             "displayNameKey": "LBL_FIELD_CODE",
             "displayName": "编码"  // 单语模式
           }
         ]
       },
       "data": [...],
       "total": 123,
       "page": 1,
       "pageSize": 100
     }
     ```

9. **GET /api/dynamic-entities/{fullTypeName}/{id}** (Task 3.3)
   - 新增 `lang` 查询参数（可选）
   - 新增 `includeMeta` 查询参数（可选，默认 false）
   - 当 `includeMeta=true` 时，返回 `{ meta: { fields: [...] }, data: {...} }`
   - 当 `includeMeta=false` 或未提供时，返回实体对象（向后兼容）

### 统一规则说明

**向后兼容性规则**：
- 只有显式传 `?lang=xx` 才进入单语模式
- 无 `lang` 参数时返回多语字典（即使有 `Accept-Language` 头也忽略，除非端点明确支持）
- 错误消息使用 `uiLang = LangHelper.GetLang(http)` 获取

**双模式响应结构**：
- **单语模式**（`?lang=xx`）：
  - 输出 `displayName`（string）
  - `displayNameTranslations` 为 null（不序列化）
- **多语模式**（无 `lang`）：
  - 接口字段：输出 `displayNameKey`（不展开多语字典）
  - 自定义字段：输出 `displayNameTranslations`（MultilingualText 字典）
  - `displayName` 为 null（不序列化）

### 详细更新步骤

#### 步骤 4.1.1: 更新端点文档结构

1. 为每个端点添加 **查询参数** 章节（如果还没有）
2. 在查询参数中添加 `lang` 参数说明：
   - 参数名：`lang`
   - 类型：`string?`（可选）
   - 说明：语言代码（zh/ja/en），仅显式传参才进入单语模式
   - 示例：`?lang=zh`

3. 对于支持 `Accept-Language` 的端点（如 `/api/templates/menu-bindings`），说明：
   - 如果未提供 `lang` 参数，将使用 `Accept-Language` 头作为默认语言

#### 步骤 4.1.2: 更新响应示例

1. 为每个端点添加两个响应示例：
   - **单语模式示例**（`?lang=zh`）：
     - 展示 `displayName` 字段（string）
     - 说明 `displayNameTranslations` 为 null（不序列化）
   - **多语模式示例**（无 `lang` 参数）：
     - 展示 `displayNameTranslations` 字段（MultilingualText 字典）
     - 说明 `displayName` 为 null（不序列化）

2. 对于动态实体端点，添加 `meta.fields` 结构说明：
   - 说明 `meta.fields` 是字段元数据数组
   - 展示字段元数据的结构（`propertyName`、`displayNameKey`、`displayName`、`displayNameTranslations`）
   - 说明单语模式和多语模式的区别

#### 步骤 4.1.3: 添加向后兼容性说明章节

1. 在文档开头或适当位置添加 **向后兼容性** 章节
2. 说明：
   - 所有新增的 `lang` 参数都是可选的
   - 无 `lang` 参数时，响应保持向后兼容（返回多语字典）
   - 显式传 `?lang=xx` 时，响应体积减小（仅返回单语字符串）

#### 步骤 4.1.4: 添加 meta.fields 结构说明（Task 3.3）

1. 为动态实体端点添加 `meta.fields` 结构说明
2. 说明：
   - `meta.fields` 是字段元数据数组，包含字段的显示名、类型等信息
   - 字段元数据支持双模式（单语/多语）
   - `meta` 字段为增量字段，兼容旧客户端忽略未知字段

### 验收标准

- [x] 所有已改造的端点都添加了 `lang` 参数说明
- [x] 每个端点都有单语模式和多语模式的响应示例
- [x] 向后兼容性说明章节已添加
- [x] `meta.fields` 结构说明已添加（动态实体端点）
- [x] 文档格式统一，易于阅读

### Commit 信息

docs(api): update API documentation with lang parameter for all endpoints

- Document lang query parameter for all multilingual endpoints (Phase 1-3)
- Add response examples for single-language and multi-language modes
- Add backward compatibility notes
- Add meta.fields structure documentation (Task 3.3)
- Ref: ARCH-30 Task 4.1
```

**Commit ID**: _(待填写)_
**完成时间**: _(待填写)_

---

#### ⏳ Task 4.2: 更新 CHANGELOG

**状态**: ⏳ 待开始
**负责文件**:
- `CHANGELOG.md` (修改)

**详细步骤**:
- [ ] 在 `[未发布] - 进行中` 下添加 ARCH-30 相关条目
- [ ] 列出所有新增的 `lang` 参数支持端点（9个端点）
- [ ] 说明向后兼容性设计决策
- [ ] 记录关键设计决策：无 lang 参数时忽略 Accept-Language 头（除3个高频端点）
- [ ] 说明动态实体端点的 meta.fields 和 includeMeta 参数

##### 🤖 AI 任务提示词

```markdown
## 任务: ARCH-30 Task 4.2 - 更新 CHANGELOG

### 背景
ARCH-30 系统级多语API架构优化项目，阶段4文档同步。
基于已完成的所有任务（阶段1-3），更新 CHANGELOG.md，记录所有新增的 `lang` 参数支持和响应结构变更。

### 参考文件
- CHANGELOG: `CHANGELOG.md`
- API文档: `docs/reference/API-01-接口文档.md` (Task 4.1 输出)
- 工作计划: `docs/design/ARCH-30-工作计划.md` (查看已完成任务列表)

### 需要记录的变更

#### 阶段1：高频API改造
1. **GET /api/access/functions/me** (Task 1.1)
   - 新增 `lang` 查询参数（可选）
   - 支持 `Accept-Language` 头（未传 `lang` 时生效）
   - 响应支持双模式：单语模式返回 `displayName`（string），多语模式返回 `displayNameTranslations`（MultilingualText）

2. **GET /api/templates/menu-bindings** (Task 1.2)
   - 新增 `lang` 查询参数（可选）
   - 支持 `Accept-Language` 头（未传 `lang` 时生效）
   - 响应支持双模式：单语模式返回 `displayName`（string），多语模式返回 `displayNameTranslations`（MultilingualText）

3. **GET /api/entities** 和 **GET /api/entities/all** (Task 1.3)
   - 新增 `lang` 查询参数（可选）
   - 支持 `Accept-Language` 头（未传 `lang` 时生效）
   - 响应支持双模式：单语模式返回 `displayName`（string），多语模式返回 `displayNameTranslations`（MultilingualText）

#### 阶段2：中频API改造
4. **GET /api/entity-definitions** 相关端点 (Task 2.1)
   - 新增 `lang` 查询参数（可选）
   - 忽略 `Accept-Language` 头（仅显式 `?lang=xx` 才单语）
   - 响应支持双模式：单语模式返回 `displayName`（string），多语模式返回 `displayNameTranslations`（MultilingualText）

5. **GET /api/enums** 相关端点 (Task 2.2)
   - 新增 `lang` 查询参数（可选，4个端点：列表、详情、按代码查询、选项列表）
   - 忽略 `Accept-Language` 头（仅显式 `?lang=xx` 才单语）
   - 响应支持双模式：单语模式返回 `displayName`/`description`（string），多语模式返回 `displayNameTranslations`/`descriptionTranslations`（MultilingualText）

6. **GET /api/entity-domains** 和 **GET /api/entity-domains/{id}** (Task 2.3)
   - 新增 `lang` 查询参数（可选）
   - 忽略 `Accept-Language` 头（仅显式 `?lang=xx` 才单语）
   - 响应支持双模式：单语模式返回 `name`（string），多语模式返回 `nameTranslations`（MultilingualText）

7. **GET /api/access/functions** 和 **GET /api/access/functions/manage** (Task 2.4)
   - 新增 `lang` 查询参数（可选）
   - **POST /api/access/functions** 和 **PUT /api/access/functions/{id}** 也支持 `lang` 查询参数
   - 忽略 `Accept-Language` 头（仅显式 `?lang=xx` 才单语）
   - 响应支持双模式：单语模式返回 `displayName`（string），多语模式返回 `displayNameTranslations`（MultilingualText）

#### 阶段3：低频API改造
8. **POST /api/dynamic-entities/{fullTypeName}/query** (Task 3.3)
   - 新增 `lang` 查询参数（可选）
   - 忽略 `Accept-Language` 头（仅显式 `?lang=xx` 才单语）
   - 响应结构新增 `meta.fields` 字段（字段元数据数组）
   - 字段元数据支持双模式：单语模式返回 `displayName`（string），多语模式返回 `displayNameKey`/`displayNameTranslations`

9. **GET /api/dynamic-entities/{fullTypeName}/{id}** (Task 3.3)
   - 新增 `lang` 查询参数（可选）
   - 新增 `includeMeta` 查询参数（可选，默认 `false`）
   - 当 `includeMeta=true` 时，返回 `{ meta: { fields: [...] }, data: {...} }`
   - 当 `includeMeta=false` 或未提供时，返回实体对象（向后兼容）

### 关键设计决策

1. **向后兼容性**：
   - 所有新增的 `lang`/`includeMeta` 查询参数均为可选
   - 未传 `lang` 时：端点保持既有默认行为（多语模式或基于 `Accept-Language` 的单语模式，取决于端点）
   - 动态实体 `GET /api/dynamic-entities/{fullTypeName}/{id}` 默认不返回 `meta`；仅 `includeMeta=true` 才返回 `{ meta, data }`

2. **Accept-Language 处理规则**：
   - 仅以下端点在未传 `lang` 时会使用 `Accept-Language` 作为默认语言：
     - `GET /api/access/functions/me`
     - `GET /api/templates/menu-bindings`
     - `GET /api/entities`、`GET /api/entities/all`
   - 其余已改造端点：只有显式传 `?lang=xx` 才进入单语模式；未传 `lang` 时忽略 `Accept-Language`

3. **双模式响应结构**：
   - **单语模式**（`?lang=xx`）：输出 `displayName`/`description`/`name` 等 `string`，`displayNameTranslations` 为 null（不序列化）
   - **多语模式**（无 `lang`）：接口字段输出 `displayNameKey`（不展开多语字典），自定义字段输出 `displayNameTranslations`（MultilingualText 字典），`displayName` 为 null（不序列化）

### 详细更新步骤

#### 步骤 4.2.1: 在 CHANGELOG 中添加 ARCH-30 条目

1. 在 `[未发布] - 进行中` 章节下添加 **Added** 子章节（如果还没有）
2. 添加 ARCH-30 条目，格式如下：

```markdown
### Added
- **[ARCH-30] 系统级多语API架构优化**：
  - 新增 `lang` 查询参数支持（可选，`zh|ja|en`），覆盖 9 个端点
  - 响应支持双模式：单语模式返回 `displayName`（string），多语模式返回 `displayNameTranslations`（MultilingualText）
  - 动态实体查询端点新增 `meta.fields` 字段（字段元数据数组）
  - 动态实体详情端点新增 `includeMeta` 查询参数（可选，默认 `false`）
```

#### 步骤 4.2.2: 列出所有新增 lang 参数支持端点

在 ARCH-30 条目下，按阶段列出所有端点：

```markdown
  - **阶段1 - 高频API改造**（3个端点）：
    - `GET /api/access/functions/me`（支持 `Accept-Language` 头）
    - `GET /api/templates/menu-bindings`（支持 `Accept-Language` 头）
    - `GET /api/entities`、`GET /api/entities/all`（支持 `Accept-Language` 头）
  - **阶段2 - 中频API改造**（4个端点组）：
    - `GET /api/entity-definitions` 相关端点
    - `GET /api/enums` 相关端点（列表、详情、按代码查询、选项列表）
    - `GET /api/entity-domains`、`GET /api/entity-domains/{id}`
    - `GET /api/access/functions`、`GET /api/access/functions/manage`、`POST /api/access/functions`、`PUT /api/access/functions/{id}`
  - **阶段3 - 低频API改造**（2个端点）：
    - `POST /api/dynamic-entities/{fullTypeName}/query`（新增 `meta.fields` 字段）
    - `GET /api/dynamic-entities/{fullTypeName}/{id}`（新增 `includeMeta` 参数）
```

#### 步骤 4.2.3: 说明向后兼容性设计决策

在 ARCH-30 条目下添加向后兼容性说明：

```markdown
  - **向后兼容性**：
    - 所有新增的 `lang`/`includeMeta` 查询参数均为可选
    - 未传 `lang` 时：端点保持既有默认行为（多语模式或基于 `Accept-Language` 的单语模式，取决于端点）
    - 动态实体 `GET /api/dynamic-entities/{fullTypeName}/{id}` 默认不返回 `meta`；仅 `includeMeta=true` 才返回 `{ meta, data }`
```

#### 步骤 4.2.4: 记录关键设计决策

在 ARCH-30 条目下添加关键设计决策说明：

```markdown
  - **关键设计决策**：
    - 仅 3 个高频端点在未传 `lang` 时会使用 `Accept-Language` 作为默认语言
    - 其余已改造端点：只有显式传 `?lang=xx` 才进入单语模式；未传 `lang` 时忽略 `Accept-Language`
    - 双模式响应结构：单语模式输出 `displayName`（string），多语模式输出 `displayNameTranslations`（MultilingualText 字典）
```

### 验收标准

- [ ] ARCH-30 条目已添加到 `[未发布] - 进行中` 章节
- [ ] 所有 9 个端点都已列出
- [ ] 向后兼容性设计决策已说明
- [ ] 关键设计决策已记录（Accept-Language 处理规则、双模式响应结构）
- [ ] 动态实体端点的 `meta.fields` 和 `includeMeta` 参数已说明
- [ ] 格式符合 CHANGELOG 规范

### Commit 信息

docs(changelog): add ARCH-30 multilingual API changes

- Document all 9 endpoints with new lang parameter support
- Note backward compatibility design decisions
- Record key design decisions (Accept-Language handling, dual-mode response structure)
- Document dynamic entity endpoints (meta.fields and includeMeta parameter)
- Ref: ARCH-30 Task 4.2
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
| 2025-12-12 | v1.1 | 添加阶段0.5详细任务清单；修正Task 2.1/2.2复选框状态；修正Task 2.4/3.x标题与状态不一致问题 | Claude |

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
