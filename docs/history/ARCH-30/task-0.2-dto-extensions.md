# Task 0.2 - DTO 扩展方法开发指南

**任务ID**: ARCH-30-Task-0.2  
**依赖**: Task 0.1（MultilingualHelper）  
**预计工作量**: 1.5-2小时  
**状态**: 🚧 进行中

---

## 📋 任务概述

创建 DTO 转换扩展方法，实现实体和字段元数据的单语/多语双模式转换，支持向后兼容。

### 目标
- 实现 `EntityDefinition.ToSummaryDto(lang?)` 扩展方法
- 实现 `FieldMetadata.ToFieldDto(loc, lang?)` 扩展方法
- 支持单语模式（lang 不为 null）和多语模式（lang 为 null）
- 优先使用 DisplayNameKey 并调用本地化服务

### 范围
- 新建 `DtoExtensions.cs` 扩展类
- 新建 `DtoExtensionsTests.cs` 测试类
- 编写 6 个单元测试覆盖所有场景

---

## ✅ 前置条件检查

在开始实现前，执行以下检查：

```bash
# 1. 验证 Task 0.1 已完成
git log --oneline | grep "feat(i18n): add MultilingualHelper"

# 2. 验证 MultilingualHelper 可用
test -f src/BobCrm.Api/Utils/MultilingualHelper.cs && echo "✅ MultilingualHelper 存在"

# 3. 验证测试通过
dotnet test --filter "FullyQualifiedName~MultilingualHelperTests" --no-build

# 4. 检查 Moq 依赖
dotnet list tests/BobCrm.Api.Tests/BobCrm.Api.Tests.csproj package | grep Moq
```

如果 Moq 未安装：
```bash
cd tests/BobCrm.Api.Tests
dotnet add package Moq --version 4.20.70
cd ../..
```

---

## 📂 文件操作清单

### 新建文件（2个）

| 文件路径 | 用途 | 预计行数 |
|---------|------|---------|
| `src/BobCrm.Api/Extensions/DtoExtensions.cs` | DTO 转换扩展方法 | 100-150 |
| `tests/BobCrm.Api.Tests/Extensions/DtoExtensionsTests.cs` | 单元测试 | 150-200 |

### 依赖的现有类型

在实现前，需要了解以下类型的准确结构：

```bash
# 检查实体定义结构
grep -A 20 "class EntityDefinition" src/BobCrm.Api/Base/Models/EntityDefinition.cs

# 检查字段元数据结构
grep -A 30 "class FieldMetadata" src/BobCrm.Api/Base/Models/FieldMetadata.cs

# 检查 DTO 结构
grep -A 15 "class EntitySummaryDto" src/BobCrm.Api/Contracts/DTOs/EntitySummaryDto.cs
grep -A 30 "class FieldMetadataDto" src/BobCrm.Api/Contracts/DTOs/FieldMetadataDto.cs

# 检查本地化服务接口
grep -A 10 "interface ILocalization" src/BobCrm.Api/Services/Localization/ILocalization.cs

# 检查 MultilingualText 定义
find src/BobCrm.Api -name "*.cs" -exec grep -l "class MultilingualText" {} \;
```

**⚠️ 重要**：根据实际类型结构调整实现代码，不要假设字段名和类型。

---

## 🔨 实现步骤

### 步骤1：创建 DtoExtensions.cs

**文件位置**: `src/BobCrm.Api/Extensions/DtoExtensions.cs`

**命名空间和引用**：
```csharp
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Common;
using BobCrm.Api.Services.Localization;
using BobCrm.Api.Utils;  // 使用 MultilingualHelper

namespace BobCrm.Api.Extensions;
```

**类结构**：
```csharp
/// <summary>
/// DTO 转换扩展方法
/// 支持单语和多语双模式，确保向后兼容性
/// </summary>
public static class DtoExtensions
{
    // 实现3个方法：
    // 1. ToSummaryDto - 公开扩展方法
    // 2. ToFieldDto - 公开扩展方法
    // 3. ResolveFieldDisplayName - 私有辅助方法
}
```

---

### 步骤2：实现 ToSummaryDto 方法

#### 方法签名

```csharp
/// <summary>
/// 转换为实体摘要 DTO（支持单语/多语双模式）
/// </summary>
/// <param name="entity">实体定义对象</param>
/// <param name="lang">
/// 目标语言代码（zh/ja/en）。
/// 为 null 时返回完整多语字典（向后兼容模式）
/// </param>
/// <returns>实体摘要 DTO</returns>
public static EntitySummaryDto ToSummaryDto(
    this EntityDefinition entity, 
    string? lang = null)
{
    // 实现内容见下方
}
```

