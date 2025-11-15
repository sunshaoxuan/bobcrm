namespace BobCrm.Api.Base;

public static class NavDisplayModes
{
    public const string Icons = "icons";
    public const string Labels = "labels";
    public const string IconText = "icon-text";

    public static string Normalize(string? value, string fallback = IconText)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var v = value.Trim().ToLowerInvariant();
        return v switch
        {
            Icons => Icons,
            Labels => Labels,
            IconText => IconText,
            _ => fallback
        };
    }

    public static bool IsValid(string? value) =>
        value is not null && (value == Icons || value == Labels || value == IconText);
}
