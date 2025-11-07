using BobCrm.Api.Domain.Aggregates;

namespace BobCrm.Api.Services.Aggregates;

/// <summary>
/// AggVO服务接口
/// 提供聚合根的保存、加载和删除操作
/// </summary>
public interface IAggVOService
{
    /// <summary>
    /// 保存聚合（级联保存主实体和所有子实体）
    /// </summary>
    /// <param name="aggVO">聚合根对象</param>
    /// <returns>主实体ID</returns>
    Task<int> SaveAggVOAsync(AggBaseVO aggVO);

    /// <summary>
    /// 加载聚合（级联加载主实体和所有子实体）
    /// </summary>
    /// <param name="aggVO">聚合根对象</param>
    /// <param name="masterId">主实体ID</param>
    Task LoadAggVOAsync(AggBaseVO aggVO, int masterId);

    /// <summary>
    /// 删除聚合（级联删除主实体和所有子实体）
    /// </summary>
    /// <param name="aggVO">聚合根对象</param>
    Task DeleteAggVOAsync(AggBaseVO aggVO);
}
