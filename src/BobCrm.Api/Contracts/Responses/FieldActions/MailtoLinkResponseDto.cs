namespace BobCrm.Api.Contracts.Responses.FieldActions;

/// <summary>
/// mailto 链接生成结果。
/// </summary>
public class MailtoLinkResponseDto
{
    /// <summary>
    /// mailto 链接。
    /// </summary>
    public string Link { get; set; } = string.Empty;
}

