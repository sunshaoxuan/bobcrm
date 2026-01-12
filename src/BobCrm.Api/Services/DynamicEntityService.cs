using System.Reflection;
using System.Runtime.Loader;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

/// <summary>
/// 动态实体服务
/// 负责动态生成、编译和加载自定义实体类
/// </summary>
public class DynamicEntityService : IDynamicEntityService
{
    private readonly AppDbContext _db;
    private readonly CSharpCodeGenerator _codeGenerator;
    private readonly RoslynCompiler _compiler;
    private readonly ILogger<DynamicEntityService> _logger;

    // 缓存已加载的程序集上下文 (Context, Assembly)
    private static readonly Dictionary<string, (AssemblyLoadContext Context, Assembly Assembly)> _loadedAssemblies = new();
    private static readonly object _lock = new();

    public DynamicEntityService(
        AppDbContext db,
        CSharpCodeGenerator codeGenerator,
        RoslynCompiler compiler,
        ILogger<DynamicEntityService> logger)
    {
        _db = db;
        _codeGenerator = codeGenerator;
        _compiler = compiler;
        _logger = logger;
    }

    /// <summary>
    /// 为实体生成代码
    /// </summary>
    public virtual async Task<string> GenerateCodeAsync(Guid entityDefinitionId)
    {
        var entity = await _db.EntityDefinitions
            .Include(e => e.Fields)
            .Include(e => e.Interfaces)
            .FirstOrDefaultAsync(e => e.Id == entityDefinitionId);

        if (entity == null)
            throw new ArgumentException($"Entity definition {entityDefinitionId} not found");

        if (entity.Status != EntityStatus.Published)
            throw new InvalidOperationException($"Entity must be published before code generation. Current status: {entity.Status}");

        return _codeGenerator.GenerateEntityClass(entity);
    }

    /// <summary>
    /// 编译实体代码
    /// </summary>
    public virtual async Task<CompilationResult> CompileEntityAsync(Guid entityDefinitionId)
    {
        var entity = await _db.EntityDefinitions
            .Include(e => e.Fields)
            .Include(e => e.Interfaces)
            .FirstOrDefaultAsync(e => e.Id == entityDefinitionId);

        if (entity == null)
            throw new ArgumentException($"Entity definition {entityDefinitionId} not found");

        _logger.LogInformation("[DynamicEntity] Compiling entity: {EntityName}", entity.EntityName);

        // 生成代码（实体 + 接口定义）
        var sources = new Dictionary<string, string>
        {
            [$"{entity.EntityName}.cs"] = _codeGenerator.GenerateEntityClass(entity),
            ["_Interfaces.cs"] = _codeGenerator.GenerateInterfaces()
        };

        // 编译
        var assemblyName = $"DynamicEntity_{entity.EntityName}_{Guid.NewGuid():N}";
        var result = _compiler.CompileMultiple(sources, assemblyName);

        if (result.Success && result.Assembly != null)
        {
            // 缓存程序集和上下文
            lock (_lock)
            {
                if (result.LoadContext != null)
                {
                    _loadedAssemblies[entity.FullTypeName] = (result.LoadContext, result.Assembly);
                }
            }

            _logger.LogInformation("[DynamicEntity] ✓ Entity compiled and loaded: {FullTypeName}",
                entity.FullTypeName);
        }

        return result;
    }

    /// <summary>
    /// 批量编译多个实体
    /// </summary>
    public virtual async Task<CompilationResult> CompileMultipleEntitiesAsync(List<Guid> entityDefinitionIds)
    {
        var entities = await _db.EntityDefinitions
            .Include(e => e.Fields)
            .Include(e => e.Interfaces)
            .Where(e => entityDefinitionIds.Contains(e.Id))
            .ToListAsync();

        if (!entities.Any())
            throw new ArgumentException("No entities found for the provided IDs");

        _logger.LogInformation("[DynamicEntity] Compiling {Count} entities", entities.Count);

        // 生成所有实体代码
        var sources = new Dictionary<string, string>();
        foreach (var entity in entities)
        {
            if (entity.Status != EntityStatus.Published)
            {
                _logger.LogWarning("[DynamicEntity] Skipping unpublished entity: {EntityName}", entity.EntityName);
                continue;
            }

            var code = _codeGenerator.GenerateEntityClass(entity);
            sources[$"{entity.EntityName}.cs"] = code;
        }

        if (!sources.Any())
            throw new InvalidOperationException("No valid entities to compile");

        // 添加接口定义
        sources["_Interfaces.cs"] = _codeGenerator.GenerateInterfaces();

        // 批量编译
        var assemblyName = $"DynamicEntities_{DateTime.UtcNow:yyyyMMddHHmmss}";
        var result = _compiler.CompileMultiple(sources, assemblyName);

        if (result.Success && result.Assembly != null)
        {
            // 缓存所有实体类型
            lock (_lock)
            {
                if (result.LoadContext != null)
                {
                    foreach (var entity in entities)
                    {
                        _loadedAssemblies[entity.FullTypeName] = (result.LoadContext, result.Assembly);
                    }
                }
            }

            _logger.LogInformation("[DynamicEntity] ✓ {Count} entities compiled and loaded", entities.Count);
        }

        return result;
    }

