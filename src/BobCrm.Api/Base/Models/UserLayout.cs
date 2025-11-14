namespace BobCrm.Api.Domain;

/// <summary>
/// 用户布局 - 支持按实体类型（EntityType）保存不同的布局模板
/// </summary>
public class UserLayout
{
    public int Id { get; set; }

    /// <summary>用户ID（"__default__" 表示系统默认模板）</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>客户ID（已废弃，保留用于兼容性，新代码请使用EntityType）</summary>
    [Obsolete("Use EntityType instead")]
    public int CustomerId { get; set; }

    /// <summary>
    /// 实体类型（如 "customer", "product", "order"）
    /// 如果为空，则使用CustomerId字段（兼容旧版）
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>布局JSON（Widget树）</summary>
    public string? LayoutJson { get; set; }
}

