using BobCrm.Api.Domain.Models;

namespace BobCrm.Api.Services.CodeGeneration;

/// <summary>
/// AggVO代码生成器接口
/// 根据实体定义生成聚合根VO类的C#代码
/// </summary>
public interface IAggVOCodeGenerator
{
    /// <summary>
    /// 生成 AggVO 类的 C# 代码
    /// </summary>
    /// <param name="masterEntity">主实体定义</param>
    /// <param name="childEntities">子实体定义列表</param>
    /// <returns>生成的C#代码</returns>
    string GenerateAggVOClass(EntityDefinition masterEntity, List<EntityDefinition> childEntities);

    /// <summary>
    /// 生成 AggVO 的辅助文件（VO 类定义）
    /// 如果 EntityVO 不存在，则生成简单的 VO 类
    /// </summary>
    /// <param name="entity">实体定义</param>
    /// <returns>生成的C#代码</returns>
    string GenerateVOClass(EntityDefinition entity);
}
