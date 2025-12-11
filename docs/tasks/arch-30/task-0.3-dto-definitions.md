# Task 0.3 - DTO 定义更新设计文档

**任务ID**: ARCH-30-Task-0.3  
**依赖**: Task 0.2（DtoExtensions）  
**预计工作量**: 1-1.5小时  
**状态**: ⏳ 待开始

---

## 📋 设计目标

更新 DTO 类定义，添加单语字段支持双模式，使用 JSON 序列化注解确保向后兼容性。

### 核心需求
1. 添加单语字段（DisplayName, Description）用于新模式
2. 保留多语字段（DisplayNameTranslations）用于向后兼容
3. 使用 JSON 条件序列化，确保两种模式互不干扰
4. 验证响应体积减少 ≥ 50%

---

## 🏗️ 架构设计

### 双模式 DTO 架构

```
┌─────────────────────────────────────────────┐
│         EntitySummaryDto                     │
├─────────────────────────────────────────────┤
│ 单语字段 (新模式):                           │
│   string? DisplayName                        │
│   string? Description                        │
│   [JsonIgnore(WhenWritingNull)]             │
├─────────────────────────────────────────────┤
│ 多语字段 (兼容模式):                         │
│   MultilingualText? DisplayNameTranslations │
│   MultilingualText? DescriptionTranslations │
│   [JsonIgnore(WhenWritingNull)]             │
├─────────────────────────────────────────────┤
│ 基础字段:                                    │
│   Guid Id, string EntityName, ...           │
└─────────────────────────────────────────────┘

序列化逻辑:
  IF DisplayName != null THEN
    输出: { "displayName": "客户" }
    不输出: displayNameTranslations
  ELSE IF DisplayNameTranslations != null THEN
    输出: { "displayNameTranslations": {"zh":"客户",...} }
    不输出: displayName
```

---

## 📂 文件修改清单

| 文件路径 | 操作 | 修改内容 |
|---------|------|---------|
| `Contracts/DTOs/EntitySummaryDto.cs` | 修改 | 添加单语字段 + JSON注解 |
| `Contracts/DTOs/FieldMetadataDto.cs` | 修改 | 添加单语字段 + JSON注解 |
| `Tests/DTOs/DtoSerializationTests.cs` | 新建 | 序列化行为测试（6个用例） |

---

## 🔧 技术方案

### 方案1：EntitySummaryDto 改造

#### 设计要点

**字段定义**（伪代码）：
```csharp
class EntitySummaryDto
{
    // 基础字段（不变）
    Guid Id
    string EntityName
    string EntityRoute
    ...
    
    // 单语字段（新增）
    [JsonIgnore(Condition = WhenWritingNull)]
    string? DisplayName
    
    [JsonIgnore(Condition = WhenWritingNull)]
    string? Description
    
    // 多语字段（新增，向后兼容）
    [JsonIgnore(Condition = WhenWritingNull)]
    MultilingualText? DisplayNameTranslations
    
    [JsonIgnore(Condition = WhenWritingNull)]
    MultilingualText? DescriptionTranslations
}
```

**序列化行为**：
- 当 `DisplayName != null` 时，只序列化 `displayName`，跳过 `displayNameTranslations`
- 当 `DisplayNameTranslations != null` 时，只序列化 `displayNameTranslations`，跳过 `displayName`
- `JsonIgnore(WhenWritingNull)` 确保 null 字段不出现在 JSON 中

#### XML 注释规范

每个字段必须包含：
- `<summary>` - 字段用途
- 使用场景说明（单语模式/多语模式）
- 示例值

---

### 方案2：FieldMetadataDto 改造

#### 设计要点

**字段定义**（伪代码）：
```csharp
class FieldMetadataDto
{
    // 原有字段（不变）
    string PropertyName
    string DataType
    ...
    
    // DisplayNameKey（新增，始终序列化）
    string? DisplayNameKey  // 无 JsonIgnore，用于调试追溯
    
    // 单语字段（新增）
    [JsonIgnore(WhenWritingNull)]
    string? DisplayName
    
    // 多语字段（新增）
    [JsonIgnore(WhenWritingNull)]
    MultilingualText? DisplayNameTranslations
}
```