#### 实现要点

**1. 映射基础字段**（根据实际 EntityDefinition 和 EntitySummaryDto 结构）：
```csharp
var dto = new EntitySummaryDto
{
    Id = entity.Id,
    EntityName = entity.EntityName,
    EntityRoute = entity.EntityRoute,
    FullTypeName = entity.FullTypeName,
    TableName = entity.TableName,
    IsEnabled = entity.IsEnabled,
    Status = entity.Status
    // 根据实际结构添加其他字段
};
```

**2. 双模式处理显示名和描述**：
```csharp
if (lang != null)
{
    // 单语模式：只返回指定语言
    dto.DisplayName = entity.DisplayName.Resolve(lang);
    dto.Description = entity.Description?.Resolve(lang);
    
    // 注意：如果当前 DTO 还没有 DisplayNameTranslations 字段
    // （Task 0.3 才会添加），则跳过设置为 null 的代码
}
else
{
    // 多语模式（向后兼容）
    // 根据当前 EntitySummaryDto 的实际结构返回现有的多语字段
    // 可能的实现：
    // - 如果 DisplayName 类型是 MultilingualText: 
    //   dto.DisplayName = new MultilingualText(entity.DisplayName);
    // - 如果 DisplayName 类型是 Dictionary:
    //   dto.DisplayName = entity.DisplayName;
}

return dto;
```

**3. 空值安全**：
- 使用 `?.` 安全导航运算符
- 使用 `??` 空合并运算符
- 确保 `entity.DisplayName` 为 null 时不抛异常

---

### 步骤3：实现 ToFieldDto 方法

#### 方法签名

```csharp
/// <summary>
/// 转换为字段元数据 DTO（支持单语/多语双模式）
/// </summary>
/// <param name="field">字段元数据对象</param>
/// <param name="loc">本地化服务（用于解析 DisplayNameKey）</param>
/// <param name="lang">目标语言代码，为 null 时返回完整多语字典</param>
/// <returns>字段元数据 DTO</returns>
public static FieldMetadataDto ToFieldDto(
    this FieldMetadata field,
    ILocalization loc,
    string? lang = null)
{
    // 实现内容见下方
}
```

#### 实现要点

**1. 映射所有字段元数据**：
```csharp
var dto = new FieldMetadataDto
{
    PropertyName = field.PropertyName,
    DisplayNameKey = field.DisplayNameKey,  // 始终映射，用于调试
    DataType = field.DataType,
    Length = field.Length,
    Precision = field.Precision,
    Scale = field.Scale,
    IsRequired = field.IsRequired,
    IsEntityRef = field.IsEntityRef,
    ReferencedEntityId = field.ReferencedEntityId,
    TableName = field.TableName,
    SortOrder = field.SortOrder,
    DefaultValue = field.DefaultValue,
    ValidationRules = field.ValidationRules,
    Source = field.Source,
    EnumDefinitionId = field.EnumDefinitionId,
    IsMultiSelect = field.IsMultiSelect
    // 根据实际结构添加其他字段
};
```

**2. 显示名解析**：
```csharp
if (lang != null)
{
    // 单语模式：使用三级解析逻辑
    dto.DisplayName = ResolveFieldDisplayName(field, loc, lang);
    // 不设置 DisplayNameTranslations（如果存在的话）
}
else
{
    // 多语模式：根据当前 DTO 结构返回现有字段
    // 根据实际 FieldMetadataDto 的定义调整
}

return dto;
```

---

### 步骤4：实现 ResolveFieldDisplayName 私有方法

#### 方法签名

```csharp
/// <summary>
/// 解析字段显示名（三级优先级）
/// 1. 优先使用 DisplayNameKey（接口字段）
/// 2. 其次使用 DisplayName 字典（扩展字段）
/// 3. 最后回退到字段名
/// </summary>
/// <param name="field">字段元数据</param>
/// <param name="loc">本地化服务</param>
/// <param name="lang">目标语言</param>
/// <returns>解析后的显示名</returns>
private static string ResolveFieldDisplayName(
    FieldMetadata field,
    ILocalization loc,
    string lang)
```

#### 三级解析逻辑

