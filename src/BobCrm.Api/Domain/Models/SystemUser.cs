using System.Collections.Generic;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Infrastructure;

namespace BobCrm.Api.Domain.Models;

/// <summary>
/// 系统用户（IdentityUser包装）- 仅用于实体定义元数据，让UI可以统一管理
/// </summary>
public class SystemUser : IBizEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }

    /// <summary>
    /// 提供系统用户实体的初始定义
    /// </summary>
    public static EntityDefinition GetInitialDefinition()
    {
        var type = typeof(SystemUser);
        var definition = new EntityDefinition
        {
            Namespace = type.Namespace ?? "BobCrm.Api.Domain.Models",
            EntityName = type.Name,
            FullTypeName = type.FullName ?? "BobCrm.Api.Domain.Models.SystemUser",
            EntityRoute = "users",
            DisplayName = ResolveText("LBL_SYSTEM_USER", "System User"),
            Description = ResolveText("DESC_SYSTEM_USER", "Manage system users and their access state"),
            ApiEndpoint = "/api/users",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            Source = EntitySource.System,
            IsRootEntity = true,
            IsEnabled = true,
            Order = 5,
            Icon = "user",
            Category = "system"
        };

        definition.Fields = new List<FieldMetadata>
        {
            new()
            {
                PropertyName = "Id",
                DisplayName = ResolveText("LBL_SYSTEM_USER_ID", "ID"),
                DataType = FieldDataType.Guid,
                IsRequired = true,
                SortOrder = 1,
                Source = FieldSource.System
            },
            new()
            {
                PropertyName = "UserName",
                DisplayName = ResolveText("LBL_SYSTEM_USER_USERNAME", "User Name"),
                DataType = FieldDataType.String,
                Length = 128,
                IsRequired = true,
                SortOrder = 2,
                Source = FieldSource.System
            },
            new()
            {
                PropertyName = "Email",
                DisplayName = ResolveText("LBL_SYSTEM_USER_EMAIL", "Email"),
                DataType = FieldDataType.String,
                Length = 256,
                IsRequired = false,
                SortOrder = 3,
                Source = FieldSource.System
            },
            new()
            {
                PropertyName = "EmailConfirmed",
                DisplayName = ResolveText("LBL_SYSTEM_USER_EMAIL_CONFIRMED", "Email Confirmed"),
                DataType = FieldDataType.Boolean,
                IsRequired = true,
                SortOrder = 4,
                Source = FieldSource.System
            },
            new()
            {
                PropertyName = "PhoneNumber",
                DisplayName = ResolveText("LBL_SYSTEM_USER_PHONE", "Phone Number"),
                DataType = FieldDataType.String,
                Length = 32,
                IsRequired = false,
                SortOrder = 5,
                Source = FieldSource.System
            },
            new()
            {
                PropertyName = "PhoneNumberConfirmed",
                DisplayName = ResolveText("LBL_SYSTEM_USER_PHONE_CONFIRMED", "Phone Confirmed"),
                DataType = FieldDataType.Boolean,
                IsRequired = true,
                SortOrder = 6,
                Source = FieldSource.System
            },
            new()
            {
                PropertyName = "TwoFactorEnabled",
                DisplayName = ResolveText("LBL_SYSTEM_USER_TWO_FACTOR", "Two Factor Enabled"),
                DataType = FieldDataType.Boolean,
                IsRequired = true,
                SortOrder = 7,
                Source = FieldSource.System
            },
            new()
            {
                PropertyName = "LockoutEnabled",
                DisplayName = ResolveText("LBL_SYSTEM_USER_LOCKOUT_ENABLED", "Lockout Enabled"),
                DataType = FieldDataType.Boolean,
                IsRequired = true,
                SortOrder = 8,
                Source = FieldSource.System
            },
            new()
            {
                PropertyName = "LockoutEnd",
                DisplayName = ResolveText("LBL_SYSTEM_USER_LOCKOUT_END", "Lockout End"),
                DataType = FieldDataType.DateTime,
                IsRequired = false,
                SortOrder = 9,
                Source = FieldSource.System
            },
            new()
            {
                PropertyName = "AccessFailedCount",
                DisplayName = ResolveText("LBL_SYSTEM_USER_ACCESS_FAILED", "Access Failed Count"),
                DataType = FieldDataType.Integer,
                IsRequired = true,
                SortOrder = 10,
                Source = FieldSource.System
            }
        };

        definition.Interfaces = new List<EntityInterface>
        {
            new()
            {
                InterfaceType = EntityInterfaceType.Base,
                IsEnabled = true,
                IsLocked = true
            }
        };

        return definition;
    }

    private static readonly Lazy<Dictionary<string, Dictionary<string, string?>>> ResourceCache =
        new(() => I18nResourceLoader.LoadResources()
            .ToDictionary(
                r => r.Key,
                r => r.Translations.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (string?)kvp.Value,
                    StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase));

    private static Dictionary<string, string?> ResolveText(string key, string fallback)
    {
        if (ResourceCache.Value.TryGetValue(key, out var translations) && translations.Count > 0)
        {
            return new Dictionary<string, string?>(translations, StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = fallback
        };
    }
}
