using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Net.Http.Json;
using BobCrm.Api.Abstractions;
using BobCrm.App.Models;
using BobCrm.App.Models.Widgets;
using BobCrm.App.Services;
using BobCrm.App.Services.Runtime;
using BobCrm.App.Services.Widgets;
using Microsoft.Extensions.Logging;

namespace BobCrm.App.ViewModels;

public sealed class PageLoaderViewModel : INotifyPropertyChanged
{
    private readonly AuthService _auth;
    private readonly II18nService _i18n;
    private readonly IJsInteropService _js;
    private readonly TemplateRuntimeClient _templateRuntime;
    private readonly LegacyLayoutParser _legacyLayoutParser;
    private readonly RuntimeLabelService _labelService;
    private readonly ILogger<PageLoaderViewModel> _logger;

    private bool _loading = true;
    private bool _saving;
    private string? _error;
    private string? _validationError;
    private bool _isEditMode;
    private JsonElement? _entityData;
    private object? _boundData;
    private List<DraggableWidget> _layoutWidgets = new();
    private TemplateRuntimeResponse? _runtimeContext;
    private string[] _appliedScopes = Array.Empty<string>();
    private string _editCode = string.Empty;
    private string _editName = string.Empty;
    private string _entityType = string.Empty;
    private int _id;

