using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

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
        return layout => layout.UserId == userId &&
            (layout.EntityType == scope ||
             (layout.EntityType == null && EF.Property<int>(layout, "CustomerId") == customerId));
    }

    public static void ApplyCustomerScope(UserLayout layout, int customerId)
    {
        layout.EntityType = ForCustomer(customerId);
        // Legacy column write (for existing schema/backward compatibility).
        typeof(UserLayout).GetProperty("CustomerId")?.SetValue(layout, customerId);
    }
}
