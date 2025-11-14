using System.Text.Json;
using BobCrm.Api.Domain.Models;

namespace BobCrm.Api.Infrastructure;

/// <summary>
/// Loads entity definition metadata (JSON archives) for system entities.
/// </summary>
public static class EntityDefinitionArchive
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static EntityDefinition Load(string archiveName)
    {
        if (string.IsNullOrWhiteSpace(archiveName))
        {
            throw new ArgumentException("Archive name is required.", nameof(archiveName));
        }

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var path = Path.Combine(baseDir, "Metadata", "Entities", $"{archiveName}.entity.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Entity definition archive not found: {path}");
        }

        using var stream = File.OpenRead(path);
        var definition = JsonSerializer.Deserialize<EntityDefinition>(stream, Options);
        if (definition == null)
        {
            throw new InvalidOperationException($"Failed to deserialize entity definition archive: {path}");
        }

        return definition;
    }
}
