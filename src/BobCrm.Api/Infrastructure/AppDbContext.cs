using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Attributes;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Base.Models.Metadata;
using BobCrm.Api.Abstractions;
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
    private static readonly JsonSerializerOptions AuditJsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string[] SensitivePropertyNameFragments =
    [
        "password",
        "pwd",
        "hash",
        "salt",
        "secret",
        "token",
        "apikey",
        "api_key",
        "refresh",
        "privatekey",
        "private_key"
    ];

    private readonly IHttpContextAccessor? _http;
    private readonly IAuditService? _auditService;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor accessor) : base(options)
    {
        _http = accessor;
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor accessor, IAuditService auditService) : base(options)
    {
        _http = accessor;
        _auditService = auditService;
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
    public DbSet<TemplateStateBinding> TemplateStateBindings => Set<TemplateStateBinding>();
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
    public DbSet<FieldPermission> FieldPermissions => Set<FieldPermission>();
    public DbSet<FieldDataTypeEntry> FieldDataTypes => Set<FieldDataTypeEntry>();
    public DbSet<FieldSourceEntry> FieldSources => Set<FieldSourceEntry>();
    public DbSet<EntityDomain> EntityDomains => Set<EntityDomain>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // 动态枚举系统
    public DbSet<EnumDefinition> EnumDefinitions => Set<EnumDefinition>();
    public DbSet<EnumOption> EnumOptions => Set<EnumOption>();

    // 数据源与权限
    public DbSet<DataSet> DataSets => Set<DataSet>();
    public DbSet<QueryDefinition> QueryDefinitions => Set<QueryDefinition>();
    public DbSet<PermissionFilter> PermissionFilters => Set<PermissionFilter>();
    public DbSet<DataSourceTypeEntry> DataSourceTypes => Set<DataSourceTypeEntry>();

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
            .HasConversion(jsonConverter!);

        b.Entity<EntityDefinition>()
            .Property(e => e.Description)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<FieldMetadata>()
            .Property(f => f.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<FunctionNode>()
            .Property(f => f.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<SubEntityDefinition>()
            .Property(s => s.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<SubEntityDefinition>()
            .Property(s => s.Description)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<EntityDomain>()
            .Property(d => d.Name)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<FunctionNode>()
            .Property(f => f.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        // 数据源相关模型的多语字段配置
        b.Entity<DataSet>()
            .Property(d => d.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<DataSet>()
            .Property(d => d.Description)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<QueryDefinition>()
            .Property(q => q.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<QueryDefinition>()
            .Property(q => q.Description)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<PermissionFilter>()
            .Property(p => p.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<PermissionFilter>()
            .Property(p => p.Description)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<DataSourceTypeEntry>()
            .Property(d => d.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<DataSourceTypeEntry>()
            .Property(d => d.Description)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        // 动态枚举系统多语言字段配置
        b.Entity<EnumDefinition>()
            .Property(e => e.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<EnumDefinition>()
            .Property(e => e.Description)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<EnumOption>()
            .Property(e => e.DisplayName)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

        b.Entity<EnumOption>()
            .Property(e => e.Description)
            .HasColumnType("jsonb")
            .HasConversion(jsonConverter!);

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

        b.Entity<TemplateStateBinding>()
            .HasIndex(tsb => new { tsb.TemplateId, tsb.ViewState })
            ;

        b.Entity<TemplateStateBinding>()
            .HasIndex(tsb => new { tsb.EntityType, tsb.ViewState })
            .IsUnique()
            .HasFilter("\"IsDefault\" = TRUE");

        b.Entity<TemplateStateBinding>()
            .HasIndex(tsb => new { tsb.EntityType, tsb.ViewState });

        b.Entity<TemplateStateBinding>()
            .Property(tsb => tsb.EntityType)
            .HasMaxLength(128)
            .IsRequired();

        b.Entity<TemplateStateBinding>()
            .Property(tsb => tsb.ViewState)
            .HasMaxLength(64)
            .IsRequired();

        b.Entity<TemplateStateBinding>()
            .Property(tsb => tsb.RequiredPermission)
            .HasMaxLength(128);

        b.Entity<TemplateStateBinding>()
            .Property(tsb => tsb.MatchFieldName)
            .HasMaxLength(128);

        b.Entity<TemplateStateBinding>()
            .Property(tsb => tsb.MatchFieldValue)
            .HasMaxLength(256);

        b.Entity<TemplateStateBinding>()
            .HasOne(tsb => tsb.Template)
            .WithMany(ft => ft.StateBindings)
            .HasForeignKey(tsb => tsb.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

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

        // 配置枚举引用（FieldMetadata -> EnumDefinition）
        b.Entity<FieldMetadata>()
            .HasOne(fm => fm.EnumDefinition)
            .WithMany()
            .HasForeignKey(fm => fm.EnumDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<FieldMetadata>()
            .HasIndex(fm => fm.EnumDefinitionId);

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

        // 动态枚举系统配置
        b.Entity<EnumDefinition>()
            .HasIndex(ed => ed.Code)
            .IsUnique();

        b.Entity<EnumDefinition>()
            .HasIndex(ed => ed.IsSystem);

        b.Entity<EnumDefinition>()
            .HasIndex(ed => ed.IsEnabled);

        b.Entity<EnumOption>()
            .HasIndex(eo => eo.EnumDefinitionId);

        b.Entity<EnumOption>()
            .HasIndex(eo => new { eo.EnumDefinitionId, eo.Value })
            .IsUnique();

        b.Entity<EnumOption>()
            .HasIndex(eo => new { eo.EnumDefinitionId, eo.SortOrder });

        b.Entity<EnumOption>()
            .HasOne(eo => eo.EnumDefinition)
            .WithMany(ed => ed.Options)
            .HasForeignKey(eo => eo.EnumDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLogs = CaptureAuditLogsIfEnabled();

            if (auditLogs is { Count: > 0 } && _auditService != null)
            {
                await _auditService.AttachAsync(this, auditLogs.Select(x => x.Log).ToList(), cancellationToken);
            }

            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException) when (Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
        {
            // InMemory provider can throw concurrency errors when entities are mutated in-memory between saves.
            return 0;
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateConcurrencyException) when (Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
        {
            return 0;
        }
    }

    private AppDbContext db => this;

    private List<(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry, AuditLog Log)>? CaptureAuditLogsIfEnabled()
    {
        if (_auditService == null)
        {
            return null;
        }

        if (ChangeTracker.AutoDetectChangesEnabled)
        {
            ChangeTracker.DetectChanges();
        }

        var http = _http?.HttpContext;
        var user = http?.User;
        var actorId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        var actorName = user?.Identity?.Name ?? user?.FindFirstValue("name") ?? actorId;
        var ipAddress = http?.Connection?.RemoteIpAddress?.ToString();

        var pending = new List<(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry, AuditLog Log)>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog)
            {
                continue;
            }

            if (entry.Metadata.IsOwned())
            {
                continue;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var operationType = ResolveOperationType(entry);
            var module = entry.Metadata.ClrType.Name;
            var target = TryGetPrimaryKeyString(entry);

            var before = entry.State == EntityState.Added ? null : CreateSnapshot(entry, useOriginalValues: true);
            var after = entry.State == EntityState.Deleted ? null : CreateSnapshot(entry, useOriginalValues: false);
            var changes = CreatePropertyChanges(entry);

            var log = new AuditLog
            {
                Module = module,
                OperationType = operationType,
                ActorId = actorId,
                ActorName = actorName,
                IpAddress = ipAddress,
                Target = target,
                BeforeJson = before == null ? null : JsonSerializer.Serialize(before, AuditJsonSerializerOptions),
                AfterJson = after == null ? null : JsonSerializer.Serialize(after, AuditJsonSerializerOptions),
                ChangesJson = changes == null ? null : JsonSerializer.Serialize(changes, AuditJsonSerializerOptions),
                OccurredAt = DateTime.UtcNow
            };

            pending.Add((entry, log));
        }

        return pending.Count == 0 ? null : pending;
    }

    private static string ResolveOperationType(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
        {
            return "C";
        }

        if (entry.State == EntityState.Deleted)
        {
            return "D";
        }

        if (entry.Entity is EntityDefinition &&
            entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(EntityDefinition.Status)) is { IsModified: true } statusProp &&
            statusProp.CurrentValue is string newStatus &&
            statusProp.OriginalValue is string oldStatus &&
            !string.Equals(oldStatus, EntityStatus.Published, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(newStatus, EntityStatus.Published, StringComparison.OrdinalIgnoreCase))
        {
            return "P";
        }

        return "U";
    }

    private static string? TryGetPrimaryKeyString(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var keyProps = entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).ToList();
        if (keyProps.Count == 0)
        {
            return null;
        }

        if (keyProps.Count == 1)
        {
            return keyProps[0].CurrentValue?.ToString() ?? keyProps[0].OriginalValue?.ToString();
        }

        return string.Join("|", keyProps.Select(p =>
        {
            var val = p.CurrentValue ?? p.OriginalValue;
            return $"{p.Metadata.Name}:{val}";
        }));
    }

    private static SortedDictionary<string, object?> CreateSnapshot(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, bool useOriginalValues)
    {
        var snapshot = new SortedDictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in entry.Properties)
        {
            if (prop.Metadata.IsShadowProperty())
            {
                continue;
            }

            var propName = prop.Metadata.Name;
            var value = useOriginalValues ? prop.OriginalValue : prop.CurrentValue;
            snapshot[propName] = IsSensitiveProperty(entry, propName) ? MaskSensitiveValue(value) : value;
        }

        return snapshot;
    }

    private static List<Dictionary<string, object?>>? CreatePropertyChanges(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        if (entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
        {
            return null;
        }

        var changes = new List<Dictionary<string, object?>>();

        foreach (var prop in entry.Properties.OrderBy(p => p.Metadata.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (prop.Metadata.IsShadowProperty())
            {
                continue;
            }

            var propName = prop.Metadata.Name;
            var isSensitive = IsSensitiveProperty(entry, propName);

            var before = prop.OriginalValue;
            var after = prop.CurrentValue;

            if (entry.State == EntityState.Modified && !prop.IsModified)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                before = null;
            }

            if (entry.State == EntityState.Deleted)
            {
                after = null;
            }

            changes.Add(new Dictionary<string, object?>
            {
                ["property"] = propName,
                ["before"] = isSensitive ? MaskSensitiveValue(before) : before,
                ["after"] = isSensitive ? MaskSensitiveValue(after) : after
            });
        }

        return changes.Count == 0 ? null : changes;
    }

    private static bool IsSensitiveProperty(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName)
    {
        if (IsSensitivePropertyName(propertyName))
        {
            return true;
        }

        var propertyInfo = entry.Metadata.ClrType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return propertyInfo?.GetCustomAttribute<SensitiveAttribute>(inherit: true) != null;
    }

    private static object? MaskSensitiveValue(object? value)
    {
        return value == null ? null : "***";
    }

    private static bool IsSensitivePropertyName(string propertyName)
    {
        foreach (var fragment in SensitivePropertyNameFragments)
        {
            if (propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
