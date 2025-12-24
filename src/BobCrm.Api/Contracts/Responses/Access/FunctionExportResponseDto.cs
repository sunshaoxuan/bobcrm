namespace BobCrm.Api.Contracts.Responses.Access;

/// <summary>
/// 菜单/功能树导出响应。
/// </summary>
public class FunctionExportResponseDto
{
    public string Version { get; set; } = "1.0";
    public DateTime ExportDate { get; set; } = DateTime.UtcNow;
    public List<FunctionExportNodeDto> Functions { get; set; } = new();
}