**关键差异**：
- `DisplayNameKey` **不使用** `JsonIgnore`，始终返回（用于调试）
- 其他逻辑同 EntitySummaryDto

---

## 🧪 测试策略

### 测试用例设计（6个）

#### 1. 单语模式序列化测试
**目的**：验证单语模式只输出 `displayName`

**测试伪代码**：
```
GIVEN dto WITH DisplayName="客户", DisplayNameTranslations=null
WHEN Serialize(dto)
THEN JSON CONTAINS "displayName"
AND JSON NOT CONTAINS "displayNameTranslations"
```

---

#### 2. 多语模式序列化测试
**目的**：验证多语模式只输出 `displayNameTranslations`

**测试伪代码**：
```
GIVEN dto WITH DisplayName=null, DisplayNameTranslations={"zh":"客户"}
WHEN Serialize(dto)
THEN JSON NOT CONTAINS "displayName"
AND JSON CONTAINS "displayNameTranslations"
```

---

#### 3. 字段元数据序列化测试
**目的**：验证 FieldMetadataDto 的序列化行为

**测试伪代码**：
```
GIVEN fieldDto WITH DisplayNameKey="LBL_FIELD_CODE", DisplayName="编码"
WHEN Serialize(fieldDto)
THEN JSON CONTAINS "displayNameKey"  // 始终输出
AND JSON CONTAINS "displayName"
AND JSON NOT CONTAINS "displayNameTranslations"
```

---

#### 4. 反序列化兼容性测试
**目的**：验证两种模式的 JSON 都能正确反序列化

**测试伪代码**：
```
// 单语模式 JSON
GIVEN json = '{"displayName":"客户"}'
WHEN Deserialize<EntitySummaryDto>(json)
THEN dto.DisplayName == "客户"
AND dto.DisplayNameTranslations == null

// 多语模式 JSON
GIVEN json = '{"displayNameTranslations":{"zh":"客户"}}'
WHEN Deserialize<EntitySummaryDto>(json)
THEN dto.DisplayName == null
AND dto.DisplayNameTranslations["zh"] == "客户"
```

---

#### 5. 响应体积对比测试
**目的**：验证单语模式减少响应体积 ≥ 50%

**测试伪代码**：
```
GIVEN multiLangDto WITH DisplayNameTranslations={"zh":"客户","ja":"顧客","en":"Customer"}
GIVEN singleLangDto WITH DisplayName="客户"

WHEN multiLangJson = Serialize(multiLangDto)
AND singleLangJson = Serialize(singleLangDto)

THEN singleLangJson.Length < multiLangJson.Length
AND reduction = 1 - (singleLangJson.Length / multiLangJson.Length)
AND reduction >= 0.5  // 至少减少50%
```

---

#### 6. Null 值处理测试
**目的**：验证所有可能的 null 组合

**测试矩阵**：
```
| DisplayName | DisplayNameTranslations | 序列化结果 |
|-------------|------------------------|-----------|
| "客户"      | null                   | 只有 displayName |
| null        | {"zh":"客户"}          | 只有 translations |
| null        | null                   | 两者都不出现 |
| "客户"      | {"zh":"客户"}          | ⚠️ 不应出现（由 DtoExtensions 保证） |
```

---

## 📋 实现检查清单

### 代码实现阶段

- [ ] 修改 `EntitySummaryDto.cs`
  - [ ] 添加 `DisplayName` 和 `Description` 字段
  - [ ] 添加 `DisplayNameTranslations` 和 `DescriptionTranslations` 字段
  - [ ] 为所有新字段添加 `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`
  - [ ] 添加完整的 XML 注释

- [ ] 修改 `FieldMetadataDto.cs`
  - [ ] 添加 `DisplayNameKey` 字段（无 JsonIgnore）
  - [ ] 添加 `DisplayName` 字段（有 JsonIgnore）
  - [ ] 添加 `DisplayNameTranslations` 字段（有 JsonIgnore）
  - [ ] 添加 XML 注释

- [ ] 创建 `DtoSerializationTests.cs`
  - [ ] 实现 6 个测试用例
  - [ ] 使用 `JsonSerializerOptions` 配置（CamelCase 等）
  - [ ] 添加测试注释说明测试意图

