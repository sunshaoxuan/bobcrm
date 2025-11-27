using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Base;

/// <summary>
/// 表单模板
/// 一个模板可以被多个视图状态共享
/// 通过 TemplateStateBinding 表实现模板与状态的 N:M 关系
/// </summary>
public class FormTemplate
{
    /// <summary>模板ID（主键）</summary>
    public int Id { get; set; }

    /// <summary>模板名称（用户可见）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 实体类型（如 "customer", "product", "order"）
    /// 一旦设置后不允许修改
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>模板创建者的用户ID</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 是否为用户默认模板
    /// 每个用户在每个实体类型下只能有一个默认模板
    /// </summary>
    public bool IsUserDefault { get; set; } = false;

    /// <summary>
    /// 是否为系统默认模板
    /// 每个实体类型只能有一个系统默认模板
    /// 系统默认模板用于新用户或没有用户默认模板的用户
    /// </summary>
    public bool IsSystemDefault { get; set; } = false;

    /// <summary>模板版本号（用于跟踪变更）</summary>
    [NotMapped]
    public int Version { get; set; } = 1;
    
    /// <summary>布局JSON（Widget树）</summary>
    public string? LayoutJson { get; set; }
    
    /// <summary>标签集合</summary>
    public List<string>? Tags { get; set; }
    
    /// <summary>访问所需功能编码</summary>
    public string? RequiredFunctionCode { get; set; }
    
    /// <summary>模板描述（可选）</summary>
    public string? Description { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最后更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ========== Master-Detail Pattern Support ==========
    
    /// <summary>
    /// 布局模式 - 定义列表和详情的排列方式
    /// 仅对 List 类型的模板有效
    /// </summary>
    public LayoutMode LayoutMode { get; set; } = LayoutMode.ListOnly;
    
    /// <summary>
    /// 详情显示模式 - 定义详情如何展示
    /// 仅对 List 类型的模板有效（用于配置从列表到详情的导航方式）
    /// </summary>
    public DetailDisplayMode DetailDisplayMode { get; set; } = DetailDisplayMode.Page;
    
    /// <summary>
    /// 详情页面路由模板 - 用于 Page 模式
    /// 例如: "/customer/{id}" 或 "/customer/edit/{id}"
    /// 仅当 DetailDisplayMode = Page 时有效
    /// </summary>
    public string? DetailRoute { get; set; }
    
    /// <summary>
    /// 模态框大小 - 用于 Modal 模式
    /// 仅当 DetailDisplayMode = Modal 时有效
    /// </summary>
    public ModalSize? ModalSize { get; set; }

    /// <summary>
    /// 是否正在被使用
    /// 用于判断模板是否可以删除
    /// </summary>
    public bool IsInUse { get; set; } = false;
    
    // ========== 导航属性 ==========
    
    /// <summary>
    /// 此模板绑定的状态列表
    /// 一个模板可以被多个状态共享
    /// </summary>
    public List<TemplateStateBinding> StateBindings { get; set; } = new();
}
