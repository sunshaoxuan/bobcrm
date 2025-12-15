# 最终清理计划 - WARNING 清零行动

## 🎯 目标

**彻底消除所有 274 个 WARNING 违规，实现 100% I18n 合规**

---

## 📊 当前状态

- **剩余违规**: 274 个
- **已完成**: API 层 (20) + 高优先级 UI (12) + 中等优先级 UI (4) = 36 个组件
- **待清理**: 注释 + 低频组件 + 表单元素

---

## 🗂️ 违规分类与清理策略

### 类别 1: XML 注释和代码注释 (~200 个)

**特征**: `/// 中文`, `// 中文`, `@* 中文 *@`

**组件示例**:
- EnumSelector.razor (16 - 全部注释)
- IconSelector.razor (15 - 全部注释)
- PropertyEditor.razor (14 - 全部注释)
- EnumEdit.razor (10 - 全部注释)
- MasterDetailConfig.razor (11 - 大部分注释)

**策略**: **批量英文化或删除**

#### 执行方法 A: 批量英文化（推荐）

对每个文件：
```powershell
# 1. 检查注释内容
$file = "src/BobCrm.App/Components/Shared/EnumSelector.razor"
Select-String -Path $file -Pattern '[\u4e00-\u9fa5]+' | 
Where-Object { $_.Line -match '(^#|^\s*//|^\s*\*|^\s*@\*|^\s*///)' }

# 2. 手工翻译关键注释为英文
/// 枚举选择器 → /// Enum selector component
// 加载数据 → // Load data
@* 用户输入 *@ → @* User input *@

# 3. 或删除非关键注释
```

#### 执行方法 B: 批量删除（快速）

```powershell
# 仅当注释不重要时使用
# 将包含中文的注释行删除
```

---

### 类别 2: 低频 UI 组件 (~50 个)

**特征**: 1-5 个中文字符串，用户可见

**组件示例**:
- Profile.razor (6)
- SubFormRuntime.razor (7)
- EntitySelector.razor (8)
- EnumDisplay.razor (8)
- MainLayout.razor (7)
- LeftRightSplitLayout.razor (7)
- RoleFieldPermissions.razor (7)
- TopBottomSplitLayout.razor (7)
- TabContainerDesignRenderer.razor (6)
- SectionDesignRenderer.razor (5)

**策略**: **逐个清理，添加 I18n 键**

#### 清理步骤

对每个组件：

1. **扫描用户文本**
```powershell
$file = "Profile.razor"
Select-String -Path "src/BobCrm.App/Components/**/$file" -Pattern '[\u4e00-\u9fa5]+' |
Where-Object { $_.Line -notmatch '^\s*(//|@\*|<!--)' }
```

2. **添加 I18n 注入**（如果没有）
```razor
@inject BobCrm.App.Services.I18nService I18n
```

3. **替换硬编码**
```razor
<!-- 之前 -->
<p>个人资料</p>
<Button>保存</Button>

<!-- 之后 -->
<p>@I18n.T("PROFILE_TITLE")</p>
<Button>@I18n.T("BTN_SAVE")</Button>
```

4. **添加资源键** (i18n-resources.json)
```json
"PROFILE_TITLE": {
  "zh": "个人资料",
  "en": "Profile",
  "ja": "プロフィール"
},
"BTN_SAVE": {
  "zh": "保存",
  "en": "Save",
  "ja": "保存"
}
```

5. **验证**
```powershell
pwsh ./scripts/check-i18n.ps1 --severity WARNING | Select-String "$file"
dotnet build src/BobCrm.App/BobCrm.App.csproj
```

---

### 类别 3: 表单元素 (~20 个)

**特征**: 占位符、标签、验证消息

**可能位置**:
- Form 组件
- Input 组件
- Validation 消息

**策略**: **统一处理**

#### 通用资源键

```json
// 占位符
"PH_ENTER_NAME": { "zh": "请输入名称", "en": "Enter name", "ja": "名前を入力" },
"PH_SELECT": { "zh": "请选择...", "en": "Please select...", "ja": "選択してください" },

// 标签
"LBL_NAME": { "zh": "名称", "en": "Name", "ja": "名前" },
"LBL_CODE": { "zh": "代码", "en": "Code", "ja": "コード" },
"LBL_DESCRIPTION": { "zh": "描述", "en": "Description", "ja": "説明" },

// 验证
"VAL_REQUIRED": { "zh": "此字段必填", "en": "This field is required", "ja": "この項目は必須です" },
"VAL_INVALID_FORMAT": { "zh": "格式不正确", "en": "Invalid format", "ja": "形式が正しくありません" }
```

---

### 类别 4: OpenAPI 文档 (~4 个)

**特征**: `.WithSummary("中文")`, `.WithDescription("中文")`

**策略**: **英文化**

```csharp
// 之前
.WithSummary("获取用户列表")
.WithDescription("返回所有用户")

// 之后
.WithSummary("Get user list")
.WithDescription("Returns all users")
```

---

