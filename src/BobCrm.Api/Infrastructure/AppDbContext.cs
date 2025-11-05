using System.Security.Claims;
using BobCrm.Api.Domain;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Infrastructure;

/// <summary>
/// 应用程序数据库上下文
/// 统一管理所有实体的数据访问
/// </summary>
public class AppDbContext : IdentityDbContext<IdentityUser>, IDataProtectionKeyContext
{
    private readonly IHttpContextAccessor? _http;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor accessor) : base(options)
    {
        _http = accessor;
    }

    // 业务实体
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAccess> CustomerAccesses => Set<CustomerAccess>();
    public DbSet<CustomerLocalization> CustomerLocalizations => Set<CustomerLocalization>();
    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();
    public DbSet<FieldValue> FieldValues => Set<FieldValue>();

    // 用户相关
    public DbSet<UserLayout> UserLayouts => Set<UserLayout>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // 本地化
    public DbSet<LocalizationResource> LocalizationResources => Set<LocalizationResource>();
    public DbSet<LocalizationLanguage> LocalizationLanguages => Set<LocalizationLanguage>();

    // 元数据
    public DbSet<Data.Entities.EntityMetadata> EntityMetadata => Set<Data.Entities.EntityMetadata>();

    // 数据保护
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // 应用所有配置
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // 全局过滤器已移除：访问控制由业务层（CustomerQueries）统一处理
        // 原因：
        // 1. 全局过滤器 + required 导航会导致 EF 警告和意外的空结果
        // 2. CustomerQueries 已实现"无访问行则放开，有访问行则严格检查"的策略
        // 3. 避免重复过滤和不可控的副作用
        // 
        // 如果未来需要重新启用，请：
        // - 确保与业务层策略一致
        // - 将 CustomerLocalization 等导航设为 optional
        // - 添加"无访问行不筛选"的表达式

        // 索引配置
        ConfigureIndexes(b);
    }

    private void ConfigureIndexes(ModelBuilder b)
    {
        // Customer 索引
        b.Entity<Customer>()
            .HasIndex(c => c.Code)
            .IsUnique();

        // CustomerAccess 索引
        b.Entity<CustomerAccess>()
            .HasIndex(ca => new { ca.UserId, ca.CustomerId })
            .IsUnique();

        // UserPreferences 索引
        b.Entity<UserPreferences>()
            .HasIndex(up => up.UserId)
            .IsUnique();

        // LocalizationResource 索引
        b.Entity<LocalizationResource>()
            .HasIndex(lr => lr.Key)
            .IsUnique();

        // LocalizationLanguage 索引
        b.Entity<LocalizationLanguage>()
            .HasIndex(ll => ll.Code)
            .IsUnique();

        // RefreshToken 索引
        b.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();

        b.Entity<RefreshToken>()
            .HasIndex(rt => new { rt.UserId, rt.ExpiresAt });

        // FieldDefinition 索引
        b.Entity<FieldDefinition>()
            .HasIndex(fd => fd.Key);

        // FieldValue 索引
        b.Entity<FieldValue>()
            .HasIndex(fv => new { fv.CustomerId, fv.FieldDefinitionId });

        // EntityMetadata 配置
        b.Entity<Data.Entities.EntityMetadata>()
            .HasKey(em => em.EntityType);
        
        b.Entity<Data.Entities.EntityMetadata>()
            .HasIndex(em => new { em.IsRootEntity, em.IsEnabled, em.Order });
    }

    private AppDbContext db => this;
}