```csharp
{
    // 优先级1: DisplayNameKey（接口字段）
    if (!string.IsNullOrWhiteSpace(field.DisplayNameKey))
    {
        var translated = loc.T(field.DisplayNameKey, lang);
        
        // 如果翻译成功（返回值不等于Key本身），使用翻译结果
        if (!string.Equals(translated, field.DisplayNameKey, StringComparison.Ordinal))
        {
            return translated;
        }
    }

    // 优先级2: DisplayName 字典（扩展字段）
    if (field.DisplayName != null)
    {
        return field.DisplayName.Resolve(lang);  // 使用 MultilingualHelper
    }

    // 优先级3: 字段名（兜底）
    return field.PropertyName;
}
```

---

## 🧪 测试实现指南

### 步骤5：创建测试文件

**文件位置**: `tests/BobCrm.Api.Tests/Extensions/DtoExtensionsTests.cs`

**测试类结构**：
```csharp
using BobCrm.Api.Base.Models;
using BobCrm.Api.Extensions;
using BobCrm.Api.Services.Localization;
using Moq;
using Xunit;

namespace BobCrm.Api.Tests.Extensions;

/// <summary>
/// DtoExtensions 扩展方法测试
/// 验证单语/多语双模式转换和优先级解析逻辑
/// </summary>
public class DtoExtensionsTests
{
    // 6个测试用例
}
```

---

### 必需的6个测试用例

#### 测试1：ToSummaryDto_WithLang_ReturnsSingleLanguage

**目的**: 验证指定语言时只返回单语字段

```csharp
[Fact]
public void ToSummaryDto_WithLang_ReturnsSingleLanguage()
{
    // Arrange
    var entity = new EntityDefinition
    {
        Id = Guid.NewGuid(),
        EntityName = "Customer",
        DisplayName = new Dictionary<string, string?>
        {
            { "zh", "客户" },
            { "ja", "顧客" },
            { "en", "Customer" }
        },
        Description = new Dictionary<string, string?>
        {
            { "zh", "客户实体" },
            { "ja", "顧客エンティティ" },
            { "en", "Customer entity" }
        }
        // 根据实际 EntityDefinition 结构添加其他必需字段
    };

    // Act
    var dto = entity.ToSummaryDto("zh");

    // Assert
    Assert.Equal("客户", dto.DisplayName);
    Assert.Equal("客户实体", dto.Description);
    
    // 注意：如果 DTO 还没有 DisplayNameTranslations 字段（Task 0.3才会添加）
    // 则跳过以下断言
    // Assert.Null(dto.DisplayNameTranslations);
}
```

---

#### 测试2：ToSummaryDto_WithoutLang_ReturnsMultilingual

**目的**: 验证不指定语言时的向后兼容行为

```csharp
[Fact]
public void ToSummaryDto_WithoutLang_ReturnsMultilingual()
{
    // Arrange
    var entity = new EntityDefinition
    {
        Id = Guid.NewGuid(),
        EntityName = "Customer",
        DisplayName = new Dictionary<string, string?>
        {
            { "zh", "客户" },
            { "ja", "顧客" }
        }
    };

    // Act
    var dto = entity.ToSummaryDto(lang: null);

    // Assert
    // 根据当前 EntitySummaryDto 的实际结构进行断言
    // 可能的断言方式：
    // 1. 如果 DisplayName 是 MultilingualText:
    //    Assert.Equal("客户", dto.DisplayName["zh"]);
    // 2. 如果 DisplayName 是 Dictionary:
    //    Assert.Equal("客户", dto.DisplayName["zh"]);
    // 3. 如果有单独的 DisplayNameTranslations 字段:
    //    Assert.Equal("客户", dto.DisplayNameTranslations["zh"]);
    
    Assert.NotNull(dto);  // 至少确保 DTO 不为 null
}
```

---

#### 测试3：ToFieldDto_WithDisplayNameKey_UsesLocalization

**目的**: 验证优先使用 DisplayNameKey 并调用本地化服务

```csharp
[Fact]
public void ToFieldDto_WithDisplayNameKey_UsesLocalization()
{
    // Arrange
    var field = new FieldMetadata
    {
        PropertyName = "Code",
        DisplayNameKey = "LBL_FIELD_CODE",
        DisplayName = null,
        DataType = "String"
        // 根据实际 FieldMetadata 结构添加其他必需字段
    };

    // Mock 本地化服务
    var mockLoc = new Mock<ILocalization>();
    mockLoc.Setup(l => l.T("LBL_FIELD_CODE", "zh"))
           .Returns("编码");

    // Act
    var dto = field.ToFieldDto(mockLoc.Object, "zh");

    // Assert
    Assert.Equal("编码", dto.DisplayName);
    Assert.Equal("LBL_FIELD_CODE", dto.DisplayNameKey);
    
    // 验证本地化服务被正确调用（关键验证）
    mockLoc.Verify(
        l => l.T("LBL_FIELD_CODE", "zh"), 
        Times.Once,
        "DisplayNameKey 应该通过本地化服务翻译"
    );
}
```

