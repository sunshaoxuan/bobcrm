using BobCrm.Api.Base;

namespace BobCrm.Api.Abstractions;

/// <summary>
/// 刷新令牌存储接口
/// 用于管理 JWT 刷新令牌的生命周期
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>
    /// 创建新的刷新令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="expiresAt">过期时间</param>
    /// <returns>令牌字符串</returns>
    Task<string> CreateAsync(string userId, DateTime expiresAt);

    /// <summary>
    /// 验证刷新令牌
    /// </summary>
    /// <param name="token">令牌字符串</param>
    /// <returns>有效的令牌实体，或null（如果无效或已过期）</returns>
    Task<RefreshToken?> ValidateAsync(string token);

    /// <summary>
    /// 撤销刷新令牌
    /// </summary>
    /// <param name="token">令牌字符串</param>
    Task RevokeAsync(string token);
}
