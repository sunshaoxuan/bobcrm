using AntDesign;
using BobCrm.App.Models.Widgets;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using RuntimeRenderContext = BobCrm.App.Models.Widgets.DraggableWidget.RuntimeRenderContext;

namespace BobCrm.Tests.Components.Widgets;

/// <summary>
/// ButtonWidget 单元测试
/// </summary>
public class ButtonWidgetTests : TestContext
{
    public ButtonWidgetTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddAntDesign();
    }

    [Fact]
    public void ButtonWidget_RenderRuntime_Should_Use_AntDesign_Button_Component()
    {
        // Arrange
        var widget = new ButtonWidget
        {
            Label = "Test Button",
            Variant = "primary"
        };

        // Act
        var cut = RenderComponent<ButtonWidgetHost>(parameters => parameters
            .Add(p => p.Widget, widget)
            .Add(p => p.Mode, RuntimeWidgetRenderMode.Edit));

        // Assert
        cut.FindComponent<Button>().Should().NotBeNull();
    }

    [Fact]
    public void ButtonWidget_WithAction_OpenUrl_Should_Configure_NavigationCallback()
    {
        // Arrange
        var widget = new ButtonWidget
        {
            Label = "Navigate",
            Action = "OpenUrl",
            ActionPayload = "https://example.com"
        };

        // Act
        var cut = RenderComponent<ButtonWidgetHost>(parameters => parameters
            .Add(p => p.Widget, widget)
            .Add(p => p.Mode, RuntimeWidgetRenderMode.Edit));

        // Assert
        var button = cut.FindComponent<Button>().Instance;
        GetEventCallbackHasDelegate(button, "OnClick")
            .Should()
            .BeTrue("ButtonWidget with OpenUrl action should have OnClick callback");
    }

    [Fact]
    public void ButtonWidget_WithAction_Download_Should_Configure_DownloadCallback()
    {
        // Arrange
        var widget = new ButtonWidget
        {
            Label = "Download",
            Action = "Download",
            ActionPayload = "file123"
        };

        // Act
        var cut = RenderComponent<ButtonWidgetHost>(parameters => parameters
            .Add(p => p.Widget, widget)
            .Add(p => p.Mode, RuntimeWidgetRenderMode.Edit));

        // Assert
        var button = cut.FindComponent<Button>().Instance;
        GetEventCallbackHasDelegate(button, "OnClick")
            .Should()
            .BeTrue("ButtonWidget with Download action should have OnClick callback");
    }

    [Fact]
    public void ButtonWidget_Variant_Should_Map_To_ButtonType()
    {
        // Arrange
        var testCases = new[]
        {
            ("primary", AntDesign.ButtonType.Primary),
            ("default", AntDesign.ButtonType.Default),
            ("dashed", AntDesign.ButtonType.Dashed),
            ("link", AntDesign.ButtonType.Link),
            ("text", AntDesign.ButtonType.Text)
        };

        foreach (var (variant, expectedType) in testCases)
        {
            var widget = new ButtonWidget { Variant = variant };

            // Act
            var cut = RenderComponent<ButtonWidgetHost>(parameters => parameters
                .Add(p => p.Widget, widget)
                .Add(p => p.Mode, RuntimeWidgetRenderMode.Edit));

            // Assert
            var button = cut.FindComponent<Button>().Instance;
            GetPropertyValue(button, "Type").Should().Be(expectedType, $"Variant '{variant}' should map to ButtonType.{expectedType}");
        }
    }

    [Fact]
    public void ButtonWidget_BrowseMode_Should_Disable_Button()
    {
        // Arrange
        var widget = new ButtonWidget { Label = "Disabled" };

        // Act
        var cut = RenderComponent<ButtonWidgetHost>(parameters => parameters
            .Add(p => p.Widget, widget)
            .Add(p => p.Mode, RuntimeWidgetRenderMode.Browse));

        // Assert
        var button = cut.FindComponent<Button>().Instance;
        GetPropertyValue(button, "Disabled").Should().Be(true, "Button in Browse mode should be disabled");
    }

    private static RuntimeRenderContext CreateContext(RenderTreeBuilder builder, ButtonWidget widget, RuntimeWidgetRenderMode mode)
    {
        return new RuntimeRenderContext
        {
            Builder = builder,
            Mode = mode,
            EventTarget = new DummyComponent(),
            Widget = widget,
            Label = widget.Label,
            RenderChild = _ => __builder => { },
            GetWidgetBackground = _ => "#fff",
            GetWidgetTextStyle = _ => "color:#333;"
        };
    }

    private static object? GetPropertyValue(object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
    }

    private static bool GetEventCallbackHasDelegate(object obj, string propertyName)
    {
        var callback = GetPropertyValue(obj, propertyName);
        if (callback == null)
        {
            return false;
        }

        var hasDelegate = callback.GetType().GetProperty("HasDelegate")?.GetValue(callback);
        return hasDelegate is true;
    }

    private sealed class DummyComponent : ComponentBase
    {
    }

    private sealed class ButtonWidgetHost : ComponentBase
    {
        [Parameter, EditorRequired]
        public ButtonWidget Widget { get; set; } = null!;

        [Parameter]
        public RuntimeWidgetRenderMode Mode { get; set; } = RuntimeWidgetRenderMode.Edit;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Widget.RenderRuntime(CreateContext(builder, Widget, Mode));
        }
    }
}
