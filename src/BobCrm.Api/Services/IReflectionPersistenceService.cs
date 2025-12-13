namespace BobCrm.Api.Services;

public interface IReflectionPersistenceService
{
    Task<List<object>> QueryAsync(string fullTypeName, QueryOptions? options = null);

    Task<object?> GetByIdAsync(string fullTypeName, int id);

    Task<object> CreateAsync(string fullTypeName, Dictionary<string, object> data);

    Task<object?> UpdateAsync(string fullTypeName, int id, Dictionary<string, object> data);

    Task<bool> DeleteAsync(string fullTypeName, int id, string? deletedBy = null);

    Task<int> CountAsync(string fullTypeName, List<FilterCondition>? filters = null);

    Task<List<Dictionary<string, object?>>> QueryRawAsync(string tableName, QueryOptions? options = null);
}

