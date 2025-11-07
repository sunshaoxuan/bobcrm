namespace BobCrm.Api.Services;

/// <summary>
/// 实体发布服务接口
/// 负责将实体定义发布到数据库（CREATE/ALTER TABLE）
/// </summary>
public interface IEntityPublishingService
{
    /// <summary>
    /// 发布新实体（CREATE TABLE）
    /// </summary>
    /// <param name="entityDefinitionId">实体定义ID</param>
    /// <param name="publishedBy">发布人</param>
    /// <returns>发布结果</returns>
    Task<PublishResult> PublishNewEntityAsync(Guid entityDefinitionId, string? publishedBy = null);

    /// <summary>
    /// 发布实体修改（ALTER TABLE）
    /// </summary>
    /// <param name="entityDefinitionId">实体定义ID</param>
    /// <param name="publishedBy">发布人</param>
    /// <returns>发布结果</returns>
    Task<PublishResult> PublishEntityChangesAsync(Guid entityDefinitionId, string? publishedBy = null);
}