---

#### 测试4：ToFieldDto_WithDisplayNameDict_UsesResolve

**目的**: 验证扩展字段使用 DisplayName 字典（不调用本地化服务）

```csharp
[Fact]
public void ToFieldDto_WithDisplayNameDict_UsesResolve()
{
    // Arrange
    var field = new FieldMetadata
    {
        PropertyName = "CustomField",
        DisplayNameKey = null,  // 扩展字段没有 Key
        DisplayName = new Dictionary<string, string?>
        {
            { "zh", "自定义字段" },
            { "ja", "カスタムフィールド" }
        },
        DataType = "String"
    };

    var mockLoc = new Mock<ILocalization>();

    // Act
    var dto = field.ToFieldDto(mockLoc.Object, "ja");

    // Assert
    Assert.Equal("カスタムフィールド", dto.DisplayName);
    
    // 验证本地化服务未被调用（关键验证）
    mockLoc.Verify(
        l => l.T(It.IsAny<string>(), It.IsAny<string>()), 
        Times.Never,
        "扩展字段不应调用本地化服务"
    );
}
```

---

#### 测试5：ToFieldDto_WithoutLang_ReturnsMultilingual

**目的**: 验证字段的多语模式（向后兼容）

```csharp
[Fact]
public void ToFieldDto_WithoutLang_ReturnsMultilingual()
{
    // Arrange
    var field = new FieldMetadata
    {
        PropertyName = "Name",
        DisplayName = new Dictionary<string, string?>
        {
            { "zh", "名称" },
            { "en", "Name" }
        },
        DataType = "String"
    };

    var mockLoc = new Mock<ILocalization>();

    // Act
    var dto = field.ToFieldDto(mockLoc.Object, lang: null);

    // Assert
    // 根据当前 FieldMetadataDto 的实际结构进行断言
    Assert.NotNull(dto);
    // 如果有 DisplayNameTranslations 字段：
    // Assert.Equal("名称", dto.DisplayNameTranslations["zh"]);
}
```

---

#### 测试6：ToFieldDto_WithNoDisplayName_ReturnsFallback

**目的**: 验证兜底机制（回退到字段名）

```csharp
[Fact]
public void ToFieldDto_WithNoDisplayName_ReturnsFallback()
{
    // Arrange
    var field = new FieldMetadata
    {
        PropertyName = "UnknownField",
        DisplayNameKey = null,
        DisplayName = null,
        DataType = "String"
    };

    var mockLoc = new Mock<ILocalization>();

    // Act
    var dto = field.ToFieldDto(mockLoc.Object, "zh");

    // Assert
    Assert.Equal("UnknownField", dto.DisplayName);  // 应回退到字段名
}
```

---

## 🔍 编译和测试

### 步骤6：编译检查

```bash
# 1. 编译 Api 项目
dotnet build src/BobCrm.Api/BobCrm.Api.csproj -c Debug

# 2. 编译测试项目
dotnet build tests/BobCrm.Api.Tests/BobCrm.Api.Tests.csproj -c Debug

# 3. 如果上述成功，尝试完整构建
dotnet build BobCrm.sln -c Debug
```

**预期结果**: ✅ 编译成功，52个预先存在的警告（可接受）

---

### 步骤7：运行测试

```bash
# 运行 DtoExtensionsTests（详细输出）
dotnet test tests/BobCrm.Api.Tests/BobCrm.Api.Tests.csproj \
  -c Debug \
  -p:BuildProjectReferences=false \
  --filter "FullyQualifiedName~DtoExtensionsTests" \
  --logger "console;verbosity=detailed"
```

**预期输出**:
```
Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6
```

---

### 步骤8：代码覆盖率

```bash
# 生成覆盖率报告
dotnet test tests/BobCrm.Api.Tests/BobCrm.Api.Tests.csproj \
  --filter "FullyQualifiedName~DtoExtensionsTests" \
  --collect:"XPlat Code Coverage"

# 查看覆盖率文件位置
ls -lh tests/BobCrm.Api.Tests/TestResults/*/coverage.cobertura.xml
```

