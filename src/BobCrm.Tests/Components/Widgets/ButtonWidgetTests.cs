using System.Linq;
using AntDesign;
using BobCrm.App.Models.Widgets;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Xunit;
using RuntimeRenderContext = BobCrm.App.Models.Widgets.DraggableWidget.RuntimeRenderContext;

namespace BobCrm.Tests.Components.Widgets;

/// <summary>
/// ButtonWidget 单元测试
/// </summary>
public class ButtonWidgetTests
{
    [Fact]
    public void ButtonWidget_RenderRuntime_Should_Use_AntDesign_Button_Component()
    {
        // Arrange
        var widget = new ButtonWidget
        {
            Label = "Test Button",
            Variant = "primary"
        };

        var builder = new RenderTreeBuilder();
        var context = CreateContext(builder, widget, RuntimeWidgetRenderMode.Edit);

        // Act
        widget.RenderRuntime(context);

        // Assert
        var frames = builder.GetFrames().Array;
        frames.Should().Contain(frame =>
            frame.FrameType == RenderTreeFrameType.Component &&
            frame.ComponentType == typeof(Button),
            "ButtonWidget should render AntDesign.Button component");
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

        var builder = new RenderTreeBuilder();
        var context = CreateContext(builder, widget, RuntimeWidgetRenderMode.Edit);

        // Act
        widget.RenderRuntime(context);

        // Assert
        HasAttribute(builder, "OnClick").Should().BeTrue("ButtonWidget with OpenUrl action should have OnClick callback");
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

        var builder = new RenderTreeBuilder();
        var context = CreateContext(builder, widget, RuntimeWidgetRenderMode.Edit);

        // Act
        widget.RenderRuntime(context);

        // Assert
        HasAttribute(builder, "OnClick").Should().BeTrue("ButtonWidget with Download action should have OnClick callback");
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
            var builder = new RenderTreeBuilder();
            var context = CreateContext(builder, widget, RuntimeWidgetRenderMode.Edit);

            // Act
            widget.RenderRuntime(context);

            // Assert
            var frames = builder.GetFrames().Array;
            var typeAttribute = frames.FirstOrDefault(f =>
                f.FrameType == RenderTreeFrameType.Attribute &&
                f.AttributeName == "Type");
            typeAttribute.AttributeValue.Should().Be(expectedType, $"Variant '{variant}' should map to ButtonType.{expectedType}");
        }
    }

    [Fact]
    public void ButtonWidget_BrowseMode_Should_Disable_Button()
    {
        // Arrange
        var widget = new ButtonWidget { Label = "Disabled" };
        var builder = new RenderTreeBuilder();
        var context = CreateContext(builder, widget, RuntimeWidgetRenderMode.Browse);

        // Act
        widget.RenderRuntime(context);

        // Assert
        var frames = builder.GetFrames().Array;
        var disabledAttribute = frames.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "Disabled");
        disabledAttribute.AttributeValue.Should().Be(true, "Button in Browse mode should be disabled");
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

    private static bool HasAttribute(RenderTreeBuilder builder, string attributeName)
    {
        var frames = builder.GetFrames().Array;
        return frames.Any(f => f.FrameType == RenderTreeFrameType.Attribute &&
                               f.AttributeName == attributeName);
    }

    private sealed class DummyComponent : ComponentBase
    {
    }
}
