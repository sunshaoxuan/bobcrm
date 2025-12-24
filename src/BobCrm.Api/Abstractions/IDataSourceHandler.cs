using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BobCrm.Api.Abstractions;

/// <summary>
/// 数据源处理器接口 - 定义数据源执行的统一契约
/// 各种数据源类型(Entity/Api/Sql/View)都需要实现此接口
/// </summary>
public interface IDataSourceHandler
{
    /// <summary>
    /// 数据源类型代码
    /// 对应 DataSourceTypeEntry.Code
    /// </summary>
    string TypeCode { get; }

    /// <summary>
    /// 执行数据源查询
    /// </summary>
    Task<DataSourceExecutionResult> ExecuteAsync(
        DataSourceExecutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证数据源配置
    /// </summary>
    Task<DataSourceValidationResult> ValidateConfigAsync(string configJson);

    /// <summary>
    /// 获取字段元数据
    /// 根据数据源配置,推断可用的字段列表
    /// </summary>
    Task<List<DataSourceFieldMetadata>> GetFieldsAsync(
        string configJson,
        CancellationToken cancellationToken = default);
}