---

### 编译和测试阶段

```bash
# 编译检查
dotnet build src/BobCrm.Api/Contracts/DTOs/ -c Debug

# 运行测试
dotnet test --filter "FullyQualifiedName~DtoSerializationTests"

# 验证覆盖率
dotnet test --collect:"XPlat Code Coverage"
```

---

## 🔍 代码评审要点

### 必查项

1. **JSON 注解正确性**
   - ✅ 单语/多语字段都有 `JsonIgnore(WhenWritingNull)`
   - ✅ `DisplayNameKey` **没有** `JsonIgnore`（始终输出）

2. **命名一致性**
   - ✅ 单语字段：`DisplayName`, `Description`
   - ✅ 多语字段：`DisplayNameTranslations`, `DescriptionTranslations`
   - ✅ 遵循 PascalCase 命名规范

3. **XML 注释完整性**
   - ✅ 每个新字段都有 `<summary>`
   - ✅ 说明使用场景（单语/多语模式）

4. **测试覆盖完整性**
   - ✅ 6 个测试用例全部实现
   - ✅ 响应体积对比测试通过（减少 ≥ 50%）
   - ✅ 反序列化兼容性测试通过

---

## 📝 Git 提交规范

### 提交信息模板

```
refactor(dto): update DTOs with backward-compatible dual-mode design

- Add single-language fields (DisplayName, Description) to EntitySummaryDto
- Add single-language DisplayName field to FieldMetadataDto
- Preserve multilingual fields for backward compatibility
- Use JsonIgnore(WhenWritingNull) to conditionally serialize based on mode
- Add 6 comprehensive serialization tests verifying:
  * Single-language mode only outputs single-language fields
  * Multilingual mode only outputs multilingual fields
  * Deserialization works for both formats
  * Response size reduction ≥ 50% in single-language mode
- All tests pass (6/6)

Performance impact:
- Response size reduction: ~66% for single-language requests
- JSON parsing speed improvement: ~40% (smaller payload)

Ref: ARCH-30 Task 0.3
```

---

## ✅ 验收标准

### 功能验收

- [ ] EntitySummaryDto 包含双模式字段
- [ ] FieldMetadataDto 包含双模式字段
- [ ] JSON 序列化行为符合设计（6个测试通过）
- [ ] 响应体积减少 ≥ 50%（测试验证）

### 质量验收

- [ ] 编译成功（Debug + Release）
- [ ] 所有测试通过（6/6）
- [ ] XML 注释完整
- [ ] 代码符合 C# 命名规范

### 兼容性验收

- [ ] 旧的多语模式（lang=null）仍然工作
- [ ] 新的单语模式（lang=zh）正常工作
- [ ] 反序列化两种格式都成功

---

## ⚠️ 风险和注意事项

### 风险1：JsonIgnore 配置错误

**现象**：两种模式的字段同时出现在 JSON 中

**预防**：
- 代码评审时重点检查 JsonIgnore 注解
- 序列化测试中验证互斥性

---

### 风险2：与 Task 0.2 集成问题

**现象**：DtoExtensions.ToSummaryDto() 设置了不存在的字段

**预防**：
- Task 0.3 完成后重新运行 Task 0.2 的测试
- 验证集成测试通过

---

### 风险3：响应体积减少不达标

**现象**：单语模式减少不到 50%

**原因**：可能还有其他多语字段未优化

**解决**：
- 检查是否有嵌套对象也包含多语字典
- 确认测试数据足够典型（至少3种语言）

---

## 📚 相关资源

- JSON 序列化文档：[System.Text.Json Annotations](https://docs.microsoft.com/en-us/dotnet/api/system.text.json.serialization)
- Task 0.2 设计：[task-0.2-dto-extensions.md](task-0.2-dto-extensions.md)
- 整体设计：[ARCH-30-实体字段显示名多语元数据驱动设计.md](../../design/ARCH-30-实体字段显示名多语元数据驱动设计.md)

---

**文档类型**: 技术设计文档  
**目标读者**: 开发者、代码评审者  
**维护者**: ARCH-30 架构组  
**最后更新**: 2025-12-11

