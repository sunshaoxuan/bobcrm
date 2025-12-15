using System.Collections.Generic;

namespace BobCrm.Api.Services.DataSources;

/// <summary>
/// 实体数据源配置
/// </summary>
public record EntityDataSourceConfig
{
    /// <summary>实体类型</summary>
    public string EntityType { get; init; } = string.Empty;

    /// <summary>包含的关系(导航属性)</summary>
    public List<string> IncludeRelations { get; init; } = new();

    /// <summary>默认过滤条件JSON</summary>
    public string? DefaultFilter { get; init; }
}