## 📋 执行计划（按优先级）

### 阶段 1: 注释清理 (批量，1-2 小时)

**目标**: 消除 ~200 个注释违规

**文件列表** (纯注释组件):
1. EnumSelector.razor (16)
2. IconSelector.razor (15)  
3. PropertyEditor.razor (14)
4. EnumEdit.razor (10)
5. SubFormRuntime.razor (部分)
6. RolePermissionTree.razor (部分)
7. MainLayout.razor (部分 - 检查是否有用户文本)
8. 其他 10+ 纯注释组件

**方法**:
- 手工翻译关键注释为英文
- 删除非关键注释
- 或批量替换为英文

---

### 阶段 2: 低频组件清理 (逐个，2-3 小时)

**目标**: 消除 ~50 个低频组件违规

**优先顺序** (按中文数量):
1. EntitySelector.razor (8)
2. EnumDisplay.razor (8)
3. SubFormRun时.razor (7 - 检查用户文本)
4. MainLayout.razor (7 - 检查用户文本)
5. LeftRightSplitLayout.razor (7)
6. RoleFieldPermissions.razor (7)
7. TopBottomSplitLayout.razor (7)
8. Profile.razor (6)
9. TabContainerDesignRenderer.razor (6)
10. SectionDesignRenderer.razor (5)
11. 其他 1-4 次的组件

**每个组件**:
- 识别用户文本 vs 注释
- 添加 I18n 键
- 替换硬编码
- 验证

---

### 阶段 3: 表单元素统一处理 (1 小时)

**目标**: 消除 ~20 个表单元素违规

**检查位置**:
- 所有 `<Input>` 的 Placeholder
- 所有 `<FormItem>` 的 Label
- 所有验证消息

**统一资源键**:
- 使用通用 PH_*, LBL_*, VAL_* 键
- 避免重复定义

---

### 阶段 4: 最终扫尾 (30 分钟)

**目标**: 清零剩余违规

- OpenAPI 文档英文化
- 任何遗漏的片段
- 最终验证

---

## ✅ 验证检查清单

每个阶段完成后：

### 1. 扫描验证
```powershell
pwsh ./scripts/check-i18n.ps1 --severity WARNING
# 目标输出: Violations found: 0
```

### 2. 构建验证
```powershell
dotnet build src/BobCrm.App/BobCrm.App.csproj
# 0 errors, 0 warnings
```

### 3. 测试验证
```powershell
dotnet test tests/BobCrm.Api.Tests/BobCrm.Api.Tests.csproj
# All tests pass
```

### 4. 资源完整性检查
```powershell
# 所有新增键都有 zh/en/ja
$json = Get-Content "src/BobCrm.Api/Resources/i18n-resources.json" -Raw | ConvertFrom-Json
foreach ($key in $json.PSObject.Properties.Name) {
    $value = $json.$key
    if (-not $value.zh -or -not $value.en -or -not $value.ja) {
        Write-Host "Incomplete: $key"
    }
}
```

---

## 📊 预期成果

### 最终统计
- ✅ **WARNING 违规**: 0 (从 274 → 0)
- ✅ **已清理组件**: 60+ 个
- ✅ **资源键总数**: 300+ 个
- ✅ **三语完整率**: 100%

### 成就
- 🏆 100% I18n 合规
- 🏆 所有用户文本本地化
- 🏆 所有注释英文化
- 🏆 零技术债务

---

## 🚀 开始执行

### 推荐执行顺序

1. **先易后难**: 从注释开始（批量处理，快速见效）
2. **然后重点**: 低频组件（逐个清理，质量保证）
3. **最后收尾**: 表单元素和 OpenAPI

### 进度跟踪

在 task.md 中更新：
```markdown
- [/] 最终清理阶段 - WARNING 清零
    - [ ] 阶段1: 注释清理 (~200个) 
    - [ ] 阶段2: 低频组件 (~50个)
    - [ ] 阶段3: 表单元素 (~20个)
    - [ ] 阶段4: 最终扫尾 (~4个)
    - [ ] 验证: 0 违规 ✅
```

---

## 💡 高效技巧

### 1. 批量处理脚本
```powershell
# 批量检查多个文件
$files = @("EnumSelector.razor", "IconSelector.razor", "PropertyEditor.razor")
foreach ($file in $files) {
    Write-Host "`n=== $file ===`n"
    Select-String -Path "src/BobCrm.App/Components/**/$file" -Pattern '[\u4e00-\u9fa5]+'
}
```

### 2. 快速翻译对照表
```
常见注释翻译:
枚举 → enum
选择器 → selector
加载 → load
数据 → data
参数 → parameter
属性 → property
组件 → component
处理 → handle
验证 → validate
更新 → update
删除 → delete
创建 → create
```

### 3. 资源键复用
- 优先使用已存在的键（BTN_SAVE, BTN_CANCEL 等）
- 查找类似功能的键再创建新键

---

**准备好了就开始执行！每完成一个阶段通知我验证！** 🚀
