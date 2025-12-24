using System.Collections.Generic;

namespace BobCrm.Api.Abstractions;

/// <summary>
/// 数据源字段元数据
/// </summary>
public record DataSourceFieldMetadata
{
    /// <summary>字段名称</summary>
    public required string Name { get; init; }

    /// <summary>数据类型</summary>
    public required string DataType { get; init; }

    /// <summary>
    /// 显示名称(多语言)
    /// 例如: { "zh-CN": "客户编码", "en-US": "Customer Code" }
    /// </summary>
    public Dictionary<string, string?>? DisplayName { get; init; }

    /// <summary>是否可排序</summary>
    public bool Sortable { get; init; } = false;

    /// <summary>是否可过滤</summary>
    public bool Filterable { get; init; } = false;

    /// <summary>是否必填</summary>
    public bool Required { get; init; } = false;
}
