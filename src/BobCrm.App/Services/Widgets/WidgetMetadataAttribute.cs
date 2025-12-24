using System;
using AntDesign;

namespace BobCrm.App.Services.Widgets;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class WidgetMetadataAttribute : Attribute
{
    public string Type { get; }
    public string LabelKey { get; }
    public string Icon { get; }
    public WidgetCategory Category { get; }

    public WidgetMetadataAttribute(string type, string labelKey, string icon, WidgetCategory category)
    {
        Type = type;
        LabelKey = labelKey;
        Icon = icon;
        Category = category;
    }
}
