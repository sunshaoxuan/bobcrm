namespace BobCrm.Api.Domain;

/// <summary>
/// 刷新令牌实体
/// 用于 JWT 刷新机制
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
}
