# check-i18n.ps1 扫描规则说明

## 🎯 工具到底扫描什么？

### ✅ 会被扫描到（违规）

```csharp
// 1. 中文字面量
return Results.BadRequest(new ErrorResponse("用户未找到", "ERROR"));
var message = "这是中文";

// 2. 日文字面量
var message = "ユーザーが見つかりません";

// 3. HTML 中的中文
<span>保存成功</span>
<Button>删除</Button>
```

### ✅ 不会被扫描到（合法）

```csharp
// 1. 英文资源键（推荐）✅
loc.T("ERR_USER_NOT_FOUND", lang)
loc.T("MSG_SAVE_SUCCESS", lang)

// 2. 常量（也是英文，但不推荐）
loc.T(AuthI18nKeys.UserNotFound, lang)  // 多此一举

// 3. 纯英文字符串（不是用户可见）
const string API_VERSION = "v1.0";
logger.LogInfo("User login successful");  // 开发者日志

// 4. 技术性字符串
var url = "https://example.com";
var code = "AUTH_FAILED";
```

## 📋 验证方法

创建测试文件 `test-i18n-scan.cs`:

```csharp
public class I18nScanTest
{
    public void TestCases()
    {
        // ❌ 会被扫描（ERROR）- 有中文
        var bad1 = Results.BadRequest(new ErrorResponse("用户未找到", "ERROR"));
        
        // ✅ 不会被扫描 - 英文资源键
        var good1 = Results.BadRequest(new ErrorResponse(loc.T("ERR_USER_NOT_FOUND", lang), "ERROR"));
        
        // ✅ 不会被扫描 - 即使用常量也是英文
        var good2 = Results.BadRequest(new ErrorResponse(loc.T(Keys.UserNotFound, lang), "ERROR"));
        
        // ❌ 会被扫描（INFO）- 中文字符串
        var bad2 = "这是一个中文字符串";
        
        // ✅ 不会被扫描 - 纯英文
        var good3 = "ERR_USER_NOT_FOUND";
        var good4 = "This is English";
    }
}
```

运行扫描：
```powershell
pwsh ./scripts/check-i18n.ps1 --severity ERROR
```

结果：只有带中文/日文的行会被报告，英文资源键不会！

## 🔍 工具的正则模式

```powershell
# 中文字符模式
ChinesePattern = '[\u4e00-\u9fa5]+'

# 日文字符模式  
JapanesePattern = '[\u3040-\u309f\u30a0-\u30ff]+'

# ERROR 级别检查
Patterns = @(
    'Results\.(Ok|BadRequest|NotFound)\(.*["\u4e00-\u9fa5]+',  # 必须包含中文
    'ErrorResponse\(.*["\u4e00-\u9fa5]+',                       # 必须包含中文
    # ...
)
```

**关键**：所有模式都包含 `[\u4e00-\u9fa5]`（中文）或 `[\u3040-\u309f\u30a0-\u30ff]`（日文）

**结论**：`loc.T("ERR_XXX")` 中的 `"ERR_XXX"` 是纯英文，**绝对不会被扫描**！

## ❓ 程序员可能的误解

### 误解 1：所有字符串字面量都会被扫描
**错误**！只有中文/日文字面量会被扫描。

### 误解 2：loc.T() 的参数会被检查
**错误**！工具不检查函数参数是否是常量，只检查是否有中日文字符。

### 误解 3：需要用常量来避免扫描
**错误**！英文资源键本身就不会被扫描，常量类是多余的。

## ✅ 最佳实践

```csharp
// ❌ 不推荐：常量类（多此一举）
public static class I18nKeys {
    public const string UserNotFound = "ERR_USER_NOT_FOUND";  // 这个常量值也是英文！
}
loc.T(I18nKeys.UserNotFound, lang);  // 增加复杂度，没有实质好处

// ✅ 推荐：直接使用字符串资源键
loc.T("ERR_USER_NOT_FOUND", lang);  // 简单、清晰、不会被扫描
```

## 🎯 总结

1. **check-i18n.ps1 只扫描中文和日文字面量**
2. **英文资源键 `"ERR_XXX"` 不会被扫描**
3. **不需要常量类来避免扫描**
4. **如果程序员说被标红，请让他展示具体错误**
5. **可能是他看错了，或者有其他中文在附近**

## 🧪 验证命令

```powershell
# 创建测试文件（如上）
# 运行扫描
pwsh ./scripts/check-i18n.ps1 --severity ERROR

# 结果：只报告中文/日文字面量，英文资源键不报告
```

如果程序员坚持说被扫描了，请他：
1. 展示具体的违规行号
2. 展示那一行的完整代码
3. 可能是那一行附近有其他中文
