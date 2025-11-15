using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Base.Models.Metadata;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
    public DbSet<TemplateBinding> TemplateBindings => Set<TemplateBinding>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();

    // 本地化
    public DbSet<LocalizationResource> LocalizationResources => Set<LocalizationResource>();
    public DbSet<LocalizationLanguage> LocalizationLanguages => Set<LocalizationLanguage>();

    // 实体自定义与发布（统一的实体定义系统）
    public DbSet<EntityDefinition> EntityDefinitions => Set<EntityDefinition>();
    public DbSet<SubEntityDefinition> SubEntityDefinitions => Set<SubEntityDefinition>();
    public DbSet<FieldMetadata> FieldMetadatas => Set<FieldMetadata>();
    public DbSet<EntityInterface> EntityInterfaces => Set<EntityInterface>();
    public DbSet<DDLScript> DDLScripts => Set<DDLScript>();
    public DbSet<OrganizationNode> OrganizationNodes => Set<OrganizationNode>();
    public DbSet<RoleProfile> RoleProfiles => Set<RoleProfile>();
    public DbSet<FunctionNode> FunctionNodes => Set<FunctionNode>();
    public DbSet<RoleFunctionPermission> RoleFunctionPermissions => Set<RoleFunctionPermission>();
    public DbSet<RoleDataScope> RoleDataScopes => Set<RoleDataScope>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
    public DbSet<FieldDataTypeEntry> FieldDataTypes => Set<FieldDataTypeEntry>();
    public DbSet<FieldSourceEntry> FieldSources => Set<FieldSourceEntry>();
    public DbSet<EntityDomain> EntityDomains => Set<EntityDomain>();
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    // 数据保护
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // 应用所有配置
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // PostgreSQL jsonb 映射配置 - 多语言字段
        // 使用值转换器将 Dictionary<string, string?> 序列化为 JSON 字符串
        var jsonOptions = new JsonSerializerOptions();
        var jsonConverter = new ValueConverter<Dictionary<string, string?>?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
            v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<Dictionary<string, string?>>(v, jsonOptions));

        // LocalizationResource.Translations 值转换器（Dictionary<string, string> → JSON string）
        var translationsConverter = new ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>());

        b.Entity<EntityDefinition>()
            .Property(e => e.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter);

        b.Entity<EntityDefinition>()
            .Property(e => e.Description)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter);

        b.Entity<FieldMetadata>()
            .Property(f => f.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter);

        b.Entity<FunctionNode>()
            .Property(f => f.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter);

        b.Entity<SubEntityDefinition>()
            .Property(s => s.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<SubEntityDefinition>()
            .Property(s => s.Description)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter);

        b.Entity<EntityDomain>()
            .Property(d => d.Name)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter);

        b.Entity<FunctionNode>()
            .Property(f => f.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter);

        // LocalizationResource 的 Translations 使用 jsonb 存储
        b.Entity<LocalizationResource>()
            .Property(lr => lr.Translations)
            .HasColumnType("jsonb")
            .HasConversion(translationsConverter);

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
        ConfigureIndexes(b, jsonOptions);
    }

    private void ConfigureIndexes(ModelBuilder b, JsonSerializerOptions jsonOptions)
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
        var tagsConverter = new ValueConverter<List<string>?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
            v => string.IsNullOrEmpty(v)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>());
        b.Entity<FormTemplate>()
            .HasIndex(ft => new { ft.UserId, ft.EntityType });

        b.Entity<FormTemplate>()
            .HasIndex(ft => new { ft.UserId, ft.EntityType, ft.IsUserDefault });

        b.Entity<FormTemplate>()
            .HasIndex(ft => new { ft.EntityType, ft.IsSystemDefault });

        b.Entity<FormTemplate>()
            .Property(ft => ft.Tags)
            .HasColumnType("jsonb")
            .HasConversion(tagsConverter);

        b.Entity<FormTemplate>()
            .Property(ft => ft.RequiredFunctionCode)
            .HasMaxLength(128);

        b.Entity<TemplateBinding>()
            .HasIndex(tb => new { tb.EntityType, tb.UsageType, tb.IsSystem })
            .IsUnique();

        b.Entity<TemplateBinding>()
            .Property(tb => tb.EntityType)
            .HasMaxLength(128)
            .IsRequired();

        b.Entity<TemplateBinding>()
            .Property(tb => tb.RequiredFunctionCode)
            .HasMaxLength(128);

        b.Entity<TemplateBinding>()
            .Property(tb => tb.UpdatedBy)
            .HasMaxLength(128);

        b.Entity<TemplateBinding>()
            .HasOne(tb => tb.Template)
            .WithMany()
            .HasForeignKey(tb => tb.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<FieldDataTypeEntry>()
            .HasIndex(dt => dt.Code)
            .IsUnique();

        b.Entity<FieldDataTypeEntry>()
            .Property(dt => dt.Description)
            .HasColumnType("jsonb")
            .HasConversion(new ValueConverter<Dictionary<string, string?>?, string?>(
                v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
                v => string.IsNullOrEmpty(v)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, string?>>(v, jsonOptions)));

        b.Entity<FieldSourceEntry>()
            .HasIndex(fs => fs.Code)
            .IsUnique();

        b.Entity<EntityDomain>()
            .HasIndex(d => d.Code)
            .IsUnique();

        b.Entity<EntityDomain>()
            .HasIndex(d => d.SortOrder);

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
            .HasIndex(fm => fm.SubEntityDefinitionId);

        b.Entity<FieldMetadata>()
            .HasIndex(fm => new { fm.EntityDefinitionId, fm.PropertyName });

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

        b.Entity<FieldMetadata>()
            .HasOne(fm => fm.SubEntityDefinition)
            .WithMany(se => se.Fields)
            .HasForeignKey(fm => fm.SubEntityDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

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
