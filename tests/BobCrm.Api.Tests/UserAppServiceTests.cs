using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.Requests.User;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BobCrm.Api.Tests;

/// <summary>
/// UserAppService 测试
/// 覆盖用户应用服务
/// </summary>
public class UserAppServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static UserManager<IdentityUser> CreateUserManager(AppDbContext context)
    {
        var store = new UserStore<IdentityUser>(context);
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<IdentityUser>();
        var userValidators = new List<IUserValidator<IdentityUser>> { new UserValidator<IdentityUser>() };
        var passwordValidators = new List<IPasswordValidator<IdentityUser>> { new PasswordValidator<IdentityUser>() };
        var normalizer = new UpperInvariantLookupNormalizer();
        var describer = new IdentityErrorDescriber();

        var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        var logger = services.GetRequiredService<ILogger<UserManager<IdentityUser>>>();

        return new UserManager<IdentityUser>(store, options, passwordHasher, userValidators, passwordValidators, normalizer, describer, services, logger);
    }

    private static UserAppService CreateService(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        var mockLoc = new Mock<ILocalization>();
        mockLoc.Setup(l => l.T(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((key, lang) => key);

        return new UserAppService(
            userManager,
            context,
            mockLoc.Object,
            NullLogger<UserAppService>.Instance);
    }

    #region GetUsersAsync Tests

    [Fact]
    public async Task GetUsersAsync_ShouldReturnAllUsers()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);

        await userManager.CreateAsync(new IdentityUser { UserName = "user1", Email = "user1@test.com" });
        await userManager.CreateAsync(new IdentityUser { UserName = "user2", Email = "user2@test.com" });

        var service = CreateService(ctx, userManager);

        // Act
        var result = await service.GetUsersAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUsersAsync_WhenNoUsers_ShouldReturnEmpty()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);
        var service = CreateService(ctx, userManager);

        // Act
        var result = await service.GetUsersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUsersAsync_ShouldOrderByUserName()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);

        await userManager.CreateAsync(new IdentityUser { UserName = "charlie", Email = "c@test.com" });
        await userManager.CreateAsync(new IdentityUser { UserName = "alice", Email = "a@test.com" });
        await userManager.CreateAsync(new IdentityUser { UserName = "bob", Email = "b@test.com" });

        var service = CreateService(ctx, userManager);

        // Act
        var result = await service.GetUsersAsync();

        // Assert
        result[0].UserName.Should().Be("alice");
        result[1].UserName.Should().Be("bob");
        result[2].UserName.Should().Be("charlie");
    }

    #endregion

    #region GetUserAsync Tests

    [Fact]
    public async Task GetUserAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);
        var user = new IdentityUser { UserName = "testuser", Email = "test@test.com" };
        await userManager.CreateAsync(user);

        var service = CreateService(ctx, userManager);

        // Act
        var result = await service.GetUserAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be("testuser");
    }

    [Fact]
    public async Task GetUserAsync_WithNonExistentId_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);
        var service = CreateService(ctx, userManager);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetUserAsync("nonexistent"));
    }

    [Fact]
    public async Task GetUserAsync_ShouldIncludeRoleAssignments()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);
        var user = new IdentityUser { UserName = "testuser", Email = "test@test.com" };
        await userManager.CreateAsync(user);

        var role = new RoleProfile { Code = "TEST.ROLE", Name = "Test Role" };
        ctx.RoleProfiles.Add(role);
        await ctx.SaveChangesAsync();

        ctx.RoleAssignments.Add(new RoleAssignment { UserId = user.Id, RoleId = role.Id });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx, userManager);

        // Act
        var result = await service.GetUserAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result.Roles.Should().HaveCount(1);
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_WithValidRequest_ShouldCreateUser()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);
        var service = CreateService(ctx, userManager);
        var request = new CreateUserRequest
        {
            UserName = "newuser",
            Email = "new@test.com",
            Password = "Password@123",
            EmailConfirmed = true
        };

        // Act
        var result = await service.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be("newuser");
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateUsername_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);
        await userManager.CreateAsync(new IdentityUser { UserName = "existinguser", Email = "existing@test.com" });

        var service = CreateService(ctx, userManager);
        var request = new CreateUserRequest
        {
            UserName = "existinguser",
            Email = "new@test.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ServiceException>(() => service.CreateUserAsync(request));
    }

    [Fact]
    public async Task CreateUserAsync_WithRoles_ShouldAssignRoles()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);

        var role = new RoleProfile { Code = "TEST.ROLE", Name = "Test Role" };
        ctx.RoleProfiles.Add(role);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx, userManager);
        var request = new CreateUserRequest
        {
            UserName = "newuser",
            Email = "new@test.com",
            Password = "Password@123",
            Roles = new List<UserRoleAssignmentRequest>
            {
                new() { RoleId = role.Id }
            }
        };

        // Act
        var result = await service.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Roles.Should().HaveCount(1);
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateEmail()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);
        var user = new IdentityUser { UserName = "testuser", Email = "old@test.com" };
        await userManager.CreateAsync(user);

        var service = CreateService(ctx, userManager);
        var request = new UpdateUserRequest
        {
            Email = "new@test.com"
        };

        // Act
        var result = await service.UpdateUserAsync(user.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task UpdateUserAsync_WithNonExistentId_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);
        var service = CreateService(ctx, userManager);
        var request = new UpdateUserRequest { Email = "new@test.com" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateUserAsync("nonexistent", request));
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldLockUser()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);
        var user = new IdentityUser { UserName = "testuser", Email = "test@test.com" };
        await userManager.CreateAsync(user);

        var service = CreateService(ctx, userManager);
        var request = new UpdateUserRequest
        {
            IsLocked = true
        };

        // Act
        var result = await service.UpdateUserAsync(user.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUnlockUser()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);
        var user = new IdentityUser 
        { 
            UserName = "testuser", 
            Email = "test@test.com",
            LockoutEnd = DateTimeOffset.UtcNow.AddYears(100)
        };
        await userManager.CreateAsync(user);

        var service = CreateService(ctx, userManager);
        var request = new UpdateUserRequest
        {
            IsLocked = false
        };

        // Act
        var result = await service.UpdateUserAsync(user.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.IsLocked.Should().BeFalse();
    }

    #endregion

    #region UpdateUserRolesAsync Tests

    [Fact]
    public async Task UpdateUserRolesAsync_ShouldUpdateRoles()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);
        var user = new IdentityUser { UserName = "testuser", Email = "test@test.com" };
        await userManager.CreateAsync(user);

        var role = new RoleProfile { Code = "NEW.ROLE", Name = "New Role" };
        ctx.RoleProfiles.Add(role);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx, userManager);
        var request = new UpdateUserRolesRequest
        {
            Roles = new List<UserRoleAssignmentRequest>
            {
                new() { RoleId = role.Id }
            }
        };

        // Act
        var result = await service.UpdateUserRolesAsync(user.Id, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Roles.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithNonExistentUser_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var userManager = CreateUserManager(ctx);
        var service = CreateService(ctx, userManager);
        var request = new UpdateUserRolesRequest
        {
            Roles = new List<UserRoleAssignmentRequest>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            service.UpdateUserRolesAsync("nonexistent", request));
    }

    #endregion
}
