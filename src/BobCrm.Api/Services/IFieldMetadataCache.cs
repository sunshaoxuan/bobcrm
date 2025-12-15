using BobCrm.Api.Contracts.Responses.Entity;
using BobCrm.Api.Infrastructure;

namespace BobCrm.Api.Services;

/// <summary>
/// 字段元数据缓存接口
/// </summary>
public interface IFieldMetadataCache
{
    /// <summary>
    /// 获取实体的字段元数据列表
    /// </summary>
    /// <param name="fullTypeName">实体的完整类型名</param>
    /// <param name="loc">本地化服务</param>
    /// <param name="lang">目标语言代码（null 表示返回多语字典模式）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>字段元数据 DTO 列表</returns>
    Task<IReadOnlyList<FieldMetadataDto>> GetFieldsAsync(
        string fullTypeName,
        ILocalization loc,
        string? lang,
        CancellationToken ct = default);

    /// <summary>
    /// 使指定实体的字段元数据缓存失效
    /// </summary>
    /// <param name="fullTypeName">实体的完整类型名</param>
    void Invalidate(string fullTypeName);
}

