using System.Collections.Concurrent;
using System.Reflection;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Responses.Entity;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Utils;

namespace BobCrm.Api.Extensions;

/// <summary>
/// DTO 转换扩展方法
/// 支持单语/多语双模式，确保向后兼容性
/// </summary>
public static class DtoExtensions
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> DisplayNameKeyPropertyCache = new();

    /// <summary>
    /// 转换为实体摘要 DTO（支持单语/多语双模式）
    /// </summary>
    /// <param name="entity">实体定义对象</param>
    /// <param name="lang">
    /// 目标语言代码（zh/ja/en）。
    /// 为 null 时返回完整多语字典（向后兼容模式）
    /// </param>
    /// <returns>实体摘要 DTO</returns>
    public static EntitySummaryDto ToSummaryDto(this EntityDefinition entity, string? lang = null)
    {
        var dto = new EntitySummaryDto
        {
            EntityType = entity.EntityRoute,
            EntityRoute = entity.EntityRoute,
            EntityName = entity.EntityName,
            ApiEndpoint = entity.ApiEndpoint,
            Icon = entity.Icon,
            Category = entity.Category,
            IsEnabled = entity.IsEnabled,
            IsRootEntity = entity.IsRootEntity,
            Status = entity.Status
        };

        if (lang != null)
        {
            var resolvedDisplayName = entity.DisplayName.Resolve(lang);
            dto.DisplayName = resolvedDisplayName;
            dto.DisplayNameTranslations = null;

            if (entity.Description != null)
            {
                var resolvedDescription = entity.Description.Resolve(lang);
                dto.Description = resolvedDescription;
                dto.DescriptionTranslations = null;
            }
        }
        else
        {
            dto.DisplayName = null;
            dto.Description = null;
            dto.DisplayNameTranslations = entity.DisplayName != null
                ? new MultilingualText(entity.DisplayName)
                : null;
            dto.DescriptionTranslations = entity.Description != null
                ? new MultilingualText(entity.Description)
                : null;
        }

        return dto;
    }

    /// <summary>
    /// 转换为字段元数据 DTO（支持单语/多语双模式）
    /// </summary>
    /// <param name="field">字段元数据对象</param>
    /// <param name="loc">本地化服务（用于解析 DisplayNameKey）</param>
    /// <param name="lang">目标语言代码，为 null 时返回完整多语字典</param>
    /// <returns>字段元数据 DTO</returns>
    public static FieldMetadataDto ToFieldDto(this FieldMetadata field, ILocalization loc, string? lang = null)
    {
        var dto = new FieldMetadataDto
        {
            Id = field.Id,
            PropertyName = field.PropertyName,
            // TODO [ARCH-30]: 待 FieldMetadata 基类添加 DisplayNameKey 属性后改为直接属性访问
            DisplayNameKey = DisplayNameKeyPropertyCache
                .GetOrAdd(field.GetType(), t => t.GetProperty("DisplayNameKey"))
                ?.GetValue(field) as string,
            DataType = field.DataType,
            Length = field.Length,
            Precision = field.Precision,
            Scale = field.Scale,
            IsRequired = field.IsRequired,
            IsEntityRef = field.IsEntityRef,
            ReferencedEntityId = field.ReferencedEntityId,
            TableName = field.TableName,
            SortOrder = field.SortOrder,
            DefaultValue = field.DefaultValue,
            ValidationRules = field.ValidationRules,
            Source = field.Source ?? string.Empty,
            EnumDefinitionId = field.EnumDefinitionId,
            IsMultiSelect = field.IsMultiSelect
        };

        if (lang != null)
        {
            var displayName = ResolveFieldDisplayName(field, loc, lang);
            dto.DisplayName = displayName;
            dto.DisplayNameTranslations = null;
        }
        else
        {
            dto.DisplayName = null;
            dto.DisplayNameTranslations = field.DisplayName != null
                ? new MultilingualText(field.DisplayName)
                : null;
        }

        return dto;
    }

    /// <summary>
    /// 解析字段显示名（三级优先级）
    /// 1. 优先使用 DisplayNameKey（接口字段）
    /// 2. 其次使用 DisplayName 字典（扩展字段）
    /// 3. 最后回退到字段名
    /// </summary>
    /// <param name="field">字段元数据</param>
    /// <param name="loc">本地化服务</param>
    /// <param name="lang">目标语言</param>
    /// <returns>解析后的显示名</returns>
    private static string ResolveFieldDisplayName(FieldMetadata field, ILocalization loc, string lang)
    {
        var propertyInfo = DisplayNameKeyPropertyCache.GetOrAdd(field.GetType(), t => t.GetProperty("DisplayNameKey"));
        var displayNameKey = propertyInfo?.GetValue(field) as string;

        if (!string.IsNullOrWhiteSpace(displayNameKey))
        {
            var translated = loc.T(displayNameKey!, lang);
            if (!string.Equals(translated, displayNameKey, StringComparison.Ordinal))
            {
                return translated;
            }
        }

        if (field.DisplayName != null)
        {
            var resolved = field.DisplayName.Resolve(lang);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }
        }

        return field.PropertyName;
    }
}
