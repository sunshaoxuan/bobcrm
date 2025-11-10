using System.Security.Claims;
using BobCrm.Api.Domain;
using BobCrm.Api.Domain.Models;
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
    public DbSet<FormTemplate> FormTemplates => Set<FormTemplate>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();

    // 本地化
    public DbSet<LocalizationResource> LocalizationResources => Set<LocalizationResource>();
    public DbSet<LocalizationLanguage> LocalizationLanguages => Set<LocalizationLanguage>();

    // 实体自定义与发布（统一的实体定义系统）
    public DbSet<EntityDefinition> EntityDefinitions => Set<EntityDefinition>();
    public DbSet<FieldMetadata> FieldMetadatas => Set<FieldMetadata>();
    public DbSet<EntityInterface> EntityInterfaces => Set<EntityInterface>();
    public DbSet<DDLScript> DDLScripts => Set<DDLScript>();

    // 数据保护
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // 应用所有配置
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // PostgreSQL jsonb 映射配置 - 多语言字段
        b.Entity<EntityDefinition>()
            .Property(e => e.DisplayName)
            .HasColumnType("jsonb");

        b.Entity<EntityDefinition>()
            .Property(e => e.Description)
            .HasColumnType("jsonb");

        b.Entity<FieldMetadata>()
            .Property(f => f.DisplayName)
            .HasColumnType("jsonb");

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

        // UserLayout 索引
        b.Entity<UserLayout>()
            .HasIndex(ul => new { ul.UserId, ul.EntityType });

        // FormTemplate 索引和约束
        b.Entity<FormTemplate>()
            .HasIndex(ft => new { ft.UserId, ft.EntityType });

        b.Entity<FormTemplate>()
            .HasIndex(ft => new { ft.UserId, ft.EntityType, ft.IsUserDefault });

        b.Entity<FormTemplate>()
            .HasIndex(ft => new { ft.EntityType, ft.IsSystemDefault });

        // EntityDefinition 配置
        b.Entity<EntityDefinition>()
            .HasIndex(ed => new { ed.Namespace, ed.EntityName })
            .IsUnique();

        b.Entity<EntityDefinition>()
            .HasIndex(ed => ed.Status);

        b.Entity<EntityDefinition>()
            .HasIndex(ed => ed.IsLocked);

        // FieldMetadata 配置
        b.Entity<FieldMetadata>()
            .HasIndex(fm => fm.EntityDefinitionId);

        b.Entity<FieldMetadata>()
            .HasIndex(fm => fm.ParentFieldId);

        b.Entity<FieldMetadata>()
            .HasIndex(fm => new { fm.EntityDefinitionId, fm.PropertyName })
            .IsUnique();

        // 配置关系
        b.Entity<FieldMetadata>()
            .HasOne(fm => fm.EntityDefinition)
            .WithMany(ed => ed.Fields)
            .HasForeignKey(fm => fm.EntityDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<FieldMetadata>()
            .HasOne(fm => fm.ParentField)
            .WithMany(pf => pf.ChildFields)
            .HasForeignKey(fm => fm.ParentFieldId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<FieldMetadata>()
            .HasOne(fm => fm.ReferencedEntity)
            .WithMany()
            .HasForeignKey(fm => fm.ReferencedEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        // EntityInterface 配置
        b.Entity<EntityInterface>()
            .HasIndex(ei => new { ei.EntityDefinitionId, ei.InterfaceType })
            .IsUnique();

        b.Entity<EntityInterface>()
            .HasOne(ei => ei.EntityDefinition)
            .WithMany(ed => ed.Interfaces)
            .HasForeignKey(ei => ei.EntityDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // DDLScript 配置
        b.Entity<DDLScript>()
            .HasIndex(ds => ds.EntityDefinitionId);

        b.Entity<DDLScript>()
            .HasIndex(ds => ds.Status);

        b.Entity<DDLScript>()
            .HasOne(ds => ds.EntityDefinition)
            .WithMany(ed => ed.DDLScripts)
            .HasForeignKey(ds => ds.EntityDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private AppDbContext db => this;
}
