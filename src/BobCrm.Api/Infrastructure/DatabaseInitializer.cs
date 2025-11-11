using BobCrm.Api.Domain;
using BobCrm.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(DbContext db)
    {
        Console.WriteLine("[DatabaseInitializer] Ensuring database is created using EnsureCreatedAsync");
        await db.Database.EnsureCreatedAsync();
        if (db is AppDbContext appDbContext)
        {
            var synchronizer = new EntityDefinitionSynchronizer(appDbContext, NullLogger<EntityDefinitionSynchronizer>.Instance);
            await synchronizer.SyncSystemEntitiesAsync();
        }
        var isNpgsql = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        // 初始化期间禁用查询过滤器，避免权限检查干扰
        if (!await db.Set<Customer>().IgnoreQueryFilters().AnyAsync())
        {
            var customer1 = new Customer { Code = "C001", Name = "示例客户A", Version = 1 };
            var customer2 = new Customer { Code = "C002", Name = "示例客户B", Version = 1 };
            await db.Set<Customer>().AddRangeAsync(customer1, customer2);
            await db.SaveChangesAsync();
            // Add localized names
            await db.Set<CustomerLocalization>().AddRangeAsync(
                new CustomerLocalization { CustomerId = customer1.Id, Language = "zh", Name = "示例客户A" },
                new CustomerLocalization { CustomerId = customer1.Id, Language = "ja", Name = "サンプル顧客A" },
                new CustomerLocalization { CustomerId = customer1.Id, Language = "en", Name = "Sample Customer A" },
                new CustomerLocalization { CustomerId = customer2.Id, Language = "zh", Name = "示例客户B" },
                new CustomerLocalization { CustomerId = customer2.Id, Language = "ja", Name = "サンプル顧客B" },
                new CustomerLocalization { CustomerId = customer2.Id, Language = "en", Name = "Sample Customer B" }
            );
        }
        if (!await db.Set<SystemSettings>().AnyAsync())
        {
            await db.Set<SystemSettings>().AddAsync(new SystemSettings
            {
                CompanyName = "OneCRM",
                DefaultTheme = "calm-light",
                DefaultPrimaryColor = "#739FD6",
                DefaultLanguage = "ja",
                DefaultHomeRoute = "/",
                DefaultNavMode = NavDisplayModes.IconText,
                TimeZoneId = "Asia/Tokyo",
                AllowSelfRegistration = false,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        if (!await db.Set<FieldDefinition>().IgnoreQueryFilters().AnyAsync())
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
        if (!await db.Set<LocalizationLanguage>().IgnoreQueryFilters().AnyAsync())
        {
            await db.Set<LocalizationLanguage>().AddRangeAsync(
                new LocalizationLanguage { Code = "ja", NativeName = "日本語" },
                new LocalizationLanguage { Code = "zh", NativeName = "中文" },
                new LocalizationLanguage { Code = "en", NativeName = "English" }
            );
        }
        // EntityDefinition 自动同步已在 Program.cs 中由 EntityDefinitionSynchronizer 处理
        if (!await db.Set<LocalizationResource>().IgnoreQueryFilters().AnyAsync())
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
                new LocalizationResource { Key = "MENU_DASHBOARD", ZH = "仪表盘", JA = "ダッシュボード", EN = "Dashboard" },
                new LocalizationResource { Key = "MENU_CUSTOMERS", ZH = "客户", JA = "顧客", EN = "Customers" },
                new LocalizationResource { Key = "MENU_ENTITY", ZH = "实体定义", JA = "エンティティ定義", EN = "Entities" },
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
                new LocalizationResource { Key = "LBL_NOTIFICATIONS", ZH = "通知中心", JA = "通知センター", EN = "Notifications" },
                new LocalizationResource { Key = "TXT_NOTIFICATION_SUBTITLE", ZH = "最近的互动提醒", JA = "最近のインサイト", EN = "Latest interaction updates" },
                new LocalizationResource { Key = "BTN_MARK_ALL_READ", ZH = "全部标记已读", JA = "すべて既読にする", EN = "Mark all read" },
                new LocalizationResource { Key = "BTN_WORK_BAR", ZH = "工作栏", JA = "ワークバー", EN = "Work Bar" },
                new LocalizationResource { Key = "BTN_TOOL_BAR", ZH = "工具条", JA = "ツールバー", EN = "Tool Bar" },
                new LocalizationResource { Key = "BTN_BULK_BAR", ZH = "批量条", JA = "一括バー", EN = "Bulk Bar" },
                new LocalizationResource { Key = "TXT_NOTIFICATION_EMPTY", ZH = "暂无新的通知", JA = "新しい通知はありません", EN = "You're all caught up" },
                new LocalizationResource { Key = "BTN_DISMISS", ZH = "忽略", JA = "閉じる", EN = "Dismiss" },
                new LocalizationResource { Key = "BTN_VIEW_DETAIL", ZH = "查看详情", JA = "詳細を見る", EN = "View detail" },
                new LocalizationResource { Key = "LBL_NOTIF_APPROVAL", ZH = "审批通过", JA = "承認完了", EN = "Approval completed" },
                new LocalizationResource { Key = "TXT_NOTIF_APPROVAL_DESC", ZH = "募资申请获批，请准备资料", JA = "資金調達申請が承認されました。資料をご準備ください", EN = "Funding request approved—prep the package." },
                new LocalizationResource { Key = "LBL_NOTIF_IMPORT", ZH = "导入成功", JA = "インポート成功", EN = "Import completed" },
                new LocalizationResource { Key = "TXT_NOTIF_IMPORT_DESC", ZH = "客户资料已经导入 CRM", JA = "顧客データを CRM に取り込みました", EN = "Customer data has been imported into CRM." },
                new LocalizationResource { Key = "LBL_NOTIF_INCIDENT", ZH = "重点事件", JA = "重要インシデント", EN = "Critical incident" },
                new LocalizationResource { Key = "TXT_NOTIF_INCIDENT_DESC", ZH = "SLA 即将到期，请快速处理", JA = "SLA 期限が迫っています。対応してください。", EN = "SLA is about to expire—please act quickly." },
                new LocalizationResource { Key = "TXT_TIME_5M_AGO", ZH = "5 分钟前", JA = "5 分前", EN = "5m ago" },
                new LocalizationResource { Key = "TXT_TIME_18M_AGO", ZH = "18 分钟前", JA = "18 分前", EN = "18m ago" },
                new LocalizationResource { Key = "TXT_TIME_1H_AGO", ZH = "1 小时前", JA = "1 時間前", EN = "1h ago" },
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
                new LocalizationResource { Key = "LBL_USERNAME_OR_EMAIL", ZH = "用户名或邮箱", JA = "ユーザー名またはメール", EN = "Username or Email" },
                new LocalizationResource { Key = "ERR_USERNAME_REQUIRED", ZH = "用户名不能为空", JA = "ユーザー名は必須です", EN = "Username is required" },
                new LocalizationResource { Key = "ERR_PASSWORD_REQUIRED", ZH = "密码不能为空", JA = "パスワードは必須です", EN = "Password is required" },
                new LocalizationResource { Key = "MSG_REGISTER_SUCCESS", ZH = "注册成功，请进行邮箱激活（开发环境查看 API 控制台）", JA = "登録成功。メール認証を実施してください（開発環境は API コンソール参照）", EN = "Registered. Please verify by email (check API console in dev)." },
                new LocalizationResource { Key = "TXT_RAIL_TAGLINE", ZH = "体验中枢", JA = "エクスペリエンスハブ", EN = "Experience Hub" },
                new LocalizationResource { Key = "TXT_HEADER_SUBTITLE", ZH = "洞察、协作与成长的一体化入口", JA = "洞察と協働をつなぐスマートハブ", EN = "Unified hub for insight and collaboration" },
                new LocalizationResource { Key = "TXT_DASH_EYEBROW", ZH = "工作总览", JA = "ワークスペース概況", EN = "Workspace Snapshot" },
                new LocalizationResource { Key = "TXT_DASH_TITLE", ZH = "客户体验总览", JA = "顧客体験の概況", EN = "Customer Experience Overview" },
                new LocalizationResource { Key = "TXT_DASH_SUBTITLE", ZH = "快速掌握客户、项目与团队的关键脉搏。", JA = "顧客・案件・チームの脈をワンビューで把握。", EN = "See key signals from customers, projects, and teams in one view." },
                new LocalizationResource { Key = "LBL_DASH_SEGMENTS", ZH = "客户分层", JA = "セグメント", EN = "Customer Segments" },
                new LocalizationResource { Key = "LBL_DASH_ACTIVITY", ZH = "动态流", JA = "アクティビティ", EN = "Activity Stream" },
                new LocalizationResource { Key = "TXT_DASH_ACTIVITY_HINT", ZH = "最近 24 小时的关键事件。", JA = "直近24時間の主要イベント。", EN = "Key events in the last 24 hours." },
                new LocalizationResource { Key = "LBL_DASH_NEXT_STEPS", ZH = "下一步行动", JA = "次のアクション", EN = "Next Actions" },
                new LocalizationResource { Key = "LBL_DASH_CUSTOMERS", ZH = "客户总数", JA = "顧客総数", EN = "Customers" },
                new LocalizationResource { Key = "LBL_DASH_PROJECTS", ZH = "项目", JA = "プロジェクト", EN = "Projects" },
                new LocalizationResource { Key = "LBL_DASH_TOUCHES", ZH = "互动次数", JA = "タッチポイント", EN = "Touches" },
                new LocalizationResource { Key = "LBL_DASH_SAT", ZH = "满意度", JA = "満足度", EN = "Satisfaction" }, new LocalizationResource { Key = "LBL_HOME", ZH = "首页", JA = "ホーム", EN = "Home" },
                new LocalizationResource { Key = "LBL_WELCOME", ZH = "欢迎使用 OneCRM", JA = "OneCRM へようこそ", EN = "Welcome to OneCRM" },
                new LocalizationResource { Key = "LBL_USER_ID", ZH = "用户ID", JA = "ユーザーID", EN = "User ID" },
                new LocalizationResource { Key = "LBL_CODE", ZH = "代码", JA = "コード", EN = "Code" },
                new LocalizationResource { Key = "LBL_CUSTOMER_DETAIL", ZH = "客户详情", JA = "顧客詳細", EN = "Customer Detail" },
                new LocalizationResource { Key = "LBL_LOADING", ZH = "加载中", JA = "読み込み中", EN = "Loading" },
                new LocalizationResource { Key = "LBL_FIELDS", ZH = "字段", JA = "フィールド", EN = "Fields" },
                new LocalizationResource { Key = "BTN_BACK", ZH = "返回", JA = "戻る", EN = "Back" },
                new LocalizationResource { Key = "LBL_NOT_FOUND", ZH = "未找到", JA = "見つかりません", EN = "Not Found" },
                new LocalizationResource { Key = "LBL_NO_FIELDS", ZH = "无字段", JA = "フィールドなし", EN = "No fields" },
                new LocalizationResource { Key = "LBL_PLEASE_SELECT_CUSTOMER", ZH = "请选择客户", JA = "顧客を選択してください", EN = "Please select a customer" },
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
                new LocalizationResource { Key = "VAL_FIELDS_REQUIRED", ZH = "必须提供字段", JA = "フィールドは必須です", EN = "Fields are required" },
                // New customer keys
                new LocalizationResource { Key = "BTN_NEW_CUSTOMER", ZH = "新建客户", JA = "新規顧客", EN = "New Customer" },
                new LocalizationResource { Key = "LBL_NEW_CUSTOMER", ZH = "新建客户", JA = "新規顧客", EN = "New Customer" },
                new LocalizationResource { Key = "LBL_CUSTOMER_CODE_HINT", ZH = "唯一标识，例如: C001", JA = "一意の識別子、例: C001", EN = "Unique identifier, e.g. C001" },
                new LocalizationResource { Key = "BTN_CANCEL", ZH = "取消", JA = "キャンセル", EN = "Cancel" },
                new LocalizationResource { Key = "LBL_SAVING", ZH = "保存中", JA = "保存中", EN = "Saving" },
                new LocalizationResource { Key = "LBL_SAVE_FAILED", ZH = "保存失败", JA = "保存に失敗", EN = "Save failed" },
                new LocalizationResource { Key = "ERR_CUSTOMER_CODE_REQUIRED", ZH = "客户编码不能为空", JA = "顧客コードは必須です", EN = "Customer code is required" },
                new LocalizationResource { Key = "ERR_CUSTOMER_NAME_REQUIRED", ZH = "客户名称不能为空", JA = "顧客名は必須です", EN = "Customer name is required" },
                new LocalizationResource { Key = "ERR_CUSTOMER_CODE_EXISTS", ZH = "客户编码已存在", JA = "顧客コードは既に存在します", EN = "Customer code already exists" },
                // View mode keys
                new LocalizationResource { Key = "MODE_BROWSE", ZH = "浏览", JA = "閲覧", EN = "Browse" },
                new LocalizationResource { Key = "MODE_EDIT", ZH = "编辑", JA = "編集", EN = "Edit" },
                new LocalizationResource { Key = "MODE_DESIGN", ZH = "设计", JA = "デザイン", EN = "Design" },
                new LocalizationResource { Key = "BTN_SAVE_LAYOUT", ZH = "保存布局", JA = "レイアウトを保存", EN = "Save Layout" },
                new LocalizationResource { Key = "BTN_GENERATE_LAYOUT", ZH = "生成布局", JA = "レイアウトを生成", EN = "Generate Layout" },
                new LocalizationResource { Key = "LBL_DESIGN_MODE_TITLE", ZH = "设计模式", JA = "デザインモード", EN = "Design Mode" },
                new LocalizationResource { Key = "LBL_DESIGN_MODE_DESC", ZH = "在此模式下可以调整字段布局，拖拽字段块进行排列", JA = "このモードでフィールドのレイアウトを調整できます", EN = "Adjust field layout in this mode" },
                // Designer keys
                new LocalizationResource { Key = "BTN_EXIT_DESIGN", ZH = "退出设计", JA = "デザインを終了", EN = "Exit Design" },
                new LocalizationResource { Key = "LBL_COMPONENTS", ZH = "组件", JA = "コンポーネント", EN = "Components" },
                new LocalizationResource { Key = "LBL_BASIC_COMPONENTS", ZH = "基础组件", JA = "基本コンポーネント", EN = "Basic Components" },
                new LocalizationResource { Key = "LBL_LAYOUT_COMPONENTS", ZH = "布局组件", JA = "レイアウトコンポーネント", EN = "Layout Components" },
                new LocalizationResource { Key = "LBL_TEXTBOX", ZH = "文本框", JA = "テキストボックス", EN = "Textbox" },
                new LocalizationResource { Key = "LBL_NUMBER", ZH = "数字框", JA = "数値", EN = "Number" },
                new LocalizationResource { Key = "LBL_SELECT", ZH = "下拉选择", JA = "選択", EN = "Select" },
                new LocalizationResource { Key = "LBL_CHECKBOX", ZH = "复选框", JA = "チェックボックス", EN = "Checkbox" },
                new LocalizationResource { Key = "LBL_RADIO", ZH = "单选按钮", JA = "ラジオボタン", EN = "Radio" },
                new LocalizationResource { Key = "LBL_TEXTAREA", ZH = "文本区域", JA = "テキストエリア", EN = "Textarea" },
                new LocalizationResource { Key = "LBL_BUTTON", ZH = "按钮", JA = "ボタン", EN = "Button" },
                new LocalizationResource { Key = "LBL_LABEL", ZH = "标签", JA = "ラベル", EN = "Label" },
                new LocalizationResource { Key = "LBL_CALENDAR", ZH = "日历", JA = "カレンダー", EN = "Calendar" },
                new LocalizationResource { Key = "LBL_LISTBOX", ZH = "列表框", JA = "リストボックス", EN = "Listbox" },
                new LocalizationResource { Key = "LBL_SECTION", ZH = "分组", JA = "セクション", EN = "Section" },
                new LocalizationResource { Key = "LBL_PANEL", ZH = "面板", JA = "パネル", EN = "Panel" },
                new LocalizationResource { Key = "LBL_GRID", ZH = "网格", JA = "グリッド", EN = "Grid" },
                new LocalizationResource { Key = "LBL_FRAME", ZH = "框架", JA = "フレーム", EN = "Frame" },
                new LocalizationResource { Key = "LBL_TABBOX", ZH = "标签容器", JA = "タブボックス", EN = "Tabbox" },
                new LocalizationResource { Key = "LBL_TAB", ZH = "标签页", JA = "タブ", EN = "Tab" },
                new LocalizationResource { Key = "LBL_DRAG_COMPONENT_HERE", ZH = "拖拽组件到这里", JA = "コンポーネントをここにドラッグ", EN = "Drag component here" },
                new LocalizationResource { Key = "LBL_PROPERTIES", ZH = "属性", JA = "プロパティ", EN = "Properties" },
                new LocalizationResource { Key = "LBL_TEMPLATE_PROPERTIES", ZH = "模板属性", JA = "テンプレートプロパティ", EN = "Template Properties" },
                new LocalizationResource { Key = "LBL_WIDGET_PROPERTIES", ZH = "组件属性", JA = "コンポーネントプロパティ", EN = "Widget Properties" },
                new LocalizationResource { Key = "PROP_CODE", ZH = "代码/名称", JA = "コード/名前", EN = "Code/Name" },
                new LocalizationResource { Key = "ERR_CODE_NOT_UNIQUE", ZH = "此代码已被使用，请输入唯一的代码", JA = "このコードは既に使用されています。一意のコードを入力してください", EN = "This code is already in use. Please enter a unique code" },
                new LocalizationResource { Key = "PROP_GROUP_BASIC", ZH = "基本", JA = "基本", EN = "Basic" },
                new LocalizationResource { Key = "LBL_TEMPLATE", ZH = "模板", JA = "テンプレート", EN = "Template" },
                new LocalizationResource { Key = "LBL_TEMPLATE_NAME", ZH = "模板名称", JA = "テンプレート名", EN = "Template Name" },
                new LocalizationResource { Key = "LBL_USER_TEMPLATE", ZH = "用户模板", JA = "ユーザーテンプレート", EN = "User Template" },
                new LocalizationResource { Key = "LBL_DEFAULT_TEMPLATE", ZH = "默认模板", JA = "デフォルトテンプレート", EN = "Default Template" },
                new LocalizationResource { Key = "LBL_ENTER_TEMPLATE_NAME", ZH = "请输入模板名称", JA = "テンプレート名を入力してください", EN = "Enter template name" },
                new LocalizationResource { Key = "LBL_ENTITY_TYPE", ZH = "实体类型", JA = "エンティティタイプ", EN = "Entity Type" },
                new LocalizationResource { Key = "LBL_SELECT_ENTITY_TYPE", ZH = "请选择实体类型", JA = "エンティティタイプを選択", EN = "Select entity type" },
                new LocalizationResource { Key = "LBL_ENTITY_TYPE_HINT", ZH = "此模板将用于哪种实体的数据展示", JA = "このテンプレートはどのエンティティのデータ表示に使用されますか", EN = "Which entity will this template be used for" },
                new LocalizationResource { Key = "LBL_ENTITY_TYPE_LOCKED", ZH = "实体类型已锁定，不可修改", JA = "エンティティタイプはロック済み、変更不可", EN = "Entity type is locked and cannot be changed" },
                new LocalizationResource { Key = "LBL_ENTITY_TYPE_NOT_SET", ZH = "未设置", JA = "未設定", EN = "Not Set" },
                new LocalizationResource { Key = "LBL_CHANGE_ENTITY_TYPE", ZH = "更换实体类型", JA = "エンティティタイプを変更", EN = "Change Entity Type" },
                new LocalizationResource { Key = "LBL_NO_AVAILABLE_ENTITIES", ZH = "暂无可用实体类型", JA = "利用可能なエンティティタイプがありません", EN = "No available entity types" },
                new LocalizationResource { Key = "LBL_CLICK_TO_SELECT_ENTITY", ZH = "点击选择实体类型", JA = "クリックしてエンティティタイプを選択", EN = "Click to select entity type" },
                new LocalizationResource { Key = "LBL_RETRY", ZH = "重试", JA = "再試行", EN = "Retry" },
                new LocalizationResource { Key = "LBL_SEARCH_PLACEHOLDER", ZH = "搜索...", JA = "検索...", EN = "Search..." },
                new LocalizationResource { Key = "LBL_SHOWING", ZH = "显示", JA = "表示中", EN = "Showing" },
                new LocalizationResource { Key = "LBL_ITEMS", ZH = "项", JA = "件", EN = "items" },
                new LocalizationResource { Key = "LBL_TEMPLATE_INFO_HINT", ZH = "点击画布背景可返回模板属性；点击组件可编辑组件属性", JA = "キャンバスの背景をクリックしてテンプレートプロパティに戻る；コンポーネントをクリックしてプロパティを編集", EN = "Click canvas background to return to template properties; Click component to edit properties" },
                new LocalizationResource { Key = "LBL_NEW_TEMPLATE", ZH = "新建模板", JA = "新しいテンプレート", EN = "New Template" },
                new LocalizationResource { Key = "LBL_COMPONENT_TYPE", ZH = "组件类型", JA = "コンポーネントタイプ", EN = "Component Type" },
                // Profile page keys
                new LocalizationResource { Key = "MENU_PROFILE", ZH = "个人中心", JA = "プロフィール", EN = "Profile" },
                new LocalizationResource { Key = "LBL_USER_INFORMATION", ZH = "用户信息", JA = "ユーザー情報", EN = "User Information" },
                new LocalizationResource { Key = "LBL_ROLE", ZH = "角色", JA = "役割", EN = "Role" },
                new LocalizationResource { Key = "LBL_CHANGE_PASSWORD", ZH = "修改密码", JA = "パスワード変更", EN = "Change Password" },
                new LocalizationResource { Key = "LBL_CURRENT_PASSWORD", ZH = "当前密码", JA = "現在のパスワード", EN = "Current Password" },
                new LocalizationResource { Key = "LBL_NEW_PASSWORD", ZH = "新密码", JA = "新しいパスワード", EN = "New Password" },
                new LocalizationResource { Key = "LBL_CONFIRM_PASSWORD", ZH = "确认密码", JA = "パスワード確認", EN = "Confirm Password" },
                new LocalizationResource { Key = "LBL_ENTER_CURRENT_PASSWORD", ZH = "请输入当前密码", JA = "現在のパスワードを入力してください", EN = "Enter current password" },
                new LocalizationResource { Key = "LBL_ENTER_NEW_PASSWORD", ZH = "请输入新密码", JA = "新しいパスワードを入力してください", EN = "Enter new password" },
                new LocalizationResource { Key = "LBL_ENTER_CONFIRM_PASSWORD", ZH = "请再次输入新密码", JA = "新しいパスワードを再入力してください", EN = "Re-enter new password" },
                new LocalizationResource { Key = "BTN_CHANGE_AVATAR", ZH = "更换头像", JA = "アバター変更", EN = "Change Avatar" },
                new LocalizationResource { Key = "BTN_LOGIN", ZH = "登录", JA = "ログイン", EN = "Login" },
                new LocalizationResource { Key = "MSG_CURRENT_PASSWORD_REQUIRED", ZH = "请输入当前密码", JA = "現在のパスワードを入力してください", EN = "Current password is required" },
                new LocalizationResource { Key = "MSG_PASSWORD_TOO_SHORT", ZH = "密码长度不能少于6个字符", JA = "パスワードは6文字以上である必要があります", EN = "Password must be at least 6 characters" },
                new LocalizationResource { Key = "MSG_PASSWORD_NOT_MATCH", ZH = "两次输入的密码不一致", JA = "パスワードが一致しません", EN = "Passwords do not match" },
                new LocalizationResource { Key = "MSG_PASSWORD_CHANGED_SUCCESS", ZH = "密码修改成功", JA = "パスワードが正常に変更されました", EN = "Password changed successfully" },
                new LocalizationResource { Key = "MSG_CHANGE_PASSWORD_FAILED", ZH = "密码修改失败", JA = "パスワードの変更に失敗しました", EN = "Failed to change password" },
                new LocalizationResource { Key = "MSG_FEATURE_COMING_SOON", ZH = "该功能即将上线", JA = "この機能は近日公開予定です", EN = "This feature is coming soon" },
                // Entity types - 只预置已实现的实体，新实体由用户添加时自动创建多语资源
                new LocalizationResource { Key = "ENTITY_CUSTOMER", ZH = "客户", JA = "顧客", EN = "Customer" },
                new LocalizationResource { Key = "ENTITY_CUSTOMER_DESC", ZH = "客户信息管理", JA = "顧客情報管理", EN = "Customer information management" },
                new LocalizationResource { Key = "LBL_WIDTH", ZH = "宽度", JA = "幅", EN = "Width" },
                new LocalizationResource { Key = "LBL_HEIGHT", ZH = "高度", JA = "高さ", EN = "Height" },
                new LocalizationResource { Key = "LBL_DATA_SOURCE", ZH = "数据源", JA = "データソース", EN = "Data Source" },
                new LocalizationResource { Key = "LBL_VISIBLE", ZH = "可见", JA = "表示", EN = "Visible" },
                new LocalizationResource { Key = "LBL_DEFAULT_VALUE", ZH = "默认值", JA = "デフォルト値", EN = "Default Value" },
                new LocalizationResource { Key = "LBL_PLACEHOLDER", ZH = "占位符", JA = "プレースホルダー", EN = "Placeholder" },
                new LocalizationResource { Key = "LBL_REQUIRED", ZH = "必填", JA = "必須", EN = "Required" },
                new LocalizationResource { Key = "LBL_READONLY", ZH = "只读", JA = "読み取り専用", EN = "Read Only" },
                new LocalizationResource { Key = "LBL_MAX_LENGTH", ZH = "最大长度", JA = "最大長", EN = "Max Length" },
                new LocalizationResource { Key = "BTN_DELETE", ZH = "删除", JA = "削除", EN = "Delete" },
                new LocalizationResource { Key = "LBL_SELECT_COMPONENT", ZH = "选择一个组件", JA = "コンポーネントを選択", EN = "Select a component" },
                // Menu and navigation keys
                new LocalizationResource { Key = "MENU_SETTINGS", ZH = "系统设置", JA = "システム設定", EN = "Settings" },
                new LocalizationResource { Key = "MENU_TEMPLATES", ZH = "模板管理", JA = "テンプレート管理", EN = "Templates" },
                new LocalizationResource { Key = "MENU_FILES", ZH = "文件中心", JA = "ファイル", EN = "Files" },
                new LocalizationResource { Key = "LBL_THEME", ZH = "主题", JA = "テーマ", EN = "Theme" },
                new LocalizationResource { Key = "LBL_COLOR", ZH = "颜色", JA = "カラー", EN = "Color" },
                new LocalizationResource { Key = "LBL_LANGUAGE", ZH = "语言", JA = "言語", EN = "Language" },
                new LocalizationResource { Key = "LBL_USER", ZH = "用户", JA = "ユーザー", EN = "User" },
                new LocalizationResource { Key = "THEME_LIGHT", ZH = "明亮", JA = "ライト", EN = "Light" },
                new LocalizationResource { Key = "THEME_DARK", ZH = "暗黑", JA = "ダーク", EN = "Dark" },
                // Template designer specific keys
                new LocalizationResource { Key = "LBL_STYLE_SETTINGS", ZH = "样式设置", JA = "スタイル設定", EN = "Style Settings" },
                new LocalizationResource { Key = "LBL_FONT_FAMILY", ZH = "字体", JA = "フォント", EN = "Font" },
                new LocalizationResource { Key = "LBL_FONT_SIZE", ZH = "字号", JA = "フォントサイズ", EN = "Font Size" },
                new LocalizationResource { Key = "LBL_FONT_COLOR", ZH = "字体颜色", JA = "フォントカラー", EN = "Font Color" },
                new LocalizationResource { Key = "LBL_BG_COLOR", ZH = "背景色", JA = "背景色", EN = "Background Color" },
                new LocalizationResource { Key = "LBL_WIDTH_UNIT", ZH = "宽度单位", JA = "幅の単位", EN = "Width Unit" },
                new LocalizationResource { Key = "LBL_HEIGHT_UNIT", ZH = "高度单位", JA = "高さの単位", EN = "Height Unit" },
                new LocalizationResource { Key = "LBL_NEW_LINE", ZH = "换行（新行开始）", JA = "改行（新しい行で開始）", EN = "New Line (Start New Row)" },
                new LocalizationResource { Key = "PH_DEFAULT_FONT", ZH = "默认字体", JA = "デフォルトフォント", EN = "Default Font" },
                new LocalizationResource { Key = "PH_DEFAULT", ZH = "默认", JA = "デフォルト", EN = "Default" },
                new LocalizationResource { Key = "PH_TRANSPARENT", ZH = "透明", JA = "透明", EN = "Transparent" },
                new LocalizationResource { Key = "FONT_MICROSOFT_YAHEI", ZH = "微软雅黑", JA = "Microsoft YaHei", EN = "Microsoft YaHei" },
                new LocalizationResource { Key = "FONT_SIMHEI", ZH = "黑体", JA = "SimHei", EN = "SimHei" },
                new LocalizationResource { Key = "FONT_SIMSUN", ZH = "宋体", JA = "SimSun", EN = "SimSun" },
                new LocalizationResource { Key = "FONT_MONOSPACE", ZH = "等宽字体", JA = "等幅フォント", EN = "Monospace" },
                new LocalizationResource { Key = "MSG_DEFAULT_TEMPLATE_SAVED_SHORT", ZH = "默认模板已保存", JA = "デフォルトテンプレートが保存されました", EN = "Default template saved" },
                new LocalizationResource { Key = "MSG_TEMPLATE_SAVED_SHORT", ZH = "模板已保存", JA = "テンプレートが保存されました", EN = "Template saved" },
                new LocalizationResource { Key = "MSG_SAVE_FAILED_STATUS", ZH = "保存失败: {0}", JA = "保存に失敗: {0}", EN = "Save failed: {0}" },
                new LocalizationResource { Key = "MSG_SAVE_FAILED_ERROR", ZH = "保存失败: {0}", JA = "保存に失敗: {0}", EN = "Save failed: {0}" },
                // Color preset keys
                new LocalizationResource { Key = "COLOR_LIGHT_GRAY", ZH = "浅灰", JA = "ライトグレー", EN = "Light Gray" },
                new LocalizationResource { Key = "COLOR_LIGHT_BLUE", ZH = "浅蓝", JA = "ライトブルー", EN = "Light Blue" },
                new LocalizationResource { Key = "COLOR_LIGHT_YELLOW", ZH = "浅黄", JA = "ライトイエロー", EN = "Light Yellow" },
                new LocalizationResource { Key = "COLOR_LIGHT_GREEN", ZH = "浅绿", JA = "ライトグリーン", EN = "Light Green" },
                // Additional UI keys
                new LocalizationResource { Key = "LBL_NOT_SET", ZH = "未设置", JA = "未設定", EN = "Not Set" },
                new LocalizationResource { Key = "ERR_LOGIN_EXPIRED", ZH = "登录过期，请重新登录", JA = "ログインの有効期限が切れました。再度ログインしてください", EN = "Login expired, please log in again" },
                // Setup page specific keys
                new LocalizationResource { Key = "LBL_OPTIONAL_DEFAULT", ZH = "可选，默认", JA = "オプション、デフォルト", EN = "Optional, default" },
                new LocalizationResource { Key = "PH_DEFAULT_VALUE", ZH = "默认: {0}", JA = "デフォルト: {0}", EN = "Default: {0}" },
                new LocalizationResource { Key = "ERR_INVALID_API_BASE", ZH = "API Base地址格式不正确：{0}。请输入有效的URL（如：http://localhost:5200）", JA = "API Baseアドレスの形式が正しくありません: {0}。有効なURLを入力してください（例: http://localhost:5200）", EN = "Invalid API Base address: {0}. Please enter a valid URL (e.g., http://localhost:5200)" },
                new LocalizationResource { Key = "ERR_ADMIN_USERNAME_EMPTY", ZH = "管理员用户名不能为空", JA = "管理者ユーザー名は空にできません", EN = "Admin username cannot be empty" },
                new LocalizationResource { Key = "ERR_ADMIN_EMAIL_EMPTY", ZH = "管理员邮箱不能为空", JA = "管理者メールは空にできません", EN = "Admin email cannot be empty" },
                new LocalizationResource { Key = "ERR_ADMIN_PASSWORD_EMPTY", ZH = "管理员密码不能为空", JA = "管理者パスワードは空にできません", EN = "Admin password cannot be empty" },
                new LocalizationResource { Key = "ERR_REQUEST_TIMEOUT", ZH = "请求超时（超过15秒）。请检查API服务器 {0} 是否正在运行。\n\n请确认：\n1. API服务器已启动\n2. 在浏览器中访问 {0}/swagger 确认API可访问", JA = "リクエストタイムアウト（15秒超過）。APIサーバー {0} が実行中か確認してください。\n\n確認事項：\n1. APIサーバーが起動している\n2. ブラウザで {0}/swagger にアクセスしてAPIにアクセス可能か確認", EN = "Request timeout (over 15 seconds). Please check if API server {0} is running.\n\nPlease verify:\n1. API server is started\n2. Access {0}/swagger in browser to confirm API is accessible" },
                new LocalizationResource { Key = "ERR_CANNOT_CONNECT_API", ZH = "无法连接到API服务器 {0}：{1}\n\n请确认：\n1. API服务器已启动（运行 BobCrm.Api 项目）\n2. API地址正确：{0}\n3. 网络连接正常\n4. 防火墙允许访问该端口", JA = "APIサーバー {0} に接続できません: {1}\n\n確認事項：\n1. APIサーバーが起動している（BobCrm.Apiプロジェクトを実行）\n2. APIアドレスが正しい: {0}\n3. ネットワーク接続が正常\n4. ファイアウォールがポートへのアクセスを許可している", EN = "Cannot connect to API server {0}: {1}\n\nPlease verify:\n1. API server is started (run BobCrm.Api project)\n2. API address is correct: {0}\n3. Network connection is normal\n4. Firewall allows access to the port" },
                new LocalizationResource { Key = "ERR_EXCEPTION_OCCURRED", ZH = "发生错误：{0} - {1}\n\n堆栈跟踪：{2}", JA = "エラーが発生しました: {0} - {1}\n\nスタックトレース: {2}", EN = "Error occurred: {0} - {1}\n\nStack trace: {2}" },
                new LocalizationResource { Key = "ERR_SAVE_FAILED_ADMIN", ZH = "无法保存管理员账户到 {0}\n\n尝试保存的用户信息：\n  用户名: {1}\n  邮箱: {2}\n\n错误详情：{3}\n\n请检查：\n1. API服务器是否正在运行 ({0})\n2. 在浏览器中访问 {0}/swagger 确认API是否可访问\n3. 网络连接是否正常\n4. API地址是否正确", JA = "管理者アカウントを {0} に保存できません\n\n保存しようとしたユーザー情報：\n  ユーザー名: {1}\n  メール: {2}\n\nエラー詳細: {3}\n\n確認事項：\n1. APIサーバーが実行中か ({0})\n2. ブラウザで {0}/swagger にアクセスしてAPIにアクセス可能か確認\n3. ネットワーク接続が正常か\n4. APIアドレスが正しいか", EN = "Cannot save admin account to {0}\n\nAttempted user info:\n  Username: {1}\n  Email: {2}\n\nError details: {3}\n\nPlease check:\n1. Is API server running ({0})\n2. Access {0}/swagger in browser to confirm API is accessible\n3. Is network connection normal\n4. Is API address correct" },
                new LocalizationResource { Key = "MSG_SETTINGS_SAVED_REDIRECTING", ZH = "设置已保存，正在跳转到首页...", JA = "設定が保存されました。ホームページにリダイレクトしています...", EN = "Settings saved, redirecting to home..." },
                new LocalizationResource { Key = "ERR_OCCURRED", ZH = "发生错误: {0}", JA = "エラーが発生しました: {0}", EN = "Error occurred: {0}" },
                // EntityDefinitionEdit.razor keys
                new LocalizationResource { Key = "LBL_CREATE_ENTITY_DEF", ZH = "新建实体定义", JA = "エンティティ定義を作成", EN = "Create Entity Definition" },
                new LocalizationResource { Key = "LBL_EDIT_ENTITY_DEF", ZH = "编辑实体定义", JA = "エンティティ定義を編集", EN = "Edit Entity Definition" },
                new LocalizationResource { Key = "LBL_NAMESPACE", ZH = "命名空间", JA = "ネームスペース", EN = "Namespace" },
                new LocalizationResource { Key = "LBL_ENTITY_NAME", ZH = "实体名称", JA = "エンティティ名", EN = "Entity Name" },
                new LocalizationResource { Key = "TXT_ENTITY_NAME_HINT", ZH = "将用作C#类名，如: Product", JA = "C#クラス名として使用されます、例: Product", EN = "Will be used as C# class name, e.g.: Product" },
                new LocalizationResource { Key = "TXT_MULTILINGUAL_DISPLAY_NAME_HINT", ZH = "请提供实体的多语言显示名称（至少一种语言）", JA = "エンティティの多言語表示名を提供してください（少なくとも1言語）", EN = "Please provide multilingual display names (at least one language)" },
                new LocalizationResource { Key = "LBL_STRUCTURE_TYPE", ZH = "结构类型", JA = "構造タイプ", EN = "Structure Type" },
                new LocalizationResource { Key = "LBL_SINGLE_ENTITY", ZH = "单一实体", JA = "単一エンティティ", EN = "Single Entity" },
                new LocalizationResource { Key = "LBL_MASTER_DETAIL_ENTITY", ZH = "主从实体", JA = "マスター・詳細エンティティ", EN = "Master-Detail Entity" },
                new LocalizationResource { Key = "LBL_MASTER_DETAIL_GRANDCHILD_ENTITY", ZH = "主从孙实体", JA = "マスター・詳細・孙エンティティ", EN = "Master-Detail-Grandchild Entity" },
                new LocalizationResource { Key = "LBL_INTERFACES", ZH = "接口", JA = "インターフェース", EN = "Interfaces" },
                new LocalizationResource { Key = "TXT_AUDIT_FIELDS", ZH = "审计字段", JA = "監査フィールド", EN = "Audit Fields" },
                new LocalizationResource { Key = "TXT_VERSION", ZH = "版本", JA = "バージョン", EN = "Version" },
                new LocalizationResource { Key = "TXT_TIME_VERSION", ZH = "时间版本", JA = "タイムバージョン", EN = "Time Version" },
                new LocalizationResource { Key = "LBL_ICON", ZH = "图标", JA = "アイコン", EN = "Icon" },
                new LocalizationResource { Key = "LBL_CATEGORY", ZH = "分类", JA = "カテゴリ", EN = "Category" },
                new LocalizationResource { Key = "TXT_CATEGORY_PLACEHOLDER", ZH = "业务管理", JA = "ビジネス管理", EN = "Business Management" },
                new LocalizationResource { Key = "LBL_SORT_ORDER", ZH = "排序", JA = "並び順", EN = "Sort Order" },
                new LocalizationResource { Key = "LBL_ENABLED", ZH = "启用", JA = "有効", EN = "Enabled" },
                new LocalizationResource { Key = "LBL_FIELD_DEFINITION", ZH = "字段定义", JA = "フィールド定義", EN = "Field Definition" },
                new LocalizationResource { Key = "BTN_ADD_FIELD", ZH = "添加字段", JA = "フィールドを追加", EN = "Add Field" },
                new LocalizationResource { Key = "LBL_PROPERTY_NAME", ZH = "属性名", JA = "プロパティ名", EN = "Property Name" },
                new LocalizationResource { Key = "LBL_DISPLAY_NAME", ZH = "显示名", JA = "表示名", EN = "Display Name" },
                new LocalizationResource { Key = "LBL_DATA_TYPE", ZH = "数据类型", JA = "データタイプ", EN = "Data Type" },
                new LocalizationResource { Key = "LBL_LENGTH", ZH = "长度", JA = "長さ", EN = "Length" },
                new LocalizationResource { Key = "LBL_ACTIONS", ZH = "操作", JA = "操作", EN = "Actions" },
                new LocalizationResource { Key = "BTN_EDIT", ZH = "编辑", JA = "編集", EN = "Edit" },
                new LocalizationResource { Key = "MSG_CONFIRM_DELETE", ZH = "确定删除？", JA = "削除してもよろしいですか？", EN = "Confirm delete?" },
                new LocalizationResource { Key = "LBL_CREATE_FIELD", ZH = "新建字段", JA = "新しいフィールド", EN = "Create Field" },
                new LocalizationResource { Key = "LBL_EDIT_FIELD", ZH = "编辑字段", JA = "フィールドを編集", EN = "Edit Field" },
                new LocalizationResource { Key = "TXT_FOR_STRING_TYPE", ZH = "对于String类型", JA = "String型の場合", EN = "For String type" },
                new LocalizationResource { Key = "TXT_FOR_DECIMAL_TYPE", ZH = "对于Decimal类型", JA = "Decimal型の場合", EN = "For Decimal type" },
                new LocalizationResource { Key = "LBL_PRECISION", ZH = "精度", JA = "精度", EN = "Precision" },
                new LocalizationResource { Key = "LBL_SCALE", ZH = "小数位", JA = "スケール", EN = "Scale" },
                new LocalizationResource { Key = "TXT_DEFAULT_VALUE_HINT", ZH = "如: 0, NOW, NEWID", JA = "例: 0, NOW, NEWID", EN = "e.g.: 0, NOW, NEWID" },
                new LocalizationResource { Key = "MSG_LOAD_FAILED", ZH = "加载失败", JA = "読み込み失敗", EN = "Load failed" },
                new LocalizationResource { Key = "MSG_DISPLAY_NAME_REQUIRED", ZH = "显示名至少需要提供一种语言的文本", JA = "表示名は少なくとも1言語必要です", EN = "Display name requires at least one language" },
                new LocalizationResource { Key = "MSG_CREATE_SUCCESS", ZH = "创建成功", JA = "作成に成功しました", EN = "Created successfully" },
                new LocalizationResource { Key = "MSG_UPDATE_SUCCESS", ZH = "更新成功", JA = "更新に成功しました", EN = "Updated successfully" },
                new LocalizationResource { Key = "MSG_SAVE_FAILED", ZH = "保存失败", JA = "保存に失敗しました", EN = "Save failed" },
                new LocalizationResource { Key = "MSG_FIELD_DISPLAY_NAME_REQUIRED", ZH = "字段显示名至少需要提供一种语言的文本", JA = "フィールドの表示名は少なくとも1言語必要です", EN = "Field display name requires at least one language" },
                new LocalizationResource { Key = "TXT_YES", ZH = "是", JA = "はい", EN = "Yes" },
                new LocalizationResource { Key = "TXT_NO", ZH = "否", JA = "いいえ", EN = "No" },
                // MasterDetailConfig.razor keys
                new LocalizationResource { Key = "LBL_MASTER_DETAIL_CONFIG", ZH = "主子表配置", JA = "マスター・詳細設定", EN = "Master-Detail Configuration" },
                new LocalizationResource { Key = "BTN_SAVE_CONFIG", ZH = "保存配置", JA = "設定を保存", EN = "Save Configuration" },
                new LocalizationResource { Key = "BTN_PREVIEW_STRUCTURE", ZH = "预览结构", JA = "構造をプレビュー", EN = "Preview Structure" },
                new LocalizationResource { Key = "MSG_ENTITY_NOT_FOUND", ZH = "实体不存在", JA = "エンティティが見つかりません", EN = "Entity not found" },
                new LocalizationResource { Key = "MSG_ENTITY_NOT_FOUND_DESC", ZH = "未找到指定的实体定义", JA = "指定されたエンティティ定義が見つかりません", EN = "Entity definition not found" },
                new LocalizationResource { Key = "LBL_ENTITY_INFO", ZH = "实体信息", JA = "エンティティ情報", EN = "Entity Information" },
                new LocalizationResource { Key = "LBL_FULL_TYPE_NAME", ZH = "完整类型名", JA = "完全な型名", EN = "Full Type Name" },
                new LocalizationResource { Key = "LBL_CURRENT_STRUCTURE", ZH = "当前结构", JA = "現在の構造", EN = "Current Structure" },
                new LocalizationResource { Key = "LBL_MASTER_DETAIL_TWO_LEVEL", ZH = "主子结构（两层）", JA = "マスター・詳細（2階層）", EN = "Master-Detail (Two Level)" },
                new LocalizationResource { Key = "LBL_MASTER_DETAIL_THREE_LEVEL", ZH = "主子孙结构（三层）", JA = "マスター・詳細・孙（3階層）", EN = "Master-Detail-Grandchild (Three Level)" },
                new LocalizationResource { Key = "LBL_CHILD_ENTITY_CONFIG", ZH = "子实体配置", JA = "子エンティティ設定", EN = "Child Entity Configuration" },
                new LocalizationResource { Key = "BTN_ADD_CHILD_ENTITY", ZH = "添加子实体", JA = "子エンティティを追加", EN = "Add Child Entity" },
                new LocalizationResource { Key = "MSG_NO_CHILD_ENTITY_CONFIGURED", ZH = "尚未配置子实体", JA = "子エンティティがまだ設定されていません", EN = "No child entity configured yet" },
                new LocalizationResource { Key = "BTN_ADD_NOW", ZH = "立即添加", JA = "今すぐ追加", EN = "Add Now" },
                new LocalizationResource { Key = "LBL_CHILD_ENTITY_NAME", ZH = "子实体名称", JA = "子エンティティ名", EN = "Child Entity Name" },
                new LocalizationResource { Key = "LBL_FOREIGN_KEY_FIELD", ZH = "外键字段", JA = "外部キーフィールド", EN = "Foreign Key Field" },
                new LocalizationResource { Key = "LBL_COLLECTION_PROPERTY", ZH = "集合属性", JA = "コレクションプロパティ", EN = "Collection Property" },
                new LocalizationResource { Key = "LBL_CASCADE_DELETE", ZH = "级联删除", JA = "カスケード削除", EN = "Cascade Delete" },
                new LocalizationResource { Key = "TXT_PLACEHOLDER_ORDER_ID", ZH = "例如: OrderId", JA = "例: OrderId", EN = "e.g.: OrderId" },
                new LocalizationResource { Key = "TXT_PLACEHOLDER_ORDER_LINES", ZH = "例如: OrderLines", JA = "例: OrderLines", EN = "e.g.: OrderLines" },
                new LocalizationResource { Key = "LBL_CASCADE_NO_ACTION", ZH = "不操作", JA = "操作なし", EN = "No Action" },
                new LocalizationResource { Key = "LBL_CASCADE_SET_NULL", ZH = "设为NULL", JA = "NULLに設定", EN = "Set NULL" },
                new LocalizationResource { Key = "LBL_CASCADE_RESTRICT", ZH = "限制", JA = "制限", EN = "Restrict" },
                new LocalizationResource { Key = "LBL_AUTO_SAVE", ZH = "自动保存", JA = "自動保存", EN = "Auto Save" },
                new LocalizationResource { Key = "BTN_REMOVE", ZH = "移除", JA = "削除", EN = "Remove" },
                new LocalizationResource { Key = "TXT_FOREIGN_KEY_FIELD_DESC", ZH = "子表中指向主表的外键字段名", JA = "子テーブルのマスターテーブルへの外部キーフィールド名", EN = "Foreign key field name in child table pointing to master" },
                new LocalizationResource { Key = "TXT_COLLECTION_PROPERTY_DESC", ZH = "主表中引用子表集合的属性名", JA = "マスターテーブルの子テーブルコレクションプロパティ名", EN = "Collection property name in master referencing children" },
                new LocalizationResource { Key = "LBL_CONFIG_DESCRIPTION", ZH = "配置说明", JA = "設定の説明", EN = "Configuration Description" },
                new LocalizationResource { Key = "LBL_CASCADE_DELETE_BEHAVIOR", ZH = "级联删除行为", JA = "カスケード削除動作", EN = "Cascade Delete Behavior" },
                new LocalizationResource { Key = "TXT_CASCADE_NO_ACTION_DESC", ZH = "删除主表时不影响子表", JA = "マスター削除時に子テーブルに影響なし", EN = "No impact on children when master is deleted" },
                new LocalizationResource { Key = "TXT_CASCADE_DELETE_DESC", ZH = "删除主表时自动删除关联子表", JA = "マスター削除時に関連する子を自動削除", EN = "Automatically delete related children when master is deleted" },
                new LocalizationResource { Key = "TXT_CASCADE_SET_NULL_DESC", ZH = "删除主表时将子表外键设为NULL", JA = "マスター削除時に子の外部キーをNULLに設定", EN = "Set child foreign key to NULL when master is deleted" },
                new LocalizationResource { Key = "TXT_CASCADE_RESTRICT_DESC", ZH = "存在子表时禁止删除主表", JA = "子が存在する場合、マスター削除を禁止", EN = "Prevent master deletion if children exist" },
                new LocalizationResource { Key = "LBL_AUTO_CASCADE_SAVE", ZH = "自动级联保存", JA = "自動カスケード保存", EN = "Auto Cascade Save" },
                new LocalizationResource { Key = "TXT_AUTO_CASCADE_SAVE_DESC", ZH = "保存主表时自动保存子表", JA = "マスター保存時に子を自動保存", EN = "Automatically save children when master is saved" },
                new LocalizationResource { Key = "LBL_ADD_CHILD_ENTITY", ZH = "添加子实体", JA = "子エンティティを追加", EN = "Add Child Entity" },
                new LocalizationResource { Key = "LBL_CHILD_ENTITY", ZH = "子实体", JA = "子エンティティ", EN = "Child Entity" },
                new LocalizationResource { Key = "TXT_SELECT_CHILD_ENTITY", ZH = "选择子实体", JA = "子エンティティを選択", EN = "Select Child Entity" },
                new LocalizationResource { Key = "LBL_ENTITY_STRUCTURE_PREVIEW", ZH = "实体结构预览", JA = "エンティティ構造プレビュー", EN = "Entity Structure Preview" },
                new LocalizationResource { Key = "LBL_MASTER_DETAIL", ZH = "主子表", JA = "マスター・詳細", EN = "Master-Detail" },
                new LocalizationResource { Key = "LBL_MASTER_DETAIL_GRANDCHILD", ZH = "主子孙表", JA = "マスター・詳細・孫", EN = "Master-Detail-Grandchild" },
                new LocalizationResource { Key = "LBL_ENTITY", ZH = "实体", JA = "エンティティ", EN = "Entity" },
                new LocalizationResource { Key = "LBL_CHILD_ENTITIES", ZH = "子实体", JA = "子エンティティ", EN = "Child Entities" },
                new LocalizationResource { Key = "LBL_FOREIGN_KEY", ZH = "外键", JA = "外部キー", EN = "Foreign Key" },
                new LocalizationResource { Key = "LBL_COLLECTION", ZH = "集合", JA = "コレクション", EN = "Collection" },
                new LocalizationResource { Key = "BTN_ADD", ZH = "添加", JA = "追加", EN = "Add" },
                new LocalizationResource { Key = "MSG_LOAD_CANDIDATES_FAILED", ZH = "加载候选实体失败", JA = "候補エンティティの読み込みに失敗しました", EN = "Failed to load candidate entities" },
                new LocalizationResource { Key = "MSG_PLEASE_SELECT_CHILD_ENTITY", ZH = "请选择子实体", JA = "子エンティティを選択してください", EN = "Please select child entity" },
                new LocalizationResource { Key = "MSG_PLEASE_INPUT_FOREIGN_KEY", ZH = "请输入外键字段名", JA = "外部キーフィールド名を入力してください", EN = "Please input foreign key field name" },
                new LocalizationResource { Key = "MSG_PLEASE_INPUT_COLLECTION_PROPERTY", ZH = "请输入集合属性名", JA = "コレクションプロパティ名を入力してください", EN = "Please input collection property name" },
                new LocalizationResource { Key = "MSG_CHILD_ENTITY_ALREADY_ADDED", ZH = "该子实体已经添加过了", JA = "この子エンティティは既に追加されています", EN = "This child entity has already been added" },
                new LocalizationResource { Key = "MSG_CONFIG_SAVED", ZH = "配置已保存", JA = "設定を保存しました", EN = "Configuration saved" },
                new LocalizationResource { Key = "MSG_PREVIEW_FAILED", ZH = "预览失败", JA = "プレビューに失敗しました", EN = "Preview failed" },
                new LocalizationResource { Key = "TXT_SINGLE_ENTITY_DESC", ZH = "独立的单一实体，无关联子表", JA = "独立した単一エンティティ、関連する子テーブルなし", EN = "Independent single entity, no related child tables" },
                new LocalizationResource { Key = "TXT_MASTER_DETAIL_DESC", ZH = "主子两层结构，主表包含一组子表记录", JA = "2階層のマスター・詳細構造、マスターが子レコードのセットを含む", EN = "Two-level master-detail structure, master contains a set of child records" },
                new LocalizationResource { Key = "TXT_MASTER_DETAIL_GRANDCHILD_DESC", ZH = "主子孙三层结构，主表→子表→孙表的嵌套关系", JA = "3階層のマスター・詳細・孫構造、マスター→子→孫のネスト関係", EN = "Three-level master-detail-grandchild structure with nested relationships" },
                // EntityDefinitionPublish.razor keys
                new LocalizationResource { Key = "LBL_ENTITY_PUBLISH", ZH = "实体发布", JA = "エンティティ公開", EN = "Entity Publish" },
                new LocalizationResource { Key = "BTN_PUBLISH_ENTITY", ZH = "发布实体", JA = "エンティティを公開", EN = "Publish Entity" },
                new LocalizationResource { Key = "BTN_GENERATE_CODE", ZH = "生成代码", JA = "コードを生成", EN = "Generate Code" },
                new LocalizationResource { Key = "BTN_COMPILE", ZH = "编译", JA = "コンパイル", EN = "Compile" },
                new LocalizationResource { Key = "LBL_STATUS", ZH = "状态", JA = "ステータス", EN = "Status" },
                new LocalizationResource { Key = "LBL_DRAFT", ZH = "草稿", JA = "下書き", EN = "Draft" },
                new LocalizationResource { Key = "LBL_MODIFIED", ZH = "已修改", JA = "変更済み", EN = "Modified" },
                new LocalizationResource { Key = "LBL_PUBLISHED", ZH = "已发布", JA = "公開済み", EN = "Published" },
                new LocalizationResource { Key = "LBL_FIELD_COUNT", ZH = "字段数", JA = "フィールド数", EN = "Field Count" },
                new LocalizationResource { Key = "LBL_INTERFACE_COUNT", ZH = "接口数", JA = "インターフェース数", EN = "Interface Count" },
                new LocalizationResource { Key = "LBL_IS_LOCKED", ZH = "是否锁定", JA = "ロック中", EN = "Is Locked" },
                new LocalizationResource { Key = "LBL_CREATED_TIME", ZH = "创建时间", JA = "作成時刻", EN = "Created Time" },
                new LocalizationResource { Key = "LBL_CREATED_BY", ZH = "创建人", JA = "作成者", EN = "Created By" },
                new LocalizationResource { Key = "LBL_DDL_PREVIEW", ZH = "DDL预览", JA = "DDLプレビュー", EN = "DDL Preview" },
                new LocalizationResource { Key = "LBL_DDL_SCRIPT", ZH = "DDL脚本", JA = "DDLスクリプト", EN = "DDL Script" },
                new LocalizationResource { Key = "MSG_NO_DDL_SCRIPT", ZH = "暂无DDL脚本", JA = "DDLスクリプトがありません", EN = "No DDL script available" },
                new LocalizationResource { Key = "MSG_CLICK_GENERATE_CODE", ZH = "请先点击【生成代码】以生成C#代码", JA = "まず[コードを生成]をクリックしてC#コードを生成してください", EN = "Please click [Generate Code] first to generate C# code" },
                new LocalizationResource { Key = "LBL_DDL_HISTORY", ZH = "DDL历史", JA = "DDL履歴", EN = "DDL History" },
                new LocalizationResource { Key = "LBL_EXECUTED_TIME", ZH = "执行时间", JA = "実行時刻", EN = "Executed Time" },
                new LocalizationResource { Key = "LBL_SOURCE", ZH = "来源", JA = "ソース", EN = "Source" },
                new LocalizationResource { Key = "LBL_GENERATED_CODE", ZH = "生成的代码", JA = "生成されたコード", EN = "Generated Code" },
                new LocalizationResource { Key = "MSG_CLICK_COMPILE", ZH = "请先点击【编译】以编译代码", JA = "まず[コンパイル]をクリックしてコードをコンパイルしてください", EN = "Please click [Compile] first to compile code" },
                new LocalizationResource { Key = "LBL_COMPILATION_STATUS", ZH = "编译状态", JA = "コンパイルステータス", EN = "Compilation Status" },
                new LocalizationResource { Key = "LBL_SUCCESS", ZH = "成功", JA = "成功", EN = "Success" },
                new LocalizationResource { Key = "LBL_FAILED", ZH = "失败", JA = "失敗", EN = "Failed" },
                new LocalizationResource { Key = "LBL_ASSEMBLY", ZH = "程序集", JA = "アセンブリ", EN = "Assembly" },
                new LocalizationResource { Key = "LBL_TYPE", ZH = "类型", JA = "型", EN = "Type" },
                new LocalizationResource { Key = "LBL_LOADED_TYPES", ZH = "已加载类型", JA = "ロード済み型", EN = "Loaded Types" },
                new LocalizationResource { Key = "LBL_COMPILATION_ERROR", ZH = "编译错误", JA = "コンパイルエラー", EN = "Compilation Error" },
                new LocalizationResource { Key = "LBL_LINE", ZH = "行", JA = "行", EN = "Line" },
                new LocalizationResource { Key = "LBL_COLUMN", ZH = "列", JA = "列", EN = "Column" },
                new LocalizationResource { Key = "MSG_PUBLISH_SUCCESS", ZH = "发布成功", JA = "公開に成功しました", EN = "Published successfully" },
                new LocalizationResource { Key = "MSG_PUBLISH_FAILED", ZH = "发布失败", JA = "公開に失敗しました", EN = "Publish failed" },
                new LocalizationResource { Key = "MSG_CODE_GENERATION_SUCCESS", ZH = "代码生成成功", JA = "コード生成に成功しました", EN = "Code generated successfully" },
                new LocalizationResource { Key = "MSG_CODE_GENERATION_FAILED", ZH = "代码生成失败", JA = "コード生成に失敗しました", EN = "Code generation failed" },
                new LocalizationResource { Key = "MSG_COMPILATION_SUCCESS", ZH = "编译成功", JA = "コンパイルに成功しました", EN = "Compiled successfully" },
                new LocalizationResource { Key = "MSG_COMPILATION_FAILED", ZH = "编译失败", JA = "コンパイルに失敗しました", EN = "Compilation failed" },
                new LocalizationResource { Key = "MSG_COMPILATION_FAILED_DETAIL", ZH = "编译失败，请查看错误详情", JA = "コンパイルに失敗しました。エラーの詳細をご確認ください", EN = "Compilation failed, please check error details" },
                new LocalizationResource { Key = "MSG_LOAD_DDL_FAILED", ZH = "加载DDL失败", JA = "DDLの読み込みに失敗しました", EN = "Failed to load DDL" },
                new LocalizationResource { Key = "MSG_LOAD_HISTORY_FAILED", ZH = "加载历史失败", JA = "履歴の読み込みに失敗しました", EN = "Failed to load history" },
                new LocalizationResource { Key = "LBL_SCRIPT_PREVIEW", ZH = "脚本预览", JA = "スクリプトプレビュー", EN = "Script Preview" },
                new LocalizationResource { Key = "BTN_REFRESH", ZH = "刷新", JA = "更新", EN = "Refresh" },
                // DynamicEntityData.razor keys
                new LocalizationResource { Key = "LBL_DATA_MANAGEMENT", ZH = "数据管理", JA = "データ管理", EN = "Data Management" },
                new LocalizationResource { Key = "BTN_CREATE", ZH = "新建", JA = "新規作成", EN = "Create" },
                new LocalizationResource { Key = "LBL_DYNAMIC_ENTITY_DATA_MANAGEMENT", ZH = "动态实体数据管理", JA = "動的エンティティデータ管理", EN = "Dynamic Entity Data Management" },
                new LocalizationResource { Key = "TXT_DYNAMIC_ENTITY_DESC", ZH = "此页面用于管理动态编译加载的实体数据。请确保实体已成功编译加载。", JA = "このページは動的にコンパイルされたエンティティデータを管理するためのものです。エンティティが正常にコンパイルされていることを確認してください。", EN = "This page is for managing dynamically compiled entity data. Please ensure the entity has been successfully compiled." },
                new LocalizationResource { Key = "LBL_DATA_LIST", ZH = "数据列表", JA = "データリスト", EN = "Data List" },
                new LocalizationResource { Key = "MSG_NO_DATA", ZH = "暂无数据", JA = "データがありません", EN = "No data available" },
                new LocalizationResource { Key = "MSG_CREATE_FEATURE_IN_DEVELOPMENT", ZH = "新建功能开发中", JA = "新規作成機能は開発中です", EN = "Create feature in development" },
                new LocalizationResource { Key = "MSG_EDIT_FEATURE_IN_DEVELOPMENT", ZH = "编辑功能开发中", JA = "編集機能は開発中です", EN = "Edit feature in development" },
                new LocalizationResource { Key = "MSG_DELETE_SUCCESS", ZH = "删除成功", JA = "削除に成功しました", EN = "Deleted successfully" },
                new LocalizationResource { Key = "MSG_DELETE_FAILED", ZH = "删除失败", JA = "削除に失敗しました", EN = "Delete failed" },
                new LocalizationResource { Key = "MSG_LOAD_DATA_FAILED", ZH = "加载数据失败", JA = "データの読み込みに失敗しました", EN = "Failed to load data" }
            );
        }
        else
        {
            // 从JSON文件加载所有i18n资源（单一数据源原则）
            var allResources = await I18nResourceLoader.LoadResourcesAsync();

            // 使用异步Ensure方法批量处理资源
            async Task EnsureAsync(LocalizationResource resource)
            {
                try
                {
                    var set = db.Set<LocalizationResource>();
                    // 先检查 ChangeTracker（内存中的实体），避免重复查询
                    var existing = set.Local.FirstOrDefault(x => x.Key == resource.Key);
                    if (existing == null)
                    {
                        // ChangeTracker 中没有，异步从数据库查询
                        existing = await set.FirstOrDefaultAsync(x => x.Key == resource.Key);
                    }

                    if (existing == null)
                    {
                        set.Add(new LocalizationResource
                        {
                            Key = resource.Key,
                            ZH = resource.ZH,
                            JA = resource.JA,
                            EN = resource.EN
                        });
                    }
                    else
                    {
                        // 更新已存在的记录
                        existing.ZH = resource.ZH;
                        existing.JA = resource.JA;
                        existing.EN = resource.EN;
                    }
                }
                catch (InvalidOperationException)
                {
                    // In rare cases of duplicate tracking (test startup concurrency), ignore and proceed
                }
            }

            // 批量处理所有资源
            foreach (var resource in allResources)
            {
                await EnsureAsync(resource);
            }
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            // Primary key constraint violation - some records already exist in database
            // This can happen in test scenarios where records are deleted then re-initialized
            // Log and continue, as the goal is to ensure records exist
            if (ex.Message.Contains("duplicate key") || ex.Message.Contains("UNIQUE constraint"))
            {
                // Ignore duplicate key errors - records already exist, which is acceptable
            }
            else
            {
                throw;
            }
        }
        // 创建默认客户档案显示模板（customerId = 0 表示全局模板，不绑定具体客户）
        // 所有用户默认使用此模板，除非他们保存了自己的模板
        if (!await db.Set<UserLayout>().IgnoreQueryFilters().AnyAsync(UserLayoutScope.ForUser("__default__", 0)))
        {
            var defaultTemplate = new
            {
                mode = "flow",
                items = new Dictionary<string, object>
                {
                    // email 字段 - 必填，占半行
                    ["email"] = new
                    {
                        order = 0,
                        w = 6,  // 向后兼容
                        Width = 50,
                        WidthUnit = "%",
                        Height = 32,
                        HeightUnit = "px",
                        visible = true,
                        label = "LBL_EMAIL",
                        type = "textbox"
                    },
                    // link 字段 - 网址链接，占半行
                    ["link"] = new
                    {
                        order = 1,
                        w = 6,
                        Width = 50,
                        WidthUnit = "%",
                        Height = 32,
                        HeightUnit = "px",
                        visible = true,
                        label = "LBL_LINK",
                        type = "textbox"
                    },
                    // file 字段 - 文件路径，占半行
                    ["file"] = new
                    {
                        order = 2,
                        w = 6,
                        Width = 50,
                        WidthUnit = "%",
                        Height = 32,
                        HeightUnit = "px",
                        visible = true,
                        label = "LBL_FILE_PATH",
                        type = "textbox"
                    },
                    // rds 字段 - 远程桌面连接，占半行
                    ["rds"] = new
                    {
                        order = 3,
                        w = 6,
                        Width = 50,
                        WidthUnit = "%",
                        Height = 32,
                        HeightUnit = "px",
                        visible = true,
                        label = "LBL_RDS",
                        type = "textbox"
                    },
                    // priority 字段 - 优先级数字，占1/4行
                    ["priority"] = new
                    {
                        order = 4,
                        w = 3,
                        Width = 25,
                        WidthUnit = "%",
                        Height = 32,
                        HeightUnit = "px",
                        visible = true,
                        label = "LBL_PRIORITY",
                        type = "textbox"
                    }
                }
            };
            var templateJson = System.Text.Json.JsonSerializer.Serialize(defaultTemplate);
            var defaultLayout = new UserLayout
            {
                UserId = "__default__",
                LayoutJson = templateJson
            };
            UserLayoutScope.ApplyCustomerScope(defaultLayout, 0); // 0 表示全局用户级模板
            db.Set<UserLayout>().Add(defaultLayout);
            await db.SaveChangesAsync();
        }
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
