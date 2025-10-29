using BobCrm.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace BobCrm.Api.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(DbContext db)
    {
        try { await db.Database.EnsureCreatedAsync(); } catch { }
        try { await db.Set<Customer>().CountAsync(); }
        catch
        {
            try { var creator = db.Database.GetService<IRelationalDatabaseCreator>(); await creator.CreateTablesAsync(); } catch { }
        }

        var isNpgsql = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        if (isNpgsql)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS \"LocalizationLanguages\" (\"Id\" SERIAL PRIMARY KEY, \"Code\" text NOT NULL, \"NativeName\" text NOT NULL);");
            }
            catch { }
        }

        if (!await db.Set<Customer>().AnyAsync())
        {
            await db.Set<Customer>().AddRangeAsync(
                new Customer { Code = "C001", Name = "LBL_CUSTOMER", Version = 1 },
                new Customer { Code = "C002", Name = "LBL_CUSTOMER", Version = 1 }
            );
        }

        if (!await db.Set<FieldDefinition>().AnyAsync())
        {
            await db.Set<FieldDefinition>().AddRangeAsync(
                new FieldDefinition
                {
                    Key = "email",
                    DisplayName = "LBL_EMAIL",
                    DataType = "email",
                    Required = true,
                    Validation = @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    DefaultValue = "",
                    Tags = "[\"常用\"]",
                    Actions = "[{\"icon\":\"mail\",\"titleKey\":\"ACT_MAIL\",\"type\":\"click\",\"action\":\"mailto\"}]"
                },
                new FieldDefinition
                {
                    Key = "link",
                    DisplayName = "LBL_LINK",
                    DataType = "link",
                    Required = false,
                    DefaultValue = "https://example.com",
                    Tags = "[\"常用\"]",
                    Actions = "[{\"icon\":\"link\",\"titleKey\":\"ACT_OPEN\",\"action\":\"openLink\"},{\"icon\":\"copy\",\"titleKey\":\"ACT_COPY\",\"action\":\"copy\"}]"
                },
                new FieldDefinition
                {
                    Key = "file",
                    DisplayName = "LBL_FILE_PATH",
                    DataType = "file",
                    Required = false,
                    DefaultValue = "C:/data/readme.txt",
                    Tags = "[\"常用\"]",
                    Actions = "[{\"icon\":\"copy\",\"titleKey\":\"ACT_COPY_PATH\",\"action\":\"copy\"}]"
                },
                new FieldDefinition
                {
                    Key = "rds",
                    DisplayName = "LBL_RDS",
                    DataType = "rds",
                    Required = false,
                    DefaultValue = null,
                    Tags = "[\"远程\"]",
                    Actions = "[{\"icon\":\"download\",\"titleKey\":\"ACT_DOWNLOAD_RDP\",\"action\":\"downloadRdp\"}]"
                },
                new FieldDefinition
                {
                    Key = "priority",
                    DisplayName = "LBL_PRIORITY",
                    DataType = "number",
                    Required = false,
                    DefaultValue = "1",
                    Tags = "[\"扩展\"]",
                    Actions = "[]"
                }
            );
        }

        if (!await db.Set<LocalizationLanguage>().AnyAsync())
        {
            await db.Set<LocalizationLanguage>().AddRangeAsync(
                new LocalizationLanguage { Code = "ja", NativeName = "日本語" },
                new LocalizationLanguage { Code = "zh", NativeName = "中文" },
                new LocalizationLanguage { Code = "en", NativeName = "English" }
            );
        }

        if (!await db.Set<LocalizationResource>().AnyAsync())
        {
            await db.Set<LocalizationResource>().AddRangeAsync(
                new LocalizationResource { Key = "LBL_CUSTOMER", ZH = "客户", JA = "顧客", EN = "Customer" },
                new LocalizationResource { Key = "LBL_EMAIL", ZH = "邮箱", JA = "メール", EN = "Email" },
                new LocalizationResource { Key = "BTN_SAVE", ZH = "保存", JA = "保存", EN = "Save" },
                new LocalizationResource { Key = "LBL_LINK", ZH = "链接", JA = "リンク", EN = "Link" },
                new LocalizationResource { Key = "LBL_FILE_PATH", ZH = "文件路径", JA = "ファイルパス", EN = "File Path" },
                new LocalizationResource { Key = "LBL_RDS", ZH = "RDS连接", JA = "RDS接続", EN = "RDS" },
                new LocalizationResource { Key = "LBL_PRIORITY", ZH = "优先级", JA = "優先度", EN = "Priority" },
                new LocalizationResource { Key = "ACT_MAIL", ZH = "发邮件", JA = "メール送信", EN = "Mail" },
                new LocalizationResource { Key = "ACT_OPEN", ZH = "打开", JA = "開く", EN = "Open" },
                new LocalizationResource { Key = "ACT_COPY", ZH = "复制", JA = "コピー", EN = "Copy" },
                new LocalizationResource { Key = "ACT_COPY_PATH", ZH = "复制路径", JA = "パスをコピー", EN = "Copy Path" },
                new LocalizationResource { Key = "ACT_DOWNLOAD_RDP", ZH = "下载RDP", JA = "RDPダウンロード", EN = "Download RDP" },
                // UI common keys used by Blazor app
                new LocalizationResource { Key = "MENU_CUSTOMERS", ZH = "客户", JA = "顧客", EN = "Customers" },
                new LocalizationResource { Key = "COL_CODE", ZH = "编码", JA = "コード", EN = "Code" },
                new LocalizationResource { Key = "COL_NAME", ZH = "名称", JA = "名称", EN = "Name" },
                new LocalizationResource { Key = "COL_ACTIONS", ZH = "操作", JA = "操作", EN = "Actions" },
                new LocalizationResource { Key = "BTN_DETAIL", ZH = "详情", JA = "詳細", EN = "Detail" },
                new LocalizationResource { Key = "LBL_LOGIN_TITLE", ZH = "登录", JA = "ログイン", EN = "Login" },
                new LocalizationResource { Key = "LBL_SETUP", ZH = "初始化设置", JA = "初期設定", EN = "Setup" },
                new LocalizationResource { Key = "LBL_API_BASE", ZH = "API 基础地址", JA = "API ベースURL", EN = "API Base URL" },
                new LocalizationResource { Key = "LBL_LANG", ZH = "语言", JA = "言語", EN = "Language" },
                new LocalizationResource { Key = "LBL_ADMIN_ACCOUNT", ZH = "管理员账号", JA = "管理者アカウント", EN = "Admin Account" },
                new LocalizationResource { Key = "LBL_ADMIN_USERNAME", ZH = "管理员用户名", JA = "管理者ユーザー名", EN = "Admin Username" },
                new LocalizationResource { Key = "LBL_ADMIN_EMAIL", ZH = "管理员邮箱", JA = "管理者メール", EN = "Admin Email" },
                new LocalizationResource { Key = "LBL_PASSWORD", ZH = "密码", JA = "パスワード", EN = "Password" },
                new LocalizationResource { Key = "LBL_ADMIN", ZH = "管理员", JA = "管理者", EN = "Admin" },
                new LocalizationResource { Key = "LBL_SEARCH", ZH = "搜索", JA = "検索", EN = "Search" },
                new LocalizationResource { Key = "BTN_LOGOUT", ZH = "退出", JA = "ログアウト", EN = "Logout" },
                new LocalizationResource { Key = "LBL_LOAD_FAILED", ZH = "加载失败", JA = "読み込み失敗", EN = "Load failed" },
                new LocalizationResource { Key = "PH_API_BASE_EXAMPLE", ZH = "例如 http://localhost:5200 或 https://your.api", JA = "例 http://localhost:5200 または https://your.api", EN = "e.g. http://localhost:5200 or https://your.api" },
                new LocalizationResource { Key = "LBL_API_BASE_HINT", ZH = "为空则使用内置默认地址（appsettings 的 Api:BaseUrl）。建议填写实际后端地址。", JA = "未指定の場合は既定のアドレス（appsettings の Api:BaseUrl）を使用します。実際の API を推奨。", EN = "If empty, uses default (appsettings Api:BaseUrl). Prefer your real API." },
                new LocalizationResource { Key = "BTN_SAVE_AND_GO_LOGIN", ZH = "保存并进入登录", JA = "保存してログインへ", EN = "Save and go to Login" },
                new LocalizationResource { Key = "BTN_GO_LOGIN", ZH = "已有配置，去登录", JA = "設定済みならログインへ", EN = "Go to Login" },
                new LocalizationResource { Key = "ERR_LOGIN_FAILED", ZH = "登录失败", JA = "ログイン失敗", EN = "Login failed" },
                new LocalizationResource { Key = "ERR_PARSE_RESPONSE", ZH = "无法解析服务器响应", JA = "サーバー応答を解析できません", EN = "Unable to parse server response" },
                new LocalizationResource { Key = "LBL_ACTIVATE_TITLE", ZH = "激活账户", JA = "アカウント有効化", EN = "Activate Account" },
                new LocalizationResource { Key = "BTN_ACTIVATE", ZH = "激活", JA = "有効化", EN = "Activate" },
                new LocalizationResource { Key = "ERR_ACTIVATE_FILL", ZH = "请填写 UserId 和 Code", JA = "UserId と Code を入力してください", EN = "Please fill UserId and Code" },
                new LocalizationResource { Key = "MSG_ACTIVATE_SUCCESS", ZH = "激活成功，请前往登录", JA = "有効化に成功。ログインへ", EN = "Activated successfully, please log in" },
                new LocalizationResource { Key = "ERR_ACTIVATE_FAILED", ZH = "激活失败", JA = "有効化に失敗", EN = "Activation failed" },
                new LocalizationResource { Key = "LBL_REGISTER", ZH = "注册", JA = "登録", EN = "Register" },
                new LocalizationResource { Key = "BTN_REGISTER", ZH = "注册", JA = "登録", EN = "Register" },
                new LocalizationResource { Key = "LBL_USERNAME", ZH = "用户名", JA = "ユーザー名", EN = "Username" },
                new LocalizationResource { Key = "MSG_REGISTER_SUCCESS", ZH = "注册成功，请进行邮箱激活（开发环境查看 API 控制台）", JA = "登録成功。メール認証を実施してください（開発環境は API コンソール参照）", EN = "Registered. Please verify by email (check API console in dev)." },
                new LocalizationResource { Key = "TXT_REGISTER_HELP", ZH = "注册成功后，请在 API 控制台查看激活链接，或前往激活页面手动激活。", JA = "登録後、APIコンソールの有効化リンクを確認するか、アクティベートページで手動有効化してください。", EN = "After registering, check activation link in API console or activate manually on the activate page." },
                new LocalizationResource { Key = "LBL_HOME", ZH = "首页", JA = "ホーム", EN = "Home" },
                new LocalizationResource { Key = "LBL_WELCOME", ZH = "欢迎使用 BobCRM", JA = "BobCRM へようこそ", EN = "Welcome to BobCRM" },
                new LocalizationResource { Key = "LBL_USER_ID", ZH = "用户ID", JA = "ユーザーID", EN = "User ID" },
                new LocalizationResource { Key = "LBL_CODE", ZH = "代码", JA = "コード", EN = "Code" },
                // Error/validation keys
                new LocalizationResource { Key = "ERR_EMAIL_NOT_CONFIRMED", ZH = "邮箱未激活", JA = "メール未確認", EN = "Email not confirmed" },
                new LocalizationResource { Key = "ERR_CONCURRENCY", ZH = "版本冲突", JA = "バージョン競合", EN = "Version mismatch" },
                new LocalizationResource { Key = "ERR_FIELD_KEY_REQUIRED", ZH = "字段键必填", JA = "フィールドキーは必須", EN = "Field key required" },
                new LocalizationResource { Key = "ERR_UNKNOWN_FIELD", ZH = "未知字段", JA = "不明なフィールド", EN = "Unknown field" },
                new LocalizationResource { Key = "ERR_LAYOUT_BODY_REQUIRED", ZH = "布局内容不能为空", JA = "レイアウト内容は必須", EN = "Layout body required" },
                new LocalizationResource { Key = "ERR_TAGS_REQUIRED", ZH = "标签不能为空", JA = "タグは必須", EN = "Tags required" },
                new LocalizationResource { Key = "ERR_BUSINESS_VALIDATION_FAILED", ZH = "业务校验失败", JA = "ビジネス検証に失敗", EN = "Business validation failed" },
                new LocalizationResource { Key = "ERR_VALIDATION_FAILED", ZH = "校验失败", JA = "検証に失敗", EN = "Validation failed" },
                new LocalizationResource { Key = "ERR_PERSISTENCE_VALIDATION_FAILED", ZH = "持久化校验失败", JA = "永続化検証に失敗", EN = "Persistence validation failed" },
                new LocalizationResource { Key = "MSG_SETUP_SAVED", ZH = "初始化已保存，正在进入首页", JA = "初期設定を保存しました。ホームへ移動します", EN = "Setup saved. Redirecting to home" },
                new LocalizationResource { Key = "ERR_SETUP_SAVE_FAILED", ZH = "初始化保存失败", JA = "初期設定の保存に失敗しました", EN = "Setup save failed" },
                // Detail templates
                new LocalizationResource { Key = "VAL_REQUIRED", ZH = "{0} 为必填项", JA = "{0} は必須です", EN = "{0} is required" },
                new LocalizationResource { Key = "VAL_INVALID_PATTERN", ZH = "{0} 的验证规则无效", JA = "{0} の検証パターンが無効です", EN = "Invalid validation pattern for {0}" },
                new LocalizationResource { Key = "VAL_INVALID_FORMAT", ZH = "{0} 格式不正确", JA = "{0} の形式が正しくありません", EN = "{0} format invalid" },
                new LocalizationResource { Key = "VAL_UNKNOWN_FIELD", ZH = "未知字段: {0}", JA = "不明なフィールド: {0}", EN = "Unknown field: {0}" },
                new LocalizationResource { Key = "VAL_FIELDS_REQUIRED", ZH = "必须提供字段", JA = "フィールドは必須です", EN = "Fields are required" }
            );
        }
        else
        {
            // Best-effort backfill: ensure critical UI keys exist
            void Ensure(string key, string zh, string ja, string en)
            {
                var set = db.Set<LocalizationResource>();
                if (!set.Any(x => x.Key == key)) set.Add(new LocalizationResource { Key = key, ZH = zh, JA = ja, EN = en });
            }
            Ensure("MENU_CUSTOMERS", "客户", "顧客", "Customers");
            Ensure("COL_CODE", "编码", "コード", "Code");
            Ensure("COL_NAME", "名称", "名称", "Name");
            Ensure("COL_ACTIONS", "操作", "操作", "Actions");
            Ensure("BTN_DETAIL", "详情", "詳細", "Detail");
            Ensure("LBL_LOGIN_TITLE", "登录", "ログイン", "Login");
            Ensure("LBL_SETUP", "初始化设置", "初期設定", "Setup");
            Ensure("LBL_API_BASE", "API 基础地址", "API ベースURL", "API Base URL");
            Ensure("LBL_LANG", "语言", "言語", "Language");
            Ensure("LBL_ADMIN_ACCOUNT", "管理员账号", "管理者アカウント", "Admin Account");
            Ensure("LBL_ADMIN_USERNAME", "管理员用户名", "管理者ユーザー名", "Admin Username");
            Ensure("LBL_ADMIN_EMAIL", "管理员邮箱", "管理者メール", "Admin Email");
            Ensure("LBL_PASSWORD", "密码", "パスワード", "Password");
            Ensure("LBL_ADMIN", "管理员", "管理者", "Admin");
            Ensure("LBL_SEARCH", "搜索", "検索", "Search");
            Ensure("BTN_LOGOUT", "退出", "ログアウト", "Logout");
            Ensure("ERR_EMAIL_NOT_CONFIRMED", "邮箱未激活", "メール未確認", "Email not confirmed");
            Ensure("ERR_CONCURRENCY", "版本冲突", "バージョン競合", "Version mismatch");
            Ensure("ERR_FIELD_KEY_REQUIRED", "字段键必填", "フィールドキーは必須", "Field key required");
            Ensure("ERR_UNKNOWN_FIELD", "未知字段", "不明なフィールド", "Unknown field");
            Ensure("ERR_LAYOUT_BODY_REQUIRED", "布局内容不能为空", "レイアウト内容は必須", "Layout body required");
            Ensure("ERR_TAGS_REQUIRED", "标签不能为空", "タグは必須", "Tags required");
            Ensure("ERR_BUSINESS_VALIDATION_FAILED", "业务校验失败", "ビジネス検証に失敗", "Business validation failed");
            Ensure("ERR_VALIDATION_FAILED", "校验失败", "検証に失敗", "Validation failed");
            Ensure("ERR_PERSISTENCE_VALIDATION_FAILED", "持久化校验失败", "永続化検証に失敗", "Persistence validation failed");
            Ensure("MSG_SETUP_SAVED", "初始化已保存，正在进入首页", "初期設定を保存しました。ホームへ移動します", "Setup saved. Redirecting to home");
            Ensure("ERR_SETUP_SAVE_FAILED", "初始化保存失败", "初期設定の保存に失敗しました", "Setup save failed");
            Ensure("VAL_REQUIRED", "{0} 为必填项", "{0} は必須です", "{0} is required");
            Ensure("VAL_INVALID_PATTERN", "{0} 的验证规则无效", "{0} の検証パターンが無効です", "Invalid validation pattern for {0}");
            Ensure("VAL_INVALID_FORMAT", "{0} 格式不正确", "{0} の形式が正しくありません", "{0} format invalid");
            Ensure("VAL_UNKNOWN_FIELD", "未知字段: {0}", "不明なフィールド: {0}", "Unknown field: {0}");
            Ensure("VAL_FIELDS_REQUIRED", "必须提供字段", "フィールドは必須です", "Fields are required");
        }

        await db.SaveChangesAsync();

        if (isNpgsql)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_fieldvalues_value_gin ON \"FieldValues\" USING GIN (\"Value\");");
                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_fielddefinitions_tags_gin ON \"FieldDefinitions\" USING GIN (\"Tags\");");
                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_fielddefinitions_actions_gin ON \"FieldDefinitions\" USING GIN (\"Actions\");");
                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_userlayouts_layoutjson_gin ON \"UserLayouts\" USING GIN (\"LayoutJson\");");
                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_fieldvalues_customer_field ON \"FieldValues\" (\"CustomerId\", \"FieldDefinitionId\");");
            }
            catch { }
        }
    }

    public static async Task RecreateAsync(DbContext db)
    {
        try { await db.Database.EnsureDeletedAsync(); } catch { }
        await InitializeAsync(db);
    }
}
