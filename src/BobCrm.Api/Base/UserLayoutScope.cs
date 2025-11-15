using System.Linq.Expressions;

namespace BobCrm.Api.Base;

/// <summary>
/// Utility helpers for mapping user layout scopes (customer, entity types, etc.).
/// Centralizing here keeps legacy CustomerId compatibility within a single file.
/// </summary>
public static class UserLayoutScope
{
    private const string CustomerPrefix = "customer:";

    public static string ForCustomer(int customerId) => $"{CustomerPrefix}{customerId}";

    public static Expression<Func<UserLayout, bool>> ForUser(string userId, int customerId)
    {
        var scope = ForCustomer(customerId);
#pragma warning disable CS0618 // Legacy data fallback
        return layout => layout.UserId == userId &&
            (layout.EntityType == scope || (layout.EntityType == null && layout.CustomerId == customerId));
#pragma warning restore CS0618
    }

    public static void ApplyCustomerScope(UserLayout layout, int customerId)
    {
        layout.EntityType = ForCustomer(customerId);
#pragma warning disable CS0618 // Maintain backward compatibility for existing rows
        layout.CustomerId = customerId;
#pragma warning restore CS0618
    }
}
