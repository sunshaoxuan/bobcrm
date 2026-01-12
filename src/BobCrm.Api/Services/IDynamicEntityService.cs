using System.Reflection;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Services;

public interface IDynamicEntityService
{
    Task<string> GenerateCodeAsync(Guid entityDefinitionId);
    Task<CompilationResult> CompileEntityAsync(Guid entityDefinitionId);
    Task<CompilationResult> CompileMultipleEntitiesAsync(List<Guid> entityDefinitionIds);
    Type? GetEntityType(string fullTypeName);
    object? CreateEntityInstance(string fullTypeName);
    List<PropertyInfo> GetEntityProperties(string fullTypeName);
    Task<ValidationResult> ValidateEntityCodeAsync(Guid entityDefinitionId);
    void UnloadEntity(string fullTypeName);
    List<string> GetLoadedEntities();
    void ClearAllLoadedEntities();
    Task<CompilationResult> RecompileEntityAsync(Guid entityDefinitionId);
    EntityTypeInfo? GetEntityTypeInfo(string fullTypeName);
}