    /// <summary>
    /// 获取已加载的实体类型
    /// </summary>
    public virtual Type? GetEntityType(string fullTypeName)
    {
        lock (_lock)
        {
            if (_loadedAssemblies.TryGetValue(fullTypeName, out var entry))
            {
                return entry.Assembly.GetType(fullTypeName);
            }
        }

        return null;
    }

    /// <summary>
    /// 创建实体实例
    /// </summary>
    public virtual object? CreateEntityInstance(string fullTypeName)
    {
        var type = GetEntityType(fullTypeName);
        if (type == null)
        {
            _logger.LogWarning("[DynamicEntity] Entity type not loaded: {FullTypeName}", fullTypeName);
            return null;
        }

        return Activator.CreateInstance(type);
    }

    /// <summary>
    /// 获取实体的所有属性
    /// </summary>
    public virtual List<PropertyInfo> GetEntityProperties(string fullTypeName)
    {
        var type = GetEntityType(fullTypeName);
        if (type == null)
            return new List<PropertyInfo>();

        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
    }

    /// <summary>
    /// 验证实体代码语法
    /// </summary>
    public virtual async Task<ValidationResult> ValidateEntityCodeAsync(Guid entityDefinitionId)
    {
        var entity = await _db.EntityDefinitions
            .Include(e => e.Fields)
            .Include(e => e.Interfaces)
            .FirstOrDefaultAsync(e => e.Id == entityDefinitionId);

        if (entity == null)
            throw new ArgumentException($"Entity definition {entityDefinitionId} not found");

        var code = _codeGenerator.GenerateEntityClass(entity);
        return _compiler.ValidateSyntax(code);
    }

    /// <summary>
    /// 卸载实体（从缓存中移除）
    /// </summary>
    public virtual void UnloadEntity(string fullTypeName)
    {
        lock (_lock)
        {
            if (_loadedAssemblies.TryGetValue(fullTypeName, out var entry))
            {
                try
                {
                    entry.Context.Unload();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[DynamicEntity] Failed to unload context for {FullTypeName}", fullTypeName);
                }
                _loadedAssemblies.Remove(fullTypeName);
            }
        }

        _logger.LogInformation("[DynamicEntity] Entity unloaded: {FullTypeName}", fullTypeName);
    }

    /// <summary>
    /// 获取所有已加载的实体
    /// </summary>
    public virtual List<string> GetLoadedEntities()
    {
        lock (_lock)
        {
            return _loadedAssemblies.Keys.ToList();
        }
    }

    /// <summary>
    /// 清空所有已加载的实体
    /// </summary>
    public virtual void ClearAllLoadedEntities()
    {
        lock (_lock)
        {
            foreach (var entry in _loadedAssemblies.Values)
            {
                try
                {
                    entry.Context.Unload();
                }
                catch { /* Ignore unload errors during clear */ }
            }
            _loadedAssemblies.Clear();
        }

        _logger.LogInformation("[DynamicEntity] All loaded entities cleared");
    }

    /// <summary>
    /// 重新编译并加载实体（用于实体定义更新后）
    /// </summary>
    public virtual async Task<CompilationResult> RecompileEntityAsync(Guid entityDefinitionId)
    {
        var entity = await _db.EntityDefinitions
            .Include(e => e.Fields)
            .Include(e => e.Interfaces)
            .FirstOrDefaultAsync(e => e.Id == entityDefinitionId);

        if (entity == null)
            throw new ArgumentException($"Entity definition {entityDefinitionId} not found");

        _logger.LogInformation("[DynamicEntity] Recompiling entity: {EntityName}", entity.EntityName);

        // 卸载旧版本
        UnloadEntity(entity.FullTypeName);

        // 重新编译
        return await CompileEntityAsync(entityDefinitionId);
    }

    /// <summary>
    /// 获取实体的元数据信息
    /// </summary>
    public virtual EntityTypeInfo? GetEntityTypeInfo(string fullTypeName)
    {
        var type = GetEntityType(fullTypeName);
        if (type == null)
            return null;

        var info = new EntityTypeInfo
        {
            FullName = type.FullName ?? type.Name,
            Name = type.Name,
            Namespace = type.Namespace ?? string.Empty,
            IsLoaded = true,
            Properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new PropertyTypeInfo
                {
                    Name = p.Name,
                    TypeName = p.PropertyType.Name,
                    IsNullable = Nullable.GetUnderlyingType(p.PropertyType) != null ||
                                !p.PropertyType.IsValueType,
                    CanRead = p.CanRead,
                    CanWrite = p.CanWrite
                })
                .ToList(),
            Interfaces = type.GetInterfaces().Select(i => i.Name).ToList()
        };

        return info;
    }
}
