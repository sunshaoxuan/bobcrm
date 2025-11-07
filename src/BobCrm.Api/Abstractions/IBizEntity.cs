using BobCrm.Api.Domain.Models;

namespace BobCrm.Api.Abstractions;

/// <summary>
/// 业务实体标记接口 - 实现此接口的实体可自定义、可建模板
/// 系统启动时会自动同步到EntityDefinition表
/// </summary>
/// <remarks>
/// 设计原则：
/// 1. System实体（如Customer）实现此接口，提供初始定义
/// 2. 初始定义仅在首次启动时插入数据库
/// 3. 用户后续可通过UI修改，以EntityDefinition中的数据为准
/// 4. System实体的状态默认为Published
/// 5. 支持"重置为默认"功能，恢复到初始定义
/// </remarks>
public interface IBizEntity
{
    /// <summary>
    /// 提供实体的初始定义（仅在首次同步时使用）
    /// 用户后续可通过UI修改，以EntityDefinition中的数据为准
    /// </summary>
    static abstract EntityDefinition GetInitialDefinition();
}
