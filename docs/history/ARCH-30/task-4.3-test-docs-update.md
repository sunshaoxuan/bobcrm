# ARCH-30 Task 4.3: 更新测试文档

**任务编号**: Task 4.3
**阶段**: 阶段4 - 文档同步
**状态**: 待开始
**预计工时**: 0.5 小时
**依赖**: 阶段0-3 实施完成

---

## 1. 任务目标

在 `docs/guides/TEST-01-测试指南.md` 中添加多语 API 测试规范，确保后续开发遵循统一的测试标准。

---

## 2. 需添加的内容

### 2.1 多语 API 测试规范章节

```markdown
## 8. 多语 API 测试规范

### 8.1 概述

所有支持 `lang` 参数的 API 端点必须包含以下测试类别，确保多语功能正确性和向后兼容性。

### 8.2 必测场景

#### 8.2.1 单语模式测试

验证提供 `lang` 参数时，API 返回指定语言的显示名。

```csharp
[Theory]
[InlineData("zh", "编码")]
[InlineData("ja", "コード")]
[InlineData("en", "Code")]
public async Task Endpoint_WithLang_ShouldReturnSingleLanguage(string lang, string expectedDisplayName)
{
    // Arrange
    var client = factory.CreateClient();

    // Act
    var response = await client.GetAsync($"/api/xxx?lang={lang}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var json = await response.ReadDataAsJsonAsync();

    // 验证返回单语字符串
    json.GetProperty("displayName").GetString().Should().Be(expectedDisplayName);

    // 验证不返回多语字典
    json.TryGetProperty("displayNameTranslations", out _).Should().BeFalse();
}
```

#### 8.2.2 多语模式测试（向后兼容）

验证不提供 `lang` 参数时，API 返回完整多语结构。

```csharp
[Fact]
public async Task Endpoint_WithoutLang_ShouldReturnMultiLanguage()
{
    // Arrange
    var client = factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/xxx");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var json = await response.ReadDataAsJsonAsync();

    // 接口字段：应有 displayNameKey
    if (json.TryGetProperty("displayNameKey", out var key))
    {
        key.GetString().Should().StartWith("LBL_");
    }

    // 自定义字段：应有多语字典
    if (json.TryGetProperty("displayNameTranslations", out var translations))
    {
        translations.EnumerateObject().Should().HaveCountGreaterOrEqualTo(1);
    }
}
```

#### 8.2.3 语言切换测试

验证同一实体在不同语言下返回正确的显示名。

```csharp
[Fact]
public async Task Endpoint_LanguageSwitch_ShouldReturnCorrectTranslation()
{
    // Arrange
    var client = factory.CreateClient();
    var entityId = "test-entity-id";

    // Act - 获取中文版本
    var zhResponse = await client.GetAsync($"/api/xxx/{entityId}?lang=zh");
    var zhJson = await zhResponse.ReadDataAsJsonAsync();

    // Act - 获取英文版本
    var enResponse = await client.GetAsync($"/api/xxx/{entityId}?lang=en");
    var enJson = await enResponse.ReadDataAsJsonAsync();

    // Assert
    zhJson.GetProperty("displayName").GetString()
        .Should().NotBe(enJson.GetProperty("displayName").GetString());
}
```

#### 8.2.4 性能测试

验证单语模式响应体积显著减少。

```csharp
[Fact]
public async Task Endpoint_SingleLanguageMode_ShouldReduceResponseSize()
{
    // Arrange
    var client = factory.CreateClient();

    // Act - 多语模式
    var multiResponse = await client.GetAsync("/api/xxx");
    var multiSize = (await multiResponse.Content.ReadAsStringAsync()).Length;

    // Act - 单语模式
    var singleResponse = await client.GetAsync("/api/xxx?lang=zh");
    var singleSize = (await singleResponse.Content.ReadAsStringAsync()).Length;

    // Assert - 单语模式应至少减少 30%
    var reduction = (double)(multiSize - singleSize) / multiSize;
    reduction.Should().BeGreaterOrEqualTo(0.3);
}
```

### 8.3 测试命名规范

多语 API 测试应遵循以下命名模式：

```
{Endpoint}_{Scenario}_{ExpectedBehavior}
```

示例：
- `FunctionsMe_WithLangZh_ShouldReturnChineseDisplayName`
- `EntitiesList_WithoutLang_ShouldReturnMultiLanguageStructure`
- `DynamicQuery_WithIncludeMeta_ShouldReturnFieldMetadata`

### 8.4 测试数据准备

多语测试需要确保测试数据包含：

1. **接口字段**：使用 `DisplayNameKey` 引用 i18n 资源
2. **自定义字段**：使用 `DisplayName` 多语字典
3. **i18n 资源**：确保测试环境包含必要的翻译资源

```csharp
// TestWebAppFactory 中预置测试数据
private void SeedI18nResources(AppDbContext db)
{
    db.I18nResources.AddRange(
        new I18nResource { Key = "LBL_FIELD_CODE", Language = "zh", Value = "编码" },
        new I18nResource { Key = "LBL_FIELD_CODE", Language = "ja", Value = "コード" },
        new I18nResource { Key = "LBL_FIELD_CODE", Language = "en", Value = "Code" }
        // ...
    );
}
```

### 8.5 回归测试清单

每次多语 API 变更后，运行以下回归测试：

```bash
# 运行所有多语相关测试
dotnet test --filter "FullyQualifiedName~Multilingual|FullyQualifiedName~I18n|FullyQualifiedName~Lang"

# 运行 ARCH-30 相关测试
dotnet test --filter "FullyQualifiedName~DtoExtensions|FullyQualifiedName~FieldMetadata"
```
```

---

## 3. 验收标准

- [ ] 测试指南已添加多语 API 测试规范章节
- [ ] 包含4种必测场景的代码示例
- [ ] 包含测试命名规范
- [ ] 包含测试数据准备指导
- [ ] 包含回归测试命令

---

## 4. Git 提交规范

```bash
git add docs/guides/TEST-01-测试指南.md
git commit -m "docs(testing): add multilingual API testing specification

- Add 4 mandatory test scenarios for lang parameter
- Include code examples for each scenario
- Define naming conventions for i18n tests
- Add test data preparation guide
- Add regression test checklist
- Ref: ARCH-30 Task 4.3"
```

---

**创建日期**: 2025-01-06
**最后更新**: 2025-01-06
**维护者**: ARCH-30 项目组
