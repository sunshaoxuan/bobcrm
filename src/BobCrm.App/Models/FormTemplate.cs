namespace BobCrm.App.Models;

/// <summary>
/// 表单模板元数据
/// 定义表单的实体类型、数据源、布局等信息
/// </summary>
public class FormTemplate
{
    /// <summary>模板ID</summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>模板名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>实体类型（如 "customer", "product", "order"）</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>实体数据API端点（如 "/api/customers"）</summary>
    public string EntityApi { get; set; } = string.Empty;

    /// <summary>侧边栏组件名称（可选，如 "CustomerSider"）</summary>
    public string? SiderComponent { get; set; }

    /// <summary>布局JSON（Widget树）</summary>
    public string? LayoutJson { get; set; }

    /// <summary>作用域（user - 用户自定义，default - 系统默认）</summary>
    public string Scope { get; set; } = "user";

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

