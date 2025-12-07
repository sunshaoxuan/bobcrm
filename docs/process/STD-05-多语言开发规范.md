# STD-05: 多语言开发规范

**版本**: 1.0  
**生效日期**: 2025-12-01  
**适用范围**: 所有前端和后端代码

---

## 1. 核心原则

### 1.1 I18n First（多语言优先）

> **强制规则**: 任何用户可见的文本**必须**使用多语言资源，严禁硬编码。

**违规示例** ❌:
```csharp
// 后端
logger.LogWarning("[Auth] 激活失败: user not found");
return Results.BadRequest(new ErrorResponse("用户未找到", "USER_NOT_FOUND"));

// 前端
<span>保存成功</span>
await MessageService.Info("操作完成");
```

**正确示例** ✅:
```csharp
// 后端
logger.LogWarning("[Auth] Activation failed: user not found, userId={UserId}", userId);
return Results.BadRequest(new ErrorResponse(I18n.T("ERR_USER_NOT_FOUND"), "USER_NOT_FOUND"));

// 前端
<span>@I18n.T("BTN_SAVE_SUCCESS")</span>
await MessageService.Info(I18n.T("MSG_OPERATION_COMPLETED"));
```

---

## 2. 资源键命名规范

### 2.1 命名格式

使用 **大写下划线分隔** 格式：`{类型}_{模块}_{描述}`

#### 类型前缀

| 前缀 | 用途 | 示例 |
|------|------|------|
| `LBL_` | 标签、字段名 | `LBL_USERNAME`, `LBL_CREATED_AT` |
| `BTN_` | 按钮文本 | `BTN_SAVE`, `BTN_CANCEL`, `BTN_SUBMIT` |
| `MSG_` | 提示消息（成功、信息） | `MSG_SAVE_SUCCESS`, `MSG_LOADING` |
| `ERR_` | 错误消息 | `ERR_REQUIRED`, `ERR_INVALID_FORMAT` |
| `WARN_` | 警告消息 | `WARN_UNSAVED_CHANGES` |
| `TXT_` | 长文本、说明 | `TXT_WELCOME_MESSAGE` |
| `TITLE_` | 页面标题 | `TITLE_LOGIN`, `TITLE_DASHBOARD` |
| `PLACEHOLDER_` | 输入框占位符 | `PLACEHOLDER_ENTER_USERNAME` |
| `CONFIRM_` | 确认对话框 | `CONFIRM_DELETE`, `CONFIRM_LOGOUT` |

#### 模块标识（可选）

对于特定模块的资源，添加模块标识：
- `LBL_AUTH_USERNAME` - 认证模块的用户名标签
- `ERR_ENTITY_NOT_FOUND` - 实体模块的错误
- `BTN_TEMPLATE_APPLY` - 模板模块的按钮

### 2.2 通用资源

建立通用资源库，避免重复定义：

```json
{
  "COMMON_SAVE": {"zh": "保存", "en": "Save", "ja": "保存"},
  "COMMON_CANCEL": {"zh": "取消", "en": "Cancel", "ja": "キャンセル"},
  "COMMON_DELETE": {"zh": "删除", "en": "Delete", "ja": "削除"},
  "COMMON_CONFIRM": {"zh": "确认", "en": "Confirm", "ja": "確認"},
  "ERR_REQUIRED": {"zh": "此字段为必填项", "en": "This field is required", "ja": "この項目は必須です"},
  "ERR_NETWORK": {"zh": "网络错误，请稍后重试", "en": "Network error, please try again", "ja": "ネットワークエラー、後でもう一度お試しください"}
}
```

---

## 3. 实施要求

### 3.1 开发流程

#### 步骤 1: 设计资源键
在编写代码**之前**，先在资源文件中定义键：

```json
// Resources/zh-CN.json
{
  "ERR_LOGIN_FAILED": {
    "zh": "登录失败：用户名或密码错误",
    "en": "Login failed: Invalid username or password",
    "ja": "ログイン失敗：ユーザー名またはパスワードが正しくありません"
  }
}
```

#### 步骤 2: 在代码中使用
```csharp
// ✅ 正确
return Results.BadRequest(new ErrorResponse(I18n.T("ERR_LOGIN_FAILED"), "LOGIN_FAILED"));
```

### 3.2 日志和调试消息

**规则**: 日志消息使用**英文**，但不允许硬编码用户可见消息。

```csharp
// ✅ 日志使用英文（开发者可见）
logger.LogInformation("User {Username} logged in successfully", username);

// ✅ 用户消息使用多语言（用户可见）
await MessageService.Success(I18n.T("MSG_LOGIN_SUCCESS"));

// ❌ 错误：日志中的用户可见消息硬编码
logger.LogWarning("激活失败：{Reason}", reason);  // 如果日志会展示给用户，必须多语化
```

### 3.3 异常消息

自定义异常消息使用资源键：

```csharp
// ✅ 正确
public class BusinessException : Exception
{
    public string I18nKey { get; }
    
    public BusinessException(string i18nKey) : base(i18nKey)
    {
        I18nKey = i18nKey;
    }
}

throw new BusinessException("ERR_INSUFFICIENT_PERMISSIONS");
```

---

## 4. 资源文件管理

### 4.1 文件结构

