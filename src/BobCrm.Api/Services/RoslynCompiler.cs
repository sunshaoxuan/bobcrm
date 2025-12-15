using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace BobCrm.Api.Services;

/// <summary>
/// Roslyn动态编译器
/// 将C#源代码编译为程序集并加载到内存
///
/// 需要NuGet包：
/// - Microsoft.CodeAnalysis.CSharp (>= 4.8.0)
/// </summary>
public class RoslynCompiler
{
    private readonly ILogger<RoslynCompiler> _logger;

    public RoslynCompiler(ILogger<RoslynCompiler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 编译C#源代码为Assembly
    /// </summary>
    /// <param name="sourceCode">C#源代码</param>
    /// <param name="assemblyName">程序集名称</param>
    /// <returns>编译结果</returns>
    public virtual CompilationResult Compile(string sourceCode, string assemblyName)
    {
        var result = new CompilationResult
        {
            AssemblyName = assemblyName
        };

        try
        {
            _logger.LogInformation("[Roslyn] Compiling assembly: {AssemblyName}", assemblyName);

            // 1. 解析语法树
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

            // 2. 获取引用程序集
            var references = GetReferences();

            // 3. 创建编译选项
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                allowUnsafe: false
            );

            // 4. 创建编译上下文
            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                references,
                compilationOptions
            );

            // 5. 编译到内存流
            using var ms = new MemoryStream();
            EmitResult emitResult = compilation.Emit(ms);

            if (!emitResult.Success)
            {
                // 编译失败，收集错误信息
                result.Success = false;
                result.Errors = emitResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => new CompilationError
                    {
                        Code = d.Id,
                        Message = d.GetMessage(),
                        Line = d.Location.GetLineSpan().StartLinePosition.Line + 1,
                        Column = d.Location.GetLineSpan().StartLinePosition.Character + 1
                    })
                    .ToList();

                _logger.LogError("[Roslyn] Compilation failed with {Count} errors", result.Errors.Count);
                foreach (var error in result.Errors)
                {
                    _logger.LogError("[Roslyn] {Code} at line {Line}: {Message}",
                        error.Code, error.Line, error.Message);
                }

                return result;
            }

            // 6. 加载程序集到内存
            ms.Seek(0, SeekOrigin.Begin);
            var loadContext = new AssemblyLoadContext($"Roslyn_{Guid.NewGuid():N}", isCollectible: true);
            Assembly? assembly = null;

            try
            {
                assembly = loadContext.LoadFromStream(ms);
            }
            catch
            {
                loadContext.Unload();
                throw;
            }

            result.Success = true;
            result.Assembly = assembly;
            result.LoadedTypes = assembly.GetTypes().Select(t => t.FullName ?? t.Name).ToList();
            result.LoadContext = loadContext;

            _logger.LogInformation("[Roslyn] ✓ Compilation succeeded: {AssemblyName}, Loaded {Count} types",
                assemblyName, result.LoadedTypes.Count);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors = new List<CompilationError>
            {
                new CompilationError
                {
                    Code = "COMPILATION_EXCEPTION",
                    Message = ex.Message
                }
            };

            _logger.LogError(ex, "[Roslyn] Compilation exception: {Message}", ex.Message);
            return result;
        }
    }

    /// <summary>
    /// 编译多个源文件
    /// </summary>
    public virtual CompilationResult CompileMultiple(Dictionary<string, string> sources, string assemblyName)
    {
        var result = new CompilationResult
        {
            AssemblyName = assemblyName
        };

        try
        {
            _logger.LogInformation("[Roslyn] Compiling {Count} source files into assembly: {AssemblyName}",
                sources.Count, assemblyName);

            // 解析所有语法树
            var syntaxTrees = sources.Select(kvp =>
            {
                var tree = CSharpSyntaxTree.ParseText(kvp.Value);
                return tree.WithFilePath(kvp.Key); // 设置文件路径以便错误定位
            }).ToArray();

            // 获取引用
            var references = GetReferences();

            // 编译选项
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release
            );

            // 创建编译
            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees,
                references,
                compilationOptions
            );

            // 编译
            using var aggregateStream = new MemoryStream();
            var emitResult = compilation.Emit(aggregateStream);

            if (!emitResult.Success)
            {
                result.Success = false;
                result.Errors = emitResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => new CompilationError
                    {
                        Code = d.Id,
                        Message = d.GetMessage(),
                        Line = d.Location.GetLineSpan().StartLinePosition.Line + 1,
                        Column = d.Location.GetLineSpan().StartLinePosition.Character + 1,
                        FilePath = d.Location.SourceTree?.FilePath
                    })
                    .ToList();

                _logger.LogError("[Roslyn] Multi-file compilation failed with {Count} errors", result.Errors.Count);
                return result;
            }

            aggregateStream.Seek(0, SeekOrigin.Begin);
            var loadContext = new AssemblyLoadContext($"Roslyn_{Guid.NewGuid():N}", isCollectible: true);
            Assembly? aggregateAssembly = null;

            try
            {
                aggregateAssembly = loadContext.LoadFromStream(aggregateStream);
            }
            catch
            {
                loadContext.Unload();
                throw;
            }

            result.Success = true;
            result.Assembly = aggregateAssembly;
            result.LoadedTypes = aggregateAssembly.GetTypes().Select(t => t.FullName ?? t.Name).ToList();
            result.LoadContext = loadContext;

            _logger.LogInformation("[Roslyn] ✓ Multi-file compilation succeeded: {Count} types loaded",
                result.LoadedTypes.Count);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors = new List<CompilationError>
            {
                new CompilationError { Code = "COMPILATION_EXCEPTION", Message = ex.Message }
            };
            _logger.LogError(ex, "[Roslyn] Multi-file compilation exception");
            return result;
        }
    }

    /// <summary>
    /// 获取编译所需的引用程序集
    /// </summary>
    protected virtual List<MetadataReference> GetReferences()
    {
        var references = new List<MetadataReference>();

        // 添加基础运行时引用
        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Collections.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Linq.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "netstandard.dll")));

        // 添加System.ComponentModel.Annotations（用于数据注解）
        references.Add(MetadataReference.CreateFromFile(
            typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location));

        // 添加Entity Framework Core引用（如果需要）
        try
        {
            references.Add(MetadataReference.CreateFromFile(
                typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly.Location));
        }
        catch
        {
            _logger.LogWarning("[Roslyn] Entity Framework Core reference not found");
        }

        return references;
    }

    /// <summary>
    /// 验证代码语法（不编译）
    /// </summary>
    public virtual ValidationResult ValidateSyntax(string sourceCode)
    {
        var result = new ValidationResult();

        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var diagnostics = syntaxTree.GetDiagnostics();

            var errors = diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (errors.Any())
            {
                result.IsValid = false;
                result.Errors = errors.Select(e => new CompilationError
                {
                    Code = e.Id,
                    Message = e.GetMessage(),
                    Line = e.Location.GetLineSpan().StartLinePosition.Line + 1,
                    Column = e.Location.GetLineSpan().StartLinePosition.Character + 1
                }).ToList();
            }
            else
            {
                result.IsValid = true;
            }

            return result;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors = new List<CompilationError>
            {
                new CompilationError { Code = "SYNTAX_ERROR", Message = ex.Message }
            };
            return result;
        }
    }
}
