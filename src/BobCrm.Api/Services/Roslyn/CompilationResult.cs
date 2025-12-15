using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace BobCrm.Api.Services;

/// <summary>
/// 编译结果
/// </summary>
public class CompilationResult
{
    public string AssemblyName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public Assembly? Assembly { get; set; }
    public List<string> LoadedTypes { get; set; } = new();
    public List<CompilationError> Errors { get; set; } = new();
    public AssemblyLoadContext? LoadContext { get; set; }
}