```
Resources/
├── common.json          # 通用资源（按钮、标签等）
├── errors.json          # 错误消息
├── validation.json      # 验证消息
├── auth.json           # 认证模块
├── entities.json       # 实体模块
└── templates.json      # 模板模块
```

### 4.2 资源定义格式

```json
{
  "KEY_NAME": {
    "zh": "中文文本",
    "en": "English text",
    "ja": "日本語テキスト"
  }
}
```

**强制要求**: 每个资源键**必须**包含 zh、en、ja 三个语言版本。

---

## 5. 自动化验证

### 5.1 CI/CD 集成

在 CI 流程中添加多语言检查：

```yaml
# .github/workflows/ci.yml
- name: Check I18n Compliance
  run: pwsh ./scripts/check-i18n.ps1
  
# 如果发现硬编码字符串，构建失败
```

### 5.2 Pre-commit Hook

在提交前自动检查：

```bash
# .git/hooks/pre-commit
#!/bin/sh
pwsh ./scripts/check-i18n.ps1 --staged
if [ $? -ne 0 ]; then
    echo "❌ I18n check failed. Please use I18n resources instead of hardcoded strings."
    exit 1
fi
```

### 5.3 IDE 集成

**推荐**: 使用 IDE 插件高亮硬编码字符串
- Visual Studio: ReSharper I18n 插件
- VS Code: i18n Ally 扩展

---

## 6. 排除规则

以下场景**允许**硬编码：

### 6.1 技术常量
```csharp
const string DateFormat = "yyyy-MM-dd";  // ✅ 技术格式
const string ApiVersion = "v1";          // ✅ API 版本号
```

### 6.2 单元测试
```csharp
[Fact]
public void Should_Validate_Username()
{
    var result = Validator.Validate("测试用户");  // ✅ 测试数据
    Assert.True(result.IsValid);
}
```

### 6.3 数据库种子数据
```csharp
new Entity { Name = "Default User" }  // ✅ 默认数据
```

### 6.4 开发者日志（不展示给用户）
```csharp
logger.LogDebug("Processing request with ID {RequestId}", requestId);  // ✅ 内部日志
```

---

## 7. 迁移策略

### 7.1 优先级

**P0 - 立即修复**:
- API 错误消息
- 表单验证消息
- 用户提示消息

**P1 - 2周内修复**:
- 按钮文本
- 标签文本
- 页面标题

**P2 - 1个月内修复**:
- 帮助文本
- 工具提示

### 7.2 渐进式修复

使用自动化脚本标记所有硬编码字符串：

```bash
# 生成清单
pwsh ./scripts/check-i18n.ps1 --export violations.csv

# 按优先级修复
# P0: 先修复 API 层
# P1: 再修复 UI 层
# P2: 最后修复辅助文本
```

---

## 8. 违规处理

### 8.1 代码审查

PR 必须通过 I18n 检查才能合并。

### 8.2 警告级别

| 级别 | 描述 | 处理方式 |
|------|------|----------|
| ERROR | API 错误消息、表单验证 | **阻止合并** |
| WARNING | UI 文本、按钮标签 | **要求修复** |
| INFO | 日志、调试消息 | 提示，不阻止 |

---

## 9. 最佳实践

### 9.1 参数化消息

```json
{
  "MSG_USER_CREATED": {
    "zh": "用户 {0} 创建成功",
    "en": "User {0} created successfully",
    "ja": "ユーザー {0} が作成されました"
  }
}
```

```csharp
I18n.T("MSG_USER_CREATED", username)
```

### 9.2 复数处理

```json
{
  "MSG_ITEMS_COUNT": {
    "zh": "{count} 个项目",
    "en": "{count} item(s)",
    "ja": "{count} 件のアイテム"
  }
}
```

### 9.3 上下文相关

同一个词在不同上下文使用不同的键：

```json
{
  "BTN_CLOSE_DIALOG": {"zh": "关闭", "en": "Close", "ja": "閉じる"},
  "BTN_CLOSE_WINDOW": {"zh": "关闭窗口", "en": "Close Window", "ja": "ウィンドウを閉じる"}
}
```

---

## 10. 检查清单

在提交代码前，确认：

- [ ] 所有用户可见文本使用 `I18n.T("KEY")`
- [ ] 资源键已在 zh/en/ja 三个语言文件中定义
- [ ] 资源键命名符合规范（前缀 + 描述）
- [ ] 运行 `pwsh ./scripts/check-i18n.ps1` 通过
- [ ] 单元测试中的硬编码已标记为 `// Test data`

---

## 附录 A: 快速参考

### 常用模式

```csharp
// 按钮
<Button>@I18n.T("BTN_SAVE")</Button>

// 消息
await MessageService.Success(I18n.T("MSG_SAVE_SUCCESS"));

// 错误
return Results.BadRequest(new ErrorResponse(I18n.T("ERR_INVALID_INPUT"), "INVALID_INPUT"));

// 标签
<label>@I18n.T("LBL_USERNAME")</label>

// 占位符
<Input Placeholder="@I18n.T("PLACEHOLDER_ENTER_EMAIL")" />
```

### 资源文件示例

参见: [`Resources/common.json`](file:///c:/workspace/bobcrm/src/BobCrm.App/Resources/common.json)
