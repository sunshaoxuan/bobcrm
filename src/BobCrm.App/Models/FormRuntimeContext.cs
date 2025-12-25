namespace BobCrm.App.Models;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 表单运行时上下文（通过 CascadingValue 级联给子组件）
/// 用于感知表单所处生命周期状态与绑定数据。
/// </summary>
public sealed class FormRuntimeContext
{
    private object? _data;
    private readonly Dictionary<string, List<Func<object?, string?>>> _validators = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
     /// 表单渲染模式：设计态 / 浏览 / 编辑 / 禁用。
     /// </summary>
    public Mode RenderMode { get; set; } = Mode.Browse;

    /// <summary>
    /// 当前表单绑定的数据对象。
    /// </summary>
    public object? Data
    {
        get => _data;
        set
        {
            if (ReferenceEquals(_data, value))
            {
                return;
            }

            _data = value;
            OnDataChanged?.Invoke(_data);
        }
    }

    /// <summary>
    /// Data 发生变化时触发。
    /// </summary>
    public event Action<object?>? OnDataChanged;

    /// <summary>
    /// 当前校验错误（按字段聚合）。
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors
        => _errors.ToDictionary(k => k.Key, v => (IReadOnlyList<string>)v.Value);

    /// <summary>
    /// 校验错误发生变化时触发。
    /// </summary>
    public event Action<IReadOnlyDictionary<string, IReadOnlyList<string>>>? OnValidationChanged;

    public bool HasErrors => _errors.Count > 0;

    public IReadOnlyList<string> GetErrors(string field)
    {
        if (_errors.TryGetValue(field, out var list))
        {
            return list;
        }

        return Array.Empty<string>();
    }

    public void ClearValidation()
    {
        if (_errors.Count == 0)
        {
            return;
        }

        _errors.Clear();
        OnValidationChanged?.Invoke(Errors);
    }

    public void RegisterValidator(string field, Func<object?, string?> validator)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return;
        }

        if (!_validators.TryGetValue(field, out var list))
        {
            list = new List<Func<object?, string?>>();
            _validators[field] = list;
        }

        list.Add(validator);
    }

    public bool ValidateField(string field, object? value)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return true;
        }

        if (!_validators.TryGetValue(field, out var validators) || validators.Count == 0)
        {
            if (_errors.Remove(field))
            {
                OnValidationChanged?.Invoke(Errors);
            }

            return true;
        }

        var newErrors = validators
            .Select(v => v(value))
            .Where(msg => !string.IsNullOrWhiteSpace(msg))
            .Select(msg => msg!)
            .ToList();

        var changed = false;
        if (newErrors.Count == 0)
        {
            changed = _errors.Remove(field);
        }
        else
        {
            if (!_errors.TryGetValue(field, out var existing) || !existing.SequenceEqual(newErrors, StringComparer.Ordinal))
            {
                _errors[field] = newErrors;
                changed = true;
            }
        }

        if (changed)
        {
            OnValidationChanged?.Invoke(Errors);
        }

        return newErrors.Count == 0;
    }

    public bool ValidateAll(Func<string, object?> valueProvider)
    {
        var allValid = true;
        foreach (var entry in _validators)
        {
            var value = valueProvider(entry.Key);
            if (!ValidateField(entry.Key, value))
            {
                allValid = false;
            }
        }

        return allValid;
    }

    /// <summary>
    /// 表单渲染模式。
    /// </summary>
    public enum Mode
    {
        Design,
        Browse,
        Edit,
        Disabled
    }
}
