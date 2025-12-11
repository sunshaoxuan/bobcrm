# BobCRM 代码规范

**版本**: v1.0  
**发布日期**: 2025-12-11  
**适用范围**: 所有 C# 后端代码  
**维护者**: 架构组

---

## 📋 目录

1. [单一类型原则](#单一类型原则)
2. [目录结构约定](#目录结构约定)
3. [命名规范](#命名规范)
4. [DTO 设计规范](#dto-设计规范)
5. [代码审查检查清单](#代码审查检查清单)
6. [技术债治理](#技术债治理)

---

## 单一类型原则

### 规则

**核心原则**: 每个 `.cs` 文件应该只包含**一个公共类型**（class/record/struct/interface/enum）。

### 例外

以下情况允许多个类型在同一文件：

1. **私有辅助类型**
   ```csharp
   // FunctionTreeBuilder.cs
   public class FunctionTreeBuilder
   {
       // ✅ 允许：私有内部类
       private class TreeNode { }
   }
   ```

2. **文件作用域类型** (C# 11+)
   ```csharp
   // FileHelper.cs
   public class FileHelper { }
   
   // ✅ 允许：文件作用域类型
   file class InternalCache { }
   ```

3. **紧密相关的泛型特化**
   ```csharp
   // SuccessResponse.cs
   public class SuccessResponse<T> { }
   
   // ✅ 允许：非泛型便捷版本
   public class SuccessResponse : SuccessResponse<object> { }
   ```

### 示例

❌ **错误示例**:

```csharp
// EnumDefinitionDto.cs (7个公共类型 - 违规)
namespace BobCrm.Api.Contracts.DTOs;

public class EnumDefinitionDto { }          // ❌ 应独立
public class EnumOptionDto { }              // ❌ 应独立
public class CreateEnumDefinitionRequest { } // ❌ 应独立
public class CreateEnumOptionRequest { }     // ❌ 应独立
public class UpdateEnumDefinitionRequest { } // ❌ 应独立
public class UpdateEnumOptionsRequest { }    // ❌ 应独立
public class UpdateEnumOptionRequest { }     // ❌ 应独立
```

✅ **正确示例**:

```
Contracts/
├── DTOs/
│   ├── EnumDefinitionDto.cs (1个类型)
│   └── EnumOptionDto.cs (1个类型)
└── Requests/
    └── Enum/
        ├── CreateEnumDefinitionRequest.cs (1个类型)
        ├── UpdateEnumDefinitionRequest.cs (1个类型)
        ├── CreateEnumOptionRequest.cs (1个类型)
        ├── UpdateEnumOptionsRequest.cs (1个类型)
        └── UpdateEnumOptionRequest.cs (1个类型)
```

### 原因

1. **单一职责原则** (SRP) - 每个文件只负责一个类型
2. **代码导航** - 文件名即类型名，易于查找
3. **版本控制** - 减少合并冲突
4. **可维护性** - 修改一个类型不影响其他类型
5. **IDE 支持** - 更好的重构、跳转、搜索体验

---

## 目录结构约定

### DTOs 目录

**路径**: `Contracts/DTOs/`  
**用途**: 数据传输对象（Data Transfer Objects）

**命名**: `{EntityName}Dto.cs`

```
Contracts/DTOs/
├── EnumDefinitionDto.cs
├── EnumOptionDto.cs
├── EntitySummaryDto.cs
├── FieldMetadataDto.cs
└── FunctionNodeDto.cs
```

---

### Requests 目录

**路径**: `Contracts/Requests/{Domain}/`  
**用途**: API 请求对象（按领域组织）

**命名**: 
- `Create{EntityName}Request.cs`
- `Update{EntityName}Request.cs`
- `Delete{EntityName}Request.cs`
- `{Action}{EntityName}Request.cs`

**示例**:

```
Contracts/Requests/
├── Enum/
│   ├── CreateEnumDefinitionRequest.cs
│   ├── UpdateEnumDefinitionRequest.cs
│   ├── CreateEnumOptionRequest.cs
│   ├── UpdateEnumOptionRequest.cs
│   └── UpdateEnumOptionsRequest.cs
├── Entity/
│   ├── CreateEntityDefinitionRequest.cs
│   ├── UpdateEntityDefinitionRequest.cs
│   └── DeleteEntityDefinitionRequest.cs
└── Access/
    ├── CreateRoleRequest.cs
    ├── UpdateRoleRequest.cs
    ├── AssignRolesToUserRequest.cs
    └── UpdateRolePermissionsRequest.cs
```

---

### Responses 目录

**路径**: `Contracts/Responses/{Domain}/`  
**用途**: API 响应对象（复杂响应）

**命名**: `{Action}{EntityName}Response.cs`

```
Contracts/Responses/
├── Entity/
│   ├── EntitySummaryDto.cs
│   ├── FieldMetadataDto.cs
│   └── CompileResultDto.cs
└── Common/
    ├── SuccessResponse.cs
    └── ErrorResponse.cs
```

---

### 完整目录结构

```
src/BobCrm.Api/
└── Contracts/
    ├── DTOs/                    # 基础 DTO（按类型）
    │   ├── EnumDefinitionDto.cs
    │   ├── EnumOptionDto.cs
    │   ├── FunctionNodeDto.cs
    │   └── ...
    ├── Requests/                # 请求对象（按领域）
    │   ├── Enum/
    │   ├── Entity/
    │   ├── Access/
    │   └── ...
    └── Responses/               # 响应对象（按领域）
        ├── Entity/
        ├── Access/
        └── Common/
```

---

## 命名规范

### 文件命名

| 类型 | 命名格式 | 示例 |
|------|---------|------|
| DTO | `{EntityName}Dto.cs` | `EnumDefinitionDto.cs` |
| Request | `{Action}{EntityName}Request.cs` | `CreateEnumDefinitionRequest.cs` |
| Response | `{Action}{EntityName}Response.cs` | `GetEnumDefinitionResponse.cs` |
| Service | `{EntityName}Service.cs` | `EnumDefinitionService.cs` |
| Endpoint | `{EntityName}Endpoints.cs` | `EnumDefinitionEndpoints.cs` |

---

### 类型命名

| 类型 | 命名格式 | 示例 |
|------|---------|------|
| 类 | PascalCase | `EnumDefinitionService` |
| 接口 | IPascalCase | `ILocalization` |
| Record | PascalCase | `FunctionNodeDto` |
| 枚举 | PascalCase | `EntityStatus` |
| 常量 | PascalCase | `DefaultPageSize` |

---

### 成员命名

| 类型 | 命名格式 | 示例 |
|------|---------|------|
| 公共属性 | PascalCase | `DisplayName` |
| 私有字段 | _camelCase | `_dbContext` |
| 方法 | PascalCase | `GetEnumDefinitionAsync` |
| 参数 | camelCase | `enumId`, `lang` |
| 局部变量 | camelCase | `entity`, `dto` |

---

## DTO 设计规范

### 双模式字段设计（ARCH-30）

**背景**: 支持单语模式（性能优化）和多语模式（向后兼容）

**标准模板**:

```csharp
using System.Text.Json.Serialization;

namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 枚举定义 DTO
/// </summary>
public class EnumDefinitionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// 单语显示名（单语模式返回）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// 单语描述（单语模式返回）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 多语显示名（向后兼容）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DisplayNameTranslations { get; set; }
    
    /// <summary>
    /// 多语描述（向后兼容）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DescriptionTranslations { get; set; }
}
```

---

### 双模式字段规则

1. **单语字段** (`string?`)
   - 命名: `DisplayName`, `Description`
   - 用途: 单语模式（`lang` 参数存在时）
   - 注解: `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`

2. **多语字段** (`MultilingualText?`)
   - 命名: `DisplayNameTranslations`, `DescriptionTranslations`
   - 用途: 多语模式（无 `lang` 参数时）
   - 注解: `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`

3. **互斥性**
   - 单语模式: 设置 `DisplayName`, `DisplayNameTranslations` = null
   - 多语模式: 设置 `DisplayNameTranslations`, `DisplayName` = null

---

### DTO 扩展方法

**文件**: `Extensions/DtoExtensions.cs`

**命名**: `To{DtoName}(this {Entity}, string? lang = null)`

**示例**:

```csharp
public static class DtoExtensions
{
    public static EnumDefinitionDto ToDto(this EnumDefinition entity, string? lang = null)
    {
        var dto = new EnumDefinitionDto
        {
            Id = entity.Id,
            Code = entity.Code,
            // ... 其他字段 ...
        };

        if (lang != null)
        {
            // 单语模式
            dto.DisplayName = entity.DisplayName.Resolve(lang);
            dto.Description = entity.Description.Resolve(lang);
            dto.DisplayNameTranslations = null;
            dto.DescriptionTranslations = null;
        }
        else
        {
            // 多语模式
            dto.DisplayName = null;
            dto.Description = null;
            dto.DisplayNameTranslations = entity.DisplayName != null
                ? new MultilingualText(entity.DisplayName)
                : null;
            dto.DescriptionTranslations = entity.Description != null
                ? new MultilingualText(entity.Description)
                : null;
        }

        return dto;
    }
}
```

---

### 多语辅助方法

**文件**: `Utils/MultilingualHelper.cs`

**方法**: `Resolve(this Dictionary<string, string?>? dict, string lang)`

**示例**:

```csharp
public static class MultilingualHelper
{
    /// <summary>
    /// 从多语字典中解析指定语言的文本，带回退逻辑
    /// </summary>
    public static string Resolve(this Dictionary<string, string?>? dict, string lang)
    {
        if (dict == null || dict.Count == 0)
            return string.Empty;

        var normalizedLang = lang.Trim().ToLowerInvariant();

        // 1. 尝试精确匹配
        if (dict.TryGetValue(normalizedLang, out var value) 
            && !string.IsNullOrWhiteSpace(value))
            return value;

        // 2. 回退到中文
        if (normalizedLang != "zh" 
            && dict.TryGetValue("zh", out var zhValue) 
            && !string.IsNullOrWhiteSpace(zhValue))
            return zhValue;

        // 3. 返回任意非空值
        return dict.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) 
            ?? string.Empty;
    }
}
```

---

## 代码审查检查清单

### 基础检查

- [ ] 每个 `.cs` 文件只包含一个公共类型
- [ ] 文件名与类型名一致（`EnumDefinitionDto.cs` → `EnumDefinitionDto`）
- [ ] 目录结构符合约定（DTOs / Requests / Responses）
- [ ] 命名符合规范（PascalCase / camelCase）

---

### DTO 检查

- [ ] 双模式字段正确实现（`DisplayName` + `DisplayNameTranslations`）
- [ ] `JsonIgnore` 注解正确使用
- [ ] XML 注释完整（`/// <summary>`）
- [ ] 扩展方法正确实现（`ToDto` 方法）

---

### API 检查

- [ ] 端点接受 `lang` 参数（`string? lang`）
- [ ] 使用 `LangHelper.GetLang(http, lang)` 获取语言
- [ ] 调用 `entity.ToDto(lang)` 生成 DTO
- [ ] 向后兼容性验证（无 `lang` 时返回多语）

---

### 测试检查

- [ ] 单语模式测试（`WithLang_ReturnsSingleLanguage`）
- [ ] 多语模式测试（`WithoutLang_ReturnsMultilingual`）
- [ ] `Accept-Language` 头测试
- [ ] 响应体积减少测试（单语 vs 多语）

---

### 编译检查

- [ ] `dotnet build BobCrm.sln` 成功
- [ ] 无编译警告（或已知警告已记录）
- [ ] 前端编译成功（Blazor App）

---

## 技术债治理

### 发现的技术债（ARCH-30 Task 2.2）

**问题**: 16个文件包含多个公共类型（共97个类型）

**优先级分类**:

| 优先级 | 文件 | 类型数 | 修正时间 |
|--------|------|--------|---------|
| 🔴 **高** | AccessDtos.cs | 14 | 阶段2后 |
| 🔴 **高** | DataSetDtos.cs | 12 | 阶段2后 |
| 🔴 **高** | TemplateDtos.cs | 8 | 阶段2后 |
| ⚠️ **中** | UserDtos.cs | 7 | 阶段3后 |
| ⚠️ **中** | EnumDefinitionDto.cs | 7 | Task 2.2 修正 |
| ⚠️ **中** | AuthDtos.cs | 5 | 阶段3后 |
| ⚠️ **中** | SettingsDtos.cs | 5 | 阶段3后 |
| ⚠️ **中** | AdminDtos.cs | 5 | 阶段3后 |
| ⚠️ **中** | CustomerDtos.cs | 4 | 按需 |
| ⚠️ **中** | LayoutDtos.cs | 4 | 按需 |
| ⚠️ **中** | ApiResponse.cs | 4 | 按需 |
| ⏳ **低** | OrganizationDtos.cs | 3 | 按需 |
| ⏳ **低** | (其他4个文件) | 2 | 按需 |

---

### 技术债修正计划

#### 阶段1: 当前任务（立即）

**Task 2.2 修正**:
- 拆分 `EnumDefinitionDto.cs`（7个类型 → 7个文件）
- 建立代码规范文档（本文档）

---

#### 阶段2: Phase 2 完成后（2周内）

**高优先级文件拆分**:
1. `AccessDtos.cs` (14个类型)
2. `DataSetDtos.cs` (12个类型)
3. `TemplateDtos.cs` (8个类型)

**预计工作量**: 4-6小时

---

#### 阶段3: Phase 3 完成后（1个月内）

**中优先级文件拆分**:
4. `UserDtos.cs` (7个类型)
5. `AuthDtos.cs` (5个类型)
6. `SettingsDtos.cs` (5个类型)
7. `AdminDtos.cs` (5个类型)

**预计工作量**: 3-4小时

---

#### 阶段4: 按需处理

**低优先级文件**: 其他8个文件（2-4个类型）

**策略**: 在修改这些文件时顺便拆分

---

### 技术债追踪

**文档位置**: `docs/tasks/arch-30/TECH-DEBT.md`

**追踪内容**:
- [ ] 文件列表
- [ ] 类型数量
- [ ] 优先级
- [ ] 修正计划
- [ ] 修正进度
- [ ] 修正日期

---

## 参考文档

### 相关标准

- [ARCH-30 设计文档](../design/ARCH-30-实体字段显示名多语元数据驱动设计.md)
- [Task 0.3 DTO定义标准](../tasks/arch-30/task-0.3-dto-definitions.md)
- [Task 2.2 代码评审](../tasks/arch-30/task-2.2-review.md)

---

### 外部参考

- [C# 命名规范 (Microsoft)](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [SOLID 原则](https://en.wikipedia.org/wiki/SOLID)
- [Clean Code (Robert C. Martin)](https://www.amazon.com/Clean-Code-Handbook-Software-Craftsmanship/dp/0132350882)

---

## 修订历史

| 版本 | 日期 | 修改内容 | 修改人 |
|------|------|---------|--------|
| v1.0 | 2025-12-11 | 初版发布（Task 2.2 评审发现） | 架构组 |

---

**维护者**: 架构组  
**最后更新**: 2025-12-11  
**文档状态**: ✅ 生效中

