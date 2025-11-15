using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Services.DataMigration;

/// <summary>
/// 数据迁移影响评估器接口
/// 在发布实体变更前，评估对现有数据的影响
/// </summary>
public interface IDataMigrationEvaluator
{
    /// <summary>
    /// 评估实体变更的数据迁移影响
    /// </summary>
    /// <param name="entityId">实体定义ID</param>
    /// <param name="newFields">新的字段列表</param>
    /// <returns>迁移影响分析</returns>
    Task<MigrationImpact> EvaluateImpactAsync(Guid entityId, List<FieldMetadata> newFields);
}
