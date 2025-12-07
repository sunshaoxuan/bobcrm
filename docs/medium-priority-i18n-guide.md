# 中等优先级组件 I18n 清理指南

## 📋 需要清理的组件清单（选项B - 仅用户可见文本）

基于扫描结果，以下组件有**真实用户可见的中文文本**需要本地化：

### 🎯 优先清理列表（6个组件）

#### 1. EntityDefinitions.razor ⭐ 高优先级
**文件路径**: `src/BobCrm.App/Components/Pages/EntityDefinitions.razor`

**需要清理的内容**:
- **Line 53**: Tab="全部" → `@I18n.T("TAB_ALL")`
- **Line 56**: Tab="系统实体" → `@I18n.T("TAB_SYSTEM_ENTITIES")`
- **Line 59**: Tab="自定义实体" → `@I18n.T("TAB_CUSTOM_ENTITIES")`
- **Line 62**: Tab="草稿" → `@I18n.T("TAB_DRAFT")`
- **Line 92**: "加载失败: {ex.Message}" → `I18n.T("ED_MSG_LOAD_FAILED") + ": " + ex.Message`
- **Line 146**: Title = "确认删除" → `Title = I18n.T("ED_CONFIRM_DELETE_TITLE")`
- **Line 147**: Content = "确定要删除..." → `Content = I18n.T("ED_CONFIRM_DELETE_CONTENT")`
- **Line 149**: OkText = "删除" → `OkText = I18n.T("BTN_DELETE")`
- **Line 150**: CancelText = "取消" → `CancelText = I18n.T("BTN_CANCEL")`
- **Line 159**: "删除成功" → `I18n.T("MSG_DELETE_SUCCESS")`
- **Line 164**: "删除失败: {ex.Message}" → `I18n.T("MSG_DELETE_FAILED") + ": " + ex.Message`

**需要添加的资源键** (zh/en/ja):
```json
"TAB_ALL": { "zh": "全部", "en": "All", "ja": "全て" },
"TAB_SYSTEM_ENTITIES": { "zh": "系统实体", "en": "System Entities", "ja": "システムエンティティ" },
"TAB_CUSTOM_ENTITIES": { "zh": "自定义实体", "en": "Custom Entities", "ja": "カスタムエンティティ" },
"TAB_DRAFT": { "zh": "草稿", "en": "Draft", "ja": "下書き" },
"ED_MSG_LOAD_FAILED": { "zh": "加载失败", "en": "Load Failed", "ja": "読み込み失敗" },
"ED_CONFIRM_DELETE_TITLE": { "zh": "确认删除", "en": "Confirm Delete", "ja": "削除確認" },
"ED_CONFIRM_DELETE_CONTENT": { "zh": "确定要删除此实体定义吗？此操作不可恢复。", "en": "Are you sure you want to delete this entity definition? This action cannot be undone.", "ja": "このエンティティ定義を削除してもよろしいですか？この操作は元に戻せません。" }
```

---

#### 2. AppHeader.razor ⭐ 高优先级  
**文件路径**: `src/BobCrm.App/Components/Shared/AppHeader.razor`

**需要清理的内容**:
- **Line 68**: `_contextLabel = "总览"` → `_contextLabel = I18n.T("LBL_OVERVIEW")`
- **Line 90**: "组织关系" → 已在 RouteLabels 字典中，使用 MENU_ORG 键（需确保有翻译）
- **Line 128**: 注释 "// 加载语言" → 改为英文 `// Load language`
- **Line 143**: 注释 "// avatarUrl 暂时为空..." → 改为英文 `// avatarUrl is currently empty...`
- **Line 184**: "当前筛选：..." → `I18n.T("DEMO_STICKY_TEXT")`
- **Line 187**: "重置" → `I18n.T("BTN_RESET")`
- **Line 188**: "保存视图" → `I18n.T("BTN_SAVE_VIEW")`
- **Line 194**: "已选中 3 条记录" → `I18n.T("DEMO_BULK_SELECTED")`
- **Line 194**: "批量更新" → `I18n.T("BTN_BULK_UPDATE")`
- **Line 194**: "取消" → `I18n.T("BTN_CANCEL")`
- **Line 211**: `_contextLabel = ... ?? "总览"` → 使用 `I18n.T("LBL_OVERVIEW")`

**需要添加的资源键**:
```json
"LBL_OVERVIEW": { "zh": "总览", "en": "Overview", "ja": "概要" },
"MENU_ORG": { "zh": "组织关系", "en": "Organizations", "ja": "組織" },
"DEMO_STICKY_TEXT": { "zh": "当前筛选：状态=潜在客户, 所属=华北大区", "en": "Current filters: Status=Potential, Region=North China", "ja": "現在のフィルター：ステータス=見込み、地域=華北" },
"BTN_RESET": { "zh": "重置", "en": "Reset", "ja": "リセット" },
"BTN_SAVE_VIEW": { "zh": "保存视图", "en": "Save View", "ja": "ビュー保存" },
"DEMO_BULK_SELECTED": { "zh": "已选中 3 条记录", "en": "3 records selected", "ja": "3件選択済み" },
"BTN_BULK_UPDATE": { "zh": "批量更新", "en": "Batch Update", "ja": "一括更新" }
```

