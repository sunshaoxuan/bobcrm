using System.Linq.Expressions;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

public sealed class LookupResolveService
{
    private static readonly string[] DefaultDisplayFieldCandidates =
    [
        "Name",
        "Title",
        "Code",
        "UserName",
        "Email"
    ];

    private readonly AppDbContext _db;
    private readonly DynamicEntityService _dynamicEntityService;

    public LookupResolveService(AppDbContext db, DynamicEntityService dynamicEntityService)
    {
        _db = db;
        _dynamicEntityService = dynamicEntityService;
    }

    public async Task<Dictionary<string, string>> ResolveAsync(
        string target,
        IReadOnlyCollection<string> ids,
        string? displayField,
        CancellationToken ct)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(target) || ids.Count == 0)
        {
            return result;
        }

        var fullTypeName = await ResolveFullTypeNameAsync(target.Trim(), ct);
        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return result;
        }

        var entityType = ResolveEntityType(fullTypeName);
        if (entityType == null)
        {
            return result;
        }

        var keyProperty = entityType.GetProperty("Id");
        if (keyProperty == null)
        {
            return result;
        }

        var displayProperty = ResolveDisplayProperty(entityType, displayField);
        var typedKeys = ConvertKeys(ids, keyProperty.PropertyType);
        if (typedKeys.Count == 0)
        {
            return result;
        }

        var query = CreateQuery(entityType);
        if (query == null)
        {
            return result;
        }

        var predicate = BuildContainsPredicate(entityType, keyProperty, typedKeys);
        query = ApplyWhere(query, entityType, predicate);
        var list = await ToListAsync(query, entityType, ct);

        foreach (var entity in list)
        {
            var idValue = keyProperty.GetValue(entity);
            if (idValue == null)
            {
                continue;
            }

            var key = idValue.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var display = displayProperty?.GetValue(entity)?.ToString();
            if (string.IsNullOrWhiteSpace(display))
            {
                display = key;
            }

            result[key] = display;
        }

        return result;
    }

    private Type? ResolveEntityType(string fullTypeName)
    {
        var dynamicType = _dynamicEntityService.GetEntityType(fullTypeName);
        if (dynamicType != null)
        {
            return dynamicType;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(fullTypeName, throwOnError: false, ignoreCase: true);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    private async Task<string?> ResolveFullTypeNameAsync(string target, CancellationToken ct)
    {
        if (target.Contains('.', StringComparison.Ordinal))
        {
            return target;
        }

        var match = await _db.EntityDefinitions
            .AsNoTracking()
            .Where(e => e.EntityRoute == target || e.EntityName == target)
            .Select(e => e.FullTypeName)
            .FirstOrDefaultAsync(ct);

        return string.IsNullOrWhiteSpace(match) ? null : match;
    }

    private static System.Reflection.PropertyInfo? ResolveDisplayProperty(Type entityType, string? displayField)
    {
        if (!string.IsNullOrWhiteSpace(displayField))
        {
            var explicitProp = entityType.GetProperty(displayField.Trim());
            if (explicitProp != null)
            {
                return explicitProp;
            }
        }

        foreach (var candidate in DefaultDisplayFieldCandidates)
        {
            var prop = entityType.GetProperty(candidate);
            if (prop != null)
            {
                return prop;
            }
        }

        return entityType.GetProperty("Id");
    }

    private static List<object> ConvertKeys(IReadOnlyCollection<string> ids, Type keyType)
    {
        var result = new List<object>(ids.Count);
        var nonNullable = Nullable.GetUnderlyingType(keyType) ?? keyType;

        foreach (var raw in ids)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var trimmed = raw.Trim();
            if (nonNullable == typeof(string))
            {
                result.Add(trimmed);
                continue;
            }

            if (nonNullable == typeof(int) && int.TryParse(trimmed, out var intValue))
            {
                result.Add(intValue);
                continue;
            }

            if (nonNullable == typeof(long) && long.TryParse(trimmed, out var longValue))
            {
                result.Add(longValue);
                continue;
            }

            if (nonNullable == typeof(Guid) && Guid.TryParse(trimmed, out var guidValue))
            {
                result.Add(guidValue);
                continue;
            }
        }

        return result;
    }

    private static LambdaExpression BuildContainsPredicate(
        Type entityType,
        System.Reflection.PropertyInfo keyProperty,
        IReadOnlyList<object> keys)
    {
        var nonNullableKeyType = Nullable.GetUnderlyingType(keyProperty.PropertyType) ?? keyProperty.PropertyType;
        var typedListType = typeof(List<>).MakeGenericType(nonNullableKeyType);
        var typedList = (System.Collections.IList)Activator.CreateInstance(typedListType)!;
        foreach (var key in keys)
        {
            typedList.Add(key);
        }

        var param = Expression.Parameter(entityType, "e");
        var member = Expression.Property(param, keyProperty);

        Expression memberValue = member;
        if (member.Type != nonNullableKeyType)
        {
            memberValue = Expression.Convert(member, nonNullableKeyType);
        }

        var containsMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(nonNullableKeyType);

        var call = Expression.Call(
            null,
            containsMethod,
            Expression.Constant(typedList),
            memberValue);

        return Expression.Lambda(call, param);
    }

    private IQueryable? CreateQuery(Type entityType)
    {
        var setMethod = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes);
        if (setMethod == null)
        {
            return null;
        }

        var dbSet = setMethod.MakeGenericMethod(entityType).Invoke(_db, null) as IQueryable;
        if (dbSet == null)
        {
            return null;
        }

        var asNoTracking = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .FirstOrDefault(m => m.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking) &&
                                 m.IsGenericMethodDefinition &&
                                 m.GetParameters().Length == 1);
        if (asNoTracking == null)
        {
            return dbSet;
        }

        return asNoTracking.MakeGenericMethod(entityType).Invoke(null, new object[] { dbSet }) as IQueryable;
    }

    private static IQueryable ApplyWhere(IQueryable source, Type entityType, LambdaExpression predicate)
    {
        var whereMethod = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == nameof(Queryable.Where) && m.GetParameters().Length == 2)
            .MakeGenericMethod(entityType);

        return (IQueryable)whereMethod.Invoke(null, new object[] { source, predicate })!;
    }

    private static async Task<List<object>> ToListAsync(IQueryable source, Type entityType, CancellationToken ct)
    {
        var toListAsync = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .First(m => m.Name == nameof(EntityFrameworkQueryableExtensions.ToListAsync) &&
                        m.IsGenericMethodDefinition &&
                        m.GetParameters().Length == 2)
            .MakeGenericMethod(entityType);

        var task = (Task)toListAsync.Invoke(null, new object[] { source, ct })!;
        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty("Result");
        var list = (System.Collections.IEnumerable?)resultProperty?.GetValue(task);
        if (list == null)
        {
            return new List<object>();
        }

        var objects = new List<object>();
        foreach (var item in list)
        {
            if (item != null)
            {
                objects.Add(item);
            }
        }

        return objects;
    }
}
