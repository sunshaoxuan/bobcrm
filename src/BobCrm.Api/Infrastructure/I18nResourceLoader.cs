using System.Text.Json;
using BobCrm.Api.Domain;

namespace BobCrm.Api.Infrastructure;

/// <summary>
/// I18n资源加载器 - 从JSON文件加载国际化资源
/// 单一数据源原则：所有i18n资源定义在JSON文件中
/// 动态语言支持：不硬编码语种，支持任意语言扩展
/// </summary>
public static class I18nResourceLoader
{
    /// <summary>
    /// 从JSON文件加载所有i18n资源（每次都创建新对象，避免EF Core追踪冲突）
    /// </summary>
    public static async Task<List<LocalizationResource>> LoadResourcesAsync()
    {
        try
        {
            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "i18n-resources.json");

            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"I18n resource file not found: {jsonPath}");
            }

            var json = await File.ReadAllTextAsync(jsonPath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dict == null || !dict.Any())
            {
                throw new InvalidOperationException("I18n resource file is empty or invalid");
            }

            // ✅ 动态加载所有语言，不硬编码 ZH/JA/EN
            return dict.Select(kvp => new LocalizationResource
            {
                Key = kvp.Key,
                Translations = new Dictionary<string, string>(kvp.Value, StringComparer.OrdinalIgnoreCase)
            }).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load i18n resources: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 同步版本 - 用于无法使用async的场景
    /// </summary>
    public static List<LocalizationResource> LoadResources()
    {
        return LoadResourcesAsync().GetAwaiter().GetResult();
    }
}
