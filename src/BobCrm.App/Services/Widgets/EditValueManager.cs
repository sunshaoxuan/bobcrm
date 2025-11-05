using BobCrm.App.Models.Widgets;

namespace BobCrm.App.Services.Widgets;

/// <summary>
/// 编辑值管理器
/// 负责管理表单编辑态的字段值，避免KeyNotFoundException
/// </summary>
public class EditValueManager
{
    private readonly Dictionary<string, string> _values = new();

    /// <summary>
    /// 从Widget列表初始化编辑值
    /// </summary>
    public void InitializeFromWidgets(IEnumerable<DraggableWidget> widgets, Func<string, string> fieldValueGetter)
    {
        _values.Clear();
        InitializeRecursive(widgets, fieldValueGetter);
    }

    private void InitializeRecursive(IEnumerable<DraggableWidget> widgets, Func<string, string> fieldValueGetter)
    {
        foreach (var widget in widgets)
        {
            if (!string.IsNullOrWhiteSpace(widget.DataField))
            {
                _values[widget.DataField] = fieldValueGetter(widget.DataField);
            }

            // 递归处理容器子控件
            if (widget is ContainerWidget container && container.Children != null)
            {
                InitializeRecursive(container.Children, fieldValueGetter);
            }
        }
    }

    /// <summary>
    /// 获取编辑值（安全）
    /// </summary>
    public string GetValue(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        return _values.TryGetValue(key, out var v) ? v : string.Empty;
    }

    /// <summary>
    /// 设置编辑值
    /// </summary>
    public void SetValue(string? key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        _values[key] = value ?? string.Empty;
    }

    /// <summary>
    /// 收集所有字段的Payload（用于保存）
    /// </summary>
    public List<FieldPayload> CollectFieldPayloads(IEnumerable<DraggableWidget> widgets, HashSet<string> allowedKeys)
    {
        var output = new List<FieldPayload>();
        CollectRecursive(widgets, allowedKeys, output);
        return output;
    }

    private void CollectRecursive(IEnumerable<DraggableWidget> widgets, HashSet<string> allowedKeys, List<FieldPayload> output)
    {
        foreach (var widget in widgets)
        {
            if (!string.IsNullOrWhiteSpace(widget.DataField) && allowedKeys.Contains(widget.DataField))
            {
                var val = GetValue(widget.DataField);
                output.Add(new FieldPayload { key = widget.DataField, value = val });
            }

            if (widget is ContainerWidget container && container.Children != null)
            {
                CollectRecursive(container.Children, allowedKeys, output);
            }
        }
    }

    /// <summary>
    /// 清空所有编辑值
    /// </summary>
    public void Clear() => _values.Clear();
}

/// <summary>
/// 字段Payload（用于API提交）
/// </summary>
public class FieldPayload
{
    public string key { get; set; } = string.Empty;
    public object? value { get; set; }
}

