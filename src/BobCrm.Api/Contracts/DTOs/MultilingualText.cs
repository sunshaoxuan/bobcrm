namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 多语言文本字典
/// </summary>
public class MultilingualText : Dictionary<string, string?>
{
    public MultilingualText() : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    public MultilingualText(IDictionary<string, string?> source) : base(source, StringComparer.OrdinalIgnoreCase)
    {
    }
}
