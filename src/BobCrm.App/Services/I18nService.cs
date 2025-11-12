using Microsoft.JSInterop;
using System.Text.Json;

namespace BobCrm.App.Services;

public class I18nService
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
        ["LBL_CLICK_TO_EDIT_MULTILINGUAL"] = new() { ["zh"] = "点击编辑多语言文本", ["ja"] = "クリックして多言語テキストを編集", ["en"] = "Click to edit multilingual text" },
        ["TXT_MULTILINGUAL_DISPLAY_NAME_HINT"] = new() { ["zh"] = "请为每种语言提供显示名称（至少一条）", ["ja"] = "各言語の表示名を入力してください（少なくとも1件）", ["en"] = "Provide display names for each language (at least one)" },
        ["TXT_ENTITY_NAME_PLACEHOLDER"] = new() { ["zh"] = "示例：Product、Order（首字母大写的 PascalCase）", ["ja"] = "例: Product / Order（頭文字大文字のPascalCase）", ["en"] = "Ex: Product / Order (PascalCase starting with uppercase)" },
        ["TXT_NAMESPACE_PLACEHOLDER"] = new() { ["zh"] = "BobCrm.Domain.<实体名>", ["ja"] = "BobCrm.Domain.<エンティティ名>", ["en"] = "BobCrm.Domain.<EntityName>" },
        ["TXT_NAMESPACE_AUTO_HINT"] = new() { ["zh"] = "命名空间由系统根据实体名自动生成，保存后不可修改。", ["ja"] = "名前空間はエンティティ名から自動生成され、保存後は変更できません。", ["en"] = "Namespace is generated from the entity name automatically and cannot be edited after save." },
        ["LBL_DOMAIN"] = new() { ["zh"] = "所属领域", ["ja"] = "ビジネス領域", ["en"] = "Business Domain" },
        ["TXT_DOMAIN_HINT"] = new() { ["zh"] = "请选择该实体所在的业务域，用于生成命名空间。", ["ja"] = "エンティティが属する業務領域を選択してください。", ["en"] = "Select the business domain used to build the namespace." },
        ["TXT_ICON_FIELD_PLACEHOLDER"] = new() { ["zh"] = "输入 AntD 图标名或图片 URL", ["ja"] = "AntD アイコン名または画像 URL を入力", ["en"] = "Enter AntD icon name or image URL" },
        ["TXT_ICON_FIELD_HINT"] = new() { ["zh"] = "示例：icon:appstore 或 https://static.example.com/icon.svg", ["ja"] = "例: icon:appstore または https://static.example.com/icon.svg", ["en"] = "Ex: icon:appstore or https://static.example.com/icon.svg" },
        ["TXT_CATEGORY_PLACEHOLDER"] = new() { ["zh"] = "输入分组或菜单名称", ["ja"] = "グループ/カテゴリ名を入力", ["en"] = "Enter grouping or menu name" },
        ["TXT_INTERFACE_AUTO_FIELDS_TIP"] = new() { ["zh"] = "勾选接口后会自动注入标准字段，字段不可单独删除，如需移除请取消勾选接口。", ["ja"] = "インターフェースを選択すると標準フィールドが自動追加されます。削除するにはチェックを外してください。", ["en"] = "Selecting an interface injects its standard fields automatically. To remove them, uncheck the interface rather than deleting fields." }
    };
}
