using Microsoft.JSInterop;

using System.Text.Json;

namespace BobCrm.App.Services;

using BobCrm.Api.Abstractions;

public class I18nService : II18nService

{

    private readonly IHttpClientFactory _httpFactory;

    private readonly AuthService _auth;

    private readonly IJSRuntime _js;

    private readonly SemaphoreSlim _loadGate = new(1, 1);

    private readonly object _retryLock = new();

    private CancellationTokenSource? _retryCts;

    private Dictionary<string, string> _dict = new(StringComparer.OrdinalIgnoreCase);

    public string CurrentLang { get; private set; } = "ja";

    public bool IsLoaded => _dict.Count > 0;

    public event Action? OnChanged;

    public I18nService(IHttpClientFactory httpFactory, AuthService auth, IJSRuntime js)

    {

        _httpFactory = httpFactory; _auth = auth; _js = js;

    }

    public string T(string key)

    {

        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        if (_dict.TryGetValue(key, out var v))

        {

            return v;

        }

        return ResolveFallback(key);

    }

    public Task LoadAsync(string lang, bool force = false, CancellationToken ct = default) =>

        LoadInternalAsync(lang, force, scheduleRetry: true, ct);

    private async Task LoadInternalAsync(string lang, bool force, bool scheduleRetry, CancellationToken ct)

    {

        lang = (lang ?? "ja").ToLowerInvariant();

        if (!force && IsLoaded && string.Equals(CurrentLang, lang, StringComparison.OrdinalIgnoreCase))

        {

            return;

        }

        await _loadGate.WaitAsync(ct);

        try

        {

            var http = await _auth.CreateClientWithLangAsync();

            using var resp = await http.GetAsync($"/api/i18n/{lang}", ct);

            if (!resp.IsSuccessStatusCode)

            {

                if (scheduleRetry)

                {

                    ScheduleRetry(lang);

                }

                await HandleLoadFailureAsync();

                return;

            }

            using var stream = await resp.Content.ReadAsStreamAsync(ct);

            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var p in doc.RootElement.EnumerateObject())

            {

                map[p.Name] = p.Value.GetString() ?? p.Name;

            }

            _dict = map;

            CurrentLang = lang;

            CancelScheduledRetry();

            OnChanged?.Invoke();

            try { await _js.InvokeVoidAsync("bobcrm.setLang", lang); } catch { }

            try { await _js.InvokeVoidAsync("bobcrm.setCookie", "lang", lang, 365); } catch { }

        }

        catch (OperationCanceledException)

        {

            throw;

        }

        catch

        {

            if (scheduleRetry)

            {

                ScheduleRetry(lang);

            }

            await HandleLoadFailureAsync();

        }

        finally

