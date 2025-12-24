namespace BobCrm.App.Models;

/// <summary>
/// 表单运行时上下文（通过 CascadingValue 级联给子组件）
/// 用于感知表单所处生命周期状态与绑定数据。
/// </summary>
public sealed class FormRuntimeContext
{
    private object? _data;

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
