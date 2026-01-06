using BobCrm.Api.Services;
using FluentAssertions;

namespace BobCrm.Api.Tests;

public class DefaultI18nServiceTests
{
    [Fact]
    public void CurrentLang_ShouldDefaultToEn()
    {
        var svc = new DefaultI18nService();
        svc.CurrentLang.Should().Be("en");
    }

    [Fact]
    public async Task LoadAsync_WithWhitespaceLang_ShouldNotChangeAndNotRaiseEvent()
    {
        var svc = new DefaultI18nService();
        var changed = 0;
        svc.OnChanged += () => changed++;

        await svc.LoadAsync("   ");

        svc.CurrentLang.Should().Be("en");
        changed.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_WithNewLang_ShouldUpdateAndRaiseEvent()
    {
        var svc = new DefaultI18nService();
        var changed = 0;
        svc.OnChanged += () => changed++;

        await svc.LoadAsync("zh");

        svc.CurrentLang.Should().Be("zh");
        changed.Should().Be(1);
    }

    [Fact]
    public async Task LoadAsync_WithSameLangWithoutForce_ShouldNotRaiseEvent()
    {
        var svc = new DefaultI18nService();
        var changed = 0;
        svc.OnChanged += () => changed++;

        await svc.LoadAsync("zh");
        await svc.LoadAsync("zh");

        svc.CurrentLang.Should().Be("zh");
        changed.Should().Be(1);
    }

    [Fact]
    public async Task LoadAsync_WithSameLangButForce_ShouldRaiseEvent()
    {
        var svc = new DefaultI18nService();
        var changed = 0;
        svc.OnChanged += () => changed++;

        await svc.LoadAsync("zh");
        await svc.LoadAsync("zh", force: true);

        svc.CurrentLang.Should().Be("zh");
        changed.Should().Be(2);
    }

    [Fact]
    public void T_ShouldReturnKey()
    {
        var svc = new DefaultI18nService();
        svc.T("ERR_TEST").Should().Be("ERR_TEST");
    }
}

