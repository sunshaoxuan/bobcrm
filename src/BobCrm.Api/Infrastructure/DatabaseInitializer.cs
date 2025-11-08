using BobCrm.Api.Domain;
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
                new LocalizationResource { Key = "LBL_USERNAME_OR_EMAIL", ZH = "用户名或邮箱", JA = "ユーザー名またはメール", EN = "Username or Email" },
                new LocalizationResource { Key = "ERR_USERNAME_REQUIRED", ZH = "用户名不能为空", JA = "ユーザー名は必須です", EN = "Username is required" },
                new LocalizationResource { Key = "ERR_PASSWORD_REQUIRED", ZH = "密码不能为空", JA = "パスワードは必須です", EN = "Password is required" },
                new LocalizationResource { Key = "MSG_REGISTER_SUCCESS", ZH = "注册成功，请进行邮箱激活（开发环境查看 API 控制台）", JA = "登録成功。メール認証を実施してください（開発環境は API コンソール参照）", EN = "Registered. Please verify by email (check API console in dev)." },
                new LocalizationResource { Key = "TXT_REGISTER_HELP", ZH = "注册成功后，请在 API 控制台查看激活链接，或前往激活页面手动激活。", JA = "登録後、APIコンソールの有効化リンクを確認するか、アクティベートページで手動有効化してください。", EN = "After registering, check activation link in API console or activate manually on the activate page." },
                new LocalizationResource { Key = "LBL_HOME", ZH = "首页", JA = "ホーム", EN = "Home" },
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
                new LocalizationResource { Key = "ERR_OCCURRED", ZH = "发生错误: {0}", JA = "エラーが発生しました: {0}", EN = "Error occurred: {0}" }
            );
        }
        else
        {
            // Best-effort backfill: ensure critical UI keys exist and update if needed
            void Ensure(string key, string zh, string ja, string en)
            {
                try
                {
                    var set = db.Set<LocalizationResource>();
                    var existing = set.FirstOrDefault(x => x.Key == key);
                    if (existing == null)
                    {
                        set.Add(new LocalizationResource { Key = key, ZH = zh, JA = ja, EN = en });
                    }
                    else
                    {
                        existing.ZH = zh;
                        existing.JA = ja;
                        existing.EN = en;
                    }
                }
                catch (InvalidOperationException)
                {
                    // In rare cases of duplicate tracking (test startup concurrency), ignore and proceed
                }
            }
            Ensure("MENU_CUSTOMERS", "客户", "顧客", "Customers");
            Ensure("COL_CODE", "编码", "コード", "Code");
            Ensure("COL_NAME", "名称", "名称", "Name");
            Ensure("COL_ACTIONS", "操作", "操作", "Actions");
            Ensure("BTN_DETAIL", "详情", "詳細", "Detail");
            Ensure("LBL_SECURE", "安全", "セキュア", "Secure");
            Ensure("TXT_AUTH_HERO_TITLE", "一体化客户体验工作台", "OneCRM ワークスペース", "Unified customer experience workspace");
            Ensure("TXT_AUTH_HERO_SUBTITLE", "登录即可管理客户、模板与布局", "ログインして顧客・テンプレート・レイアウトを一元管理", "Manage customers, templates and layouts in one place");
            Ensure("TXT_AUTH_HERO_POINT1", "统一工作空间", "統合されたワークスペース", "Unified workspace");
            Ensure("TXT_AUTH_HERO_POINT2", "实时协作", "リアルタイムコラボレーション", "Real-time collaboration");
            Ensure("TXT_AUTH_HERO_POINT3", "多语言界面", "多言語 UI", "Multi-language UI");
            Ensure("LBL_LOGIN_TITLE", "登录", "ログイン", "Login");
            Ensure("LBL_USERNAME_OR_EMAIL", "用户名或邮箱", "ユーザー名またはメール", "Username or Email");
            Ensure("ERR_USERNAME_REQUIRED", "用户名不能为空", "ユーザー名は必須です", "Username is required");
            Ensure("ERR_PASSWORD_REQUIRED", "密码不能为空", "パスワードは必須です", "Password is required");
            Ensure("ERR_LOGIN_FAILED", "登录失败", "ログイン失敗", "Login failed");
            Ensure("TXT_REDIRECTING", "正在跳转...", "リダイレクト中です...", "Redirecting...");
            Ensure("TXT_REDIRECTING_HINT", "即将带你进入工作台，请稍候...", "安全なワークスペースを準備しています...", "Preparing your workspace...");
            Ensure("TXT_LOGIN_DESCRIPTION", "请输入已激活的账号和密码", "有効化済みのアカウント情報を入力してください", "Enter your activated credentials to continue");
            Ensure("TXT_NO_ACCOUNT", "还没有账号？", "アカウントがありませんか？", "Don't have an account?");
            Ensure("BTN_REGISTER", "注册", "登録", "Register");
            Ensure("ERR_PARSE_RESPONSE", "无法解析服务器响应", "サーバー応答を解析できません", "Unable to parse server response");
            Ensure("LBL_SETUP", "初始化设置", "初期設定", "Setup");
            Ensure("LBL_API_BASE", "API 基础地址", "API ベースURL", "API Base URL");
            // Backfill missing hint/button keys for setup page
            Ensure("LBL_API_BASE_HINT", "为空则使用内置默认地址（appsettings 的 Api:BaseUrl）。建议填写实际后端地址。", "未指定の場合は既定のアドレス（appsettings の Api:BaseUrl）を使用します。実際の API を推奨。", "If empty, uses default (appsettings Api:BaseUrl). Prefer your real API.");
            Ensure("BTN_SAVE_AND_GO_LOGIN", "保存并进入登录", "保存してログインへ", "Save and go to Login");
            Ensure("BTN_GO_LOGIN", "已有配置，去登录", "設定済みならログインへ", "Go to Login");
            Ensure("LBL_LANG", "语言", "言語", "Language");
            Ensure("LBL_ADMIN_ACCOUNT", "管理员账号", "管理者アカウント", "Admin Account");
            Ensure("LBL_ADMIN_USERNAME", "管理员用户名", "管理者ユーザー名", "Admin Username");
            Ensure("LBL_ADMIN_EMAIL", "管理员邮箱", "管理者メール", "Admin Email");
            Ensure("LBL_PASSWORD", "密码", "パスワード", "Password");
            Ensure("LBL_ADMIN", "管理员", "管理者", "Admin");
            Ensure("LBL_SEARCH", "搜索", "検索", "Search");
            Ensure("BTN_LOGOUT", "退出", "ログアウト", "Logout");
            Ensure("LBL_HOME", "首页", "ホーム", "Home");
            Ensure("LBL_WELCOME", "欢迎使用 OneCRM", "OneCRM へようこそ", "Welcome to OneCRM");
            Ensure("LBL_CUSTOMER_DETAIL", "客户详情", "顧客詳細", "Customer Detail");
            Ensure("LBL_LOADING", "加载中", "読み込み中", "Loading");
            Ensure("LBL_FIELDS", "字段", "フィールド", "Fields");
            Ensure("BTN_BACK", "返回", "戻る", "Back");
            Ensure("LBL_NOT_FOUND", "未找到", "見つかりません", "Not Found");
            Ensure("LBL_NO_FIELDS", "无字段", "フィールドなし", "No fields");
            Ensure("LBL_PLEASE_SELECT_CUSTOMER", "请选择客户", "顧客を選択してください", "Please select a customer");
            Ensure("BTN_RESET_SETUP", "重置初始化设置", "初期設定をリセット", "Reset Setup");
            Ensure("MSG_RESET_CONFIRM", "确定要重置初始化设置吗？这将删除当前管理员账户，您需要重新设置。", "初期設定をリセットしますか？現在の管理者アカウントが削除され、再設定が必要です。", "Are you sure to reset setup? This will delete the current admin account and require reconfiguration.");
            Ensure("MSG_RESET_SETUP_SUCCESS", "重置成功，请重新配置", "リセット完了。再設定してください", "Reset successful, please reconfigure");
            Ensure("ERR_RESET_SETUP_FAILED", "重置失败", "リセットに失敗", "Reset failed");
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
            /* Auth hero copy (override to latest wording)
            Ensure("TXT_AUTH_HERO_TITLE", "智能连接的客户体验中枢", "インテリジェントに接続された顧客体験の中枢", "The Intelligent Hub for Connected Customer Experience");
            Ensure("TXT_AUTH_HERO_SUBTITLE", "让团队与客户的关系更简单、更高效、更有温度。在同一个平台上完成洞察、协作与成长。", "チームと顧客の関係を、よりシンプルに、より効率的に、より温かく。同じプラットフォームで洞察・協働・成長を完結。", "Make team–customer relationships simpler, faster, and warmer. Gain insight, collaborate, and grow on a single platform.");
            Ensure("TXT_AUTH_HERO_POINT1", "统一视图 — 打通客户、项目与数据的全局视角", "統一ビュー — 顧客・プロジェクト・データを横断する全体視点", "Unified view — A global perspective across customers, projects, and data");
            Ensure("TXT_AUTH_HERO_POINT2", "智能协作 — 实时共享信息，让决策更快一步", "スマートな協働 — 情報を即時共有し、意思決定を一歩先へ", "Intelligent collaboration — Share in real time and decide faster");
            Ensure("TXT_AUTH_HERO_POINT3", "体验一致 — 无论何处登录，体验始终如一", "一貫した体験 — どこからログインしても変わらない体験", "Consistent experience — The same experience wherever you sign in");
            Ensure("TXT_AUTH_HERO_POINT4", "多语言支持 — 为全球团队打造无边界协作空间", "多言語対応 — グローバルチームのための境界のない協働空間", "Multilingual support — A boundaryless workspace for global teams");
            Ensure("TXT_AUTH_TAGLINE", "让关系更智能，让协作更自然。", "関係をよりスマートに、協働をより自然に。", "Make relationships smarter, collaboration more natural.");
            */
            // New customer keys
            Ensure("BTN_NEW_CUSTOMER", "新建客户", "新規顧客", "New Customer");
            Ensure("LBL_NEW_CUSTOMER", "新建客户", "新規顧客", "New Customer");
            Ensure("LBL_CUSTOMER_CODE_HINT", "唯一标识，例如: C001", "一意の識別子、例: C001", "Unique identifier, e.g. C001");
            // Profile page keys
            Ensure("MENU_PROFILE", "个人中心", "プロフィール", "Profile");
            Ensure("LBL_USER_INFORMATION", "用户信息", "ユーザー情報", "User Information");
            Ensure("LBL_ROLE", "角色", "役割", "Role");
            Ensure("LBL_CHANGE_PASSWORD", "修改密码", "パスワード変更", "Change Password");
            Ensure("LBL_CURRENT_PASSWORD", "当前密码", "現在のパスワード", "Current Password");
            Ensure("LBL_NEW_PASSWORD", "新密码", "新しいパスワード", "New Password");
            Ensure("LBL_CONFIRM_PASSWORD", "确认密码", "パスワード確認", "Confirm Password");
            Ensure("LBL_ENTER_CURRENT_PASSWORD", "请输入当前密码", "現在のパスワードを入力してください", "Enter current password");
            Ensure("LBL_ENTER_NEW_PASSWORD", "请输入新密码", "新しいパスワードを入力してください", "Enter new password");
            Ensure("LBL_ENTER_CONFIRM_PASSWORD", "请再次输入新密码", "新しいパスワードを再入力してください", "Re-enter new password");
            Ensure("BTN_CHANGE_AVATAR", "更换头像", "アバター変更", "Change Avatar");
            Ensure("BTN_LOGIN", "登录", "ログイン", "Login");
            Ensure("MSG_CURRENT_PASSWORD_REQUIRED", "请输入当前密码", "現在のパスワードを入力してください", "Current password is required");
            Ensure("MSG_PASSWORD_TOO_SHORT", "密码长度不能少于6个字符", "パスワードは6文字以上である必要があります", "Password must be at least 6 characters");
            Ensure("MSG_PASSWORD_NOT_MATCH", "两次输入的密码不一致", "パスワードが一致しません", "Passwords do not match");
            Ensure("MSG_PASSWORD_CHANGED_SUCCESS", "密码修改成功", "パスワードが正常に変更されました", "Password changed successfully");
            Ensure("MSG_CHANGE_PASSWORD_FAILED", "密码修改失败", "パスワードの変更に失敗しました", "Failed to change password");
            Ensure("MSG_FEATURE_COMING_SOON", "该功能即将上线", "この機能は近日公開予定です", "This feature is coming soon");
            Ensure("BTN_CANCEL", "取消", "キャンセル", "Cancel");
            Ensure("LBL_SAVING", "保存中", "保存中", "Saving");
            Ensure("LBL_SAVE_FAILED", "保存失败", "保存に失敗", "Save failed");
            Ensure("ERR_CUSTOMER_CODE_REQUIRED", "客户编码不能为空", "顧客コードは必須です", "Customer code is required");
            Ensure("ERR_CUSTOMER_NAME_REQUIRED", "客户名称不能为空", "顧客名は必須です", "Customer name is required");
            Ensure("ERR_CUSTOMER_CODE_EXISTS", "客户编码已存在", "顧客コードは既に存在します", "Customer code already exists");
            // View mode keys
            Ensure("MODE_BROWSE", "浏览", "閲覧", "Browse");
            Ensure("MODE_EDIT", "编辑", "編集", "Edit");
            Ensure("MODE_DESIGN", "设计", "デザイン", "Design");
            Ensure("BTN_SAVE_LAYOUT", "保存布局", "レイアウトを保存", "Save Layout");
            Ensure("BTN_GENERATE_LAYOUT", "生成布局", "レイアウトを生成", "Generate Layout");
            Ensure("LBL_DESIGN_MODE_TITLE", "设计模式", "デザインモード", "Design Mode");
            Ensure("LBL_DESIGN_MODE_DESC", "在此模式下可以调整字段布局，拖拽字段块进行排列", "このモードでフィールドのレイアウトを調整できます", "Adjust field layout in this mode");
            // Designer keys
            Ensure("BTN_EXIT_DESIGN", "退出设计", "デザインを終了", "Exit Design");
            Ensure("LBL_COMPONENTS", "组件", "コンポーネント", "Components");
            Ensure("LBL_BASIC_COMPONENTS", "基础组件", "基本コンポーネント", "Basic Components");
            Ensure("LBL_LAYOUT_COMPONENTS", "布局组件", "レイアウトコンポーネント", "Layout Components");
            Ensure("LBL_TEXTBOX", "文本框", "テキストボックス", "Textbox");
            Ensure("LBL_NUMBER", "数字框", "数値", "Number");
            Ensure("LBL_SELECT", "下拉选择", "選択", "Select");
            Ensure("LBL_CHECKBOX", "复选框", "チェックボックス", "Checkbox");
            Ensure("LBL_RADIO", "单选按钮", "ラジオボタン", "Radio");
            Ensure("LBL_TEXTAREA", "文本区域", "テキストエリア", "Textarea");
            Ensure("LBL_BUTTON", "按钮", "ボタン", "Button");
            Ensure("LBL_LABEL", "标签", "ラベル", "Label");
            Ensure("LBL_CALENDAR", "日历", "カレンダー", "Calendar");
            Ensure("LBL_LISTBOX", "列表框", "リストボックス", "Listbox");
            Ensure("LBL_SECTION", "分组", "セクション", "Section");
            Ensure("LBL_PANEL", "面板", "パネル", "Panel");
            Ensure("LBL_GRID", "网格", "グリッド", "Grid");
            Ensure("LBL_FRAME", "框架", "フレーム", "Frame");
            Ensure("LBL_TABBOX", "标签容器", "タブボックス", "Tabbox");
            Ensure("LBL_TAB", "标签页", "タブ", "Tab");
            Ensure("LBL_DRAG_COMPONENT_HERE", "拖拽组件到这里", "コンポーネントをここにドラッグ", "Drag component here");
            Ensure("LBL_PROPERTIES", "属性", "プロパティ", "Properties");
            Ensure("LBL_TEMPLATE_PROPERTIES", "模板属性", "テンプレートプロパティ", "Template Properties");
            Ensure("LBL_WIDGET_PROPERTIES", "组件属性", "コンポーネントプロパティ", "Widget Properties");
            Ensure("LBL_TEMPLATE", "模板", "テンプレート", "Template");
            Ensure("LBL_TEMPLATE_NAME", "模板名称", "テンプレート名", "Template Name");
            Ensure("LBL_USER_TEMPLATE", "用户模板", "ユーザーテンプレート", "User Template");
            Ensure("LBL_DEFAULT_TEMPLATE", "默认模板", "デフォルトテンプレート", "Default Template");
            Ensure("LBL_ENTER_TEMPLATE_NAME", "请输入模板名称", "テンプレート名を入力してください", "Enter template name");
            Ensure("LBL_ENTITY_TYPE", "实体类型", "エンティティタイプ", "Entity Type");
            Ensure("LBL_SELECT_ENTITY_TYPE", "请选择实体类型", "エンティティタイプを選択", "Select entity type");
            Ensure("LBL_ENTITY_TYPE_HINT", "此模板将用于哪种实体的数据展示", "このテンプレートはどのエンティティのデータ表示に使用されますか", "Which entity will this template be used for");
            Ensure("LBL_ENTITY_TYPE_LOCKED", "实体类型已锁定，不可修改", "エンティティタイプはロック済み、変更不可", "Entity type is locked and cannot be changed");
            Ensure("LBL_ENTITY_TYPE_NOT_SET", "未设置", "未設定", "Not Set");
            Ensure("LBL_CHANGE_ENTITY_TYPE", "更换实体类型", "エンティティタイプを変更", "Change Entity Type");
            Ensure("LBL_LOADING", "加载中...", "読み込み中...", "Loading...");
            Ensure("LBL_NO_AVAILABLE_ENTITIES", "暂无可用实体类型", "利用可能なエンティティタイプがありません", "No available entity types");
            Ensure("LBL_CLICK_TO_SELECT_ENTITY", "点击选择实体类型", "クリックしてエンティティタイプを選択", "Click to select entity type");
            Ensure("LBL_LOAD_FAILED", "加载失败", "読み込み失敗", "Load Failed");
            Ensure("LBL_RETRY", "重试", "再試行", "Retry");
            Ensure("LBL_SEARCH_PLACEHOLDER", "搜索...", "検索...", "Search...");
            Ensure("LBL_SHOWING", "显示", "表示中", "Showing");
            Ensure("LBL_ITEMS", "项", "件", "items");
            Ensure("LBL_TEMPLATE_INFO_HINT", "点击画布背景可返回模板属性；点击组件可编辑组件属性", "キャンバスの背景をクリックしてテンプレートプロパティに戻る；コンポーネントをクリックしてプロパティを編集", "Click canvas background to return to template properties; Click component to edit properties");
            Ensure("LBL_NEW_TEMPLATE", "新建模板", "新しいテンプレート", "New Template");
            Ensure("LBL_COMPONENT_TYPE", "组件类型", "コンポーネントタイプ", "Component Type");
            // Entity types - 只预置已实现的实体
            Ensure("ENTITY_CUSTOMER", "客户", "顧客", "Customer");
            Ensure("ENTITY_CUSTOMER_DESC", "客户信息管理", "顧客情報管理", "Customer information management");
            Ensure("LBL_WIDTH", "宽度", "幅", "Width");
            Ensure("LBL_DATA_SOURCE", "数据源", "データソース", "Data Source");
            Ensure("LBL_VISIBLE", "可见", "表示", "Visible");
            // Property editor keys
            Ensure("PROP_COLUMNS", "列数", "列数", "Columns");
            Ensure("PROP_GAP", "间距 (px)", "間隔 (px)", "Gap (px)");
            Ensure("PROP_PADDING", "内边距 (px)", "内側余白 (px)", "Padding (px)");
            Ensure("PROP_BACKGROUND_COLOR", "背景色", "背景色", "Background Color");
            Ensure("PROP_SHOW_BORDER", "显示边框", "枠線を表示", "Show Border");
            Ensure("PROP_TITLE", "标题", "タイトル", "Title");
            Ensure("PROP_SHOW_HEADER", "显示标题", "ヘッダーを表示", "Show Header");
            Ensure("PROP_SHOW_TITLE", "显示标题", "タイトルを表示", "Show Title");
            Ensure("PROP_COLLAPSIBLE", "可折叠", "折りたたみ可能", "Collapsible");
            Ensure("PROP_COLLAPSED_DEFAULT", "默认折叠", "デフォルトで折りたたみ", "Collapsed by Default");
            Ensure("PROP_BORDER_STYLE", "边框样式", "枠線スタイル", "Border Style");
            Ensure("PROP_BORDER_COLOR", "边框颜色", "枠線の色", "Border Color");
            Ensure("PROP_BORDER_WIDTH", "边框宽度 (px)", "枠線の幅 (px)", "Border Width (px)");
            Ensure("PROP_BORDER_RADIUS", "圆角 (px)", "角丸 (px)", "Border Radius (px)");
            Ensure("PROP_ANIMATED", "动画效果", "アニメーション", "Animated");
            Ensure("PROP_CENTERED", "居中显示", "中央揃え", "Centered");
            Ensure("PROP_SIZE", "尺寸", "サイズ", "Size");
            Ensure("PROP_TYPE", "类型", "タイプ", "Type");
            Ensure("PROP_TAB_POSITION", "标签位置", "タブの位置", "Tab Position");
            Ensure("PROP_LABEL", "标签", "ラベル", "Label");
            Ensure("PROP_GROUP_LAYOUT", "布局设置", "レイアウト設定", "Layout Settings");
            Ensure("PROP_FLEX_DIRECTION", "方向", "方向", "Direction");
            Ensure("PROP_FLEX_WRAP", "自动换行", "自動折り返し", "Flex Wrap");
            // Property option values
            Ensure("PROP_DIRECTION_ROW", "横向", "横方向", "Row");
            Ensure("PROP_DIRECTION_COLUMN", "纵向", "縦方向", "Column");
            Ensure("PROP_BORDER_SOLID", "实线", "実線", "Solid");
            Ensure("PROP_BORDER_DASHED", "虚线", "破線", "Dashed");
            Ensure("PROP_BORDER_DOTTED", "点线", "点線", "Dotted");
            Ensure("PROP_BORDER_NONE", "无边框", "枠線なし", "None");
            Ensure("PROP_SIZE_SMALL", "小", "小", "Small");
            Ensure("PROP_SIZE_DEFAULT", "默认", "デフォルト", "Default");
            Ensure("PROP_SIZE_LARGE", "大", "大", "Large");
            Ensure("PROP_TAB_TYPE_LINE", "线条", "ライン", "Line");
            Ensure("PROP_TAB_TYPE_CARD", "卡片", "カード", "Card");
            Ensure("PROP_POSITION_TOP", "顶部", "上部", "Top");
            Ensure("PROP_POSITION_BOTTOM", "底部", "下部", "Bottom");
            Ensure("PROP_POSITION_LEFT", "左侧", "左", "Left");
            Ensure("PROP_POSITION_RIGHT", "右侧", "右", "Right");
            // Property placeholders
            Ensure("PROP_PANEL_TITLE_PLACEHOLDER", "面板标题", "パネルタイトル", "Panel Title");
            Ensure("PROP_SECTION_TITLE_PLACEHOLDER", "分组标题", "セクションタイトル", "Section Title");
            // Additional property keys for all widgets
            Ensure("PROP_TEXT", "文本", "テキスト", "Text");
            Ensure("PROP_BOLD", "加粗", "太字", "Bold");
            Ensure("PROP_MIN_VALUE", "最小值", "最小値", "Min Value");
            Ensure("PROP_MAX_VALUE", "最大值", "最大値", "Max Value");
            Ensure("PROP_STEP", "步长", "ステップ", "Step");
            Ensure("PROP_ALLOW_DECIMAL", "允许小数", "小数を許可", "Allow Decimal");
            Ensure("PROP_DATE_FORMAT", "日期格式", "日付フォーマット", "Date Format");
            Ensure("PROP_SHOW_TIME", "显示时间", "時間を表示", "Show Time");
            Ensure("PROP_BUTTON_VARIANT", "按钮样式", "ボタンスタイル", "Button Variant");
            Ensure("PROP_BUTTON_PRIMARY", "主要按钮", "プライマリ", "Primary");
            Ensure("PROP_BUTTON_DEFAULT", "默认按钮", "デフォルト", "Default");
            Ensure("PROP_BUTTON_DASHED", "虚线按钮", "破線", "Dashed");
            Ensure("PROP_BUTTON_LINK", "链接按钮", "リンク", "Link");
            Ensure("PROP_BUTTON_TEXT", "文本按钮", "テキスト", "Text");
            Ensure("PROP_BUTTON_BLOCK", "块级按钮", "ブロックボタン", "Block Button");
            Ensure("PROP_BUTTON_STYLE", "按钮样式", "ボタンスタイル", "Button Style");
            Ensure("PROP_ALLOW_SEARCH", "允许搜索", "検索を許可", "Allow Search");
            Ensure("PROP_MULTI_SELECT", "多选", "複数選択", "Multi Select");
            Ensure("PROP_ROWS", "行数", "行数", "Rows");
            Ensure("PROP_AUTO_SIZE", "自动调整大小", "自動サイズ調整", "Auto Size");
            Ensure("BTN_DELETE", "删除", "削除", "Delete");
            Ensure("LBL_SELECT_COMPONENT", "选择一个组件", "コンポーネントを選択", "Select a component");
            // Menu and navigation keys
            Ensure("MENU_SETTINGS", "系统设置", "システム設定", "Settings");
            Ensure("MENU_TEMPLATES", "模板管理", "テンプレート管理", "Templates");
            Ensure("LBL_THEME", "主题", "テーマ", "Theme");
            Ensure("LBL_COLOR", "颜色", "カラー", "Color");
            Ensure("LBL_LANGUAGE", "语言", "言語", "Language");
            Ensure("LBL_USER", "用户", "ユーザー", "User");
            Ensure("THEME_LIGHT", "明亮", "ライト", "Light");
            Ensure("THEME_DARK", "暗黑", "ダーク", "Dark");
            Ensure("LBL_HEIGHT", "高度", "高さ", "Height");
            Ensure("LBL_DEFAULT_VALUE", "默认值", "デフォルト値", "Default Value");
            Ensure("LBL_PLACEHOLDER", "占位符", "プレースホルダー", "Placeholder");
            Ensure("LBL_REQUIRED", "必填", "必須", "Required");
            Ensure("LBL_READONLY", "只读", "読み取り専用", "Read Only");
            Ensure("LBL_MAX_LENGTH", "最大长度", "最大長", "Max Length");
            Ensure("LBL_NEW_LINE", "换行（新行开始）", "改行（新しい行で開始）", "New Line (Start New Row)");
            // Template designer specific keys
            Ensure("LBL_STYLE_SETTINGS", "样式设置", "スタイル設定", "Style Settings");
            Ensure("LBL_FONT_FAMILY", "字体", "フォント", "Font");
            Ensure("LBL_FONT_SIZE", "字号", "フォントサイズ", "Font Size");
            Ensure("LBL_FONT_COLOR", "字体颜色", "フォントカラー", "Font Color");
            Ensure("LBL_BG_COLOR", "背景色", "背景色", "Background Color");
            Ensure("LBL_WIDTH_UNIT", "宽度单位", "幅の単位", "Width Unit");
            Ensure("LBL_HEIGHT_UNIT", "高度单位", "高さの単位", "Height Unit");
            Ensure("PH_DEFAULT_FONT", "默认字体", "デフォルトフォント", "Default Font");
            Ensure("PH_DEFAULT", "默认", "デフォルト", "Default");
            Ensure("PH_TRANSPARENT", "透明", "透明", "Transparent");
            Ensure("FONT_MICROSOFT_YAHEI", "微软雅黑", "Microsoft YaHei", "Microsoft YaHei");
            Ensure("FONT_SIMHEI", "黑体", "SimHei", "SimHei");
            Ensure("FONT_SIMSUN", "宋体", "SimSun", "SimSun");
            Ensure("FONT_MONOSPACE", "等宽字体", "等幅フォント", "Monospace");
            Ensure("MSG_DEFAULT_TEMPLATE_SAVED_SHORT", "默认模板已保存", "デフォルトテンプレートが保存されました", "Default template saved");
            Ensure("MSG_TEMPLATE_SAVED_SHORT", "模板已保存", "テンプレートが保存されました", "Template saved");
            Ensure("MSG_SAVE_FAILED_STATUS", "保存失败: {0}", "保存に失敗: {0}", "Save failed: {0}");
            Ensure("MSG_SAVE_FAILED_ERROR", "保存失败: {0}", "保存に失敗: {0}", "Save failed: {0}");
            // Save scope toggles
            Ensure("BTN_SAVE_AS_MINE", "保存为我的", "自分用に保存", "Save as Mine");
            Ensure("BTN_SAVE_AS_DEFAULT", "保存为默认", "デフォルトとして保存", "Save as Default");
            // Color preset keys
            Ensure("COLOR_LIGHT_GRAY", "浅灰", "ライトグレー", "Light Gray");
            Ensure("COLOR_LIGHT_BLUE", "浅蓝", "ライトブルー", "Light Blue");
            Ensure("COLOR_LIGHT_YELLOW", "浅黄", "ライトイエロー", "Light Yellow");
            Ensure("COLOR_LIGHT_GREEN", "浅绿", "ライトグリーン", "Light Green");
            // Additional UI keys
            Ensure("LBL_NOT_SET", "未设置", "未設定", "Not Set");
            Ensure("ERR_LOGIN_EXPIRED", "登录过期，请重新登录", "ログインの有効期限が切れました。再度ログインしてください", "Login expired, please log in again");
            // Setup page specific keys
            Ensure("LBL_OPTIONAL_DEFAULT", "可选，默认", "オプション、デフォルト", "Optional, default");
            Ensure("PH_DEFAULT_VALUE", "默认: {0}", "デフォルト: {0}", "Default: {0}");
            Ensure("ERR_INVALID_API_BASE", "API Base地址格式不正确：{0}。请输入有效的URL（如：http://localhost:5200）", "API Baseアドレスの形式が正しくありません: {0}。有効なURLを入力してください（例: http://localhost:5200）", "Invalid API Base address: {0}. Please enter a valid URL (e.g., http://localhost:5200)");
            Ensure("ERR_ADMIN_USERNAME_EMPTY", "管理员用户名不能为空", "管理者ユーザー名は空にできません", "Admin username cannot be empty");
            Ensure("ERR_ADMIN_EMAIL_EMPTY", "管理员邮箱不能为空", "管理者メールは空にできません", "Admin email cannot be empty");
            Ensure("ERR_ADMIN_PASSWORD_EMPTY", "管理员密码不能为空", "管理者パスワードは空にできません", "Admin password cannot be empty");
            Ensure("ERR_REQUEST_TIMEOUT", "请求超时（超过15秒）。请检查API服务器 {0} 是否正在运行。\n\n请确认：\n1. API服务器已启动\n2. 在浏览器中访问 {0}/swagger 确认API可访问", "リクエストタイムアウト（15秒超過）。APIサーバー {0} が実行中か確認してください。\n\n確認事項：\n1. APIサーバーが起動している\n2. ブラウザで {0}/swagger にアクセスしてAPIにアクセス可能か確認", "Request timeout (over 15 seconds). Please check if API server {0} is running.\n\nPlease verify:\n1. API server is started\n2. Access {0}/swagger in browser to confirm API is accessible");
            Ensure("ERR_CANNOT_CONNECT_API", "无法连接到API服务器 {0}：{1}\n\n请确认：\n1. API服务器已启动（运行 BobCrm.Api 项目）\n2. API地址正确：{0}\n3. 网络连接正常\n4. 防火墙允许访问该端口", "APIサーバー {0} に接続できません: {1}\n\n確認事項：\n1. APIサーバーが起動している（BobCrm.Apiプロジェクトを実行）\n2. APIアドレスが正しい: {0}\n3. ネットワーク接続が正常\n4. ファイアウォールがポートへのアクセスを許可している", "Cannot connect to API server {0}: {1}\n\nPlease verify:\n1. API server is started (run BobCrm.Api project)\n2. API address is correct: {0}\n3. Network connection is normal\n4. Firewall allows access to the port");
            Ensure("ERR_EXCEPTION_OCCURRED", "发生错误：{0} - {1}\n\n堆栈跟踪：{2}", "エラーが発生しました: {0} - {1}\n\nスタックトレース: {2}", "Error occurred: {0} - {1}\n\nStack trace: {2}");
            Ensure("ERR_SAVE_FAILED_ADMIN", "无法保存管理员账户到 {0}\n\n尝试保存的用户信息：\n  用户名: {1}\n  邮箱: {2}\n\n错误详情：{3}\n\n请检查：\n1. API服务器是否正在运行 ({0})\n2. 在浏览器中访问 {0}/swagger 确认API是否可访问\n3. 网络连接是否正常\n4. API地址是否正确", "管理者アカウントを {0} に保存できません\n\n保存しようとしたユーザー情報：\n  ユーザー名: {1}\n  メール: {2}\n\nエラー詳細: {3}\n\n確認事項：\n1. APIサーバーが実行中か ({0})\n2. ブラウザで {0}/swagger にアクセスしてAPIにアクセス可能か確認\n3. ネットワーク接続が正常か\n4. APIアドレスが正しいか", "Cannot save admin account to {0}\n\nAttempted user info:\n  Username: {1}\n  Email: {2}\n\nError details: {3}\n\nPlease check:\n1. Is API server running ({0})\n2. Access {0}/swagger in browser to confirm API is accessible\n3. Is network connection normal\n4. Is API address correct");
            Ensure("MSG_SETTINGS_SAVED_REDIRECTING", "设置已保存，正在跳转到首页...", "設定が保存されました。ホームページにリダイレクトしています...", "Settings saved, redirecting to home...");
            Ensure("ERR_OCCURRED", "发生错误: {0}", "エラーが発生しました: {0}", "Error occurred: {0}");
            // Templates page
            Ensure("LBL_CURRENT_USER_TEMPLATE", "当前用户模板", "現在のユーザーテンプレート", "Current User Template");
            Ensure("LBL_CUSTOMER_LAYOUT", "客户详情页布局", "顧客詳細ページレイアウト", "Customer Detail Layout");
            Ensure("LBL_DEFAULT_LAYOUT_DESC", "当前用户的默认客户详情页布局模板，应用于所有客户档案", "現在のユーザーのデフォルト顧客詳細ページレイアウトテンプレート、すべての顧客に適用", "Default customer detail layout template for current user, applies to all customers");
            Ensure("LBL_LAST_UPDATED", "最后更新", "最終更新", "Last Updated");
            Ensure("LBL_WIDGET_COUNT", "个控件", "個のウィジェット", "widgets");
            Ensure("BTN_EDIT_TEMPLATE", "编辑模板", "テンプレートを編集", "Edit Template");
            Ensure("BTN_RESET_TO_DEFAULT", "重置为默认", "デフォルトにリセット", "Reset to Default");
            Ensure("LBL_ABOUT_TEMPLATE", "关于模板", "テンプレートについて", "About Template");
            Ensure("LBL_TEMPLATE_INFO", "模板是用户级别的布局配置，保存了您自定义的字段排列、宽度、高度等属性。编辑模板后，所有客户档案都会使用新的布局。未来版本将支持多模板管理功能。", "テンプレートはユーザーレベルのレイアウト設定で、カスタマイズしたフィールドの配置、幅、高さなどの属性を保存します。テンプレート編集後、すべての顧客がこの新しいレイアウトを使用します。今後のバージョンでは複数テンプレート管理をサポートします。", "Templates are user-level layout configurations that save your customized field arrangements, widths, heights and other properties. After editing the template, all customer records will use the new layout. Future versions will support multi-template management.");
            Ensure("MSG_CONFIRM_RESET_TEMPLATE", "确定要重置为默认模板吗？这将删除您的所有自定义布局设置。", "デフォルトテンプレートにリセットしますか？すべてのカスタムレイアウト設定が削除されます。", "Are you sure you want to reset to default template? This will delete all your custom layout settings.");
            Ensure("MSG_TEMPLATE_RESET_SUCCESS", "模板已重置为默认设置", "テンプレートがデフォルト設定にリセットされました", "Template has been reset to default settings");
            Ensure("MSG_TEMPLATE_RESET_FAILED", "重置失败，请稍后重试", "リセットに失敗しました。後でもう一度お試しください", "Reset failed, please try again later");
            Ensure("LBL_NOT_FOUND", "未找到", "見つかりません", "Not Found");
            Ensure("LBL_NO_TEMPLATE", "暂无模板", "テンプレートがありません", "No Template");
            Ensure("MSG_DEFAULT_TEMPLATE_SAVED", "默认模板已保存，所有用户将使用此模板", "デフォルトテンプレートが保存されました。すべてのユーザーがこのテンプレートを使用します", "Default template saved. All users will use this template.");
            Ensure("MSG_USER_TEMPLATE_SAVED", "您的模板已保存", "テンプレートが保存されました", "Your template has been saved");
        }

        await db.SaveChangesAsync();

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