---

#### 3. EnumOptionEditor.razor
**文件路径**: `src/BobCrm.App/Components/Shared/EnumOptionEditor.razor`

**需要检查并清理** - 请程序员扫描此文件，找出非注释的中文文本并本地化

---

#### 4. ListTemplateHost.razor
**文件路径**: `src/BobCrm.App/Components/Designer/ListTemplateHost.razor`

**需要检查并清理** - 请程序员扫描此文件

---

#### 5. MasterDetailConfig.razor
**文件路径**: `src/BobCrm.App/Components/Shared/MasterDetailConfig.razor`

**需要检查并清理** - 请程序员扫描此文件

---

#### 6. SubEntityTabs.razor
**文件路径**: `src/BobCrm.App/Components/Shared/SubEntityTabs.razor`

**需要检查并清理** - 请程序员扫描此文件

---

## 🚫 跳过的组件（仅注释，无需清理）

以下组件**仅包含 XML 注释或代码注释**，不影响用户界面，暂时跳过：

- IconSelector.razor
- EntityDefinitionEdit.razor (pages版本，已清理过)
- PropertyEditor.razor
- MasterDetailConfig.razor (如果仅注释)
- EnumEdit.razor
- SubFormRuntime.razor
- RolePermissionTree.razor
- MainLayout.razor
- LeftRightSplitLayout.razor
- TopBottomSplitLayout.razor
- Profile.razor (如果仅注释)
- TabContainerDesignRenderer.razor
- EnumDefinitionEdit.razor (shared版本)
- SectionDesignRenderer.razor

---

## 📝 清理步骤（程序员执行）

### 对每个需要清理的组件：

1. **检查中文内容**
   ```powershell
   Select-String -Path "路径/文件.razor" -Pattern '[\u4e00-\u9fa5]+' | 
   Where-Object { $_.Line -notmatch '^\s*(//|@\*|<!--)' }
   ```

2. **添加 I18n 注入**（如果还没有）
   ```razor
   @inject BobCrm.App.Services.I18nService I18n
   ```

3. **替换硬编码文本**
   - 属性值: `Title="中文"` → `Title="@I18n.T("KEY")"`
   - C# 代码: `"中文"` → `I18n.T("KEY")`
   - Tab/按钮: `Tab="中文"` → `Tab="@I18n.T("KEY")"`

4. **添加资源键**
   在 `src/BobCrm.Api/Resources/i18n-resources.json` 添加：
   ```json
   "YOUR_KEY": {
     "zh": "中文",
     "en": "English",
     "ja": "日本語"
   }
   ```

5. **验证**
   ```powershell
   # 检查无中文
   pwsh ./scripts/check-i18n.ps1 --severity WARNING | Select-String "文件名"
   
   # 构建测试
   dotnet build src/BobCrm.App/BobCrm.App.csproj
   ```

---

## ✅ 验证标准

每个组件清理完成后，必须满足：

1. ✅ **无硬编码中文/日文**
   ```powershell
   # 应该无输出（或仅注释）
   Select-String -Path "文件.razor" -Pattern '[\u4e00-\u9fa5]+' | 
   Where-Object { $_.Line -notmatch '^\s*(//|@\*)' }
   ```

2. ✅ **WARNING 扫描通过**
   ```powershell
   pwsh ./scripts/check-i18n.ps1 --severity WARNING | Select-String "文件名"
   # 应该无输出
   ```

3. ✅ **构建成功**
   ```powershell
   dotnet build src/BobCrm.App/BobCrm.App.csproj
   # 0 errors, 0 warnings
   ```

4. ✅ **资源键完整**
   - 所有新增键都有 zh/en/ja 翻译
   - 翻译内容准确

---

## 🎯 预期成果

完成所有组件后：
- ✅ 6 个核心组件本地化
- ✅ 约 30-50 个新资源键
- ✅ 用户界面完全支持中/英/日切换
- ✅ 扫描器 WARNING 显著减少

---

## 📊 进度跟踪

请在 task.md 中更新进度：
- [ ] EntityDefinitions.razor
- [ ] AppHeader.razor
- [ ] EnumOptionEditor.razor
- [ ] ListTemplateHost.razor
- [ ] MasterDetailConfig.razor
- [ ] SubEntityTabs.razor

完成每个组件后，标记为 `[x]` 并通知我进行代码评审！
