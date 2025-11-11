using System.Text.Json;
using BobCrm.Api.Domain;

namespace BobCrm.Api.Infrastructure;

/// <summary>
/// I18n资源加载器 - 从JSON文件加载国际化资源
/// 单一数据源原则：所有i18n资源定义在JSON文件中
/// </summary>
public static class I18nResourceLoader
{
    private static List<LocalizationResource>? _cachedResources;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// 从JSON文件加载所有i18n资源
    /// </summary>
    public static async Task<List<LocalizationResource>> LoadResourcesAsync()
    {
        // 使用缓存避免重复读取文件
        if (_cachedResources != null)
        {
            return _cachedResources.ToList(); // 返回副本避免并发修改
        }

        await _semaphore.WaitAsync();
        try
        {
            // 双重检查锁定模式
            if (_cachedResources != null)
            {
                return _cachedResources.ToList();
            }

            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "i18n-resources.json");

            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"I18n resource file not found: {jsonPath}");
            }

            var json = await File.ReadAllTextAsync(jsonPath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, I18nResourceDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dict == null || !dict.Any())
            {
                throw new InvalidOperationException("I18n resource file is empty or invalid");
            }

            _cachedResources = dict.Select(kvp => new LocalizationResource
            {
                Key = kvp.Key,
                ZH = kvp.Value.Zh ?? string.Empty,
                JA = kvp.Value.Ja ?? string.Empty,
                EN = kvp.Value.En ?? string.Empty
            }).ToList();

            return _cachedResources.ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load i18n resources: {ex.Message}", ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 同步版本 - 用于无法使用async的场景
    /// </summary>
    public static List<LocalizationResource> LoadResources()
    {
        return LoadResourcesAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 清除缓存 - 用于测试或热重载场景
    /// </summary>
    public static async Task ClearCacheAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _cachedResources = null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// JSON资源DTO
    /// </summary>
    private class I18nResourceDto
    {
        public string? Zh { get; set; }
        public string? Ja { get; set; }
        public string? En { get; set; }
    }
}
