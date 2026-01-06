using BobCrm.Api.Infrastructure;
using Xunit;

namespace BobCrm.Api.Tests;

public class LangHelperTests
{
    [Fact]
    public void ResolveDisplayName_WhenKeyProvided_UsesLocalization()
    {
        var loc = new FakeLocalization((key, lang) => $"{key}:{lang}");

        var resolved = LangHelper.ResolveDisplayName("LBL_HELLO", rawJson: null, targetLang: "zh", loc);

        Assert.Equal("LBL_HELLO:zh", resolved);
    }

    [Fact]
    public void ResolveDisplayName_WhenJsonProvided_ResolvesTargetLanguage()
    {
        var loc = new FakeLocalization((key, _) => key);

        var resolved = LangHelper.ResolveDisplayName(
            key: null,
            rawJson: "{\"zh\":\"中文\",\"en\":\"English\"}",
            targetLang: "zh",
            loc);

        Assert.Equal("中文", resolved);
    }

    [Fact]
    public void ResolveDisplayName_WhenJsonMissingTarget_FallsBackToEn()
    {
        var loc = new FakeLocalization((key, _) => key);

        var resolved = LangHelper.ResolveDisplayName(
            key: null,
            rawJson: "{\"en\":\"English\"}",
            targetLang: "ja",
            loc);

        Assert.Equal("English", resolved);
    }

    [Fact]
    public void ResolveDisplayName_WhenNotJson_ReturnsRaw()
    {
        var loc = new FakeLocalization((key, _) => key);

        var resolved = LangHelper.ResolveDisplayName(
            key: null,
            rawJson: "PlainText",
            targetLang: "zh",
            loc);

        Assert.Equal("PlainText", resolved);
    }

    private sealed class FakeLocalization : ILocalization
    {
        private readonly Func<string, string, string> _translator;

        public FakeLocalization(Func<string, string, string> translator)
        {
            _translator = translator;
        }

        public string T(string key, string lang) => _translator(key, lang);

        public Dictionary<string, string> GetDictionary(string lang) => new();

        public void InvalidateCache()
        {
        }

        public long GetCacheVersion() => 1;
    }
}

