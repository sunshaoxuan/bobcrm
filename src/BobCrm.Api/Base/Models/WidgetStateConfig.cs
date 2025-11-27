using System;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// Widget 在特定状态下的配置
/// 控制控件在不同视图状态下的可见性和可编辑性
/// </summary>
public class WidgetStateConfig
{
    /// <summary>
    /// 是否可见
    /// </summary>
    public bool Visible { get; set; } = true;
    
    /// <summary>
    /// 是否可编辑
    /// 仅当 Visible = true 时有效
    /// </summary>
    public bool Editable { get; set; } = true;
    
    /// <summary>
    /// 是否必填
    /// 仅当 Editable = true 时有效
    /// </summary>
    public bool Required { get; set; } = false;
    
    /// <summary>
    /// 自定义验证规则（可选）
    /// 格式：正则表达式或验证表达式
    /// 例如："^[A-Z0-9]+$" 或 "length > 5"
    /// </summary>
    public string? ValidationRule { get; set; }
}
