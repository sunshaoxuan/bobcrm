namespace BobCrm.Api.Core.Constants;

/// <summary>
/// 字段数据类型常量
/// </summary>
public static class FieldDataTypeConstants
{
    /// <summary>
    /// 字符串类型
    /// </summary>
    public const string String = "String";

    /// <summary>
    /// 整数类型
    /// </summary>
    public const string Integer = "Integer";

    /// <summary>
    /// 长整数类型
    /// </summary>
    public const string Long = "Long";

    /// <summary>
    /// 小数类型
    /// </summary>
    public const string Decimal = "Decimal";

    /// <summary>
    /// 布尔类型
    /// </summary>
    public const string Boolean = "Boolean";

    /// <summary>
    /// 日期时间类型
    /// </summary>
    public const string DateTime = "DateTime";

    /// <summary>
    /// 日期类型
    /// </summary>
    public const string Date = "Date";

    /// <summary>
    /// 时间类型
    /// </summary>
    public const string Time = "Time";

    /// <summary>
    /// 大文本类型
    /// </summary>
    public const string Text = "Text";

    /// <summary>
    /// GUID类型
    /// </summary>
    public const string Guid = "Guid";

    /// <summary>
    /// 图片类型
    /// </summary>
    public const string Image = "Image";

    /// <summary>
    /// 图片数组类型
    /// </summary>
    public const string ImageArray = "ImageArray";

    /// <summary>
    /// 文件类型
    /// </summary>
    public const string File = "File";

    /// <summary>
    /// 地理位置类型
    /// </summary>
    public const string Location = "Location";

    /// <summary>
    /// 所有有效的数据类型
    /// </summary>
    public static readonly string[] ValidDataTypes =
    {
        String, Integer, Long, Decimal, Boolean,
        DateTime, Date, Time, Text, Guid,
        Image, ImageArray, File, Location
    };

    /// <summary>
    /// 数值类型
    /// </summary>
    public static readonly string[] NumericTypes = { Integer, Long, Decimal };

    /// <summary>
    /// 日期时间类型
    /// </summary>
    public static readonly string[] DateTimeTypes = { DateTime, Date, Time };

    /// <summary>
    /// 文件相关类型
    /// </summary>
    public static readonly string[] FileTypes = { Image, ImageArray, File };
}