**验收标准**: 代码覆盖率 ≥ 80%

---

## 📝 Git 提交

### 步骤9：提交代码

```bash
# 1. 查看变更
git status
git diff src/BobCrm.Api/Extensions/DtoExtensions.cs
git diff tests/BobCrm.Api.Tests/Extensions/DtoExtensionsTests.cs

# 2. 添加文件
git add src/BobCrm.Api/Extensions/DtoExtensions.cs
git add tests/BobCrm.Api.Tests/Extensions/DtoExtensionsTests.cs

# 3. 提交（使用标准格式）
git commit -m "feat(dto): add DTO extension methods with language parameter support

- Implement ToSummaryDto() with optional lang parameter for dual-mode support
- Implement ToFieldDto() with DisplayNameKey resolution priority
- Add ResolveFieldDisplayName() helper with 3-level fallback logic:
  1. DisplayNameKey → ILocalization.T() (interface fields)
  2. DisplayName dictionary → MultilingualHelper.Resolve() (custom fields)
  3. PropertyName (fallback)
- Support backward compatibility (return full dict when lang is null)
- Add 6 comprehensive unit tests with mocked ILocalization service
- Verify mock invocation counts with Moq (Times.Once/Times.Never)
- All tests pass (6/6), code coverage ≥ 80%

Ref: ARCH-30 Task 0.2"

# 4. 验证提交
git log --oneline -1
```

---

## ✅ 验收标准

运行以下验收脚本确认完成：

```bash
#!/bin/bash
echo "╔════════════════════════════════════════════╗"
echo "║   Task 0.2 验收检查                         ║"
echo "╚════════════════════════════════════════════╝"

# 1. 文件存在检查
echo ""
echo "📂 1. 文件存在性检查..."
[ -f "src/BobCrm.Api/Extensions/DtoExtensions.cs" ] && echo "  ✅ DtoExtensions.cs" || echo "  ❌ DtoExtensions.cs 缺失"
[ -f "tests/BobCrm.Api.Tests/Extensions/DtoExtensionsTests.cs" ] && echo "  ✅ DtoExtensionsTests.cs" || echo "  ❌ DtoExtensionsTests.cs 缺失"

# 2. 编译检查
echo ""
echo "🔨 2. 编译检查..."
if dotnet build src/BobCrm.Api/BobCrm.Api.csproj -c Debug > /dev/null 2>&1; then
    echo "  ✅ Api 项目编译成功"
else
    echo "  ❌ Api 项目编译失败"
fi

if dotnet build tests/BobCrm.Api.Tests/BobCrm.Api.Tests.csproj -c Debug > /dev/null 2>&1; then
    echo "  ✅ Tests 项目编译成功"
else
    echo "  ❌ Tests 项目编译失败"
fi

# 3. 测试检查
echo ""
echo "🧪 3. 测试检查..."
TEST_OUTPUT=$(dotnet test --filter "FullyQualifiedName~DtoExtensionsTests" --no-build 2>&1)
if echo "$TEST_OUTPUT" | grep -q "Passed:     6"; then
    echo "  ✅ 所有测试通过 (6/6)"
else
    echo "  ❌ 测试未全部通过"
    echo "$TEST_OUTPUT" | grep "Passed\|Failed"
fi

# 4. Git 提交检查
echo ""
echo "📋 4. Git 提交检查..."
if git log --oneline -1 | grep -q "feat(dto)"; then
    echo "  ✅ Git 提交符合规范"
    git log --oneline -1 | head -1
else
    echo "  ❌ Git 提交缺失或格式不符"
fi

# 5. XML 文档检查
echo ""
echo "📖 5. XML 文档检查..."
DOC_COUNT=$(grep -c "/// <summary>" src/BobCrm.Api/Extensions/DtoExtensions.cs 2>/dev/null)
if [ "$DOC_COUNT" -ge 3 ]; then
    echo "  ✅ XML 文档完整 ($DOC_COUNT 个注释块)"
else
    echo "  ⚠️  XML 文档不完整 ($DOC_COUNT 个注释块，期望 ≥ 3)"
fi

# 6. Mock 验证检查
echo ""
echo "🎭 6. Mock 验证检查..."
if grep -q "mockLoc.Verify" tests/BobCrm.Api.Tests/Extensions/DtoExtensionsTests.cs 2>/dev/null; then
    echo "  ✅ 包含 Mock 验证逻辑"
else
    echo "  ❌ 缺少 Mock 验证逻辑"
fi

echo ""
echo "╔════════════════════════════════════════════╗"
echo "║   验收完成                                  ║"
echo "╚════════════════════════════════════════════╝"
```