    public PageLoaderViewModel(
        AuthService auth,
        II18nService i18n,
        IJsInteropService js,
        TemplateRuntimeClient templateRuntime,
        LegacyLayoutParser legacyLayoutParser,
        RuntimeLabelService labelService,
        ILogger<PageLoaderViewModel> logger)
    {
        _auth = auth;
        _i18n = i18n;
        _js = js;
        _templateRuntime = templateRuntime;
        _legacyLayoutParser = legacyLayoutParser;
        _labelService = labelService;
        _logger = logger;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool Loading
    {
        get => _loading;
        private set => Set(ref _loading, value);
    }

    public bool Saving
    {
        get => _saving;
        private set => Set(ref _saving, value);
    }

    public string? Error
    {
        get => _error;
        private set => Set(ref _error, value);
    }

    public string? ValidationError
    {
        get => _validationError;
        private set => Set(ref _validationError, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        private set => Set(ref _isEditMode, value);
    }

    public string EntityType
    {
        get => _entityType;
        private set => Set(ref _entityType, value);
    }

    public int Id
    {
        get => _id;
        private set => Set(ref _id, value);
    }

    public JsonElement? EntityData
    {
        get => _entityData;
        private set => Set(ref _entityData, value);
    }

    public object? BoundData
    {
        get => _boundData;
        private set => Set(ref _boundData, value);
    }

    public List<DraggableWidget> LayoutWidgets
    {
        get => _layoutWidgets;
        private set => Set(ref _layoutWidgets, value);
    }

    public TemplateRuntimeResponse? RuntimeContext
    {
        get => _runtimeContext;
        private set => Set(ref _runtimeContext, value);
    }

    public string[] AppliedScopes
    {
        get => _appliedScopes;
        private set => Set(ref _appliedScopes, value);
    }

    public string EditCode
    {
        get => _editCode;
        set => Set(ref _editCode, value);
    }

    public string EditName
    {
        get => _editName;
        set => Set(ref _editName, value);
    }

    public EditValueManager EditValueManager { get; } = new();
    public FormRuntimeContext FormContext { get; } = new();
    public RuntimeLabelService LabelService => _labelService;

    public async Task LoadDataAsync(string entityType, int id, CancellationToken ct = default)
    {
        try
        {
            Loading = true;
            Error = null;

            RuntimeContext = null;
            AppliedScopes = Array.Empty<string>();
            string? layoutJson = null;

            await _labelService.RefreshFieldLabelsAsync(ct);

            EntityType = entityType;
            Id = id;

            var lang = _i18n.CurrentLang?.Trim();
            var langQuery = string.IsNullOrWhiteSpace(lang) ? string.Empty : $"?lang={Uri.EscapeDataString(lang)}";
            var dataResp = await _auth.GetWithRefreshAsync($"/api/{EntityType}s/{Id}{langQuery}");
            if (!dataResp.IsSuccessStatusCode)
            {
                Error = string.Format(_i18n.T("PL_LOAD_DATA_FAILED"), dataResp.StatusCode);
                Loading = false;
                return;
            }

            var dataDoc = await dataResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            EntityData = dataDoc;
            BoundData = EntityData;
            FormContext.Data = BoundData;

            var runtime = await _templateRuntime.GetRuntimeAsync(
                EntityType,
                TemplateUsageType.Detail,
                entityId: Id,
                entityData: EntityData,
                cancellationToken: ct);

            if (runtime != null && !string.IsNullOrWhiteSpace(runtime.Template?.LayoutJson))
            {
                RuntimeContext = runtime;
                AppliedScopes = runtime.AppliedScopes?
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray() ?? Array.Empty<string>();
                layoutJson = runtime.Template.LayoutJson;
            }

            if (string.IsNullOrWhiteSpace(layoutJson))
            {
                var templateResp = await _auth.GetWithRefreshAsync($"/api/templates/effective/{EntityType}");

                if (templateResp.IsSuccessStatusCode)
                {
                    var templateContent = await templateResp.Content.ReadAsStringAsync(ct);
                    var template = JsonSerializer.Deserialize<FormTemplate>(
                        templateContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (template != null)
                    {
                        layoutJson = template.LayoutJson;
                    }
                }

                if (string.IsNullOrWhiteSpace(layoutJson))
                {
                    var layoutResp = await _auth.GetWithRefreshAsync($"/api/layout/entity/{EntityType}?scope=effective");
                    if (layoutResp.IsSuccessStatusCode)
                    {
                        var layoutDoc = await layoutResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                        layoutJson = layoutDoc.GetRawText();
                    }
                }

                if (string.IsNullOrWhiteSpace(layoutJson))
                {
                    Error = _i18n.T("PL_LOAD_TEMPLATE_FAILED");
                }
            }

            if (!string.IsNullOrWhiteSpace(layoutJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(layoutJson);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        LayoutWidgets = JsonSerializer.Deserialize<List<DraggableWidget>>(
                                layoutJson,
                                new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true,
                                    Converters = { new WidgetJsonConverter() }
                                }) ??
                            new List<DraggableWidget>();
                    }
                    else
                    {
                        LayoutWidgets = _legacyLayoutParser.ParseLayoutFromJson(doc.RootElement);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "[PageLoader] Failed to parse layout JSON.");
                    LayoutWidgets = new List<DraggableWidget>();
                }
            }
            else
            {
                LayoutWidgets = new List<DraggableWidget>();
            }

            if (EntityData.HasValue && EntityData.Value.TryGetProperty("fields", out var fieldsElement))
            {
                EditValueManager.Clear();
                foreach (var field in fieldsElement.EnumerateArray())
                {
                    try
                    {
                        if (field.TryGetProperty("key", out var keyProp) &&
                            field.TryGetProperty("value", out var valueProp))
                        {
                            EditValueManager.SetValue(keyProp.GetString(), valueProp.GetString());
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "[PageLoader] Failed to bind field, skipping.");
                    }
                }
            }

            Loading = false;
        }
        catch (Exception ex)
        {
            Error = string.Format(_i18n.T("PL_LOAD_EXCEPTION"), ex.Message);
            Loading = false;
        }
    }

    public string GetEntityDisplayName()
    {
        if (EntityData.HasValue && EntityData.Value.TryGetProperty("name", out var nameElement))
        {
            return nameElement.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    public string GetEntityCode()
    {
        if (EntityData.HasValue && EntityData.Value.TryGetProperty("code", out var codeElement))
        {
            return codeElement.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    public bool HasWidgetForField(string fieldKey)
    {
        if (string.IsNullOrWhiteSpace(fieldKey)) return false;
        return HasWidgetRecursive(LayoutWidgets, fieldKey);
    }

    private static bool HasWidgetRecursive(IEnumerable<DraggableWidget> widgets, string fieldKey)
    {
        foreach (var widget in widgets)
        {
            if (!string.IsNullOrWhiteSpace(widget.DataField) &&
                string.Equals(widget.DataField, fieldKey, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (widget.Children != null && widget.Children.Count > 0 && HasWidgetRecursive(widget.Children, fieldKey))
            {
                return true;
            }
        }

        return false;
    }

    public string GetFieldValue(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || !EntityData.HasValue) return string.Empty;

        if (EntityData.Value.TryGetProperty("fields", out var fieldsElement))
        {
            foreach (var field in fieldsElement.EnumerateArray())
            {
                if (field.TryGetProperty("key", out var keyProp) &&
                    string.Equals(keyProp.GetString(), key, StringComparison.OrdinalIgnoreCase) &&
                    field.TryGetProperty("value", out var valueProp))
                {
                    return valueProp.GetString() ?? string.Empty;
                }
            }
        }

        try
        {
            foreach (var prop in EntityData.Value.EnumerateObject())
            {
                if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase))
                {
                    return prop.Value.ToString();
                }
            }
        }
        catch
        {
            // ignore
        }

        return string.Empty;
    }

    public Task EnterEditModeAsync()
    {
        IsEditMode = true;
        try
        {
            EditCode = GetEntityCode();
            EditName = GetEntityDisplayName();
            EditValueManager.InitializeFromWidgets(LayoutWidgets, GetFieldValue);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[PageLoader] EnterEditMode failed.");
            Error = string.Format(_i18n.T("PL_LOAD_EXCEPTION"), ex.Message);
        }

        return Task.CompletedTask;
    }

    public void CancelEdit()
    {
        IsEditMode = false;
        EditValueManager.Clear();
        ValidationError = null;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            Saving = true;
            ValidationError = null;

            var isValid = FormContext.ValidateAll(key => EditValueManager.GetValue(key));
            if (!isValid)
            {
                ValidationError = _i18n.T("MSG_FORM_VALIDATION_FAILED");
                await ScrollToFirstValidationErrorAsync();
                return;
            }

            HashSet<string>? allowedKeys = null;
            if (EntityData.HasValue && EntityData.Value.TryGetProperty("fields", out var fieldsElement))
            {
                allowedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var field in fieldsElement.EnumerateArray())
                {
                    if (field.TryGetProperty("key", out var keyProp))
                    {
                        var key = keyProp.GetString();
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            allowedKeys.Add(key!);
                        }
                    }
                }
            }

            var fieldPayloads = EditValueManager.CollectFieldPayloads(LayoutWidgets, allowedKeys);

            if (allowedKeys != null && EntityData.HasValue && EntityData.Value.TryGetProperty("fields", out var fieldsElement2))
            {
                var existingKeys = new HashSet<string>(fieldPayloads.Select(f => f.key), StringComparer.OrdinalIgnoreCase);
                foreach (var field in fieldsElement2.EnumerateArray())
                {
                    if (!field.TryGetProperty("key", out var keyProp)) continue;
                    var key = keyProp.GetString();
                    if (string.IsNullOrWhiteSpace(key) || existingKeys.Contains(key))
                    {
                        continue;
                    }

                    object? valueObj = null;
                    if (field.TryGetProperty("value", out var valProp))
                    {
                        try { valueObj = JsonSerializer.Deserialize<object>(valProp.GetRawText()); }
                        catch { valueObj = valProp.ToString(); }
                    }

                    fieldPayloads.Add(new FieldPayload { key = key!, value = valueObj });
                }
            }

            var payload = new
            {
                code = EditCode,
                name = EditName,
                fields = fieldPayloads
            };

            try
            {
                var debugJson = JsonSerializer.Serialize(payload);
                _logger.LogDebug("[PageLoader] Save payload: {Payload}", debugJson);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[PageLoader] Failed to serialize save payload for debug logging.");
            }

            var client = await _auth.CreateClientWithAuthAsync();
            var resp = await client.PutAsJsonAsync($"/api/{EntityType}s/{Id}", payload, ct);

            if (resp.IsSuccessStatusCode)
            {
                IsEditMode = false;
                await LoadDataAsync(EntityType, Id, ct);
            }
            else
            {
                var respBody = await resp.Content.ReadAsStringAsync(ct);
                Error = string.Format(_i18n.T("PL_SAVE_FAILED"), $"{resp.StatusCode}: {respBody}");
            }
        }
        catch (Exception ex)
        {
            Error = string.Format(_i18n.T("PL_SAVE_FAILED"), ex.Message);
        }
        finally
        {
            Saving = false;
        }
    }

    private async Task ScrollToFirstValidationErrorAsync()
    {
        try
        {
            var firstField = FormContext.Errors.Keys.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(firstField))
            {
                return;
            }

            await _js.TryInvokeVoidAsync("bobcrm.scrollToValidationField", firstField);
        }
        catch
        {
            // ignore
        }
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        if (propertyName != null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
