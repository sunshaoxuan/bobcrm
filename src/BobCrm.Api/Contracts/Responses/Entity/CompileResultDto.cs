namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 实体编译结果 DTO
/// </summary>
public class CompileResultDto
{
    /// <summary>
    /// 是否编译成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 生成的程序集名称
    /// </summary>
    public string? AssemblyName { get; set; }

    /// <summary>
    /// 已加载的类型名称列表
    /// </summary>
    public List<string> LoadedTypes { get; set; } = new();

    /// <summary>
    /// 已加载类型的数量
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 结果消息
    /// </summary>
    public string? Message { get; set; }
}
