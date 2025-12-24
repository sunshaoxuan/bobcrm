namespace BobCrm.Api.Contracts.Responses.Access;

/// <summary>
/// 菜单/功能导入结果。
/// </summary>
public class FunctionImportResultDto
{
    public string Message { get; set; } = string.Empty;
    public int Imported { get; set; }
    public int Skipped { get; set; }
}

