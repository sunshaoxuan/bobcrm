namespace BobCrm.Api.Base.Attributes;

/// <summary>
/// Marks a property as sensitive and should be masked in audit logs and snapshots.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SensitiveAttribute : Attribute
{
}

