using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Infrastructure;

/// <summary>
/// Entity Framework 实现的刷新令牌存储
/// </summary>
public class EfRefreshTokenStore : IRefreshTokenStore
{
    private readonly AppDbContext _db;

    public EfRefreshTokenStore(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 创建新的刷新令牌
    /// </summary>
    public async Task<string> CreateAsync(string userId, DateTime expiresAt)
    {
        // 生成随机令牌（双GUID Base64编码）
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
                    Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt
        });

        await _db.SaveChangesAsync();
        return token;
    }

    /// <summary>
    /// 验证刷新令牌
    /// </summary>
    public async Task<RefreshToken?> ValidateAsync(string token)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token);
        if (rt == null) return null;

        // 检查是否已撤销或已过期
        if (rt.RevokedAt != null || rt.ExpiresAt <= DateTime.UtcNow)
            return null;

        return rt;
    }

    /// <summary>
    /// 撤销刷新令牌
    /// </summary>
    public async Task RevokeAsync(string token)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token);
        if (rt != null)
        {
            rt.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
