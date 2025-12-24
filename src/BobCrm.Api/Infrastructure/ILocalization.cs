namespace BobCrm.Api.Infrastructure;

public interface ILocalization
{
    string T(string key, string lang);

    /// <summary>
    /// 获取指定语言的完整字典
    /// </summary>
    Dictionary<string, string> GetDictionary(string lang);

    /// <summary>
    /// 清除缓存，在多语资源更新后调用
    /// </summary>
    void InvalidateCache();

    /// <summary>
    /// 获取当前缓存版本号（用于 ETag）
    /// </summary>
    long GetCacheVersion();
}
