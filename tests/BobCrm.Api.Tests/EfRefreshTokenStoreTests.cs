using BobCrm.Api.Base;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Tests;

/// <summary>
/// EfRefreshTokenStore 测试
/// 覆盖 JWT 令牌存储
/// </summary>
public class EfRefreshTokenStoreTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ShouldGenerateAndStoreToken()
    {
        // Arrange
        await using var ctx = CreateContext();
        var store = new EfRefreshTokenStore(ctx);
        var userId = "test-user-id";
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var token = await store.CreateAsync(userId, expiresAt);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var stored = await ctx.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
        stored.Should().NotBeNull();
        stored!.UserId.Should().Be(userId);
        stored.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateAsync_ShouldGenerateUniqueTokens()
    {
        // Arrange
        await using var ctx = CreateContext();
        var store = new EfRefreshTokenStore(ctx);
        var userId = "test-user-id";
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var token1 = await store.CreateAsync(userId, expiresAt);
        var token2 = await store.CreateAsync(userId, expiresAt);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public async Task ValidateAsync_WhenTokenExists_ShouldReturnToken()
    {
        // Arrange
        await using var ctx = CreateContext();
        var store = new EfRefreshTokenStore(ctx);
        var userId = "test-user-id";
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var token = await store.CreateAsync(userId, expiresAt);

        // Act
        var result = await store.ValidateAsync(token);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.Token.Should().Be(token);
    }

    [Fact]
    public async Task ValidateAsync_WhenTokenNotExists_ShouldReturnNull()
    {
        // Arrange
        await using var ctx = CreateContext();
        var store = new EfRefreshTokenStore(ctx);

        // Act
        var result = await store.ValidateAsync("non-existent-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WhenTokenExpired_ShouldReturnNull()
    {
        // Arrange
        await using var ctx = CreateContext();
        var store = new EfRefreshTokenStore(ctx);
        
        // Create an expired token directly in DB
        var expiredToken = new RefreshToken
        {
            UserId = "test-user-id",
            Token = "expired-token-123",
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        ctx.RefreshTokens.Add(expiredToken);
        await ctx.SaveChangesAsync();

        // Act
        var result = await store.ValidateAsync("expired-token-123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WhenTokenRevoked_ShouldReturnNull()
    {
        // Arrange
        await using var ctx = CreateContext();
        var store = new EfRefreshTokenStore(ctx);
        
        // Create a revoked token directly in DB
        var revokedToken = new RefreshToken
        {
            UserId = "test-user-id",
            Token = "revoked-token-123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow
        };
        ctx.RefreshTokens.Add(revokedToken);
        await ctx.SaveChangesAsync();

        // Act
        var result = await store.ValidateAsync("revoked-token-123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAsync_WhenTokenExists_ShouldSetRevokedAt()
    {
        // Arrange
        await using var ctx = CreateContext();
        var store = new EfRefreshTokenStore(ctx);
        var userId = "test-user-id";
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var token = await store.CreateAsync(userId, expiresAt);

        // Act
        await store.RevokeAsync(token);

        // Assert
        var revoked = await ctx.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
        revoked.Should().NotBeNull();
        revoked!.RevokedAt.Should().NotBeNull();
        revoked.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RevokeAsync_WhenTokenNotExists_ShouldNotThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var store = new EfRefreshTokenStore(ctx);

        // Act & Assert (should not throw)
        await store.RevokeAsync("non-existent-token");
    }

    [Fact]
    public async Task RevokeAsync_ShouldPreventFutureValidation()
    {
        // Arrange
        await using var ctx = CreateContext();
        var store = new EfRefreshTokenStore(ctx);
        var userId = "test-user-id";
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var token = await store.CreateAsync(userId, expiresAt);

        // Verify token is valid before revocation
        var beforeRevoke = await store.ValidateAsync(token);
        beforeRevoke.Should().NotBeNull();

        // Act
        await store.RevokeAsync(token);

        // Assert
        var afterRevoke = await store.ValidateAsync(token);
        afterRevoke.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldStoreCorrectUserId()
    {
        // Arrange
        await using var ctx = CreateContext();
        var store = new EfRefreshTokenStore(ctx);
        var userId1 = "user-1";
        var userId2 = "user-2";
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var token1 = await store.CreateAsync(userId1, expiresAt);
        var token2 = await store.CreateAsync(userId2, expiresAt);

        // Assert
        var result1 = await store.ValidateAsync(token1);
        var result2 = await store.ValidateAsync(token2);
        result1!.UserId.Should().Be(userId1);
        result2!.UserId.Should().Be(userId2);
    }
}