**所有项目必须 ✅ 才能算完成。**

---

## ❓ 常见问题

### 问题1：EntityDefinition 或 FieldMetadata 字段不匹配

**症状**: 编译错误 "XXX 不包含 YYY 的定义"

**原因**: 假设的字段名与实际不符

**解决方案**:
1. 运行前置条件检查中的 `grep` 命令
2. 查看实际的类定义结构
3. 调整代码以匹配实际字段名和类型

---

### 问题2：MultilingualText 构造函数错误

**症状**: 无法从 `Dictionary<string, string?>` 转换到 `MultilingualText`

**解决方案**: 根据 MultilingualText 的实际定义调整

**检查方式**:
```bash
grep -A 10 "class MultilingualText" src/BobCrm.Api/Contracts/Common/*.cs
```

**可能的构造方式**:
```csharp
// 方式1: 继承自 Dictionary
new MultilingualText(entity.DisplayName)

// 方式2: 集合初始化器
new MultilingualText { { "zh", "..." }, { "ja", "..." } }

// 方式3: 直接返回 Dictionary
entity.DisplayName  // 如果 DTO 字段类型是 Dictionary
```

---

### 问题3：Moq Setup 不生效

**症状**: 测试失败，Mock 对象返回 null 或默认值

**常见原因**:
1. ❌ 传递了 Mock 对象本身而非 `.Object`
   ```csharp
   // 错误
   field.ToFieldDto(mockLoc, "zh");
   // 正确
   field.ToFieldDto(mockLoc.Object, "zh");
   ```

2. ❌ Setup 的参数与实际调用不一致
   ```csharp
   // Setup
   mockLoc.Setup(l => l.T("LBL_FIELD_CODE", "zh"))...
   // 实际调用（参数不同）
   loc.T("LBL_FIELD_CODE", "en");  // ❌ 不匹配
   ```

3. ❌ 使用 `It.IsAny<T>()` 时类型不匹配

**调试方法**:
```csharp
// 添加日志输出
mockLoc.Setup(l => l.T(It.IsAny<string>(), It.IsAny<string>()))
       .Returns<string, string>((key, lang) => 
       {
           Console.WriteLine($"Called T({key}, {lang})");
           return "测试翻译";
       });
```

---

### 问题4：DTO 当前没有 DisplayNameTranslations 字段

**症状**: 编译错误 "EntitySummaryDto 不包含 DisplayNameTranslations 的定义"

**原因**: Task 0.3 才会添加这个字段

**解决方案**: 暂时跳过设置或验证这个字段
```csharp
// 单语模式
dto.DisplayName = entity.DisplayName.Resolve(lang);
// 不设置 DisplayNameTranslations（Task 0.3 会添加）

// 测试中也跳过相关断言
// Assert.Null(dto.DisplayNameTranslations);  // 注释掉
```

---

### 问题5：ILocalization.T() 方法签名不匹配

**症状**: 编译错误 "ILocalization 不包含接受2个参数的 T 方法"

**解决方案**: 检查实际的方法签名
```bash
grep "interface ILocalization" -A 15 src/BobCrm.Api/Services/Localization/ILocalization.cs
```

可能的签名变体：
```csharp
// 变体1: 两个参数
string T(string key, string lang);

// 变体2: 一个参数（lang 在上下文中）
string T(string key);

// 变体3: 带默认值
string T(string key, string? lang = null);
```

根据实际签名调整调用方式。

---

## 📊 完成后更新

完成 Task 0.2 后，更新以下文档：

1. **更新 README.md 的进度表**:
   ```markdown
   | Task 0.2 | ✅ 完成 | [task-0.2-dto-extensions.md](task-0.2-dto-extensions.md) | AI | abc1234 | 2025-12-11 |
   ```

2. **更新工作计划文档**:
   - 在 `docs/plans/PLAN-09-系统级多语API架构优化-工作计划.md` 中标记 Task 0.2 为完成

3. **准备开始 Task 0.3**:
   - 报告完成情况
   - 请求 Task 0.3 的开发指南

---

**文档维护者**: ARCH-30 项目组  
**最后更新**: 2025-12-11  
**版本**: v1.0

