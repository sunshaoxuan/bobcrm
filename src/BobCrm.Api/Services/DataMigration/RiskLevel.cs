namespace BobCrm.Api.Services.DataMigration;

/// <summary>
/// 风险等级
/// </summary>
public static class RiskLevel
{
    /// <summary>低风险：仅添加可空字段</summary>
    public const string Low = "Low";

    /// <summary>中等风险：修改字段类型但可以安全转换</summary>
    public const string Medium = "Medium";

    /// <summary>高风险：可能导致数据丢失</summary>
    public const string High = "High";

    /// <summary>严重风险：必定导致数据丢失或系统异常</summary>
    public const string Critical = "Critical";
}

