using BobCrm.Api.Data.Entities;

namespace BobCrm.Api.Abstractions;

/// <summary>
/// 实体元数据提供者接口
/// 实现此接口的实体会在系统启动时自动注册到EntityMetadata表
/// </summary>
public interface IEntityMetadataProvider
{
    /// <summary>
    /// 提供实体的元数据信息
    /// 系统启动时会自动调用此方法并注册到数据库
    /// </summary>
    static abstract EntityMetadata GetMetadata();
}