        {

            _loadGate.Release();

        }

    }

    private void ScheduleRetry(string lang)

    {

        lock (_retryLock)

        {

            _retryCts?.Cancel();

            _retryCts?.Dispose();

            _retryCts = new CancellationTokenSource();

            var token = _retryCts.Token;

            _ = Task.Run(() => RetryAsync(lang, token), CancellationToken.None);

        }

    }

    private async Task RetryAsync(string lang, CancellationToken token)

    {

        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)

        {

            try

            {

                var backoff = TimeSpan.FromSeconds(Math.Min(5, attempt * 2));

                await Task.Delay(backoff, token);

                await LoadInternalAsync(lang, force: true, scheduleRetry: false, token);

                if (IsLoaded && string.Equals(CurrentLang, lang, StringComparison.OrdinalIgnoreCase))

                {

                    return;

                }

            }

            catch (OperationCanceledException)

            {

                return;

            }

            catch

            {

                // swallow and continue to next attempt

            }

        }

    }

    private void CancelScheduledRetry()

    {

        lock (_retryLock)

        {

            _retryCts?.Cancel();

            _retryCts?.Dispose();

            _retryCts = null;

        }

    }

    private Task HandleLoadFailureAsync()

    {

        // Keep the last successful dictionary so the UI does not regress to raw keys.

        if (_dict.Count == 0)

        {

            _dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        }

        OnChanged?.Invoke();

        return Task.CompletedTask;

    }

    private string ResolveFallback(string key)

    {

        if (!Fallbacks.TryGetValue(key, out var map))

        {

            return key;

        }

        var lang = (CurrentLang ?? "ja").ToLowerInvariant();

        if (map.TryGetValue(lang, out var localized) && !string.IsNullOrWhiteSpace(localized))

        {

            return localized!;

        }

        // fallback to English if specific locale missing

        if (map.TryGetValue("en", out var english) && !string.IsNullOrWhiteSpace(english))

        {

            return english!;

        }

        return key;

    }

    private static readonly Dictionary<string, Dictionary<string, string>> Fallbacks = new(StringComparer.OrdinalIgnoreCase)

    {

        ["TXT_DASH_EYEBROW"] = new() { ["zh"] = "工作总览", ["ja"] = "ワークスペース概況", ["en"] = "Workspace Snapshot" },

        ["TXT_DASH_TITLE"] = new() { ["zh"] = "客户体验总览", ["ja"] = "顧客体験の概況", ["en"] = "Customer Experience Overview" },

        ["TXT_DASH_SUBTITLE"] = new() { ["zh"] = "快速掌握客户、项目与团队的关键脉搏。", ["ja"] = "顧客・案件・チームの脈をワンビューで把握。", ["en"] = "See key signals from customers, projects, and teams in one view." },

        ["LBL_METRIC_PIPELINE"] = new() { ["zh"] = "销售管道", ["ja"] = "パイプライン", ["en"] = "Pipeline" },

        ["TXT_METRIC_PIPELINE_TREND"] = new() { ["zh"] = "+8% 环比", ["ja"] = "+8% 前月比", ["en"] = "+8% MoM" },

        ["LBL_METRIC_ACTIVE"] = new() { ["zh"] = "活跃项目", ["ja"] = "稼働中案件", ["en"] = "Active Projects" },

        ["TXT_METRIC_ACTIVE_TREND"] = new() { ["zh"] = "+3 新建", ["ja"] = "+3 新規", ["en"] = "+3 new" },

        ["LBL_METRIC_DAYS"] = new() { ["zh"] = "平均周期", ["ja"] = "平均サイクル", ["en"] = "Avg Cycle" },

        ["TXT_METRIC_DAYS_TREND"] = new() { ["zh"] = "下降 1.2d", ["ja"] = "1.2 日短縮", ["en"] = "Down 1.2d" },

        ["LBL_METRIC_FEEDBACK"] = new() { ["zh"] = "反馈数", ["ja"] = "フィードバック", ["en"] = "Feedback" },

        ["TXT_METRIC_FEEDBACK_TREND"] = new() { ["zh"] = "+2 本周新增", ["ja"] = "今週 +2", ["en"] = "+2 this week" },

        ["LBL_DASH_CUSTOMERS"] = new() { ["zh"] = "客户总数", ["ja"] = "顧客総数", ["en"] = "Customers" },

        ["TXT_DASH_CUSTOMERS_HINT"] = new() { ["zh"] = "新增 6", ["ja"] = "+6 追加", ["en"] = "+6 new" },

        ["LBL_DASH_PROJECTS"] = new() { ["zh"] = "项目", ["ja"] = "プロジェクト", ["en"] = "Projects" },

        ["TXT_DASH_PROJECTS_HINT"] = new() { ["zh"] = "进行中", ["ja"] = "進行中", ["en"] = "In progress" },

        ["LBL_DASH_TOUCHES"] = new() { ["zh"] = "互动次数", ["ja"] = "タッチポイント", ["en"] = "Touches" },

        ["TXT_DASH_TOUCHES_HINT"] = new() { ["zh"] = "本周互动", ["ja"] = "今週のタッチポイント", ["en"] = "Touches this week" },

        ["LBL_DASH_SAT"] = new() { ["zh"] = "满意度", ["ja"] = "満足度", ["en"] = "Satisfaction" },

        ["TXT_DASH_SAT_HINT"] = new() { ["zh"] = "客服满意", ["ja"] = "サポート満足度", ["en"] = "Support satisfaction" },

        ["LBL_DASH_SEGMENTS"] = new() { ["zh"] = "客户分层", ["ja"] = "セグメント", ["en"] = "Customer Segments" },

        ["LBL_SEG_ENTERPRISE"] = new() { ["zh"] = "企业客户", ["ja"] = "エンタープライズ顧客", ["en"] = "Enterprise Accounts" },

        ["TXT_SEG_ENTERPRISE_DESC"] = new() { ["zh"] = "关键账户群", ["ja"] = "主要アカウント群", ["en"] = "Core accounts" },

        ["LBL_SEG_GROWTH"] = new() { ["zh"] = "成长型团队", ["ja"] = "成長チーム", ["en"] = "Growth Teams" },

        ["TXT_SEG_GROWTH_DESC"] = new() { ["zh"] = "扩张中的团队", ["ja"] = "拡大中のチーム", ["en"] = "Teams in expansion" },

        ["LBL_SEG_TRIAL"] = new() { ["zh"] = "试用期", ["ja"] = "トライアル期間", ["en"] = "Trial Window" },

        ["TXT_SEG_TRIAL_DESC"] = new() { ["zh"] = "正在评估的客户", ["ja"] = "評価中の顧客", ["en"] = "Evaluating customers" },

        ["LBL_DASH_ACTIVITY"] = new() { ["zh"] = "动态流", ["ja"] = "アクティビティ", ["en"] = "Activity Stream" },

        ["TXT_DASH_ACTIVITY_HINT"] = new() { ["zh"] = "最近 24 小时的关键事件。", ["ja"] = "直近24時間の主要イベント。", ["en"] = "Key events in the last 24 hours." },

        ["TXT_ACTIVITY_NOVA_DESC"] = new() { ["zh"] = "提交新的集成需求", ["ja"] = "新しい連携要望を提出", ["en"] = "Submitted a new integration request" },

        ["TXT_ACTIVITY_HELIOS_DESC"] = new() { ["zh"] = "SLA 审阅完成", ["ja"] = "SLA レビュー完了", ["en"] = "SLA review completed" },

        ["TXT_ACTIVITY_ACME_DESC"] = new() { ["zh"] = "反馈调查回传", ["ja"] = "フィードバック調査を返信", ["en"] = "Returned feedback survey" },

        ["TXT_TIME_YESTERDAY"] = new() { ["zh"] = "昨天", ["ja"] = "昨日", ["en"] = "Yesterday" },

        ["LBL_DASH_NEXT_STEPS"] = new() { ["zh"] = "下一步行动", ["ja"] = "次のアクション", ["en"] = "Next Actions" },

        ["BTN_RECORD_NEXT_TOUCH"] = new() { ["zh"] = "记录下一次跟进", ["ja"] = "次のフォローを記録", ["en"] = "Log next touch" },

        ["BTN_NOTIFY_ACCOUNT"] = new() { ["zh"] = "提醒账户经理", ["ja"] = "アカウント担当に通知", ["en"] = "Notify account manager" },

        ["BTN_IMPORT_CONTACTS"] = new() { ["zh"] = "导入外部联系人", ["ja"] = "外部連絡先をインポート", ["en"] = "Import external contacts" },

        ["BTN_SAVE_ROLE"] = new() { ["zh"] = "保存角色", ["ja"] = "ロールを保存", ["en"] = "Save Role" },

        ["BTN_SAVE_PERMISSIONS"] = new() { ["zh"] = "保存权限", ["ja"] = "権限を保存", ["en"] = "Save Permissions" },

        ["MENU_USERS"] = new() { ["zh"] = "用户档案", ["ja"] = "ユーザー台帳", ["en"] = "User Profiles" },
        ["MENU_MENU_MANAGE"] = new() { ["zh"] = "菜单管理", ["ja"] = "メニュー管理", ["en"] = "Menu Management" },
        ["TXT_MENU_MANAGE_DESC"] = new() { ["zh"] = "维护功能树、菜单路由与模板绑定。", ["ja"] = "機能ツリー・メニュー経路・テンプレート割当をメンテナンスします。", ["en"] = "Maintain the function tree, menu routes, and template bindings." },
        ["BTN_ADD_ROOT_MENU"] = new() { ["zh"] = "新增根菜单", ["ja"] = "ルートメニューを追加", ["en"] = "Add Root Menu" },
        ["BTN_ADD_CHILD_MENU"] = new() { ["zh"] = "新增子菜单", ["ja"] = "子メニューを追加", ["en"] = "Add Child Menu" },
        ["BTN_DELETE_MENU"] = new() { ["zh"] = "删除菜单", ["ja"] = "メニューを削除", ["en"] = "Delete Menu" },
        ["BTN_ADD_MENU"] = new() { ["zh"] = "新增菜单", ["ja"] = "メニューを追加", ["en"] = "Add Menu" },
        ["BTN_REFRESH"] = new() { ["zh"] = "刷新", ["ja"] = "再読み込み", ["en"] = "Refresh" },
        ["LBL_MENU_DETAIL"] = new() { ["zh"] = "菜单详情", ["ja"] = "メニュー詳細", ["en"] = "Menu Details" },
        ["LBL_MENU_PARENT"] = new() { ["zh"] = "上级菜单", ["ja"] = "親メニュー", ["en"] = "Parent Menu" },
        ["LBL_MENU_ROOT"] = new() { ["zh"] = "根节点", ["ja"] = "ルート", ["en"] = "Root" },
        ["LBL_MENU_NAME"] = new() { ["zh"] = "菜单名称", ["ja"] = "メニュー名", ["en"] = "Menu Name" },
        ["LBL_MENU_CODE"] = new() { ["zh"] = "菜单编码", ["ja"] = "メニューコード", ["en"] = "Menu Code" },
        ["LBL_MENU_I18N"] = new() { ["zh"] = "多语言名称", ["ja"] = "多言語名称", ["en"] = "Multilingual Name" },
        ["LBL_MENU_ICON"] = new() { ["zh"] = "图标", ["ja"] = "アイコン", ["en"] = "Icon" },
        ["LBL_MENU_SORT"] = new() { ["zh"] = "排序值", ["ja"] = "並び順", ["en"] = "Sort Order" },
        ["LBL_MENU_IS_MENU"] = new() { ["zh"] = "作为菜单显示", ["ja"] = "メニューとして表示", ["en"] = "Show as Menu" },
        ["LBL_MENU_NAVIGATION_TYPE"] = new() { ["zh"] = "导航方式", ["ja"] = "ナビゲーション方式", ["en"] = "Navigation Type" },
        ["LBL_MENU_NAV_ROUTE"] = new() { ["zh"] = "路由", ["ja"] = "ルート", ["en"] = "Route" },
        ["LBL_MENU_NAV_TEMPLATE"] = new() { ["zh"] = "模板", ["ja"] = "テンプレート", ["en"] = "Template" },
        ["LBL_MENU_TEMPLATE"] = new() { ["zh"] = "模板", ["ja"] = "テンプレート", ["en"] = "Template" },
        ["LBL_MENU_TEMPLATE_SELECT"] = new() { ["zh"] = "选择模板", ["ja"] = "テンプレートを選択", ["en"] = "Select Template" },
        ["LBL_MENU_ROUTE"] = new() { ["zh"] = "路由地址", ["ja"] = "ルート", ["en"] = "Route" },
        ["TXT_MENU_TEMPLATE_EMPTY"] = new() { ["zh"] = "暂无可用模板。", ["ja"] = "利用可能なテンプレートがありません。", ["en"] = "No templates available." },
        ["TXT_MENU_DROP_ROOT"] = new() { ["zh"] = "拖拽到这里将节点移动到根级", ["ja"] = "ここにドロップするとルートに移動します", ["en"] = "Drop here to move the node to the root" },
        ["TXT_MENU_EMPTY"] = new() { ["zh"] = "暂无菜单节点。", ["ja"] = "メニューがまだありません。", ["en"] = "No menu nodes yet." },
        ["TXT_MENU_SELECT_HINT"] = new() { ["zh"] = "选择左侧菜单查看详情", ["ja"] = "左のメニューを選択して詳細を表示", ["en"] = "Select a menu item to view details." },
        ["MSG_MENU_DELETE_CONFIRM"] = new() { ["zh"] = "确认删除该菜单节点？此操作不可恢复。", ["ja"] = "このメニューを削除しますか？この操作は取り消せません。", ["en"] = "Delete this menu node? This action cannot be undone." },
        ["MSG_MENU_DELETE_FAILED"] = new() { ["zh"] = "删除菜单失败", ["ja"] = "メニューの削除に失敗しました", ["en"] = "Failed to delete menu." },
        ["MSG_MENU_DELETE_SUCCESS"] = new() { ["zh"] = "菜单已删除", ["ja"] = "メニューを削除しました", ["en"] = "Menu deleted." },
        ["MSG_MENU_SAVE_FAILED"] = new() { ["zh"] = "保存菜单失败", ["ja"] = "メニューの保存に失敗しました", ["en"] = "Failed to save menu." },
        ["MSG_MENU_CREATE_SUCCESS"] = new() { ["zh"] = "菜单创建成功", ["ja"] = "メニューを作成しました", ["en"] = "Menu created successfully." },
        ["MSG_MENU_SAVE_SUCCESS"] = new() { ["zh"] = "菜单已更新", ["ja"] = "メニューを更新しました", ["en"] = "Menu updated." },
        ["MSG_MENU_REORDER_FAILED"] = new() { ["zh"] = "排序操作失败，请刷新后重试", ["ja"] = "並び替えに失敗しました。再読み込みしてやり直してください。", ["en"] = "Failed to reorder menus. Refresh and try again." },
        ["MSG_MENU_INVALID_DROP"] = new() { ["zh"] = "不能将菜单移动到自身或其子节点下", ["ja"] = "自分自身または子ノードの下には移動できません", ["en"] = "Cannot move a menu under itself or its descendants." },
        ["MSG_MENU_LOAD_FAILED"] = new() { ["zh"] = "加载菜单失败", ["ja"] = "メニューの読み込みに失敗しました", ["en"] = "Failed to load menus." },
        ["MSG_MENU_CODE_REQUIRED"] = new() { ["zh"] = "请输入唯一的菜单编码", ["ja"] = "一意のメニューコードを入力してください", ["en"] = "Menu code is required." },

        ["BTN_NEW_USER"] = new() { ["zh"] = "新建用户", ["ja"] = "ユーザー新規作成", ["en"] = "New User" },

        ["BTN_SAVE_USER"] = new() { ["zh"] = "保存用户", ["ja"] = "ユーザーを保存", ["en"] = "Save User" },

        ["TXT_USER_EMPTY"] = new() { ["zh"] = "暂无用户，请先新建。", ["ja"] = "ユーザーがありません。新規作成してください。", ["en"] = "No users yet. Create one to get started." },

        ["TXT_USER_SELECT_HINT"] = new() { ["zh"] = "选择一个用户查看详情", ["ja"] = "ユーザーを選択して詳細を表示", ["en"] = "Select a user to view details." },

        ["MSG_USER_REQUIRED"] = new() { ["zh"] = "用户名和邮箱不能为空", ["ja"] = "ユーザー名とメールは必須です", ["en"] = "Username and email are required." },

        ["LBL_USER_INFO"] = new() { ["zh"] = "用户信息", ["ja"] = "ユーザー情報", ["en"] = "User Information" },

        ["LBL_USER_STATUS"] = new() { ["zh"] = "用户状态", ["ja"] = "ユーザー状態", ["en"] = "User Status" },

        ["LBL_USER_ROLES"] = new() { ["zh"] = "角色分配", ["ja"] = "ロール割り当て", ["en"] = "Assigned Roles" },

        ["LBL_USER_LOCKED"] = new() { ["zh"] = "已锁定", ["ja"] = "ロック中", ["en"] = "Locked" },

        ["LBL_USER_UNLOCKED"] = new() { ["zh"] = "正常", ["ja"] = "有効", ["en"] = "Active" },

        ["LBL_EMAIL_CONFIRMED"] = new() { ["zh"] = "邮箱已验证", ["ja"] = "メール確認済み", ["en"] = "Email Confirmed" },

        ["LBL_EMAIL_CONFIRMED_FLAG"] = new() { ["zh"] = "邮箱验证", ["ja"] = "メール確認", ["en"] = "Email Confirmation" },

        ["LBL_PHONE"] = new() { ["zh"] = "电话", ["ja"] = "電話", ["en"] = "Phone" },

        ["LBL_CLICK_TO_EDIT_MULTILINGUAL"] = new() { ["zh"] = "点击编辑多语言文本", ["ja"] = "クリックして多言語テキストを編集", ["en"] = "Click to edit multilingual text" },

        ["TXT_MULTILINGUAL_DISPLAY_NAME_HINT"] = new() { ["zh"] = "请为每种语言提供显示名称（至少一条）", ["ja"] = "各言語の表示名を入力してください（少なくとも1件）", ["en"] = "Provide display names for each language (at least one)" },

        ["TXT_ENTITY_NAME_PLACEHOLDER"] = new() { ["zh"] = "示例：Product、Order（首字母大写的 PascalCase）", ["ja"] = "例: Product / Order（頭文字大文字のPascalCase）", ["en"] = "Ex: Product / Order (PascalCase starting with uppercase)" },

        ["LBL_ROLE_STATUS"] = new() { ["zh"] = "角色状态", ["ja"] = "ロール状態", ["en"] = "Role Status" },

        ["LBL_ROLE_ENABLED"] = new() { ["zh"] = "启用", ["ja"] = "有効", ["en"] = "Enabled" },

        ["LBL_ROLE_DISABLED"] = new() { ["zh"] = "停用", ["ja"] = "無効", ["en"] = "Disabled" },

        ["LBL_ROLE_PERMISSIONS"] = new() { ["zh"] = "权限设置", ["ja"] = "権限設定", ["en"] = "Permissions" },

        ["TXT_ROLE_EMPTY"] = new() { ["zh"] = "暂无角色，请先新建。", ["ja"] = "ロールがありません。新規作成してください。", ["en"] = "No roles yet. Create one to get started." },

        ["TXT_ROLE_SELECT_HINT"] = new() { ["zh"] = "选择一个角色查看详情", ["ja"] = "ロールを選択して詳細を表示", ["en"] = "Select a role to view details." },

        ["TXT_ROLE_NO_FUNCTION"] = new() { ["zh"] = "暂无可配置的功能", ["ja"] = "設定可能な機能がありません", ["en"] = "No functions available to assign." },

        ["MSG_ROLE_REQUIRED"] = new() { ["zh"] = "角色编码和名称不能为空", ["ja"] = "ロールコードと名前は必須です", ["en"] = "Role code and name are required." },

        ["TXT_NAMESPACE_PLACEHOLDER"] = new() { ["zh"] = "BobCrm.Base.<实体名>", ["ja"] = "BobCrm.Base.<エンティティ名>", ["en"] = "BobCrm.Base.<EntityName>" },

        ["TXT_NAMESPACE_AUTO_HINT"] = new() { ["zh"] = "命名空间由系统根据实体名自动生成，保存后不可修改。", ["ja"] = "名前空間はエンティティ名から自動生成され、保存後は変更できません。", ["en"] = "Namespace is generated from the entity name automatically and cannot be edited after save." },

        ["LBL_DOMAIN"] = new() { ["zh"] = "所属领域", ["ja"] = "ビジネス領域", ["en"] = "Business Domain" },

        ["TXT_DOMAIN_HINT"] = new() { ["zh"] = "请选择该实体所在的业务域，用于生成命名空间。", ["ja"] = "エンティティが属する業務領域を選択してください。", ["en"] = "Select the business domain used to build the namespace." },

        ["TXT_ICON_FIELD_PLACEHOLDER"] = new() { ["zh"] = "输入 AntD 图标名或图片 URL", ["ja"] = "AntD アイコン名または画像 URL を入力", ["en"] = "Enter AntD icon name or image URL" },

        ["TXT_ICON_FIELD_HINT_TITLE"] = new() { ["zh"] = "可选格式", ["ja"] = "利用できる形式", ["en"] = "Allowed formats" },

        ["TXT_ICON_FIELD_HINT_ANTD"] = new() { ["zh"] = "Ant Design 图标名：例如 user、team、setting", ["ja"] = "Ant Design アイコン名: 例) user / team / setting", ["en"] = "Ant Design icon name, e.g., user, team, setting" },

        ["TXT_ICON_FIELD_HINT_URL"] = new() { ["zh"] = "或图标图片 URL：例如 https://static.example.com/icon.svg", ["ja"] = "またはアイコン画像の URL: 例) https://static.example.com/icon.svg", ["en"] = "Or an image URL, e.g., https://static.example.com/icon.svg" },

        ["TXT_DELETE_FIELD_WARNING"] = new() { ["zh"] = "删除后无法恢复该字段，请确认操作。", ["ja"] = "削除すると元に戻せません。よろしいですか？", ["en"] = "This field will be removed permanently. Continue?" },

        ["TXT_MULTILINGUAL_PLACEHOLDER"] = new() { ["zh"] = "点击编辑多语言", ["ja"] = "クリックして多言語を編集", ["en"] = "Click to edit translations" },

        ["TXT_MULTILINGUAL_PLACEHOLDER_DEFAULT"] = new() { ["zh"] = "默认语言内容", ["ja"] = "既定言語の内容", ["en"] = "Default language text" },

        ["TXT_MULTILINGUAL_PLACEHOLDER_OTHER"] = new() { ["zh"] = "翻译内容", ["ja"] = "翻訳内容", ["en"] = "Translation" },

        ["LBL_MULTILINGUAL_EDITOR"] = new() { ["zh"] = "多语言编辑", ["ja"] = "多言語エディター", ["en"] = "Multilingual Editor" },

        ["BTN_DONE"] = new() { ["zh"] = "完成", ["ja"] = "完了", ["en"] = "Done" },

        ["LBL_FIELD_UNNAMED"] = new() { ["zh"] = "未命名字段", ["ja"] = "未命名フィールド", ["en"] = "Unnamed field" },

        ["MSG_FIELD_PROPERTY_REQUIRED"] = new() { ["zh"] = "字段 {0} 的编码不能为空", ["ja"] = "フィールド {0} のコードは必須です", ["en"] = "Field {0} requires a property name." },

        ["MSG_FIELD_DISPLAYNAME_REQUIRED"] = new() { ["zh"] = "字段 {0} 需要填写显示名称", ["ja"] = "フィールド {0} の表示名を入力してください", ["en"] = "Field {0} requires a display name." },

        ["MSG_FIELD_TYPE_REQUIRED"] = new() { ["zh"] = "字段 {0} 需要选择数据类型", ["ja"] = "フィールド {0} のデータ型を選択してください", ["en"] = "Field {0} requires a data type." },

        ["MSG_FIELD_STRING_LENGTH_REQUIRED"] = new() { ["zh"] = "字符串字段 {0} 需要设置长度", ["ja"] = "文字列フィールド {0} の長さを入力してください", ["en"] = "String field {0} requires a length." },

        ["MSG_FIELD_DECIMAL_REQUIRED"] = new() { ["zh"] = "小数字段 {0} 需要设置精度和小数位", ["ja"] = "小数フィールド {0} の精度と小数桁を入力してください", ["en"] = "Decimal field {0} requires precision and scale." },

        ["MSG_DOMAIN_LOAD_FAILED"] = new() { ["zh"] = "领域列表加载失败", ["ja"] = "ドメイン一覧の読み込みに失敗しました", ["en"] = "Failed to load domains." },

        ["TXT_INTERFACE_AUTO_FIELDS_TIP"] = new() { ["zh"] = "勾选接口后会自动注入标准字段，字段不可单独删除，如需移除请取消勾选接口。", ["ja"] = "インターフェースを選択すると標準フィールドが自動追加されます。削除するにはチェックを外してください。", ["en"] = "Selecting an interface injects its standard fields automatically. To remove them, uncheck the interface rather than deleting fields." },

        ["INTERFACE_ORGANIZATION"] = new() { ["zh"] = "组织维度", ["ja"] = "組織ディメンション", ["en"] = "Organization" },

        ["INTERFACE_ORGANIZATION_DESC"] = new() { ["zh"] = "IOrganizational - OrganizationId", ["ja"] = "IOrganizational - OrganizationId", ["en"] = "IOrganizational - OrganizationId" },

        ["LBL_INTERFACE_ORGANIZATION"] = new() { ["zh"] = "Organization (IOrganizational - 组织ID)", ["ja"] = "Organization (IOrganizational - 組織ID)", ["en"] = "Organization (IOrganizational - Organization Id)" },

        ["MENU_ORG"] = new() { ["zh"] = "组织关系", ["ja"] = "組織管理", ["en"] = "Organizations" },

        ["LBL_ORG_TREE"] = new() { ["zh"] = "组织树", ["ja"] = "組織ツリー", ["en"] = "Organization Tree" },

        ["BTN_ADD_ORG"] = new() { ["zh"] = "新增节点", ["ja"] = "ノード追加", ["en"] = "Add Node" },

        ["TXT_ORG_EMPTY"] = new() { ["zh"] = "暂无组织，请先创建根节点。", ["ja"] = "組織がありません。まずはルートを作成してください。", ["en"] = "No organization nodes yet. Create the root first." },

        ["LBL_ORG_ROOT"] = new() { ["zh"] = "根组织", ["ja"] = "ルート組織", ["en"] = "Root Organization" },

        ["LBL_ORG_CODE"] = new() { ["zh"] = "组织编码", ["ja"] = "組織コード", ["en"] = "Code" },

        ["LBL_ORG_NAME"] = new() { ["zh"] = "组织名称", ["ja"] = "組織名", ["en"] = "Name" },

        ["LBL_ORG_PATH"] = new() { ["zh"] = "层级编码", ["ja"] = "階層コード", ["en"] = "Path" },

        ["TXT_ORG_NO_CHILD"] = new() { ["zh"] = "暂无子节点。", ["ja"] = "子ノードがありません。", ["en"] = "No child nodes yet." },

        ["MSG_ORG_DELETE_CONFIRM"] = new() { ["zh"] = "确定要删除该组织节点吗？", ["ja"] = "この組織ノードを削除しますか？", ["en"] = "Delete this organization node?" },

        ["MSG_ORG_ROOT_EXISTS"] = new() { ["zh"] = "已存在根组织，无法重复创建。", ["ja"] = "ルート組織はすでに存在します。", ["en"] = "A root organization already exists." },

        ["MSG_ORG_CODE_REQUIRED"] = new() { ["zh"] = "请填写组织编码。", ["ja"] = "組織コードを入力してください。", ["en"] = "Organization code is required." },

        ["MSG_ORG_NAME_REQUIRED"] = new() { ["zh"] = "请填写组织名称。", ["ja"] = "組織名を入力してください。", ["en"] = "Organization name is required." },

        ["MSG_ORG_SELECT_PARENT"] = new() { ["zh"] = "请选择一个父级组织后再新增子节点。", ["ja"] = "子ノードを追加する前に親組織を選択してください。", ["en"] = "Select a parent organization before adding a child." },

        ["MSG_ORG_SAVE_PARENT_FIRST"] = new() { ["zh"] = "请先保存当前节点，再创建子节点。", ["ja"] = "子ノードを追加する前に親ノードを保存してください。", ["en"] = "Save the current node before adding child nodes." },

        ["LBL_ORG_DETAIL"] = new() { ["zh"] = "节点信息", ["ja"] = "ノード情報", ["en"] = "Node Details" },

        ["TXT_ORG_DETAIL_HINT"] = new() { ["zh"] = "编辑当前选中节点的编码与名称，保存后同步至组织树。", ["ja"] = "選択中ノードのコードと名称を編集し、保存するとツリーに反映されます。", ["en"] = "Edit the selected node's code and name. Save to update the tree." },

        ["LBL_ORG_CHILDREN"] = new() { ["zh"] = "子节点列表", ["ja"] = "子ノード一覧", ["en"] = "Children" },

        ["MSG_FORM_VALIDATION_FAILED"] = new() { ["zh"] = "表单校验失败，请先修正错误字段。", ["ja"] = "入力内容の検証に失敗しました。エラー項目を修正してください。", ["en"] = "Validation failed. Please fix the highlighted fields." },

        ["MSG_VALIDATION_REQUIRED"] = new() { ["zh"] = "必填项", ["ja"] = "必須項目です", ["en"] = "Required." },

        ["MSG_VALIDATION_MIN_LENGTH"] = new() { ["zh"] = "最少输入 {0} 个字符", ["ja"] = "{0} 文字以上で入力してください", ["en"] = "Min length: {0}." },

        ["MSG_VALIDATION_MAX_LENGTH"] = new() { ["zh"] = "最多输入 {0} 个字符", ["ja"] = "{0} 文字以内で入力してください", ["en"] = "Max length: {0}." },

        ["MSG_VALIDATION_PATTERN"] = new() { ["zh"] = "格式不正确", ["ja"] = "形式が正しくありません", ["en"] = "Invalid format." }

    };

}

