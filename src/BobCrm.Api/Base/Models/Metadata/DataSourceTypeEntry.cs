using System;
using System.Collections.Generic;

namespace BobCrm.Api.Base.Models.Metadata;

/// <summary>
/// 数据源类型元数据表 - 定义系统支持的数据源类型
/// </summary>
public class DataSourceTypeEntry
{
    /// <summary>ID</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 类型代码(业务主键)
    /// 例如: "entity", "api", "sql", "view", "custom"
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称(多语)
    /// 例如: { "zh-CN": "实体数据源", "en-US": "Entity Data Source", "ja-JP": "エンティティデータソース" }
    /// </summary>
    public Dictionary<string, string?>? DisplayName { get; set; }

    /// <summary>
    /// 描述(多语)
    /// </summary>
    public Dictionary<string, string?>? Description { get; set; }

    /// <summary>
    /// 处理器类名(完整限定名)
    /// 例如: "BobCrm.Api.Services.DataSources.EntityDataSourceHandler"
    /// 系统会通过反射或DI容器解析此类型
    /// </summary>
    public string HandlerType { get; set; } = string.Empty;

    /// <summary>
    /// 配置架构(JSON Schema,可选)
    /// 定义此数据源类型需要的配置参数结构
    /// </summary>
    public string? ConfigSchema { get; set; }

    /// <summary>
    /// 类别(用于分组)
    /// 例如: "Database", "WebService", "Custom"
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// 图标(Ant Design 图标名称)
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 是否为系统预置
    /// </summary>
    public bool IsSystem { get; set; } = true;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 100;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
